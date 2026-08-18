using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Devices
{
    public enum EvidenceConfidence
    {
        High,
        Medium,
        Low,
        Insufficient,
        Conflicted
    }

    public enum DeviceProfileLifecycleStatus
    {
        Exploratory,
        Candidate,
        Verified,
        Deprecated
    }

    public sealed class CaptureStepDefinitionData
    {
        public CaptureStepDefinitionData(string id, string displayName, bool optional, int targetSamples)
        {
            KitElement.EnsureStableId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            if (targetSamples <= 0) throw new ArgumentOutOfRangeException(nameof(targetSamples));
            Id = id;
            DisplayName = displayName;
            Optional = optional;
            TargetSamples = targetSamples;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public bool Optional { get; }
        public int TargetSamples { get; }
    }

    public sealed class CaptureStepStateData
    {
        public CaptureStepStateData(string id, bool completed, bool skipped, int eventCount)
        {
            KitElement.EnsureStableId(id, nameof(id));
            if (completed && skipped) throw new ArgumentException("A step cannot be completed and skipped.");
            if (eventCount < 0) throw new ArgumentOutOfRangeException(nameof(eventCount));
            Id = id;
            Completed = completed;
            Skipped = skipped;
            EventCount = eventCount;
        }

        public string Id { get; }
        public bool Completed { get; }
        public bool Skipped { get; }
        public int EventCount { get; }
    }

    public sealed class CaptureEventData
    {
        public CaptureEventData(
            long sequence,
            double elapsedSeconds,
            string stepId,
            string rawKind,
            int? channel,
            int? data1,
            int? data2,
            bool noteOffEquivalent,
            int? stepAttempt)
        {
            if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            if (!IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (string.IsNullOrWhiteSpace(stepId)) throw new ArgumentException("Step ID is required.", nameof(stepId));
            if (string.IsNullOrWhiteSpace(rawKind)) throw new ArgumentException("Raw kind is required.", nameof(rawKind));
            if (channel.HasValue && (channel.Value < 0 || channel.Value > 15)) throw new ArgumentOutOfRangeException(nameof(channel));
            if (data1.HasValue && (data1.Value < 0 || data1.Value > 127)) throw new ArgumentOutOfRangeException(nameof(data1));
            int maximumData2 = string.Equals(rawKind, "pitchBend", StringComparison.Ordinal) ? 16383 : 127;
            if (data2.HasValue && (data2.Value < 0 || data2.Value > maximumData2)) throw new ArgumentOutOfRangeException(nameof(data2));
            if (stepAttempt.HasValue && stepAttempt.Value <= 0) throw new ArgumentOutOfRangeException(nameof(stepAttempt));
            Sequence = sequence;
            ElapsedSeconds = elapsedSeconds;
            StepId = stepId;
            RawKind = rawKind;
            Channel = channel;
            Data1 = data1;
            Data2 = data2;
            IsNoteOffEquivalent = noteOffEquivalent;
            StepAttempt = stepAttempt;
        }

        public long Sequence { get; }
        public double ElapsedSeconds { get; }
        public string StepId { get; }
        public string RawKind { get; }
        public int? Channel { get; }
        public int? Data1 { get; }
        public int? Data2 { get; }
        public bool IsNoteOffEquivalent { get; }
        public int? StepAttempt { get; }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class GuidedCaptureAnalysisInput
    {
        private readonly IReadOnlyList<CaptureStepDefinitionData> stepDefinitions;
        private readonly IReadOnlyList<CaptureStepStateData> steps;
        private readonly IReadOnlyList<CaptureEventData> events;

        public GuidedCaptureAnalysisInput(
            string captureId,
            string captureSha256,
            string deviceDisplayName,
            string observedManufacturer,
            IReadOnlyList<CaptureStepDefinitionData> stepDefinitions,
            IReadOnlyList<CaptureStepStateData> steps,
            IReadOnlyList<CaptureEventData> events)
        {
            KitElement.EnsureStableId(captureId, nameof(captureId));
            if (!IsSha256(captureSha256)) throw new ArgumentException("Capture SHA-256 must contain 64 hexadecimal characters.", nameof(captureSha256));
            if (string.IsNullOrWhiteSpace(deviceDisplayName)) throw new ArgumentException("Device display name is required.", nameof(deviceDisplayName));
            CaptureStepDefinitionData[] definitionCopy = Copy(stepDefinitions, nameof(stepDefinitions));
            CaptureStepStateData[] stateCopy = Copy(steps, nameof(steps));
            CaptureEventData[] eventCopy = Copy(events, nameof(events));
            CaptureId = captureId;
            CaptureSha256 = captureSha256.ToLowerInvariant();
            DeviceDisplayName = deviceDisplayName;
            ObservedManufacturer = string.IsNullOrWhiteSpace(observedManufacturer) ? null : observedManufacturer;
            this.stepDefinitions = Array.AsReadOnly(definitionCopy);
            this.steps = Array.AsReadOnly(stateCopy);
            this.events = Array.AsReadOnly(eventCopy);
        }

        public string CaptureId { get; }
        public string CaptureSha256 { get; }
        public string DeviceDisplayName { get; }
        public string ObservedManufacturer { get; }
        public IReadOnlyList<CaptureStepDefinitionData> StepDefinitions => stepDefinitions;
        public IReadOnlyList<CaptureStepStateData> Steps => steps;
        public IReadOnlyList<CaptureEventData> Events => events;

        internal static T[] Copy<T>(IReadOnlyList<T> source, string name) where T : class
        {
            if (source == null) throw new ArgumentNullException(name);
            var result = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
                result[index] = source[index] ?? throw new ArgumentException($"Entry at index {index} is null.", name);
            return result;
        }

        internal static bool IsSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
            for (int index = 0; index < value.Length; index++)
                if (!Uri.IsHexDigit(value[index])) return false;
            return true;
        }
    }

    public sealed class NoteEvidence
    {
        private readonly IReadOnlyList<int> attempts;

        public NoteEvidence(int note, int count, IReadOnlyList<int> attempts)
        {
            if (note < 0 || note > 127) throw new ArgumentOutOfRangeException(nameof(note));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            Note = note;
            Count = count;
            this.attempts = Array.AsReadOnly(CopyValues(attempts));
        }

        public int Note { get; }
        public int Count { get; }
        public IReadOnlyList<int> Attempts => attempts;

        private static int[] CopyValues(IReadOnlyList<int> values)
        {
            if (values == null) return Array.Empty<int>();
            var result = new int[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index];
            return result;
        }
    }

    public sealed class MessageEvidence
    {
        public MessageEvidence(string kind, int? data1, int count, int minimumValue, int maximumValue)
        {
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Kind is required.", nameof(kind));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            Kind = kind;
            Data1 = data1;
            Count = count;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
        }

        public string Kind { get; }
        public int? Data1 { get; }
        public int Count { get; }
        public int MinimumValue { get; }
        public int MaximumValue { get; }
    }

    public sealed class CaptureStepObservation
    {
        private readonly IReadOnlyList<NoteEvidence> notes;
        private readonly IReadOnlyList<MessageEvidence> controlChanges;
        private readonly IReadOnlyList<MessageEvidence> aftertouch;
        private readonly IReadOnlyList<int> channels;

        public CaptureStepObservation(
            string stepId,
            bool completed,
            bool skipped,
            int eventCount,
            int positiveNoteOnCount,
            int noteOffCount,
            int attemptCount,
            IReadOnlyList<NoteEvidence> notes,
            IReadOnlyList<MessageEvidence> controlChanges,
            IReadOnlyList<MessageEvidence> aftertouch,
            IReadOnlyList<int> channels,
            int? velocityMinimum,
            int? velocityMaximum,
            double? velocityMedian,
            bool possibleContamination,
            EvidenceConfidence confidence,
            string recommendation)
        {
            KitElement.EnsureStableId(stepId, nameof(stepId));
            StepId = stepId;
            Completed = completed;
            Skipped = skipped;
            EventCount = eventCount;
            PositiveNoteOnCount = positiveNoteOnCount;
            NoteOffCount = noteOffCount;
            AttemptCount = attemptCount;
            this.notes = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(notes, nameof(notes)));
            this.controlChanges = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(controlChanges, nameof(controlChanges)));
            this.aftertouch = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(aftertouch, nameof(aftertouch)));
            this.channels = Array.AsReadOnly(CopyChannels(channels));
            VelocityMinimum = velocityMinimum;
            VelocityMaximum = velocityMaximum;
            VelocityMedian = velocityMedian;
            PossibleContamination = possibleContamination;
            Confidence = confidence;
            Recommendation = recommendation;
        }

        public string StepId { get; }
        public bool Completed { get; }
        public bool Skipped { get; }
        public int EventCount { get; }
        public int PositiveNoteOnCount { get; }
        public int NoteOffCount { get; }
        public int AttemptCount { get; }
        public IReadOnlyList<NoteEvidence> Notes => notes;
        public IReadOnlyList<MessageEvidence> ControlChanges => controlChanges;
        public IReadOnlyList<MessageEvidence> Aftertouch => aftertouch;
        public IReadOnlyList<int> Channels => channels;
        public int? VelocityMinimum { get; }
        public int? VelocityMaximum { get; }
        public double? VelocityMedian { get; }
        public bool PossibleContamination { get; }
        public EvidenceConfidence Confidence { get; }
        public string Recommendation { get; }

        private static int[] CopyChannels(IReadOnlyList<int> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var result = new int[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index];
            return result;
        }
    }

    public sealed class CandidateTrigger
    {
        public CandidateTrigger(string kind, int? channel, int? data1, int minimumValue, int maximumValue)
        {
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Kind is required.", nameof(kind));
            if (channel.HasValue && (channel.Value < 0 || channel.Value > 15)) throw new ArgumentOutOfRangeException(nameof(channel));
            if (data1.HasValue && (data1.Value < 0 || data1.Value > 127)) throw new ArgumentOutOfRangeException(nameof(data1));
            int maximumAllowed = string.Equals(kind, "pitchBend", StringComparison.Ordinal) ? 16383 : 127;
            if (minimumValue < 0 || minimumValue > maximumAllowed) throw new ArgumentOutOfRangeException(nameof(minimumValue));
            if (maximumValue < minimumValue || maximumValue > maximumAllowed) throw new ArgumentOutOfRangeException(nameof(maximumValue));
            Kind = kind;
            Channel = channel;
            Data1 = data1;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
        }

        public string Kind { get; }
        public int? Channel { get; }
        public int? Data1 { get; }
        public int MinimumValue { get; }
        public int MaximumValue { get; }
    }

    public sealed class CandidateMappingEvidence
    {
        private readonly IReadOnlyList<string> evidence;
        private readonly IReadOnlyList<string> warnings;

        public CandidateMappingEvidence(
            string stepId,
            string targetElementId,
            string articulation,
            string role,
            CandidateTrigger trigger,
            EvidenceConfidence confidence,
            IReadOnlyList<string> evidence,
            IReadOnlyList<string> warnings,
            bool requiresRecapture)
        {
            KitElement.EnsureStableId(stepId, nameof(stepId));
            KitElement.EnsureStableId(targetElementId, nameof(targetElementId));
            StepId = stepId;
            TargetElementId = targetElementId;
            Articulation = articulation ?? "unresolved";
            Role = role ?? "strike";
            Trigger = trigger;
            Confidence = confidence;
            this.evidence = Array.AsReadOnly(CopyStrings(evidence));
            this.warnings = Array.AsReadOnly(CopyStrings(warnings));
            RequiresRecapture = requiresRecapture;
        }

        public string StepId { get; }
        public string TargetElementId { get; }
        public string Articulation { get; }
        public string Role { get; }
        public CandidateTrigger Trigger { get; }
        public EvidenceConfidence Confidence { get; }
        public IReadOnlyList<string> Evidence => evidence;
        public IReadOnlyList<string> Warnings => warnings;
        public bool RequiresRecapture { get; }

        private static string[] CopyStrings(IReadOnlyList<string> values)
        {
            if (values == null) return Array.Empty<string>();
            var result = new string[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index] ?? string.Empty;
            return result;
        }
    }

    public sealed class CaptureAnomaly
    {
        public CaptureAnomaly(string code, string stepId, string description)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
            Code = code;
            StepId = stepId;
            Description = description ?? string.Empty;
        }

        public string Code { get; }
        public string StepId { get; }
        public string Description { get; }
    }

    public sealed class CaptureAnalysisResult
    {
        private readonly IReadOnlyList<CaptureStepObservation> observations;
        private readonly IReadOnlyList<CandidateMappingEvidence> candidateMappings;
        private readonly IReadOnlyList<CaptureAnomaly> anomalies;
        private readonly IReadOnlyList<string> recommendedRecaptureSteps;

        public CaptureAnalysisResult(
            GuidedCaptureAnalysisInput source,
            IReadOnlyList<CaptureStepObservation> observations,
            IReadOnlyList<CandidateMappingEvidence> candidateMappings,
            IReadOnlyList<CaptureAnomaly> anomalies,
            IReadOnlyList<string> recommendedRecaptureSteps,
            DeviceProfileCandidate profileCandidate)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            this.observations = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(observations, nameof(observations)));
            this.candidateMappings = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(candidateMappings, nameof(candidateMappings)));
            this.anomalies = Array.AsReadOnly(GuidedCaptureAnalysisInput.Copy(anomalies, nameof(anomalies)));
            this.recommendedRecaptureSteps = Array.AsReadOnly(CopyStrings(recommendedRecaptureSteps));
            ProfileCandidate = profileCandidate;
        }

        public GuidedCaptureAnalysisInput Source { get; }
        public int EventCount => Source.Events.Count;
        public IReadOnlyList<CaptureStepObservation> Observations => observations;
        public IReadOnlyList<CandidateMappingEvidence> CandidateMappings => candidateMappings;
        public IReadOnlyList<CaptureAnomaly> Anomalies => anomalies;
        public IReadOnlyList<string> RecommendedRecaptureSteps => recommendedRecaptureSteps;
        public DeviceProfileCandidate ProfileCandidate { get; }

        private static string[] CopyStrings(IReadOnlyList<string> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var result = new string[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index];
            return result;
        }
    }
}
