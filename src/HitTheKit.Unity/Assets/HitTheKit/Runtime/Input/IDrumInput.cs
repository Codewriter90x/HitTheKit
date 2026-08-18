using System;

namespace HitTheKit.Unity.Input
{
    public interface IDrumInput
    {
        event Action<DrumInputEvent> HitReceived;
    }
}
