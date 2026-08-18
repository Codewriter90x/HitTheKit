#include "HitTheKitCoreMidi.h"
#include "BoundedMidiQueue.h"
#include "Midi1Parser.h"

#include <CoreFoundation/CoreFoundation.h>
#include <CoreMIDI/CoreMIDI.h>
#include <mach/mach_time.h>

#include <algorithm>
#include <atomic>
#include <cstring>
#include <mutex>
#include <string>
#include <vector>

namespace {

constexpr std::size_t kQueueCapacity = 4096;

struct DeviceRecord {
    HtkMidiDeviceInfo info{};
    MIDIEndpointRef endpoint = 0;
};

struct RuntimeState {
    std::mutex operation_mutex;
    std::mutex mutex;
    MIDIClientRef client = 0;
    MIDIPortRef input_port = 0;
    MIDIEndpointRef selected_endpoint = 0;
    int64_t selected_endpoint_id = 0;
    uint64_t selected_validation_generation = 0;
    uint64_t active_input_generation = 0;
    uint64_t next_input_generation = 0;
    bool selected_endpoint_present = false;
    bool accepting = false;
    std::vector<DeviceRecord> devices;
    htk::BoundedMidiQueue queue{kQueueCapacity};
    htk::Midi1Parser parser;
    std::atomic<uint64_t> device_generation{0};
    std::string last_error;
};

RuntimeState g_state;

void SetError(const std::string& message) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    g_state.last_error = message;
}

void SetStatusError(const char* operation, OSStatus status) {
    SetError(std::string(operation) + " failed with OSStatus " + std::to_string(status) + ".");
}

void ClearErrorLocked() {
    g_state.last_error.clear();
}

void CopyText(char* output, std::size_t capacity, const std::string& value) {
    if (output == nullptr || capacity == 0) return;
    const std::size_t count = std::min(capacity - 1, value.size());
    std::memcpy(output, value.data(), count);
    output[count] = '\0';
}

std::string ReadString(MIDIObjectRef object, CFStringRef property) {
    if (object == 0) return {};
    CFStringRef value = nullptr;
    if (MIDIObjectGetStringProperty(object, property, &value) != noErr || value == nullptr) return {};
    char buffer[HTK_COREMIDI_TEXT_CAPACITY]{};
    const Boolean copied = CFStringGetCString(value, buffer, sizeof(buffer), kCFStringEncodingUTF8);
    CFRelease(value);
    return copied ? std::string(buffer) : std::string();
}

int32_t ReadInteger(MIDIObjectRef object, CFStringRef property, int32_t fallback) {
    SInt32 value = fallback;
    return object != 0 && MIDIObjectGetIntegerProperty(object, property, &value) == noErr
        ? static_cast<int32_t>(value)
        : fallback;
}

double HostTicksToSeconds(uint64_t ticks) {
    static mach_timebase_info_data_t timebase = [] {
        mach_timebase_info_data_t result{};
        mach_timebase_info(&result);
        return result;
    }();
    return static_cast<double>(ticks) * static_cast<double>(timebase.numer) /
        static_cast<double>(timebase.denom) / 1'000'000'000.0;
}

int64_t StableEndpointId(MIDIEndpointRef endpoint) {
    const int32_t unique_id = ReadInteger(endpoint, kMIDIPropertyUniqueID, 0);
    if (unique_id != 0) return static_cast<int64_t>(unique_id);
    return (static_cast<int64_t>(1) << 32) | static_cast<uint32_t>(endpoint);
}

bool EndpointIsPresentAndOnline(MIDIEndpointRef endpoint) {
    bool present = false;
    const ItemCount count = MIDIGetNumberOfSources();
    for (ItemCount index = 0; index < count; ++index) {
        if (MIDIGetSource(index) == endpoint) {
            present = true;
            break;
        }
    }
    if (!present) return false;
    SInt32 offline = 0;
    return MIDIObjectGetIntegerProperty(endpoint, kMIDIPropertyOffline, &offline) != noErr || offline == 0;
}

