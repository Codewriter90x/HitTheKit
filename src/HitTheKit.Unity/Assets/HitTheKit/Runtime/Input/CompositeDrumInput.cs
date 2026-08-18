using System;
using UnityEngine;

namespace HitTheKit.Unity.Input
{
    public sealed class CompositeDrumInput : MonoBehaviour, IDrumInput
    {
        [SerializeField] private MonoBehaviour keyboardSource;
        [SerializeField] private MonoBehaviour midiSource;
        private IDrumInput subscribedKeyboard;
        private IDrumInput subscribedMidi;

        public event Action<DrumInputEvent> HitReceived;
        public IDrumInput KeyboardSource => keyboardSource as IDrumInput;
        public IDrumInput MidiSource => midiSource as IDrumInput;

        private void OnEnable()
        {
            Subscribe(KeyboardSource, ref subscribedKeyboard);
            Subscribe(MidiSource, ref subscribedMidi);
        }

        private void OnDisable()
        {
            Unsubscribe(ref subscribedKeyboard);
            Unsubscribe(ref subscribedMidi);
        }

        private void Subscribe(IDrumInput source, ref IDrumInput slot)
        {
            if (source == null || ReferenceEquals(source, slot)) return;
            Unsubscribe(ref slot);
            source.HitReceived += Relay;
            slot = source;
        }

        private void Unsubscribe(ref IDrumInput source)
        {
            if (source == null) return;
            source.HitReceived -= Relay;
            source = null;
        }

        private void Relay(DrumInputEvent input) => HitReceived?.Invoke(input);
    }
}
