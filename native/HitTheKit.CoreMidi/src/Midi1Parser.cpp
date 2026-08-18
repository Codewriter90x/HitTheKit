#include "Midi1Parser.h"

namespace htk {

void Midi1Parser::Parse(const uint8_t* bytes, std::size_t length, const Sink& sink) {
    if (bytes == nullptr || !sink) return;

    for (std::size_t index = 0; index < length; ++index) {
        const uint8_t byte = bytes[index];
        if (byte >= 0xF8) continue; // Realtime bytes do not disturb running status.

        if ((byte & 0x80U) != 0) {
            if (byte == 0xF0) {
                in_sysex_ = true;
                running_status_ = 0;
            } else if (byte == 0xF7) {
                in_sysex_ = false;
                running_status_ = 0;
            } else if (byte >= 0xF0) {
                in_sysex_ = false;
                running_status_ = 0;
            } else {
                in_sysex_ = false;
                running_status_ = byte;
                data_count_ = 0;
                data_needed_ = DataLength(byte);
                if (data_needed_ == 0) running_status_ = 0;
            }
            continue;
        }

        if (in_sysex_ || running_status_ == 0 || byte > 0x7F) continue;
        data_[data_count_++] = byte;
        if (data_count_ == data_needed_) {
            Emit(sink);
            data_count_ = 0;
        }
    }
}

void Midi1Parser::Reset() {
    running_status_ = 0;
    data_count_ = 0;
    data_needed_ = 0;
    in_sysex_ = false;
}

int Midi1Parser::DataLength(uint8_t status) {
    switch ((status >> 4U) & 0x0FU) {
        case 0x8: case 0x9: case 0xA: case 0xB: case 0xE: return 2;
        case 0xC: case 0xD: return 1;
        default: return 0;
    }
}

int Midi1Parser::MessageKind(uint8_t status) {
    switch ((status >> 4U) & 0x0FU) {
        case 0x8: return HTK_MIDI_NOTE_OFF;
        case 0x9: return HTK_MIDI_NOTE_ON;
        case 0xA: return HTK_MIDI_POLY_AFTERTOUCH;
        case 0xB: return HTK_MIDI_CONTROL_CHANGE;
        case 0xC: return HTK_MIDI_PROGRAM_CHANGE;
        case 0xD: return HTK_MIDI_CHANNEL_AFTERTOUCH;
        case 0xE: return HTK_MIDI_PITCH_BEND;
        default: return -1;
    }
}

void Midi1Parser::Emit(const Sink& sink) {
    const int kind = MessageKind(running_status_);
    if (kind < 0) return;
    const int channel = running_status_ & 0x0F;
    const int data1 = kind == HTK_MIDI_CHANNEL_AFTERTOUCH ? -1 : data_[0];
    int data2 = 0;
    if (kind == HTK_MIDI_CHANNEL_AFTERTOUCH) {
        data2 = data_[0];
    } else if (kind == HTK_MIDI_PROGRAM_CHANGE) {
        data2 = 0;
    } else if (kind == HTK_MIDI_PITCH_BEND) {
        data2 = static_cast<int>(data_[0]) | (static_cast<int>(data_[1]) << 7);
    } else {
        data2 = data_[1];
    }
    sink(kind, channel, data1, data2);
}

} // namespace htk
