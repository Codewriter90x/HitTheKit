#ifndef HITTHEKIT_COREMIDI_H
#define HITTHEKIT_COREMIDI_H

#include <stdint.h>

#if defined(__cplusplus)
extern "C" {
#endif

#define HTK_COREMIDI_API_VERSION 2
#define HTK_COREMIDI_TEXT_CAPACITY 256
#if defined(__GNUC__)
#define HTK_COREMIDI_EXPORT __attribute__((visibility("default")))
#else
#define HTK_COREMIDI_EXPORT
#endif

typedef enum HtkMidiMessageKind {
    HTK_MIDI_NOTE_ON = 0,
    HTK_MIDI_NOTE_OFF = 1,
    HTK_MIDI_CONTROL_CHANGE = 2,
    HTK_MIDI_POLY_AFTERTOUCH = 3,
    HTK_MIDI_CHANNEL_AFTERTOUCH = 4,
    HTK_MIDI_PITCH_BEND = 5,
    HTK_MIDI_PROGRAM_CHANGE = 6
} HtkMidiMessageKind;

typedef struct HtkMidiDeviceInfo {
    int32_t index;
    int64_t endpoint_id;
    char name[HTK_COREMIDI_TEXT_CAPACITY];
    char device_name[HTK_COREMIDI_TEXT_CAPACITY];
    char manufacturer[HTK_COREMIDI_TEXT_CAPACITY];
    char model[HTK_COREMIDI_TEXT_CAPACITY];
    char entity_name[HTK_COREMIDI_TEXT_CAPACITY];
    int32_t protocol;
    int32_t is_online;
} HtkMidiDeviceInfo;

typedef struct HtkMidiMessage {
    uint64_t sequence;
    double monotonic_seconds;
    int32_t message_kind;
    int32_t channel;
    int32_t data1;
    int32_t data2;
} HtkMidiMessage;

typedef struct HtkCoreMidiDiagnostics {
    uint64_t device_generation;
    uint64_t messages_received;
    uint64_t dropped_messages;
    int32_t queue_size;
    int32_t queue_capacity;
    int64_t selected_endpoint_id;
    int32_t client_state;
    int32_t connection_state;
} HtkCoreMidiDiagnostics;

HTK_COREMIDI_EXPORT int32_t htk_coremidi_get_api_version(void);
HTK_COREMIDI_EXPORT double htk_coremidi_get_monotonic_seconds(void);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_create_client(void);
HTK_COREMIDI_EXPORT void htk_coremidi_destroy_client(void);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_refresh_devices(void);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_get_device_count(void);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_get_device_info(int32_t index, HtkMidiDeviceInfo* output);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_open_input(int64_t endpoint_id);
HTK_COREMIDI_EXPORT void htk_coremidi_close_input(void);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_poll_messages(HtkMidiMessage* buffer, int32_t capacity);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_get_connection_state(void);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_get_diagnostics(HtkCoreMidiDiagnostics* output);
HTK_COREMIDI_EXPORT int32_t htk_coremidi_get_last_error(char* buffer, int32_t capacity);

#if defined(HTK_COREMIDI_TESTING)
HTK_COREMIDI_EXPORT uint64_t htk_coremidi_test_begin_ingress(void);
HTK_COREMIDI_EXPORT void htk_coremidi_test_end_ingress(void);
HTK_COREMIDI_EXPORT void htk_coremidi_test_inject_bytes(const uint8_t* bytes, int32_t length, double timestamp);
HTK_COREMIDI_EXPORT void htk_coremidi_test_inject_bytes_for_generation(
    const uint8_t* bytes,
    int32_t length,
    double timestamp,
    uint64_t input_generation);
#endif

#if defined(__cplusplus)
}
#endif

#endif
