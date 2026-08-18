using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HitTheKit.Unity.Devices
{
    public sealed class GuidedCaptureSchemaLoadException : Exception
    {
        public GuidedCaptureSchemaLoadException(string message) : base(message) { }
        public GuidedCaptureSchemaLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class GuidedCaptureSchemaV1Loader
    {
        public const int SupportedSchemaVersion = 1;

        public GuidedCaptureAnalysisInput Load(
            string sessionJson,
            string eventsJsonl,
            string captureId,
            string captureSha256,
            string observedManufacturer = null)
        {
            CaptureSessionDto session = Parse<CaptureSessionDto>(sessionJson, "session");
            var eventDtos = new List<CaptureEventDto>();
            if (eventsJsonl == null) throw new GuidedCaptureSchemaLoadException("events.jsonl is required.");
            if (eventsJsonl.Length > 0 && !eventsJsonl.EndsWith("\n", StringComparison.Ordinal))
                throw new GuidedCaptureSchemaLoadException("events.jsonl must end with a newline; the final record may be truncated.");
            using (var reader = new StringReader(eventsJsonl))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line)) throw new GuidedCaptureSchemaLoadException($"events.jsonl line {lineNumber} is empty.");
                    eventDtos.Add(Parse<CaptureEventDto>(line, $"events.jsonl line {lineNumber}"));
                }
            }
            return Build(session, eventDtos.ToArray(), captureId, captureSha256, observedManufacturer);
        }

        public GuidedCaptureAnalysisInput LoadSyntheticFixture(string json)
        {
            CaptureAnalysisFixtureDto fixture = Parse<CaptureAnalysisFixtureDto>(json, "synthetic fixture");
            if (!fixture.isSynthetic) throw new GuidedCaptureSchemaLoadException("Analysis fixture must be explicitly marked synthetic.");
            if (fixture.session == null) throw new GuidedCaptureSchemaLoadException("Synthetic fixture session is required.");
            if (fixture.events == null) throw new GuidedCaptureSchemaLoadException("Synthetic fixture events are required.");
            return Build(
                fixture.session,
                fixture.events,
                fixture.captureId,
                fixture.captureSha256,
                fixture.observedManufacturer);
        }

        private static GuidedCaptureAnalysisInput Build(
            CaptureSessionDto session,
            CaptureEventDto[] eventDtos,
            string captureId,
            string captureSha256,
            string observedManufacturer)
        {
            try
            {
                RequireVersion(session.schemaVersion, "session");
                if (!string.Equals(session.captureMode, "guided-capture", StringComparison.Ordinal) &&
                    !string.Equals(session.captureMode, "synthetic-guided-capture", StringComparison.Ordinal))
                    throw new ArgumentException("captureMode must be guided-capture or synthetic-guided-capture.");
                if (string.IsNullOrWhiteSpace(session.deviceDisplayName)) throw new ArgumentException("deviceDisplayName is required.");
                if (session.stepDefinitions == null) throw new ArgumentException("stepDefinitions is required.");
                if (session.steps == null) throw new ArgumentException("steps is required.");
                if (session.eventCount == long.MinValue || session.eventCount != eventDtos.Length)
                    throw new ArgumentException($"session eventCount {session.eventCount} does not match {eventDtos.Length} events.");

                var definitions = new CaptureStepDefinitionData[session.stepDefinitions.Length];
                var knownStepIds = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < definitions.Length; index++)
                {
                    CaptureStepDefinitionDto dto = session.stepDefinitions[index] ?? throw new ArgumentException($"Step definition {index} is null.");
                    if (!knownStepIds.Add(dto.id)) throw new ArgumentException($"Duplicate step definition '{dto.id}'.");
                    definitions[index] = new CaptureStepDefinitionData(dto.id, dto.displayName, dto.optional, dto.targetSamples);
                }

                var states = new CaptureStepStateData[session.steps.Length];
                var stateIds = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < states.Length; index++)
                {
                    CaptureStepStateDto dto = session.steps[index] ?? throw new ArgumentException($"Step state {index} is null.");
                    if (!knownStepIds.Contains(dto.id)) throw new ArgumentException($"Step state '{dto.id}' has no definition.");
                    if (!stateIds.Add(dto.id)) throw new ArgumentException($"Duplicate step state '{dto.id}'.");
                    states[index] = new CaptureStepStateData(dto.id, dto.completed, dto.skipped, dto.eventCount);
                }
                if (stateIds.Count != knownStepIds.Count)
                    throw new ArgumentException("Every step definition must have exactly one step state.");

                var events = new CaptureEventData[eventDtos.Length];
                long expectedSequence = 1;
                double previousElapsed = 0;
                var countsByStep = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int index = 0; index < eventDtos.Length; index++)
                {
                    CaptureEventDto dto = eventDtos[index] ?? throw new ArgumentException($"Event {index} is null.");
                    RequireVersion(dto.schemaVersion, $"event {index}");
                    if (dto.sequence != expectedSequence) throw new ArgumentException($"Expected sequence {expectedSequence}, got {dto.sequence}.");
                    if (double.IsNaN(dto.elapsedSeconds) || double.IsInfinity(dto.elapsedSeconds) || dto.elapsedSeconds < previousElapsed)
                        throw new ArgumentException($"Event {dto.sequence} has a non-monotonic timestamp.");
                    if (!knownStepIds.Contains(dto.stepId)) throw new ArgumentException($"Event {dto.sequence} uses unknown step '{dto.stepId}'.");
                    events[index] = new CaptureEventData(
                        dto.sequence,
                        dto.elapsedSeconds,
                        dto.stepId,
                        dto.rawKind,
                        Optional(dto.channel),
                        Optional(dto.data1),
                        Optional(dto.data2),
                        dto.isNoteOffEquivalent,
                        Optional(dto.stepAttempt));
                    countsByStep[dto.stepId] = countsByStep.TryGetValue(dto.stepId, out int count) ? count + 1 : 1;
                    expectedSequence++;
                    previousElapsed = dto.elapsedSeconds;
                }

                for (int index = 0; index < states.Length; index++)
                {
                    int observed = countsByStep.TryGetValue(states[index].Id, out int count) ? count : 0;
                    if (states[index].EventCount != observed)
                        throw new ArgumentException($"Step '{states[index].Id}' declares {states[index].EventCount} events but contains {observed}.");
                }

                return new GuidedCaptureAnalysisInput(
                    captureId,
                    captureSha256,
                    session.deviceDisplayName,
                    observedManufacturer,
                    definitions,
                    states,
                    events);
            }
            catch (Exception exception) when (!(exception is GuidedCaptureSchemaLoadException))
            {
                throw new GuidedCaptureSchemaLoadException($"Capture schema validation failed: {exception.Message}", exception);
            }
        }

        private static T Parse<T>(string json, string label) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) throw new GuidedCaptureSchemaLoadException($"{label} JSON is required.");
            try
            {
                T value = JsonUtility.FromJson<T>(json);
                return value ?? throw new GuidedCaptureSchemaLoadException($"{label} JSON did not contain an object.");
            }
            catch (GuidedCaptureSchemaLoadException) { throw; }
            catch (Exception exception)
            {
                throw new GuidedCaptureSchemaLoadException($"{label} JSON is malformed.", exception);
            }
        }

        private static void RequireVersion(int version, string label)
        {
            if (version != SupportedSchemaVersion) throw new ArgumentException($"{label} schemaVersion must be 1.");
        }

        private static int? Optional(int value) => value == int.MinValue ? (int?)null : value;
    }

    [Serializable]
    internal sealed class CaptureAnalysisFixtureDto
    {
        public bool isSynthetic;
        public string captureId;
        public string captureSha256;
        public string observedManufacturer;
        public CaptureSessionDto session;
        public CaptureEventDto[] events;
    }

    [Serializable]
    internal sealed class CaptureSessionDto
    {
        public int schemaVersion = int.MinValue;
        public string deviceDisplayName;
        public string captureMode;
        public CaptureStepDefinitionDto[] stepDefinitions;
        public CaptureStepStateDto[] steps;
        public long eventCount = long.MinValue;
    }

    [Serializable]
    internal sealed class CaptureStepDefinitionDto
    {
        public string id;
        public string displayName;
        public bool optional;
        public int targetSamples = int.MinValue;
    }

    [Serializable]
    internal sealed class CaptureStepStateDto
    {
        public string id;
        public bool completed;
        public bool skipped;
        public int eventCount = int.MinValue;
    }

    [Serializable]
    internal sealed class CaptureEventDto
    {
        public int schemaVersion = int.MinValue;
        public long sequence = long.MinValue;
        public double elapsedSeconds = double.NaN;
        public string stepId;
        public string rawKind;
        public int channel = int.MinValue;
        public int data1 = int.MinValue;
        public int data2 = int.MinValue;
        public bool isNoteOffEquivalent;
        public int stepAttempt = int.MinValue;
    }
}
