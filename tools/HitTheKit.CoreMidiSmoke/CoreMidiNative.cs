using System.Runtime.InteropServices;
using System.Text;

namespace HitTheKit.CoreMidiSmoke;

internal enum CoreMidiMessageKind
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
internal struct CoreMidiDeviceInfo
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
internal struct CoreMidiMessage
{
    public ulong Sequence;
    public double MonotonicSeconds;
    public int MessageKind;
    public int Channel;
    public int Data1;
    public int Data2;

    public readonly bool IsNoteOffEquivalent =>
        MessageKind == (int)CoreMidiMessageKind.NoteOff ||
        (MessageKind == (int)CoreMidiMessageKind.NoteOn && Data2 == 0);
}

[StructLayout(LayoutKind.Sequential)]
internal struct CoreMidiDiagnostics
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

internal sealed class CoreMidiNativeApi
{
    internal const int ExpectedApiVersion = 2;
    internal const string PluginName = "HitTheKitCoreMidi";

    public bool IsLoaded { get; private set; }
    public int ApiVersion { get; private set; }
    public string? LoadError { get; private set; }
    public static bool AbiLayoutMatches =>
        Marshal.SizeOf<CoreMidiDeviceInfo>() == 1304 &&
        Marshal.SizeOf<CoreMidiMessage>() == 32 &&
        Marshal.SizeOf<CoreMidiDiagnostics>() == 48;

    public CoreMidiNativeApi()
    {
        try
        {
            ApiVersion = NativeGetApiVersion();
            IsLoaded = true;
        }
        catch (Exception exception) when (IsLoadException(exception))
        {
            LoadError = "CoreMIDI plug-in is not present, loadable, or compatible.";
        }
    }

    public int CreateClient() => NativeCreateClient();
    public double GetMonotonicSeconds() => NativeGetMonotonicSeconds();
    public void DestroyClient() => NativeDestroyClient();
    public int RefreshDevices() => NativeRefreshDevices();
    public int GetDeviceCount() => NativeGetDeviceCount();
    public int GetDeviceInfo(int index, out CoreMidiDeviceInfo info) => NativeGetDeviceInfo(index, out info);
    public int OpenInput(long endpointId) => NativeOpenInput(endpointId);
    public void CloseInput() => NativeCloseInput();
    public int PollMessages(CoreMidiMessage[] buffer) => NativePollMessages(buffer, buffer.Length);
    public int GetConnectionState() => NativeGetConnectionState();
    public int GetDiagnostics(out CoreMidiDiagnostics diagnostics) => NativeGetDiagnostics(out diagnostics);

    public string GetLastError()
    {
        var buffer = new StringBuilder(512);
        NativeGetLastError(buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static bool IsLoadException(Exception exception) =>
        exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;

    [DllImport(PluginName, EntryPoint = "htk_coremidi_get_api_version", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeGetApiVersion();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_get_monotonic_seconds", CallingConvention = CallingConvention.Cdecl)]
    private static extern double NativeGetMonotonicSeconds();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_create_client", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeCreateClient();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_destroy_client", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeDestroyClient();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_refresh_devices", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeRefreshDevices();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_get_device_count", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeGetDeviceCount();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_get_device_info", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int NativeGetDeviceInfo(int index, out CoreMidiDeviceInfo info);
    [DllImport(PluginName, EntryPoint = "htk_coremidi_open_input", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeOpenInput(long endpointId);
    [DllImport(PluginName, EntryPoint = "htk_coremidi_close_input", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeCloseInput();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_poll_messages", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativePollMessages([Out] CoreMidiMessage[] buffer, int capacity);
    [DllImport(PluginName, EntryPoint = "htk_coremidi_get_connection_state", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeGetConnectionState();
    [DllImport(PluginName, EntryPoint = "htk_coremidi_get_diagnostics", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeGetDiagnostics(out CoreMidiDiagnostics diagnostics);
    [DllImport(PluginName, EntryPoint = "htk_coremidi_get_last_error", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int NativeGetLastError(StringBuilder buffer, int capacity);
}

internal sealed class CoreMidiSession : IDisposable
{
    private bool created;

    public CoreMidiSession(CoreMidiNativeApi api)
    {
        Api = api;
    }

    public CoreMidiNativeApi Api { get; }

    public bool Create(out string? error)
    {
        error = null;
        if (!Api.IsLoaded)
        {
            error = Api.LoadError;
            return false;
        }
        if (Api.ApiVersion != CoreMidiNativeApi.ExpectedApiVersion)
        {
            error = $"CoreMIDI ABI {Api.ApiVersion} does not match expected ABI {CoreMidiNativeApi.ExpectedApiVersion}.";
            return false;
        }
        if (!CoreMidiNativeApi.AbiLayoutMatches)
        {
            error = "CoreMIDI ABI structure layout is incompatible.";
            return false;
        }
        if (Api.CreateClient() != 0)
        {
            error = Api.GetLastError();
            return false;
        }
        created = true;
        return true;
    }

    public void Dispose()
    {
        if (!created) return;
        Api.CloseInput();
        Api.DestroyClient();
        created = false;
    }
}
