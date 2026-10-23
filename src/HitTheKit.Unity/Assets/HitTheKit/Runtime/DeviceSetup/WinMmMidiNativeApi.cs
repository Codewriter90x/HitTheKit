using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HitTheKit.Unity.DeviceSetup
{
    /// <summary>
    /// Windows MIDI input through the in-box WinMM API (winmm.dll), exposed behind the same
    /// <see cref="ICoreMidiNativeApi"/> boundary as the macOS CoreMIDI plug-in, so discovery,
    /// capture, mapping, timing and gameplay stay unchanged. No native plug-in is required.
    /// </summary>
    public sealed class WinMmMidiNativeApi : ICoreMidiNativeApi
    {
        public const int QueueCapacity = 1024;
        private const uint CallbackFunction = 0x00030000;
        private const uint MimOpen = 0x3C1;
        private const uint MimClose = 0x3C2;
        private const uint MimData = 0x3C3;
        private const uint MimMoreData = 0x3CC;
        private const uint MmSysErrNoError = 0;
        private const double DeviceCheckIntervalSeconds = 1.0;

        private static readonly MidiInProc Callback = OnMidiInput;
        private readonly object gate = new object();
        private readonly Queue<CoreMidiNativeMessage> queue = new Queue<CoreMidiNativeMessage>();
        private readonly List<WinMmDevice> devices = new List<WinMmDevice>();
        private readonly bool isWindows;
        private string availabilityMessage;
        private bool isAvailable;
        private bool clientCreated;
        private IntPtr handle = IntPtr.Zero;
        private GCHandle self;
        private long openEndpointId;
        private string openDeviceName;
        private bool connected;
        private double lastDeviceCheck = double.NegativeInfinity;
        private ulong deviceGeneration;
        private ulong sequence;
        private ulong messagesReceived;
        private ulong droppedMessages;
        private string lastError = string.Empty;

        public WinMmMidiNativeApi()
        {
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!isWindows)
            {
                availabilityMessage = "WinMM MIDI is available only on Windows.";
                return;
            }
            try
            {
                NativeMidiInGetNumDevs();
                isAvailable = true;
            }
            catch (Exception exception) when (exception is DllNotFoundException || exception is EntryPointNotFoundException)
            {
                availabilityMessage = "winmm.dll could not be loaded.";
            }
        }

        public bool IsAvailable => isAvailable;
        public string AvailabilityMessage => availabilityMessage;
        public int GetApiVersion() => isAvailable ? CoreMidiNativeApi.ExpectedApiVersion : -1000;
        public double GetMonotonicSeconds() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        public int CreateClient()
        {
            if (!isAvailable) return -1000;
            clientCreated = true;
            return 0;
        }

        public void DestroyClient()
        {
            CloseInput();
            clientCreated = false;
        }

        public int RefreshDevices()
        {
            if (!isAvailable || !clientCreated) return Fail(-1, "MIDI client is not created.");
            List<WinMmDevice> current = Enumerate();
            lock (gate)
            {
                if (!SameDevices(devices, current))
                {
                    devices.Clear();
                    devices.AddRange(current);
                    deviceGeneration++;
                }
            }
            return 0;
        }

        public int GetDeviceCount()
        {
            lock (gate) return devices.Count;
        }

        public int GetDeviceInfo(int index, out CoreMidiNativeDeviceInfo info)
        {
            info = default;
            lock (gate)
            {
                if (index < 0 || index >= devices.Count) return Fail(-2, "Device index is out of range.");
                WinMmDevice device = devices[index];
                info = new CoreMidiNativeDeviceInfo
                {
                    Index = index,
                    EndpointId = device.EndpointId,
                    Name = device.Name,
                    DeviceName = device.Name,
                    Manufacturer = string.Empty,
                    Model = string.Empty,
                    EntityName = device.Name,
                    Protocol = 1,
                    IsOnline = 1
                };
                return 0;
            }
        }

        public int OpenInput(long endpointId)
        {
            if (!isAvailable || !clientCreated) return Fail(-1, "MIDI client is not created.");
            CloseInput();
            List<WinMmDevice> current = Enumerate();
            WinMmDevice target = current.Find(device => device.EndpointId == endpointId);
            if (target == null) return Fail(-3, "The selected MIDI input is not connected.");

            self = GCHandle.Alloc(this);
            uint result = NativeMidiInOpen(out IntPtr opened, target.DeviceIndex, Callback, GCHandle.ToIntPtr(self), CallbackFunction);
            if (result != MmSysErrNoError)
            {
                self.Free();
                return Fail(-4, $"midiInOpen failed with MMSYSERR {result}.");
            }
            result = NativeMidiInStart(opened);
            if (result != MmSysErrNoError)
            {
                NativeMidiInClose(opened);
                self.Free();
                return Fail(-5, $"midiInStart failed with MMSYSERR {result}.");
            }

            lock (gate)
            {
                handle = opened;
                openEndpointId = endpointId;
                openDeviceName = target.Name;
                connected = true;
                queue.Clear();
                lastDeviceCheck = GetMonotonicSeconds();
            }
            return 0;
        }

        public void CloseInput()
        {
            IntPtr closing;
            lock (gate)
            {
                closing = handle;
                handle = IntPtr.Zero;
                connected = false;
                openEndpointId = 0;
                openDeviceName = null;
                queue.Clear();
            }
            if (closing != IntPtr.Zero)
            {
                NativeMidiInStop(closing);
                NativeMidiInReset(closing);
                NativeMidiInClose(closing);
            }
            if (self.IsAllocated) self.Free();
        }

        public int PollMessages(CoreMidiNativeMessage[] buffer, int capacity)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (capacity < 0 || capacity > buffer.Length) throw new ArgumentOutOfRangeException(nameof(capacity));
            lock (gate)
            {
                int count = 0;
                while (count < capacity && queue.Count > 0) buffer[count++] = queue.Dequeue();
                return count;
            }
        }

        public int GetConnectionState()
        {
            string name;
            long endpoint;
            lock (gate)
            {
                if (handle == IntPtr.Zero || !connected) return 0;
                double now = GetMonotonicSeconds();
                if (now - lastDeviceCheck < DeviceCheckIntervalSeconds) return 1;
                lastDeviceCheck = now;
                name = openDeviceName;
                endpoint = openEndpointId;
            }
            // WinMM has no hot-plug notification: confirm the open input is still enumerated.
            bool present = Enumerate().Exists(device => device.EndpointId == endpoint);
            lock (gate)
            {
                if (!present && string.Equals(openDeviceName, name, StringComparison.Ordinal)) connected = false;
                return connected ? 1 : 0;
            }
        }

        public int GetDiagnostics(out CoreMidiNativeDiagnostics diagnostics)
        {
            lock (gate)
            {
                diagnostics = new CoreMidiNativeDiagnostics
                {
                    DeviceGeneration = deviceGeneration,
                    MessagesReceived = messagesReceived,
                    DroppedMessages = droppedMessages,
                    QueueSize = queue.Count,
                    QueueCapacity = QueueCapacity,
                    SelectedEndpointId = openEndpointId,
                    ClientState = clientCreated ? 1 : 0,
                    ConnectionState = connected ? 1 : 0
                };
            }
            return isAvailable ? 0 : -1000;
        }

        public string GetLastError()
        {
            if (!isAvailable) return availabilityMessage;
            lock (gate) return lastError;
        }

        /// <summary>
        /// Decodes a WinMM short message (MIM_DATA dwParam1) into the CoreMIDI-compatible shape.
        /// Returns false for realtime, system and malformed messages.
        /// </summary>
        public static bool TryDecodeShortMessage(uint packed, out int kind, out int channel, out int data1, out int data2)
        {
            int status = (int)(packed & 0xFF);
            int first = (int)((packed >> 8) & 0xFF);
            int second = (int)((packed >> 16) & 0xFF);
            kind = channel = data1 = data2 = 0;
            if (status < 0x80 || status >= 0xF0) return false;
            channel = status & 0x0F;
            switch (status >> 4)
            {
                case 0x8: kind = (int)CoreMidiNativeMessageKind.NoteOff; break;
                case 0x9: kind = (int)CoreMidiNativeMessageKind.NoteOn; break;
                case 0xA: kind = (int)CoreMidiNativeMessageKind.PolyAftertouch; break;
                case 0xB: kind = (int)CoreMidiNativeMessageKind.ControlChange; break;
                case 0xC: kind = (int)CoreMidiNativeMessageKind.ProgramChange; break;
                case 0xD: kind = (int)CoreMidiNativeMessageKind.ChannelAftertouch; break;
                case 0xE: kind = (int)CoreMidiNativeMessageKind.PitchBend; break;
                default: return false;
            }
            if (first > 0x7F || second > 0x7F) return false;
            switch ((CoreMidiNativeMessageKind)kind)
            {
                case CoreMidiNativeMessageKind.ChannelAftertouch:
                    data1 = -1; data2 = first; break;
                case CoreMidiNativeMessageKind.ProgramChange:
                    data1 = first; data2 = 0; break;
                case CoreMidiNativeMessageKind.PitchBend:
                    data1 = first; data2 = first | (second << 7); break;
                default:
                    data1 = first; data2 = second; break;
            }
            return true;
        }

        /// <summary>
        /// Stable endpoint identifier: WinMM device indices shift when devices are unplugged, so the
        /// ID is derived from the device name plus its occurrence among equally named inputs.
        /// </summary>
        public static long StableEndpointId(string name, int occurrence)
        {
            if (occurrence < 0) throw new ArgumentOutOfRangeException(nameof(occurrence));
            uint hash = 2166136261;
            foreach (char character in name ?? string.Empty)
            {
                hash ^= character;
                hash *= 16777619;
            }
            return ((long)(occurrence & 0x7FFF) << 32) | hash;
        }

        private void Enqueue(uint packed)
        {
            if (!TryDecodeShortMessage(packed, out int kind, out int channel, out int data1, out int data2)) return;
            double now = GetMonotonicSeconds();
            lock (gate)
            {
                if (handle == IntPtr.Zero) return;
                messagesReceived++;
                if (queue.Count >= QueueCapacity)
                {
                    queue.Dequeue();
                    droppedMessages++;
                }
                queue.Enqueue(new CoreMidiNativeMessage
                {
                    Sequence = ++sequence,
                    MonotonicSeconds = now,
                    MessageKind = kind,
                    Channel = channel,
                    Data1 = data1,
                    Data2 = data2
                });
            }
        }

        private void MarkClosed()
        {
            lock (gate) connected = false;
        }

