#ifndef HITTHEKIT_MIDI1_PARSER_H
#define HITTHEKIT_MIDI1_PARSER_H

#include <cstddef>
#include <cstdint>
#include <functional>

#include "HitTheKitCoreMidi.h"

namespace htk {

class Midi1Parser final {
public:
    using Sink = std::function<void(int32_t kind, int32_t channel, int32_t data1, int32_t data2)>;

    void Parse(const uint8_t* bytes, std::size_t length, const Sink& sink);
    void Reset();

private:
    static int DataLength(uint8_t status);
    static int MessageKind(uint8_t status);
    void Emit(const Sink& sink);

    uint8_t running_status_ = 0;
    uint8_t data_[2] = {0, 0};
    int data_count_ = 0;
    int data_needed_ = 0;
    bool in_sysex_ = false;
};

} // namespace htk

#endif