std::vector<DeviceRecord> EnumerateDevices() {
    std::vector<DeviceRecord> result;
    const ItemCount count = MIDIGetNumberOfSources();
    result.reserve(static_cast<std::size_t>(count));
    for (ItemCount index = 0; index < count; ++index) {
        const MIDIEndpointRef endpoint = MIDIGetSource(index);
        if (endpoint == 0) continue;

        MIDIEntityRef entity = 0;
        MIDIDeviceRef device = 0;
        MIDIEndpointGetEntity(endpoint, &entity);
        if (entity != 0) MIDIEntityGetDevice(entity, &device);

        DeviceRecord record{};
        record.endpoint = endpoint;
        record.info.index = static_cast<int32_t>(result.size());
        record.info.endpoint_id = StableEndpointId(endpoint);
        const std::string endpoint_name = ReadString(endpoint, kMIDIPropertyDisplayName);
        const std::string fallback_name = ReadString(endpoint, kMIDIPropertyName);
        CopyText(record.info.name, sizeof(record.info.name), endpoint_name.empty() ? fallback_name : endpoint_name);
        CopyText(record.info.device_name, sizeof(record.info.device_name), ReadString(device, kMIDIPropertyName));
        CopyText(record.info.manufacturer, sizeof(record.info.manufacturer),
            !ReadString(endpoint, kMIDIPropertyManufacturer).empty()
                ? ReadString(endpoint, kMIDIPropertyManufacturer)
                : ReadString(device, kMIDIPropertyManufacturer));
        CopyText(record.info.model, sizeof(record.info.model),
            !ReadString(endpoint, kMIDIPropertyModel).empty()
                ? ReadString(endpoint, kMIDIPropertyModel)
                : ReadString(device, kMIDIPropertyModel));
        CopyText(record.info.entity_name, sizeof(record.info.entity_name), ReadString(entity, kMIDIPropertyName));
        record.info.protocol = ReadInteger(endpoint, kMIDIPropertyProtocolID, 0);
        record.info.is_online = ReadInteger(endpoint, kMIDIPropertyOffline, 0) == 0 ? 1 : 0;
        result.push_back(record);
    }
    return result;
}

void EnqueueMessageLocked(int32_t kind, int32_t channel, int32_t data1, int32_t data2, double timestamp) {
    if (!g_state.accepting) return;
    HtkMidiMessage message{};
    message.monotonic_seconds = timestamp;
    message.message_kind = kind;
    message.channel = channel;
    message.data1 = data1;
    message.data2 = data2;
    g_state.queue.Push(message);
}

void ParseBytesLocked(const uint8_t* bytes, std::size_t length, double timestamp) {
    if (!g_state.accepting) return;
    g_state.parser.Parse(bytes, length,
        [timestamp](int32_t kind, int32_t channel, int32_t data1, int32_t data2) {
            EnqueueMessageLocked(kind, channel, data1, data2, timestamp);
        });
}

uint64_t NextInputGenerationLocked() {
    ++g_state.next_input_generation;
    if (g_state.next_input_generation == 0) ++g_state.next_input_generation;
    return g_state.next_input_generation;
}

void ParseCallbackBytes(uint64_t input_generation, const uint8_t* bytes, std::size_t length, double timestamp) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    if (!g_state.accepting || input_generation == 0 || input_generation != g_state.active_input_generation) return;
    ParseBytesLocked(bytes, length, timestamp);
}

void ReadCallback(const MIDIPacketList* packet_list, void* read_proc_ref_con, void*) noexcept {
    try {
        if (packet_list == nullptr) return;
        const uint64_t input_generation = static_cast<uint64_t>(
            reinterpret_cast<std::uintptr_t>(read_proc_ref_con));
        const MIDIPacket* packet = &packet_list->packet[0];
        for (UInt32 packet_index = 0; packet_index < packet_list->numPackets; ++packet_index) {
            const double timestamp = HostTicksToSeconds(packet->timeStamp != 0 ? packet->timeStamp : mach_absolute_time());
            ParseCallbackBytes(input_generation, packet->data, packet->length, timestamp);
            packet = MIDIPacketNext(packet);
        }
    } catch (const std::exception& exception) {
        SetError(std::string("CoreMIDI callback exception: ") + exception.what());
    } catch (...) {
        SetError("Unknown CoreMIDI callback exception.");
    }
}

void NotifyCallback(const MIDINotification*, void*) noexcept {
    g_state.device_generation.fetch_add(1, std::memory_order_relaxed);
}

void CloseInputInternal() {
    MIDIPortRef port = 0;
    MIDIEndpointRef endpoint = 0;
    {
        std::lock_guard<std::mutex> lock(g_state.mutex);
        g_state.accepting = false;
        g_state.active_input_generation = 0;
        port = g_state.input_port;
        endpoint = g_state.selected_endpoint;
        g_state.input_port = 0;
        g_state.selected_endpoint = 0;
        g_state.selected_endpoint_id = 0;
        g_state.selected_endpoint_present = false;
        g_state.queue.Clear();
        g_state.parser.Reset();
    }
    if (port != 0 && endpoint != 0) MIDIPortDisconnectSource(port, endpoint);
    if (port != 0) MIDIPortDispose(port);
}

