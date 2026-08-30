using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HitTheKit.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Gameplay
{
    /// <summary>
    /// Owns the complete bottom instrument ribbon. The gameplay controller only
    /// supplies pulse state; layout, ordering and presentation stay isolated here.
    /// </summary>
    public sealed class GameplayTargetBar
    {
        private static readonly DrumPad[] OrderedPads = BuildOrderedPads();

        private static readonly float[] WidthWeights =
        {
            1.10f,
            1.15f,
            0.94f,
            0.92f,
            1.48f,
            0.99f,
            1.17f,
            1.08f
        };

        private static readonly IReadOnlyList<DrumPad> ReadOnlyDisplayOrder =
            new ReadOnlyCollection<DrumPad>(OrderedPads);

        private readonly VisualElement host;
        private readonly Dictionary<DrumPad, VisualElement> hitZones =
            new Dictionary<DrumPad, VisualElement>();

        public GameplayTargetBar(VisualElement host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            Build();
        }

        public static IReadOnlyList<DrumPad> DisplayOrder => ReadOnlyDisplayOrder;
        public int Count => hitZones.Count;

        public VisualElement TargetFor(DrumPad pad)
        {
            if (!hitZones.TryGetValue(pad, out VisualElement target))
                throw new ArgumentOutOfRangeException(nameof(pad), pad, "Unknown gameplay target.");
            return target;
        }

        public void UpdatePulseState(IReadOnlyDictionary<DrumPad, float> pulseDeadlines, float now)
        {
            if (pulseDeadlines == null) throw new ArgumentNullException(nameof(pulseDeadlines));
            foreach (KeyValuePair<DrumPad, VisualElement> pair in hitZones)
            {
                bool active = pulseDeadlines.TryGetValue(pair.Key, out float deadline) && deadline > now;
                pair.Value.EnableInClassList("target--active", active);
            }
        }

        private void Build()
        {
            host.Clear();
            host.AddToClassList("gameplay-target-bar");
            host.pickingMode = PickingMode.Ignore;

            for (int index = 0; index < OrderedPads.Length; index++)
            {
                DrumPad pad = OrderedPads[index];
                GameplayLaneDefinition lane = GameplayHighwayLanes.Find(pad);
                var target = new VisualElement
                {
                    name = $"target-{lane.Id}",
                    pickingMode = PickingMode.Ignore,
                    tooltip = lane.Label
                };
                target.AddToClassList("target-bar-hit-zone");
                target.AddToClassList($"lane--{lane.Id}");
                target.style.flexGrow = WidthWeights[index];
                target.style.flexBasis = 0;

                var pulse = new VisualElement
                {
                    name = $"target-pulse-{lane.Id}",
                    pickingMode = PickingMode.Ignore
                };
                pulse.AddToClassList("target-bar-pulse");
                Color pulseColor = lane.Color;
                pulseColor.a = 0.26f;
                pulse.style.backgroundColor = pulseColor;
                target.Add(pulse);

                host.Add(target);
                hitZones.Add(pad, target);
            }
        }

        private static DrumPad[] BuildOrderedPads()
        {
            var pads = new DrumPad[GameplayHighwayLanes.All.Count];
            for (int index = 0; index < pads.Length; index++)
                pads[index] = GameplayHighwayLanes.All[index].Pad;
            return pads;
        }
    }
}