#if ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(MidiInProc))]
#endif
        private static void OnMidiInput(IntPtr midiIn, uint message, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            if (instance == IntPtr.Zero) return;
            WinMmMidiNativeApi api;
            try
            {
                api = GCHandle.FromIntPtr(instance).Target as WinMmMidiNativeApi;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            if (api == null) return;
            switch (message)
            {
                case MimData:
                case MimMoreData:
                    api.Enqueue(unchecked((uint)param1.ToInt64()));
                    break;
                case MimClose:
                    api.MarkClosed();
                    break;
                case MimOpen:
                default:
                    break;
            }
        }

        private List<WinMmDevice> Enumerate()
        {
            var result = new List<WinMmDevice>();
            if (!isAvailable) return result;
            uint count = NativeMidiInGetNumDevs();
            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            for (uint index = 0; index < count; index++)
            {
                var caps = new MidiInCaps();
                if (NativeMidiInGetDevCaps((UIntPtr)index, ref caps, (uint)Marshal.SizeOf(typeof(MidiInCaps))) != MmSysErrNoError) continue;
                string name = string.IsNullOrWhiteSpace(caps.ProductName) ? $"MIDI Input {index + 1}" : caps.ProductName.Trim();
                occurrences.TryGetValue(name, out int occurrence);
                occurrences[name] = occurrence + 1;
                result.Add(new WinMmDevice(index, name, StableEndpointId(name, occurrence)));
            }
            return result;
        }

        private static bool SameDevices(List<WinMmDevice> left, List<WinMmDevice> right)
        {
            if (left.Count != right.Count) return false;
            for (int index = 0; index < left.Count; index++)
                if (left[index].EndpointId != right[index].EndpointId || left[index].DeviceIndex != right[index].DeviceIndex) return false;
            return true;
        }

        private int Fail(int code, string message)
        {
            lock (gate) lastError = message;
            return code;
        }

        private sealed class WinMmDevice
        {
            public WinMmDevice(uint deviceIndex, string name, long endpointId)
            {
                DeviceIndex = deviceIndex;
                Name = name;
                EndpointId = endpointId;
            }

            public uint DeviceIndex { get; }
            public string Name { get; }
            public long EndpointId { get; }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void MidiInProc(IntPtr midiIn, uint message, IntPtr instance, IntPtr param1, IntPtr param2);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MidiInCaps
        {
            public ushort ManufacturerId;
            public ushort ProductId;
            public uint DriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string ProductName;
            public uint Support;
        }

        [DllImport("winmm.dll", EntryPoint = "midiInGetNumDevs")] private static extern uint NativeMidiInGetNumDevs();
        [DllImport("winmm.dll", EntryPoint = "midiInGetDevCapsW", CharSet = CharSet.Unicode)] private static extern uint NativeMidiInGetDevCaps(UIntPtr deviceId, ref MidiInCaps caps, uint size);
        [DllImport("winmm.dll", EntryPoint = "midiInOpen")] private static extern uint NativeMidiInOpen(out IntPtr handle, uint deviceId, MidiInProc callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll", EntryPoint = "midiInStart")] private static extern uint NativeMidiInStart(IntPtr handle);
        [DllImport("winmm.dll", EntryPoint = "midiInStop")] private static extern uint NativeMidiInStop(IntPtr handle);
        [DllImport("winmm.dll", EntryPoint = "midiInReset")] private static extern uint NativeMidiInReset(IntPtr handle);
        [DllImport("winmm.dll", EntryPoint = "midiInClose")] private static extern uint NativeMidiInClose(IntPtr handle);
    }

    public static class NativeMidiApiFactory
    {
        /// <summary>The MIDI backend for the current platform; unavailable where none exists.</summary>
        public static ICoreMidiNativeApi Create()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return new WinMmMidiNativeApi();
#else
            return new CoreMidiNativeApi();
#endif
        }
    }
}
