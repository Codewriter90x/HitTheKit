using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HitTheKit.Unity.Devices;

namespace HitTheKit.Unity.DeviceSetup
{
    public enum DeviceSetupInputBackend
    {
        Simulated,
        CoreMidiMacOS
    }

    public enum CoreMidiNativeMessageKind
    {
        NoteOn,
        NoteOff,
        ControlChange,
        PolyAftertouch,
        ChannelAftertouch,
        PitchBend,
        ProgramChange
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CoreMidiNativeDeviceInfo
    {
        public int Index;
        public long EndpointId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Manufacturer;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Model;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string EntityName;
        public int Protocol;
        public int IsOnline;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CoreMidiNativeMessage
    {
        public ulong Sequence;
        public double MonotonicSeconds;
        public int MessageKind;
        public int Channel;
        public int Data1;
        public int Data2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CoreMidiNativeDiagnostics
    {
        public ulong DeviceGeneration;
        public ulong MessagesReceived;
        public ulong DroppedMessages;
        public int QueueSize;
        public int QueueCapacity;
        public long SelectedEndpointId;
        public int ClientState;
        public int ConnectionState;
    }

    public interface ICoreMidiNativeApi
    {
        bool IsAvailable { get; }
        string AvailabilityMessage { get; }
        int GetApiVersion();
        double GetMonotonicSeconds();
        int CreateClient();
        void DestroyClient();
        int RefreshDevices();
        int GetDeviceCount();
        int GetDeviceInfo(int index, out CoreMidiNativeDeviceInfo info);
        int OpenInput(long endpointId);
        void CloseInput();
        int PollMessages(CoreMidiNativeMessage[] buffer, int capacity);
        int GetConnectionState();
        int GetDiagnostics(out CoreMidiNativeDiagnostics diagnostics);
        string GetLastError();
    }

    public sealed class CoreMidiNativeApi : ICoreMidiNativeApi
    {
        public const int ExpectedApiVersion = 2;
        private const string Library = "HitTheKitCoreMidi";
        private string availabilityMessage;
        private bool isAvailable;

        public CoreMidiNativeApi()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            try
            {
                int version = NativeGetApiVersion();
                isAvailable = version == ExpectedApiVersion;
                availabilityMessage = isAvailable
                    ? null
                    : $"CoreMIDI plug-in API {version} does not match expected API {ExpectedApiVersion}.";
            }
            catch (Exception exception) when (IsLoadException(exception))
            {
                isAvailable = false;
                availabilityMessage = "CoreMIDI plug-in is not built or cannot be loaded.";
            }
#else
            isAvailable = false;
            availabilityMessage = "CoreMIDI runtime is available only on macOS.";
#endif
        }

        public bool IsAvailable => isAvailable;
        public string AvailabilityMessage => availabilityMessage;
        public int GetApiVersion() => Invoke(NativeGetApiVersion);
        public double GetMonotonicSeconds()
        {
            if (!IsAvailable) return double.NaN;
            try { return NativeGetMonotonicSeconds(); }
            catch (Exception exception) when (IsLoadException(exception)) { MarkUnavailable(); return double.NaN; }
        }
        public int CreateClient() => Invoke(NativeCreateClient);
        public void DestroyClient() => InvokeVoid(NativeDestroyClient);
        public int RefreshDevices() => Invoke(NativeRefreshDevices);
        public int GetDeviceCount() => Invoke(NativeGetDeviceCount);
        public int GetDeviceInfo(int index, out CoreMidiNativeDeviceInfo info)
        {
            info = default;
            if (!IsAvailable) return -1000;
            try { return NativeGetDeviceInfo(index, out info); }
            catch (Exception exception) when (IsLoadException(exception)) { MarkUnavailable(); return -1000; }
        }
        public int OpenInput(long endpointId) => Invoke(() => NativeOpenInput(endpointId));
        public void CloseInput() => InvokeVoid(NativeCloseInput);
        public int PollMessages(CoreMidiNativeMessage[] buffer, int capacity)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (capacity < 0 || capacity > buffer.Length) throw new ArgumentOutOfRangeException(nameof(capacity));
            return Invoke(() => NativePollMessages(buffer, capacity));
        }
        public int GetConnectionState() => Invoke(NativeGetConnectionState);
        public int GetDiagnostics(out CoreMidiNativeDiagnostics diagnostics)
        {
            diagnostics = default;
            if (!IsAvailable) return -1000;
            try { return NativeGetDiagnostics(out diagnostics); }
            catch (Exception exception) when (IsLoadException(exception)) { MarkUnavailable(); return -1000; }
        }

        public string GetLastError()
        {
            if (!IsAvailable) return availabilityMessage;
            var buffer = new StringBuilder(512);
            try { NativeGetLastError(buffer, buffer.Capacity); return buffer.ToString(); }
            catch (Exception exception) when (IsLoadException(exception)) { MarkUnavailable(); return availabilityMessage; }
        }

        private int Invoke(Func<int> action)
        {
            if (!IsAvailable) return -1000;
            try { return action(); }
            catch (Exception exception) when (IsLoadException(exception)) { MarkUnavailable(); return -1000; }
        }

        private void InvokeVoid(Action action)
        {
            if (!IsAvailable) return;
            try { action(); }
            catch (Exception exception) when (IsLoadException(exception)) { MarkUnavailable(); }
        }

        private void MarkUnavailable()
        {
            isAvailable = false;
            availabilityMessage = "CoreMIDI plug-in became unavailable.";
        }

        private static bool IsLoadException(Exception exception) =>
            exception is DllNotFoundException || exception is EntryPointNotFoundException || exception is BadImageFormatException;

        [DllImport(Library, EntryPoint = "htk_coremidi_get_api_version", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeGetApiVersion();
        [DllImport(Library, EntryPoint = "htk_coremidi_get_monotonic_seconds", CallingConvention = CallingConvention.Cdecl)] private static extern double NativeGetMonotonicSeconds();
        [DllImport(Library, EntryPoint = "htk_coremidi_create_client", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeCreateClient();
        [DllImport(Library, EntryPoint = "htk_coremidi_destroy_client", CallingConvention = CallingConvention.Cdecl)] private static extern void NativeDestroyClient();
        [DllImport(Library, EntryPoint = "htk_coremidi_refresh_devices", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeRefreshDevices();
        [DllImport(Library, EntryPoint = "htk_coremidi_get_device_count", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeGetDeviceCount();
        [DllImport(Library, EntryPoint = "htk_coremidi_get_device_info", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] private static extern int NativeGetDeviceInfo(int index, out CoreMidiNativeDeviceInfo info);
        [DllImport(Library, EntryPoint = "htk_coremidi_open_input", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeOpenInput(long endpointId);
        [DllImport(Library, EntryPoint = "htk_coremidi_close_input", CallingConvention = CallingConvention.Cdecl)] private static extern void NativeCloseInput();
        [DllImport(Library, EntryPoint = "htk_coremidi_poll_messages", CallingConvention = CallingConvention.Cdecl)] private static extern int NativePollMessages([Out] CoreMidiNativeMessage[] buffer, int capacity);
        [DllImport(Library, EntryPoint = "htk_coremidi_get_connection_state", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeGetConnectionState();
        [DllImport(Library, EntryPoint = "htk_coremidi_get_diagnostics", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeGetDiagnostics(out CoreMidiNativeDiagnostics diagnostics);
        [DllImport(Library, EntryPoint = "htk_coremidi_get_last_error", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] private static extern int NativeGetLastError(StringBuilder buffer, int capacity);
    }

    public sealed class CoreMidiNativeSession : IDisposable
    {
        private readonly ICoreMidiNativeApi api;
        private bool created;
        private bool disposed;

        public CoreMidiNativeSession(ICoreMidiNativeApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public ICoreMidiNativeApi Api => api;
        public bool IsAvailable => !disposed && api.IsAvailable && api.GetApiVersion() == CoreMidiNativeApi.ExpectedApiVersion;
        public string UnavailableReason => api.AvailabilityMessage ?? api.GetLastError();

        public bool EnsureCreated()
        {
            if (disposed || !IsAvailable) return false;
            if (created) return true;
            created = api.CreateClient() == 0;
            return created;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (created)
            {
                api.CloseInput();
                api.DestroyClient();
            }
            created = false;
        }
    }

    public sealed class CoreMidiDrumDeviceDiscovery : IDrumDeviceDiscovery
    {
        private readonly CoreMidiNativeSession session;
        private readonly Func<CoreMidiNativeDeviceInfo, IReadOnlyList<DeviceProfileOption>> profileResolver;
        private ulong observedGeneration;

        public CoreMidiDrumDeviceDiscovery(
            CoreMidiNativeSession session,
            Func<CoreMidiNativeDeviceInfo, IReadOnlyList<DeviceProfileOption>> profileResolver = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.profileResolver = profileResolver ?? (_ => Array.AsReadOnly(Array.Empty<DeviceProfileOption>()));
        }

        public bool HasDevicesChanged
        {
            get
            {
                return session.Api.GetDiagnostics(out CoreMidiNativeDiagnostics diagnostics) == 0 &&
                    diagnostics.DeviceGeneration != observedGeneration;
            }
        }

        public DeviceDiscoverySnapshot Refresh()
        {
            if (!session.EnsureCreated())
                return new DeviceDiscoverySnapshot(DeviceDiscoveryState.Failed, Array.Empty<DrumDeviceDescriptor>(), session.UnavailableReason);
            if (session.Api.RefreshDevices() < 0)
                return new DeviceDiscoverySnapshot(DeviceDiscoveryState.Failed, Array.Empty<DrumDeviceDescriptor>(), session.Api.GetLastError());

            int count = session.Api.GetDeviceCount();
            var devices = new List<DrumDeviceDescriptor>(Math.Max(0, count));
            for (int index = 0; index < count; index++)
            {
                if (session.Api.GetDeviceInfo(index, out CoreMidiNativeDeviceInfo info) != 0) continue;
                string name = string.IsNullOrWhiteSpace(info.Name) ? $"MIDI Input {index + 1}" : info.Name;
                string displayName = string.IsNullOrWhiteSpace(info.DeviceName) ? name : info.DeviceName;
                devices.Add(new DrumDeviceDescriptor(
                    DeviceId(info.EndpointId),
                    displayName,
                    info.Manufacturer ?? string.Empty,
                    name,
                    info.IsOnline == 0 ? DeviceConnectionState.Disconnected : DeviceConnectionState.Connected,
                    profileResolver(info)));
            }
            if (session.Api.GetDiagnostics(out CoreMidiNativeDiagnostics diagnostics) == 0)
                observedGeneration = diagnostics.DeviceGeneration;
            return new DeviceDiscoverySnapshot(DeviceDiscoveryState.Ready, devices,
                devices.Count == 0 ? "No CoreMIDI input endpoints are available." : null);
        }

        public static string DeviceId(long endpointId) => "coremidi." + endpointId.ToString(CultureInfo.InvariantCulture);

        public static bool TryParseDeviceId(string deviceId, out long endpointId)
        {
            const string prefix = "coremidi.";
            endpointId = 0;
            return deviceId != null && deviceId.StartsWith(prefix, StringComparison.Ordinal) &&
                long.TryParse(deviceId.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out endpointId);
        }
    }

    public sealed class CoreMidiGuidedMidiCaptureSource : IGuidedMidiCaptureSource, IDisposable
    {
        private const int PollCapacity = 128;
        private const int MaximumBatchesPerPoll = 8;
        private readonly CoreMidiNativeSession session;
        private readonly CoreMidiNativeMessage[] buffer = new CoreMidiNativeMessage[PollCapacity];
        private readonly int pollingThreadId;
        private long selectedEndpointId;
        private bool hasSelection;
        private bool disposed;

        public CoreMidiGuidedMidiCaptureSource(CoreMidiNativeSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            pollingThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public event Action<RawMidiMessage> MessageReceived;
        public event Action<DeviceConnectionState> ConnectionChanged;
        public DeviceConnectionState ConnectionState { get; private set; } = DeviceConnectionState.Disconnected;
        public bool IsCapturing { get; private set; }
        public string ActiveStepId { get; private set; }
        public ulong LastSequence { get; private set; }
        public CoreMidiNativeDiagnostics Diagnostics { get; private set; }
        public string LastError { get; private set; }

        public void SelectDevice(string deviceId)
        {
            if (!CoreMidiDrumDeviceDiscovery.TryParseDeviceId(deviceId, out long endpointId))
                throw new ArgumentException("CoreMIDI device ID is invalid.", nameof(deviceId));
            Stop();
            selectedEndpointId = endpointId;
            hasSelection = true;
        }

        public void Start(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId)) throw new ArgumentException("Step ID is required.", nameof(stepId));
            if (disposed || !hasSelection || !session.EnsureCreated())
            {
                SetConnection(DeviceConnectionState.Disconnected);
                LastError = !hasSelection ? "No CoreMIDI endpoint is selected." : session.UnavailableReason;
                return;
            }
            if (session.Api.OpenInput(selectedEndpointId) != 0)
            {
                LastError = session.Api.GetLastError();
                SetConnection(DeviceConnectionState.Disconnected);
                return;
            }
            ActiveStepId = stepId;
            IsCapturing = true;
            LastError = null;
            SetConnection(DeviceConnectionState.Connected);
        }

        public void Stop()
        {
            if (IsCapturing) session.Api.CloseInput();
            IsCapturing = false;
            ActiveStepId = null;
        }

        public int Poll()
        {
            if (disposed || !IsCapturing) return 0;
            if (Thread.CurrentThread.ManagedThreadId != pollingThreadId)
                throw new InvalidOperationException("CoreMIDI polling must run on the thread that created the capture source.");
            if (session.Api.GetConnectionState() != 1)
            {
                session.Api.CloseInput();
                IsCapturing = false;
                ActiveStepId = null;
                SetConnection(DeviceConnectionState.Disconnected);
                return 0;
            }

            int total = 0;
            for (int batch = 0; batch < MaximumBatchesPerPoll; batch++)
            {
                int count = session.Api.PollMessages(buffer, buffer.Length);
                if (count < 0)
                {
                    LastError = session.Api.GetLastError();
                    break;
                }
                for (int index = 0; index < count; index++)
                {
                    LastSequence = buffer[index].Sequence;
                    RawMidiMessage message = Convert(buffer[index]);
                    if (message != null) MessageReceived?.Invoke(message);
                }
                total += count;
                if (count < buffer.Length) break;
            }
            if (session.Api.GetDiagnostics(out CoreMidiNativeDiagnostics diagnostics) == 0) Diagnostics = diagnostics;
            return total;
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            disposed = true;
            MessageReceived = null;
            ConnectionChanged = null;
        }

        public static RawMidiMessage Convert(CoreMidiNativeMessage message)
        {
            switch ((CoreMidiNativeMessageKind)message.MessageKind)
            {
                case CoreMidiNativeMessageKind.NoteOn: return RawMidiMessage.NoteOn(message.Channel, message.Data1, message.Data2, message.MonotonicSeconds);
                case CoreMidiNativeMessageKind.NoteOff: return RawMidiMessage.NoteOff(message.Channel, message.Data1, message.Data2, message.MonotonicSeconds);
                case CoreMidiNativeMessageKind.ControlChange: return RawMidiMessage.ControlChange(message.Channel, message.Data1, message.Data2, message.MonotonicSeconds);
                case CoreMidiNativeMessageKind.PolyAftertouch: return RawMidiMessage.PolyAftertouch(message.Channel, message.Data1, message.Data2, message.MonotonicSeconds);
                case CoreMidiNativeMessageKind.ChannelAftertouch: return RawMidiMessage.ChannelAftertouch(message.Channel, message.Data2, message.MonotonicSeconds);
                case CoreMidiNativeMessageKind.PitchBend: return RawMidiMessage.PitchBend(message.Channel, message.Data2, message.MonotonicSeconds);
                case CoreMidiNativeMessageKind.ProgramChange: return RawMidiMessage.ProgramChange(message.Channel, message.Data1, message.MonotonicSeconds);
                default: return null;
            }
        }

        private void SetConnection(DeviceConnectionState state)
        {
            if (ConnectionState == state) return;
            ConnectionState = state;
            ConnectionChanged?.Invoke(state);
        }
    }

    public static class DeviceSetupProfileCatalog
    {
        public static IReadOnlyList<DeviceProfileOption> ForCoreMidiDevice(CoreMidiNativeDeviceInfo info)
        {
            bool isObservedHampbackEndpoint = string.Equals(info.Manufacturer, "DREAM S.A.S.", StringComparison.OrdinalIgnoreCase) &&
                ((info.Name ?? string.Empty).IndexOf("eDrum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 (info.EntityName ?? string.Empty).IndexOf("eDrum", StringComparison.OrdinalIgnoreCase) >= 0);
            return isObservedHampbackEndpoint
                ? Array.AsReadOnly(new[] { SimulatedDrumDeviceDiscovery.CreateHampbackCandidateProfile() })
                : Array.AsReadOnly(Array.Empty<DeviceProfileOption>());
        }
    }
}
