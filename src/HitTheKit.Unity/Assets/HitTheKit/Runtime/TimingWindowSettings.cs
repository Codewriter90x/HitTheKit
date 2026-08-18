using System;
using HitTheKit.Core;
using UnityEngine;

namespace HitTheKit.Unity
{
    [Serializable]
    public sealed class TimingWindowSettings
    {
        [SerializeField] private float perfectMilliseconds = 40f;
        [SerializeField] private float goodMilliseconds = 90f;
        [SerializeField] private float hitMilliseconds = 150f;

        public TimingWindows ToCore()
        {
            const double millisecondsPerSecond = 1000.0;
            return new TimingWindows(
                perfectMilliseconds / millisecondsPerSecond,
                goodMilliseconds / millisecondsPerSecond,
                hitMilliseconds / millisecondsPerSecond);
        }
    }
}
