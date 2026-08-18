using System;
using System.Collections.Generic;
using UnityEngine;

namespace HitTheKit.Unity.Devices
{
    public sealed class DeviceProfileCandidateLoadException : Exception
    {
        public DeviceProfileCandidateLoadException(string message) : base(message) { }
        public DeviceProfileCandidateLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class DeviceProfileCandidate
    {
        private readonly IReadOnlyList<CandidateMappingEvidence> mappings;
        private readonly IReadOnlyList<CandidateMappingEvidence> excludedOrConflictedMappings;
        private readonly IReadOnlyList<string> notes;

        public DeviceProfileCandidate(
            int schemaVersion,
            string profileCandidateId,
            int profileCandidateVersion,
            string manufacturer,
            string deviceDisplayName,
            string sourceCaptureId,
            string captureSha256,
            DeviceProfileLifecycleStatus status,
            bool productionReady,
            bool autoSelectable,
            bool requiresConfirmation,
            IReadOnlyList<CandidateMappingEvidence> mappings,
            IReadOnlyList<CandidateMappingEvidence> excludedOrConflictedMappings,
            IReadOnlyList<string> notes)
        {
            if (schemaVersion != DeviceProfileCandidateLoader.SupportedSchemaVersion) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            KitElement.EnsureStableId(profileCandidateId, nameof(profileCandidateId));
            if (profileCandidateVersion <= 0) throw new ArgumentOutOfRangeException(nameof(profileCandidateVersion));
            if (string.IsNullOrWhiteSpace(deviceDisplayName)) throw new ArgumentException("Device display name is required.", nameof(deviceDisplayName));
            KitElement.EnsureStableId(sourceCaptureId, nameof(sourceCaptureId));
            if (!GuidedCaptureAnalysisInput.IsSha256(captureSha256)) throw new ArgumentException("Capture SHA-256 is invalid.", nameof(captureSha256));
            if (!Enum.IsDefined(typeof(DeviceProfileLifecycleStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (status != DeviceProfileLifecycleStatus.Verified && (productionReady || autoSelectable || !requiresConfirmation))
                throw new ArgumentException("Only a Verified profile may be production-ready, auto-selectable or not require confirmation.");

            SchemaVersion = schemaVersion;
            ProfileCandidateId = profileCandidateId;
            ProfileCandidateVersion = profileCandidateVersion;
            Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer;
            DeviceDisplayName = deviceDisplayName;
            SourceCaptureId = sourceCaptureId;
            CaptureSha256 = captureSha256.ToLowerInvariant();
            Status = status;
            ProductionReady = productionReady;
            AutoSelectable = autoSelectable;
            RequiresConfirmation = requiresConfirmation;
            this.mappings = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(mappings, nameof(mappings)));
            this.excludedOrConflictedMappings = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(excludedOrConflictedMappings, nameof(excludedOrConflictedMappings)));
            this.notes = Array.AsReadOnly(CopyStrings(notes));
        }

        public int SchemaVersion { get; }
        public string ProfileCandidateId { get; }
        public int ProfileCandidateVersion { get; }
        public string Manufacturer { get; }
        public string DeviceDisplayName { get; }
        public string SourceCaptureId { get; }
        public string CaptureSha256 { get; }
        public DeviceProfileLifecycleStatus Status { get; }
        public bool ProductionReady { get; }
        public bool AutoSelectable { get; }
        public bool RequiresConfirmation { get; }
        public IReadOnlyList<CandidateMappingEvidence> Mappings => mappings;
        public IReadOnlyList<CandidateMappingEvidence> ExcludedOrConflictedMappings => excludedOrConflictedMappings;
        public IReadOnlyList<string> Notes => notes;
        public bool CanEnterBuiltInLibrary =>
            Status == DeviceProfileLifecycleStatus.Verified && ProductionReady && AutoSelectable && !RequiresConfirmation;

        private static string[] CopyStrings(IReadOnlyList<string> values)
        {
            if (values == null) return Array.Empty<string>();
            var result = new string[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index] ?? string.Empty;
            return result;
        }
    }

    public sealed class DeviceProfileCandidateLoader
    {
        public const int SupportedSchemaVersion = 1;

        public DeviceProfileCandidate Load(string json)
        {
            CandidateProfileDto dto;
            if (string.IsNullOrWhiteSpace(json)) throw new DeviceProfileCandidateLoadException("Candidate profile JSON is required.");
            try
            {
                dto = JsonUtility.FromJson<CandidateProfileDto>(json);
            }
            catch (Exception exception)
            {
                throw new DeviceProfileCandidateLoadException("Candidate profile JSON is malformed.", exception);
            }
            if (dto == null) throw new DeviceProfileCandidateLoadException("Candidate profile JSON did not contain an object.");

            try
            {
                if (dto.schemaVersion != SupportedSchemaVersion) throw new ArgumentException("schemaVersion must be 1.");
                if (dto.profileCandidateVersion <= 0) throw new ArgumentException("profileCandidateVersion must be positive.");
                CandidateMappingEvidence[] mappings = ReadMappings(dto.mappings ?? Array.Empty<CandidateMappingDto>());
                CandidateMappingEvidence[] excluded = ReadMappings(dto.excludedOrConflictedMappings ?? Array.Empty<CandidateMappingDto>());
                return new DeviceProfileCandidate(
                    dto.schemaVersion,
                    dto.profileCandidateId,
                    dto.profileCandidateVersion,
                    dto.manufacturer,
                    dto.deviceDisplayName,
                    dto.sourceCaptureId,
                    dto.captureSha256,
                    ParseStatus(dto.status),
                    dto.productionReady,
                    dto.autoSelectable,
                    dto.requiresConfirmation,
                    mappings,
                    excluded,
                    dto.notes ?? Array.Empty<string>());
            }
            catch (Exception exception) when (!(exception is DeviceProfileCandidateLoadException))
            {
                throw new DeviceProfileCandidateLoadException($"Candidate profile validation failed: {exception.Message}", exception);
            }
        }

        private static CandidateMappingEvidence[] ReadMappings(CandidateMappingDto[] dtos)
        {
            var result = new CandidateMappingEvidence[dtos.Length];
            for (int index = 0; index < dtos.Length; index++)
            {
                CandidateMappingDto dto = dtos[index] ?? throw new ArgumentException($"Mapping at index {index} is null.");
                CandidateTrigger trigger = string.IsNullOrWhiteSpace(dto.triggerKind)
                    ? null
                    : new CandidateTrigger(
                        dto.triggerKind,
                        dto.channel == int.MinValue ? (int?)null : dto.channel,
                        dto.data1 == int.MinValue ? (int?)null : dto.data1,
                        dto.minimumValue,
                        dto.maximumValue);
                result[index] = new CandidateMappingEvidence(
                    dto.stepId,
                    dto.targetElementId,
                    dto.articulation,
                    dto.role,
                    trigger,
                    ParseConfidence(dto.confidence),
                    dto.evidence ?? Array.Empty<string>(),
                    dto.warnings ?? Array.Empty<string>(),
                    dto.requiresRecapture);
            }
            return result;
        }

        private static DeviceProfileLifecycleStatus ParseStatus(string value)
        {
            switch (value)
            {
                case "exploratory": return DeviceProfileLifecycleStatus.Exploratory;
                case "candidate": return DeviceProfileLifecycleStatus.Candidate;
                case "verified": return DeviceProfileLifecycleStatus.Verified;
                case "deprecated": return DeviceProfileLifecycleStatus.Deprecated;
                default: throw new ArgumentException($"Unknown profile status '{value}'.");
            }
        }

        internal static EvidenceConfidence ParseConfidence(string value)
        {
            switch (value)
            {
                case "high": return EvidenceConfidence.High;
                case "medium": return EvidenceConfidence.Medium;
                case "low": return EvidenceConfidence.Low;
                case "insufficient": return EvidenceConfidence.Insufficient;
                case "conflicted": return EvidenceConfidence.Conflicted;
                default: throw new ArgumentException($"Unknown confidence '{value}'.");
            }
        }
    }

    [Serializable]
    internal sealed class CandidateProfileDto
    {
        public int schemaVersion = int.MinValue;
        public string profileCandidateId;
        public int profileCandidateVersion = int.MinValue;
        public string manufacturer;
        public string deviceDisplayName;
        public string sourceCaptureId;
        public string captureSha256;
        public string status;
        public bool productionReady;
        public bool autoSelectable;
        public bool requiresConfirmation;
        public CandidateMappingDto[] mappings;
        public CandidateMappingDto[] excludedOrConflictedMappings;
        public string[] notes;
    }

    [Serializable]
    internal sealed class CandidateMappingDto
    {
        public string stepId;
        public string targetElementId;
        public string articulation;
        public string role;
        public string triggerKind;
        public int channel = int.MinValue;
        public int data1 = int.MinValue;
        public int minimumValue = int.MinValue;
        public int maximumValue = int.MinValue;
        public string confidence;
        public string[] evidence;
        public string[] warnings;
        public bool requiresRecapture;
    }
}
