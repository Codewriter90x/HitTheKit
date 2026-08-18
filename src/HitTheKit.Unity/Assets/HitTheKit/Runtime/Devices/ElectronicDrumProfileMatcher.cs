using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Devices
{
    public enum ElectronicDrumProfileMatchKind
    {
        Exact,
        Probable,
        GenericFallback,
        NoMatch,
        Ambiguous
    }

    public sealed class ElectronicDrumProfileCandidate
    {
        private readonly IReadOnlyList<string> reasons;

        internal ElectronicDrumProfileCandidate(ElectronicDrumProfile profile, int confidence, string[] reasons)
        {
            Profile = profile;
            Confidence = confidence;
            this.reasons = Array.AsReadOnly(reasons);
        }

        public ElectronicDrumProfile Profile { get; }
        public int Confidence { get; }
        public IReadOnlyList<string> Reasons => reasons;
    }

    public sealed class ElectronicDrumProfileMatchResult
    {
        private readonly IReadOnlyList<ElectronicDrumProfileCandidate> candidates;

        internal ElectronicDrumProfileMatchResult(
            ElectronicDrumProfileMatchKind kind,
            ElectronicDrumProfileCandidate[] candidates)
        {
            Kind = kind;
            this.candidates = Array.AsReadOnly(candidates);
        }

        public ElectronicDrumProfileMatchKind Kind { get; }
        public IReadOnlyList<ElectronicDrumProfileCandidate> Candidates => candidates;
        public ElectronicDrumProfileCandidate SelectedCandidate =>
            Kind == ElectronicDrumProfileMatchKind.Ambiguous || candidates.Count == 0 ? null : candidates[0];
    }

    public sealed class ElectronicDrumProfileMatcher
    {
        public ElectronicDrumProfileMatchResult Match(
            MidiDeviceIdentity identity,
            IReadOnlyList<ElectronicDrumProfile> profiles)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));

            var matches = new List<ElectronicDrumProfileCandidate>();
            ElectronicDrumProfile generic = null;
            for (int index = 0; index < profiles.Count; index++)
            {
                ElectronicDrumProfile profile = profiles[index] ??
                    throw new ArgumentException($"Profile at index {index} is null.", nameof(profiles));
                if (string.Equals(profile.ProfileId, "generic-gm-drums-v1", StringComparison.Ordinal))
                {
                    generic = profile;
                    continue;
                }

                var reasons = new List<string>();
                int confidence = Score(identity, profile, reasons);
                if (confidence > 0)
                {
                    matches.Add(new ElectronicDrumProfileCandidate(profile, confidence, reasons.ToArray()));
                }
            }

            matches.Sort((left, right) =>
            {
                int byConfidence = right.Confidence.CompareTo(left.Confidence);
                return byConfidence != 0
                    ? byConfidence
                    : string.CompareOrdinal(left.Profile.ProfileId, right.Profile.ProfileId);
            });

            if (matches.Count > 1 && matches[0].Confidence == matches[1].Confidence)
            {
                return new ElectronicDrumProfileMatchResult(
                    ElectronicDrumProfileMatchKind.Ambiguous,
                    matches.ToArray());
            }
            if (matches.Count > 0)
            {
                ElectronicDrumProfileMatchKind kind = matches[0].Confidence >= 100
                    ? ElectronicDrumProfileMatchKind.Exact
                    : ElectronicDrumProfileMatchKind.Probable;
                return new ElectronicDrumProfileMatchResult(kind, matches.ToArray());
            }
            if (generic != null)
            {
                return new ElectronicDrumProfileMatchResult(
                    ElectronicDrumProfileMatchKind.GenericFallback,
                    new[] { new ElectronicDrumProfileCandidate(generic, 1, new[] { "generic fallback; user confirmation required" }) });
            }
            return new ElectronicDrumProfileMatchResult(
                ElectronicDrumProfileMatchKind.NoMatch,
                Array.Empty<ElectronicDrumProfileCandidate>());
        }

        private static int Score(
            MidiDeviceIdentity identity,
            ElectronicDrumProfile profile,
            ICollection<string> reasons)
        {
            int score = 0;
            if (HasText(identity.VendorId) && HasText(identity.ProductId) &&
                EqualsIgnoreCase(identity.VendorId, profile.VendorId) &&
                EqualsIgnoreCase(identity.ProductId, profile.ProductId))
            {
                score = 110;
                reasons.Add("vendor/product ID match");
            }
            if (HasText(identity.Manufacturer) && HasText(identity.Model) &&
                EqualsIgnoreCase(identity.Manufacturer, profile.Manufacturer) &&
                EqualsIgnoreCase(identity.Model, profile.Model))
            {
                score = Math.Max(score, 100);
                reasons.Add("manufacturer/model match");
            }
            for (int index = 0; index < profile.Aliases.Count; index++)
            {
                if (EqualsIgnoreCase(identity.PortName, profile.Aliases[index]))
                {
                    score = Math.Max(score, 80);
                    reasons.Add("exact alias match");
                    break;
                }
            }
            for (int index = 0; index < profile.PortNamePatterns.Count; index++)
            {
                if (identity.PortName.IndexOf(profile.PortNamePatterns[index], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = Math.Max(score, 60);
                    reasons.Add("port-name pattern match");
                    break;
                }
            }
            return score;
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return HasText(left) && HasText(right) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasText(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }

    public sealed class ElectronicDrumProfileLibrary
    {
        private readonly IReadOnlyList<ElectronicDrumProfile> profiles;
        private readonly Dictionary<string, ElectronicDrumProfile> byId;
        private readonly ElectronicDrumProfileMatcher matcher;

        public ElectronicDrumProfileLibrary(
            IReadOnlyList<ElectronicDrumProfile> profiles,
            ElectronicDrumProfileMatcher matcher = null)
        {
            ElectronicDrumProfile[] copy = ElectronicDrumProfile.CopyReferences(profiles, nameof(profiles));
            byId = new Dictionary<string, ElectronicDrumProfile>(StringComparer.Ordinal);
            foreach (ElectronicDrumProfile profile in copy)
            {
                if (!byId.TryAdd(profile.ProfileId, profile))
                {
                    throw new ArgumentException($"Duplicate profile ID '{profile.ProfileId}'.", nameof(profiles));
                }
            }
            this.profiles = Array.AsReadOnly(copy);
            this.matcher = matcher ?? new ElectronicDrumProfileMatcher();
        }

        public IReadOnlyList<ElectronicDrumProfile> Profiles => profiles;

        public bool TryGet(string profileId, out ElectronicDrumProfile profile)
        {
            if (profileId == null)
            {
                profile = null;
                return false;
            }
            return byId.TryGetValue(profileId, out profile);
        }

        public ElectronicDrumProfileMatchResult Match(MidiDeviceIdentity identity)
        {
            return matcher.Match(identity, profiles);
        }
    }
}