template <typename Action>
int32_t Guard(Action action) noexcept {
    try {
        return action();
    } catch (const std::exception& exception) {
        SetError(std::string("Native exception: ") + exception.what());
    } catch (...) {
        SetError("Unknown native exception.");
    }
    return -1;
}

} // namespace

extern "C" {

int32_t htk_coremidi_get_api_version(void) { return HTK_COREMIDI_API_VERSION; }
double htk_coremidi_get_monotonic_seconds(void) { return HostTicksToSeconds(mach_absolute_time()); }

int32_t htk_coremidi_create_client(void) {
    return Guard([] {
        std::lock_guard<std::mutex> operation_lock(g_state.operation_mutex);
        std::lock_guard<std::mutex> lock(g_state.mutex);
        if (g_state.client != 0) return int32_t{0};
        MIDIClientRef client = 0;
        const OSStatus status = MIDIClientCreate(CFSTR("HitTheKit CoreMIDI"), NotifyCallback, nullptr, &client);
        if (status != noErr) {
            g_state.last_error = "MIDIClientCreate failed with OSStatus " + std::to_string(status) + ".";
            return static_cast<int32_t>(status == 0 ? -1 : status);
        }
        g_state.client = client;
        ClearErrorLocked();
        return int32_t{0};
    });
}

void htk_coremidi_destroy_client(void) {
    try {
        std::lock_guard<std::mutex> operation_lock(g_state.operation_mutex);
        CloseInputInternal();
        MIDIClientRef client = 0;
        {
            std::lock_guard<std::mutex> lock(g_state.mutex);
            client = g_state.client;
            g_state.client = 0;
            g_state.devices.clear();
        }
        if (client != 0) MIDIClientDispose(client);
    } catch (...) {
        SetError("Native exception while destroying CoreMIDI client.");
    }
}

int32_t htk_coremidi_refresh_devices(void) {
    return Guard([] {
        std::lock_guard<std::mutex> operation_lock(g_state.operation_mutex);
        {
            std::lock_guard<std::mutex> lock(g_state.mutex);
            if (g_state.client == 0) {
                g_state.last_error = "CoreMIDI client has not been created.";
                return int32_t{-2};
            }
        }
        std::vector<DeviceRecord> devices = EnumerateDevices();
        std::lock_guard<std::mutex> lock(g_state.mutex);
        g_state.devices = std::move(devices);
        ClearErrorLocked();
        return static_cast<int32_t>(g_state.devices.size());
    });
}

int32_t htk_coremidi_get_device_count(void) {
    return Guard([] {
        std::lock_guard<std::mutex> lock(g_state.mutex);
        return static_cast<int32_t>(g_state.devices.size());
    });
}

int32_t htk_coremidi_get_device_info(int32_t index, HtkMidiDeviceInfo* output) {
    return Guard([=] {
        if (output == nullptr) return int32_t{-3};
        std::lock_guard<std::mutex> lock(g_state.mutex);
        if (index < 0 || static_cast<std::size_t>(index) >= g_state.devices.size()) {
            g_state.last_error = "Device index is out of range.";
            return int32_t{-4};
        }
        *output = g_state.devices[static_cast<std::size_t>(index)].info;
        return int32_t{0};
    });
}

int32_t htk_coremidi_open_input(int64_t endpoint_id) {
    return Guard([=] {
        std::lock_guard<std::mutex> operation_lock(g_state.operation_mutex);
        CloseInputInternal();
        MIDIClientRef client = 0;
        MIDIEndpointRef endpoint = 0;
        uint64_t input_generation = 0;
        {
            std::lock_guard<std::mutex> lock(g_state.mutex);
            client = g_state.client;
            for (const DeviceRecord& device : g_state.devices)
                if (device.info.endpoint_id == endpoint_id && device.info.is_online != 0) endpoint = device.endpoint;
            if (client == 0) {
                g_state.last_error = "CoreMIDI client has not been created.";
                return int32_t{-2};
            }
            if (endpoint == 0) {
                g_state.last_error = "Selected CoreMIDI endpoint is missing or offline.";
                return int32_t{-5};
            }
            input_generation = NextInputGenerationLocked();
        }

        MIDIPortRef port = 0;
        void* callback_context = reinterpret_cast<void*>(static_cast<std::uintptr_t>(input_generation));
        OSStatus status = MIDIInputPortCreate(client, CFSTR("HitTheKit Input"), ReadCallback, callback_context, &port);
        if (status != noErr) {
            SetStatusError("MIDIInputPortCreate", status);
            return static_cast<int32_t>(status == 0 ? -1 : status);
        }
        status = MIDIPortConnectSource(port, endpoint, nullptr);
        if (status != noErr) {
            MIDIPortDispose(port);
            SetStatusError("MIDIPortConnectSource", status);
            return static_cast<int32_t>(status == 0 ? -1 : status);
        }
        std::lock_guard<std::mutex> lock(g_state.mutex);
        g_state.input_port = port;
        g_state.selected_endpoint = endpoint;
        g_state.selected_endpoint_id = endpoint_id;
        g_state.selected_validation_generation = g_state.device_generation.load(std::memory_order_relaxed);
        g_state.selected_endpoint_present = true;
        g_state.active_input_generation = input_generation;
        g_state.accepting = true;
        g_state.queue.Clear();
        g_state.parser.Reset();
        ClearErrorLocked();
        return int32_t{0};
    });
}

void htk_coremidi_close_input(void) {
    try { std::lock_guard<std::mutex> operation_lock(g_state.operation_mutex); CloseInputInternal(); }
    catch (...) { SetError("Native exception while closing CoreMIDI input."); }
}

int32_t htk_coremidi_poll_messages(HtkMidiMessage* buffer, int32_t capacity) {
    return Guard([=] {
        if (capacity < 0 || (capacity > 0 && buffer == nullptr)) return int32_t{-3};
        return g_state.queue.Pop(buffer, capacity);
    });
}

int32_t htk_coremidi_get_connection_state(void) {
    return Guard([] {
        std::lock_guard<std::mutex> lock(g_state.mutex);
        if (!g_state.accepting || g_state.selected_endpoint == 0) return int32_t{0};
        const uint64_t generation = g_state.device_generation.load(std::memory_order_relaxed);
        if (generation != g_state.selected_validation_generation) {
            g_state.selected_endpoint_present = EndpointIsPresentAndOnline(g_state.selected_endpoint);
            g_state.selected_validation_generation = generation;
        }
        if (!g_state.selected_endpoint_present) return int32_t{0};
        return ReadInteger(g_state.selected_endpoint, kMIDIPropertyOffline, 0) == 0 ? int32_t{1} : int32_t{0};
    });
}

int32_t htk_coremidi_get_diagnostics(HtkCoreMidiDiagnostics* output) {
    return Guard([=] {
        if (output == nullptr) return int32_t{-3};
        std::lock_guard<std::mutex> lock(g_state.mutex);
        output->device_generation = g_state.device_generation.load(std::memory_order_relaxed);
        output->messages_received = g_state.queue.Received();
        output->dropped_messages = g_state.queue.Dropped();
        output->queue_size = static_cast<int32_t>(g_state.queue.Size());
        output->queue_capacity = static_cast<int32_t>(g_state.queue.Capacity());
        output->selected_endpoint_id = g_state.selected_endpoint_id;
        output->client_state = g_state.client == 0 ? 0 : 1;
        output->connection_state = g_state.accepting && g_state.selected_endpoint != 0 ? 1 : 0;
        return int32_t{0};
    });
}

int32_t htk_coremidi_get_last_error(char* buffer, int32_t capacity) {
    return Guard([=] {
        if (buffer == nullptr || capacity <= 0) return int32_t{-3};
        std::lock_guard<std::mutex> lock(g_state.mutex);
        CopyText(buffer, static_cast<std::size_t>(capacity), g_state.last_error);
        return static_cast<int32_t>(g_state.last_error.size());
    });
}

#if defined(HTK_COREMIDI_TESTING)
uint64_t htk_coremidi_test_begin_ingress(void) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    g_state.queue.Clear();
    g_state.parser.Reset();
    g_state.active_input_generation = NextInputGenerationLocked();
    g_state.accepting = true;
    return g_state.active_input_generation;
}

void htk_coremidi_test_end_ingress(void) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    g_state.accepting = false;
    g_state.active_input_generation = 0;
    g_state.queue.Clear();
    g_state.parser.Reset();
}

void htk_coremidi_test_inject_bytes(const uint8_t* bytes, int32_t length, double timestamp) {
    if (length < 0) return;
    uint64_t input_generation = 0;
    {
        std::lock_guard<std::mutex> lock(g_state.mutex);
        input_generation = g_state.active_input_generation;
    }
    ParseCallbackBytes(input_generation, bytes, static_cast<std::size_t>(length), timestamp);
}

void htk_coremidi_test_inject_bytes_for_generation(
    const uint8_t* bytes,
    int32_t length,
    double timestamp,
    uint64_t input_generation) {
    if (length < 0) return;
    ParseCallbackBytes(input_generation, bytes, static_cast<std::size_t>(length), timestamp);
}
#endif

} // extern "C"
