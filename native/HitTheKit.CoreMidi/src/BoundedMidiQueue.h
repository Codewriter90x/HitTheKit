#ifndef HITTHEKIT_BOUNDED_MIDI_QUEUE_H
#define HITTHEKIT_BOUNDED_MIDI_QUEUE_H

#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <deque>
#include <mutex>

#include "HitTheKitCoreMidi.h"

namespace htk {

class BoundedMidiQueue final {
public:
    explicit BoundedMidiQueue(std::size_t capacity) : capacity_(capacity) {}

    bool Push(HtkMidiMessage message) {
        std::lock_guard<std::mutex> lock(mutex_);
        ++received_;
        if (queue_.size() >= capacity_) {
            ++dropped_;
            return false;
        }
        message.sequence = next_sequence_++;
        queue_.push_back(message);
        return true;
    }

    int32_t Pop(HtkMidiMessage* output, int32_t capacity) {
        if (capacity < 0 || (capacity > 0 && output == nullptr)) return -1;
        std::lock_guard<std::mutex> lock(mutex_);
        const int32_t count = std::min(capacity, static_cast<int32_t>(queue_.size()));
        for (int32_t index = 0; index < count; ++index) {
            output[index] = queue_.front();
            queue_.pop_front();
        }
        return count;
    }

    void Clear() {
        std::lock_guard<std::mutex> lock(mutex_);
        queue_.clear();
    }

    std::size_t Size() const {
        std::lock_guard<std::mutex> lock(mutex_);
        return queue_.size();
    }

    std::size_t Capacity() const { return capacity_; }
    uint64_t Received() const { std::lock_guard<std::mutex> lock(mutex_); return received_; }
    uint64_t Dropped() const { std::lock_guard<std::mutex> lock(mutex_); return dropped_; }

private:
    const std::size_t capacity_;
    mutable std::mutex mutex_;
    std::deque<HtkMidiMessage> queue_;
    uint64_t next_sequence_ = 1;
    uint64_t received_ = 0;
    uint64_t dropped_ = 0;
};

} // namespace htk

#endif
