using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Devices
{
    public sealed class ElectronicDrumProfile
    {
        private readonly IReadOnlyList<string> aliases;
        private readonly IReadOnlyList<string> portNamePatterns;
        private readonly IReadOnlyList<KitElement> elements;
        private readonly IReadOnlyList<MidiMappingEntry> defaultMappings;

        public ElectronicDrumProfile(
            int schemaVersion,
            string profileId,
            int profileVersion,
            string manufacturer,
            string model,
            string displayName,
            IReadOnlyList<string> aliases,
            IReadOnlyList<string> portNamePatterns,
            IReadOnlyList<KitElement> elements,
            IReadOnlyList<MidiMappingEntry> defaultMappings,
            ElectronicDrumCapability capabilities,
            string notes = null,
            string vendorId = null,
            string productId = null)
        {
            if (schemaVersion != ElectronicDrumProfileLoader.SupportedSchemaVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            }
            KitElement.EnsureStableId(profileId, nameof(profileId));
            if (profileVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(profileVersion));
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name must not be empty.", nameof(displayName));
            }
            if ((capabilities & ~AllCapabilities) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capabilities));
            }

            string[] aliasArray = CopyStrings(aliases, nameof(aliases));
            string[] patternArray = CopyStrings(portNamePatterns, nameof(portNamePatterns));
            KitElement[] elementArray = CopyReferences(elements, nameof(elements));
            MidiMappingEntry[] mappingArray = CopyReferences(defaultMappings, nameof(defaultMappings));
            ValidateElementsAndMappings(elementArray, mappingArray, true);

            SchemaVersion = schemaVersion;
            ProfileId = profileId;
            ProfileVersion = profileVersion;
            Manufacturer = EmptyToNull(manufacturer);
            Model = EmptyToNull(model);
            DisplayName = displayName;
            this.aliases = Array.AsReadOnly(aliasArray);
            this.portNamePatterns = Array.AsReadOnly(patternArray);
            this.elements = Array.AsReadOnly(elementArray);
            this.defaultMappings = Array.AsReadOnly(mappingArray);
            Capabilities = capabilities;
            Notes = notes;
            VendorId = EmptyToNull(vendorId);
            ProductId = EmptyToNull(productId);
        }

        public int SchemaVersion { get; }
        public string ProfileId { get; }
        public int ProfileVersion { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> Aliases => aliases;
        public IReadOnlyList<string> PortNamePatterns => portNamePatterns;
        public IReadOnlyList<KitElement> Elements => elements;
        public IReadOnlyList<MidiMappingEntry> DefaultMappings => defaultMappings;
        public ElectronicDrumCapability Capabilities { get; }
        public string Notes { get; }
        public string VendorId { get; }
        public string ProductId { get; }

        internal static void ValidateElementsAndMappings(
            IReadOnlyList<KitElement> elements,
            IReadOnlyList<MidiMappingEntry> mappings,
            bool rejectDuplicateTriggerDefinitions)
        {
            var elementIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < elements.Count; index++)
            {
                KitElement element = elements[index] ??
                    throw new ArgumentException($"Element at index {index} is null.", nameof(elements));
                if (!elementIds.Add(element.Id))
                {
                    throw new ArgumentException($"Duplicate element ID '{element.Id}'.", nameof(elements));
                }
            }

            var mappingIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < mappings.Count; index++)
            {
                MidiMappingEntry mapping = mappings[index] ??
                    throw new ArgumentException($"Mapping at index {index} is null.", nameof(mappings));
                if (!mappingIds.Add(mapping.Id))
                {
                    throw new ArgumentException($"Duplicate mapping ID '{mapping.Id}'.", nameof(mappings));
                }
                if (!elementIds.Contains(mapping.ElementId))
                {
                    throw new ArgumentException(
                        $"Mapping '{mapping.Id}' refers to missing element '{mapping.ElementId}'.",
                        nameof(mappings));
                }

                for (int previous = 0; rejectDuplicateTriggerDefinitions && previous < index; previous++)
                {
                    if (mappings[previous].Trigger.HasSameDefinition(mapping.Trigger))
                    {
                        throw new ArgumentException(
                            $"Mapping '{mapping.Id}' duplicates trigger '{mappings[previous].Id}'.",
                            nameof(mappings));
                    }
                }
            }
        }

        private static string[] CopyStrings(IReadOnlyList<string> source, string parameterName)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            var result = new string[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(source[index]))
                {
                    throw new ArgumentException($"Entry at index {index} is empty.", parameterName);
                }
                result[index] = source[index];
            }
            return result;
        }

        internal static T[] CopyReferences<T>(IReadOnlyList<T> source, string parameterName) where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var result = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index] ??
                    throw new ArgumentException($"Entry at index {index} is null.", parameterName);
            }
            return result;
        }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private const ElectronicDrumCapability AllCapabilities =
            ElectronicDrumCapability.MultiZoneSnare |
            ElectronicDrumCapability.MultiZoneToms |
            ElectronicDrumCapability.HiHatContinuousController |
            ElectronicDrumCapability.HiHatPedalNote |
            ElectronicDrumCapability.CrashChoke |
            ElectronicDrumCapability.RideBell |
            ElectronicDrumCapability.RideEdge;
    }

    public sealed class UserKitConfiguration
    {
        private readonly IReadOnlyList<KitElement> elements;
        private readonly IReadOnlyList<MidiMappingEntry> mappings;
        private readonly IReadOnlyList<string> disabledElementIds;
        private readonly IReadOnlyList<KitMappingReviewIssue> reviewIssues;

        public UserKitConfiguration(
            int schemaVersion,
            string configurationId,
            string displayName,
            string baseProfileId,
            int? baseProfileVersion,
            MidiDeviceIdentity midiDeviceIdentity,
            IReadOnlyList<KitElement> elements,
            IReadOnlyList<MidiMappingEntry> mappings,
            IReadOnlyList<string> disabledElementIds,
            string userNotes = null,
            bool isComplete = true,
            IReadOnlyList<KitMappingReviewIssue> reviewIssues = null)
        {
            if (schemaVersion != UserKitConfigurationLoader.SupportedSchemaVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            }
            KitElement.EnsureStableId(configurationId, nameof(configurationId));
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name must not be empty.", nameof(displayName));
            }
            if (baseProfileVersion.HasValue && baseProfileVersion.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseProfileVersion));
            }
            if (!string.IsNullOrWhiteSpace(baseProfileId))
            {
                KitElement.EnsureStableId(baseProfileId, nameof(baseProfileId));
            }

            KitElement[] elementArray = ElectronicDrumProfile.CopyReferences(elements, nameof(elements));
            MidiMappingEntry[] mappingArray = ElectronicDrumProfile.CopyReferences(mappings, nameof(mappings));
            ElectronicDrumProfile.ValidateElementsAndMappings(elementArray, mappingArray, false);
            string[] disabledArray = CopyDisabledIds(disabledElementIds);
            KitMappingReviewIssue[] issueArray = reviewIssues == null
                ? Array.Empty<KitMappingReviewIssue>()
                : ElectronicDrumProfile.CopyReferences(reviewIssues, nameof(reviewIssues));

            if (UserKitConfigurationInvariantValidator.TryGetViolation(
                    elementArray,
                    mappingArray,
                    issueArray,
                    disabledArray,
                    isComplete,
                    out string violation))
            {
                throw new ArgumentException(violation);
            }

            bool hasBaseProfileId = !string.IsNullOrWhiteSpace(baseProfileId);
            if (hasBaseProfileId != baseProfileVersion.HasValue)
            {
                throw new ArgumentException("Base profile ID and version must either both be present or both be absent.");
            }
            SchemaVersion = schemaVersion;
            ConfigurationId = configurationId;
            DisplayName = displayName;
            BaseProfileId = hasBaseProfileId ? baseProfileId : null;
            BaseProfileVersion = baseProfileVersion;
            MidiDeviceIdentity = midiDeviceIdentity;
            this.elements = Array.AsReadOnly(elementArray);
            this.mappings = Array.AsReadOnly(mappingArray);
            this.disabledElementIds = Array.AsReadOnly(disabledArray);
            this.reviewIssues = Array.AsReadOnly(issueArray);
            UserNotes = userNotes;
            IsComplete = isComplete;
        }

        public int SchemaVersion { get; }
        public string ConfigurationId { get; }
        public string DisplayName { get; }
        public string BaseProfileId { get; }
        public int? BaseProfileVersion { get; }
        public MidiDeviceIdentity MidiDeviceIdentity { get; }
        public IReadOnlyList<KitElement> Elements => elements;
        public IReadOnlyList<MidiMappingEntry> Mappings => mappings;
        public IReadOnlyList<string> DisabledElementIds => disabledElementIds;
        public IReadOnlyList<KitMappingReviewIssue> ReviewIssues => reviewIssues;
        public string UserNotes { get; }
        public bool IsComplete { get; }

        public static UserKitConfiguration FromProfile(
            ElectronicDrumProfile profile,
            string configurationId,
            string displayName,
            MidiDeviceIdentity identity = null)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                configurationId,
                displayName,
                profile.ProfileId,
                profile.ProfileVersion,
                identity,
                profile.Elements,
                profile.DefaultMappings,
                Array.Empty<string>());
        }

        public bool IsElementDisabled(string elementId)
        {
            for (int index = 0; index < disabledElementIds.Count; index++)
            {
                if (string.Equals(disabledElementIds[index], elementId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static string[] CopyDisabledIds(IReadOnlyList<string> disabledIds)
        {
            if (disabledIds == null)
            {
                return Array.Empty<string>();
            }

            var result = new string[disabledIds.Count];
            for (int index = 0; index < disabledIds.Count; index++)
            {
                result[index] = disabledIds[index];
            }
            return result;
        }
    }

    internal static class UserKitConfigurationInvariantValidator
    {
        internal static bool TryGetViolation(
            IReadOnlyList<KitElement> elements,
            IReadOnlyList<MidiMappingEntry> mappings,
            IReadOnlyList<KitMappingReviewIssue> reviewIssues,
            IReadOnlyList<string> disabledElementIds,
            bool isComplete,
            out string violation)
        {
            if (disabledElementIds == null)
            {
                violation = "Disabled element IDs must not be null.";
                return true;
            }

            var elementsById = new Dictionary<string, KitElement>(StringComparer.Ordinal);
            for (int index = 0; index < elements.Count; index++)
            {
                elementsById.Add(elements[index].Id, elements[index]);
            }

            var issueKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < reviewIssues.Count; index++)
            {
                KitMappingReviewIssue issue = reviewIssues[index];
                if (issue == null)
                {
                    violation = $"Review issue at index {index} is null.";
                    return true;
                }
                if (!elementsById.ContainsKey(issue.ElementId))
                {
                    violation = $"Review issue at index {index} targets missing element '{issue.ElementId}'.";
                    return true;
                }

                string key = issue.ElementId + "\n" + issue.Kind;
                if (!issueKeys.Add(key))
                {
                    violation = $"Review issue at index {index} duplicates '{issue.Kind}' for '{issue.ElementId}'.";
                    return true;
                }
            }

            var disabledIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < disabledElementIds.Count; index++)
            {
                string elementId = disabledElementIds[index];
                if (string.IsNullOrWhiteSpace(elementId))
                {
                    violation = $"Disabled element ID at index {index} is empty.";
                    return true;
                }
                if (!elementsById.TryGetValue(elementId, out KitElement element))
                {
                    violation = $"Disabled element ID '{elementId}' at index {index} does not identify an element.";
                    return true;
                }
                if (!disabledIds.Add(elementId))
                {
                    violation = $"Disabled element ID '{elementId}' at index {index} is duplicated.";
                    return true;
                }
                if (!element.IsOptional)
                {
                    violation =
                        $"Required element '{element.Id}' cannot be disabled in a {(isComplete ? "Complete" : "Draft")} configuration.";
                    return true;
                }
            }

            if (!isComplete)
            {
                violation = null;
                return false;
            }

            for (int index = 0; index < mappings.Count; index++)
            {
                if (mappings[index].VerificationState != MidiMappingVerificationState.Confirmed)
                {
                    violation = $"Complete configuration mapping '{mappings[index].Id}' is not confirmed.";
                    return true;
                }
            }

            for (int index = 0; index < reviewIssues.Count; index++)
            {
                if (reviewIssues[index].BlocksCompletion)
                {
                    violation =
                        $"Complete configuration element '{reviewIssues[index].ElementId}' has a blocking review issue.";
                    return true;
                }
            }

            for (int elementIndex = 0; elementIndex < elements.Count; elementIndex++)
            {
                KitElement element = elements[elementIndex];
                if (element.IsOptional) continue;

                bool hasActiveConfirmedMapping = false;
                for (int mappingIndex = 0; mappingIndex < mappings.Count; mappingIndex++)
                {
                    MidiMappingEntry mapping = mappings[mappingIndex];
                    if (mapping.Enabled &&
                        mapping.VerificationState == MidiMappingVerificationState.Confirmed &&
                        string.Equals(mapping.ElementId, element.Id, StringComparison.Ordinal))
                    {
                        hasActiveConfirmedMapping = true;
                        break;
                    }
                }

                if (!hasActiveConfirmedMapping)
                {
                    violation =
                        $"Complete configuration required element '{element.DisplayName}' ('{element.Id}') has no enabled confirmed mapping.";
                    return true;
                }
            }

            violation = null;
            return false;
        }
    }
}
