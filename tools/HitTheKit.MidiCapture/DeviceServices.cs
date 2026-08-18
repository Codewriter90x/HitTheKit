using Melanchall.DryWetMidi.Multimedia;

namespace HitTheKit.MidiCapture;

public static class DeviceServices
{
    public static int SelectIndex(IReadOnlyList<DeviceSnapshot> devices, int? index, string? name)
    {
        ArgumentNullException.ThrowIfNull(devices);
        if (index.HasValue && name is not null) throw new ArgumentException("Select by index or name, not both.");
        if (index.HasValue)
        {
            if (index.Value < 0 || index.Value >= devices.Count || devices[index.Value].Index != index.Value)
                throw new ArgumentOutOfRangeException(nameof(index), $"Device index {index.Value} is unavailable.");
            return index.Value;
        }
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A device index or exact name is required.");
        DeviceSnapshot[] matches = devices.Where(device => string.Equals(device.Name, name, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1) throw new ArgumentException(matches.Length == 0 ? $"Device '{name}' was not found." : $"Device name '{name}' is ambiguous.");
        return matches[0].Index;
    }

    public static IReadOnlyList<DeviceSnapshot> List()
    {
        var values = new List<DeviceSnapshot>();
        int index = 0;
        foreach (InputDevice device in InputDevice.GetAll())
        {
            using (device)
            {
                values.Add(new(index++, device.Name, TryManufacturer(device)));
            }
        }
        return values;
    }

    public static InputDevice Open(int? index, string? name)
    {
        IReadOnlyList<DeviceSnapshot> devices = List();
        int selectedIndex = SelectIndex(devices, index, name);
        InputDevice device;
        try
        {
            device = InputDevice.GetByIndex(selectedIndex);
        }
        catch (Exception exception)
        {
            throw new IOException("The selected MIDI input disappeared before it could be opened.", exception);
        }
        if (!string.Equals(device.Name, devices[selectedIndex].Name, StringComparison.Ordinal))
        {
            device.Dispose();
            throw new IOException("The MIDI input list changed while the selected device was being opened. Run 'list' and try again.");
        }
        return device;
    }

    public static string? TryManufacturer(InputDevice device)
    {
        try
        {
            if (!InputDevice.GetSupportedProperties().Contains(InputDeviceProperty.Manufacturer)) return null;
            return device.GetProperty(InputDeviceProperty.Manufacturer) as string;
        }
        catch
        {
            return null;
        }
    }
}
