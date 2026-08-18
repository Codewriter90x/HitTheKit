#include "BoundedMidiQueue.h"
#include "HitTheKitCoreMidi.h"
#include "Midi1Parser.h"

#include <cassert>
#include <atomic>
#include <cstdlib>
#include <cstring>
#include <cstdint>
#include <iostream>
#include <thread>
#include <vector>

namespace {

struct Parsed {
    int kind;
    int channel;
    int data1;
    int data2;
};

std::vector<Parsed> Parse(htk::Midi1Parser& parser, const std::vector<uint8_t>& bytes) {
    std::vector<Parsed> result;
    parser.Parse(bytes.data(), bytes.size(), [&](int kind, int channel, int data1, int data2) {
        result.push_back({kind, channel, data1, data2});
    });
    return result;
}

void ParserTests() {
    htk::Midi1Parser parser;
    auto values = Parse(parser, {0x99, 36, 104, 38, 0, 0x89, 36, 64});
    assert(values.size() == 3);
    assert(values[0].kind == HTK_MIDI_NOTE_ON && values[0].channel == 9 && values[0].data1 == 36 && values[0].data2 == 104);
    assert(values[1].kind == HTK_MIDI_NOTE_ON && values[1].data2 == 0);
    assert(values[2].kind == HTK_MIDI_NOTE_OFF);

    parser.Reset();
    values = Parse(parser, {0xB2, 4, 78, 0xA2, 59, 127, 0xD2, 63, 0xC2, 8, 0xE2, 0, 64});
    assert(values.size() == 5);
    assert(values[0].kind == HTK_MIDI_CONTROL_CHANGE && values[0].data1 == 4 && values[0].data2 == 78);
    assert(values[1].kind == HTK_MIDI_POLY_AFTERTOUCH && values[1].data1 == 59);
    assert(values[2].kind == HTK_MIDI_CHANNEL_AFTERTOUCH && values[2].data1 == -1 && values[2].data2 == 63);
    assert(values[3].kind == HTK_MIDI_PROGRAM_CHANGE && values[3].data1 == 8 && values[3].data2 == 0);
    assert(values[4].kind == HTK_MIDI_PITCH_BEND && values[4].data2 == 8192);

    parser.Reset();
    values = Parse(parser, {0x99, 40});
    assert(values.empty());
    values = Parse(parser, {100, 41, 101});
    assert(values.size() == 2 && values[0].data1 == 40 && values[1].data1 == 41);

    parser.Reset();
    values = Parse(parser, {0x99, 36, 0xF8, 100, 38, 101, 0xF0, 1, 2, 0xF7, 40, 102});
    assert(values.size() == 2);
    values = Parse(parser, {0xF4, 36, 100, 0xFF});
    assert(values.empty());
}

void QueueTests() {
    htk::BoundedMidiQueue queue(2);
    HtkMidiMessage input{};
    input.data1 = 36;
    assert(queue.Push(input));
    input.data1 = 38;
    assert(queue.Push(input));
    input.data1 = 40;
    assert(!queue.Push(input));
    assert(queue.Received() == 3 && queue.Dropped() == 1 && queue.Size() == 2);
    HtkMidiMessage output[2]{};
    assert(queue.Pop(output, 2) == 2);
    assert(output[0].sequence == 1 && output[1].sequence == 2);
    assert(output[0].data1 == 36 && output[1].data1 == 38);
    queue.Clear();
    assert(queue.Size() == 0);

    htk::BoundedMidiQueue concurrent(10000);
    std::vector<std::thread> producers;
    for (int producer = 0; producer < 4; ++producer) {
        producers.emplace_back([&concurrent, producer] {
            for (int index = 0; index < 2500; ++index) {
                HtkMidiMessage message{};
                message.data1 = producer;
                assert(concurrent.Push(message));
            }
        });
    }
    for (std::thread& producer : producers) producer.join();
    std::vector<HtkMidiMessage> drained(10000);
    assert(concurrent.Pop(drained.data(), static_cast<int32_t>(drained.size())) == 10000);
    for (std::size_t index = 0; index < drained.size(); ++index)
        assert(drained[index].sequence == index + 1);
}

void AbiTests() {
    assert(htk_coremidi_get_api_version() == HTK_COREMIDI_API_VERSION);
    assert(htk_coremidi_get_monotonic_seconds() > 0.0);
    assert(htk_coremidi_refresh_devices() < 0);
    assert(htk_coremidi_open_input(INT64_C(0x7FFFFFFFFFFFFFFF)) != 0);
    htk_coremidi_close_input();
    htk_coremidi_destroy_client();
}

void LiveLifecycleTests() {
    assert(htk_coremidi_create_client() == 0);
    assert(htk_coremidi_create_client() == 0);
    assert(htk_coremidi_refresh_devices() >= 0);
    const int32_t device_count = htk_coremidi_get_device_count();
    assert(device_count >= 0);
    std::cout << "CoreMIDI inputs visible during smoke: " << device_count << "\n";
    for (int32_t index = 0; index < device_count; ++index) {
        HtkMidiDeviceInfo info{};
        assert(htk_coremidi_get_device_info(index, &info) == 0);
        std::cout << "  [" << index << "] " << info.name << " / device " << info.device_name
                  << " / " << info.manufacturer
                  << " / endpoint " << info.endpoint_id << " / online " << info.is_online << "\n";
    }
    assert(htk_coremidi_open_input(INT64_C(0x7FFFFFFFFFFFFFFF)) != 0);
    htk_coremidi_close_input();
    htk_coremidi_close_input();
    htk_coremidi_destroy_client();
    htk_coremidi_destroy_client();
}

void StopBoundaryTests() {
    const uint8_t kick[] = {0x99, 36, 100};
    const uint64_t first_generation = htk_coremidi_test_begin_ingress();
    htk_coremidi_test_inject_bytes(kick, 3, 5.0);
    HtkMidiMessage output{};
    assert(htk_coremidi_poll_messages(&output, 1) == 1);
    assert(output.message_kind == HTK_MIDI_NOTE_ON && output.data1 == 36);
    htk_coremidi_test_end_ingress();
    htk_coremidi_test_inject_bytes(kick, 3, 6.0);
    assert(htk_coremidi_poll_messages(&output, 1) == 0);

    const uint64_t second_generation = htk_coremidi_test_begin_ingress();
    assert(second_generation != first_generation);
    htk_coremidi_test_inject_bytes_for_generation(kick, 3, 6.5, first_generation);
    assert(htk_coremidi_poll_messages(&output, 1) == 0);
    htk_coremidi_test_inject_bytes_for_generation(kick, 3, 6.6, second_generation);
    assert(htk_coremidi_poll_messages(&output, 1) == 1);
    htk_coremidi_test_end_ingress();

    std::atomic<bool> start{false};
    htk_coremidi_test_begin_ingress();
    std::thread callback([&] {
        while (!start.load()) { }
        for (int index = 0; index < 1000; ++index)
            htk_coremidi_test_inject_bytes(kick, 3, 7.0);
    });
    start.store(true);
    htk_coremidi_test_end_ingress();
    callback.join();
    assert(htk_coremidi_poll_messages(&output, 1) == 0);
}

} // namespace

int main() {
    ParserTests();
    QueueTests();
    AbiTests();
    const char* live_tests = std::getenv("HTK_COREMIDI_RUN_LIVE_TESTS");
    if (live_tests != nullptr && std::strcmp(live_tests, "1") == 0) LiveLifecycleTests();
    StopBoundaryTests();
    std::cout << "HitTheKit CoreMIDI native tests passed.\n";
    return 0;
}
