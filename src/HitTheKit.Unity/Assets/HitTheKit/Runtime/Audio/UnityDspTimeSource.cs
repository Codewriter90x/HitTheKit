using UnityEngine;

namespace HitTheKit.Unity.Audio
{
    public sealed class UnityDspTimeSource : IDspTimeSource
    {
        public double Now => AudioSettings.dspTime;
    }
}
