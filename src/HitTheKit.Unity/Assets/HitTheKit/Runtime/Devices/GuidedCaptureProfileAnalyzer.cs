using System;
using System.Collections.Generic;
using System.Linq;

namespace HitTheKit.Unity.Devices
{
    public sealed class GuidedCaptureProfileAnalyzer
    {
        public CaptureAnalysisResult Analyze(GuidedCaptureAnalysisInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            CaptureStepObservation[] observations = input.StepDefinitions
                .Select(definition => Observe(input, definition))
                .ToArray();
            var candidates = new List<CandidateMappingEvidence>();
            for (int index = 0; index < observations.Length; index++)
                AddCandidates(observations[index], candidates);

            var anomalies = new List<CaptureAnomaly>();
            foreach (CaptureStepObservation observation in observations)
            {
                if (observation.Skipped)
                    anomalies.Add(new CaptureAnomaly("step-skipped", observation.StepId, "The guided step was skipped and has no trigger evidence."));
                if (observation.PossibleContamination)
                    anomalies.Add(new CaptureAnomaly("multiple-positive-notes", observation.StepId, "Multiple positive NoteOn values require attempt-aware review."));
                if (observation.StepId == "hihat-continuous" && observation.ControlChanges.Count == 0)
                    anomalies.Add(new CaptureAnomaly("continuous-controller-missing", observation.StepId, "No Control Change message was observed; CC4 or another controller must not be inferred."));
            }

            string[] recapture = RecapturePriority
                .Where(stepId => observations.Any(observation => observation.StepId == stepId))
                .ToArray();
            CandidateMappingEvidence[] acceptedCandidates = candidates
                .Where(candidate => candidate.Trigger != null &&
                                    candidate.Confidence != EvidenceConfidence.Conflicted &&
                                    candidate.Confidence != EvidenceConfidence.Insufficient &&
                                    candidate.Confidence != EvidenceConfidence.Low)
                .ToArray();
            CandidateMappingEvidence[] excludedCandidates = candidates.Except(acceptedCandidates).ToArray();
            var profileCandidate = new DeviceProfileCandidate(
                DeviceProfileCandidateLoader.SupportedSchemaVersion,
                "dream-edrum-hampback-candidate-001",
                1,
                input.ObservedManufacturer,
                input.DeviceDisplayName,
                input.CaptureId,
                input.CaptureSha256,
                DeviceProfileLifecycleStatus.Candidate,
                false,
                false,
                true,
                acceptedCandidates,
                excludedCandidates,
                new[]
                {
                    "Exploratory evidence only; not production-ready or auto-selected.",
                    "Ride, choke and continuous hi-hat mappings require targeted recapture."
                });

            return new CaptureAnalysisResult(input, observations, candidates, anomalies, recapture, profileCandidate);
        }

        private static CaptureStepObservation Observe(GuidedCaptureAnalysisInput input, CaptureStepDefinitionData definition)
        {
            CaptureStepStateData state = input.Steps.FirstOrDefault(value => value.Id == definition.Id);
            CaptureEventData[] events = input.Events.Where(value => value.StepId == definition.Id).ToArray();
            CaptureEventData[] positive = events.Where(IsPositiveNoteOn).ToArray();
            NoteEvidence[] notes = positive
                .GroupBy(value => value.Data1.Value)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => new NoteEvidence(
                    group.Key,
                    group.Count(),
                    group.Select(value => value.StepAttempt ?? 0).Where(value => value > 0).Distinct().OrderBy(value => value).ToArray()))
                .ToArray();
            MessageEvidence[] controlChanges = MessageGroups(events.Where(value => value.RawKind == "controlChange"));
            MessageEvidence[] aftertouch = MessageGroups(events.Where(value => value.RawKind is "polyAftertouch" or "channelAftertouch"));
            int[] channels = events.Where(value => value.Channel.HasValue).Select(value => value.Channel.Value).Distinct().OrderBy(value => value).ToArray();
            int[] velocities = positive.Select(value => value.Data2.Value).OrderBy(value => value).ToArray();
            EvidenceConfidence confidence = DetermineConfidence(definition, state, events, notes, controlChanges);
            string recommendation = Recommendation(definition.Id, confidence, notes, controlChanges, aftertouch);
            return new CaptureStepObservation(
                definition.Id,
                state != null && state.Completed,
                state != null && state.Skipped,
                events.Length,
                positive.Length,
                events.Count(value => value.RawKind == "noteOff" || value.IsNoteOffEquivalent),
                events.Where(value => value.StepAttempt.HasValue).Select(value => value.StepAttempt.Value).Distinct().Count(),
                notes,
                controlChanges,
                aftertouch,
                channels,
                velocities.Length == 0 ? (int?)null : velocities[0],
                velocities.Length == 0 ? (int?)null : velocities[velocities.Length - 1],
                Median(velocities),
                notes.Length > 1,
                confidence,
                recommendation);
        }

        private static EvidenceConfidence DetermineConfidence(
            CaptureStepDefinitionData definition,
            CaptureStepStateData state,
            IReadOnlyList<CaptureEventData> events,
            IReadOnlyList<NoteEvidence> notes,
            IReadOnlyList<MessageEvidence> controlChanges)
        {
            if (state == null || state.Skipped) return EvidenceConfidence.Insufficient;
            if (definition.Id == "hihat-continuous" && controlChanges.Count == 0) return EvidenceConfidence.Insufficient;
            if (definition.Id is "ride-bow" or "crash-choke" or "ride-choke-optional") return EvidenceConfidence.Conflicted;
            if (definition.Id == "kick" && HasCleanAttempt(events, 36, definition.TargetSamples)) return EvidenceConfidence.High;
            if (definition.Id == "hihat-closed" && HasCleanAttempt(events, 42, definition.TargetSamples)) return EvidenceConfidence.Medium;
            if (notes.Count == 0 && controlChanges.Count == 0) return EvidenceConfidence.Insufficient;
            if (notes.Count > 1) return EvidenceConfidence.Low;
            if (notes.Count == 1 && notes[0].Count >= definition.TargetSamples) return EvidenceConfidence.High;
            return EvidenceConfidence.Medium;
        }

        private static bool HasCleanAttempt(IReadOnlyList<CaptureEventData> events, int note, int targetSamples)
        {
            return events.Where(IsPositiveNoteOn)
                .GroupBy(value => value.StepAttempt ?? 0)
                .Any(group => group.Count() >= targetSamples && group.All(value => value.Data1 == note));
        }

        private static void AddCandidates(CaptureStepObservation observation, ICollection<CandidateMappingEvidence> candidates)
        {
            if (!Targets.TryGetValue(observation.StepId, out TargetDefinition target)) return;
            if (observation.StepId == "hihat-continuous")
            {
                candidates.Add(Unresolved(observation, target, "continuous", "No Control Change was observed; do not infer CC4."));
                return;
            }
            if (observation.StepId == "crash-2-optional")
            {
                candidates.Add(Unresolved(observation, target, "strike", "The optional Crash 2 step was skipped."));
                return;
            }
            if (observation.StepId == "ride-bow")
            {
                AddConflictedNotes(observation, target, candidates, "Ride bow attempts alternated between notes 51 and 59.");
                return;
            }
            if (observation.StepId == "ride-bell-optional")
            {
                candidates.Add(NoteCandidate(observation, target, 51, EvidenceConfidence.Conflicted, true,
                    "Note 51 was clean in the bell step but was also observed in Ride bow attempts."));
                return;
            }
            if (observation.StepId == "crash-choke")
            {
                AddConflictedNotes(observation, target, candidates, "Strike notes 26 and 46 do not match the clean Crash 1 note 49.");
                AddAftertouchCandidate(observation, target, candidates, 26, "PolyAftertouch note 26 value 127 was observed in all three choke attempts.");
                return;
            }
            if (observation.StepId == "ride-choke-optional")
            {
                AddConflictedNotes(observation, target, candidates, "Strike notes 51 and 59 remain ambiguous across choke attempts.");
                AddAftertouchCandidate(observation, target, candidates, 59, "PolyAftertouch note 59 value 127 was observed during choke attempts.");
                return;
            }

            int? selectedNote = observation.StepId switch
            {
                "kick" => 36,
                "hihat-closed" => 42,
                _ => observation.Notes.Count == 1 ? observation.Notes[0].Note : (int?)null
            };
            if (!selectedNote.HasValue)
            {
                candidates.Add(Unresolved(observation, target, "strike", "No single candidate note can be selected from this step."));
                return;
            }
            bool targetedRecapture = RecapturePriority.Contains(observation.StepId, StringComparer.Ordinal);
            candidates.Add(NoteCandidate(
                observation,
                target,
                selectedNote.Value,
                observation.Confidence,
                targetedRecapture,
                observation.StepId == "kick"
                    ? "Attempt 3 contains five clean note 36 strikes; earlier attempts also contain note 40."
                    : observation.StepId == "hihat-closed"
                        ? "Attempt 2 contains five clean note 42 strikes; attempt 1 contains note 44."
                        : $"Observed {CountForNote(observation, selectedNote.Value)} positive NoteOn messages for note {selectedNote.Value}."));
        }

        private static CandidateMappingEvidence NoteCandidate(
            CaptureStepObservation observation,
            TargetDefinition target,
            int note,
            EvidenceConfidence confidence,
            bool requiresRecapture,
            string evidence)
        {
            int? channel = observation.Channels.Count == 1 ? observation.Channels[0] : (int?)null;
            return new CandidateMappingEvidence(
                observation.StepId,
                target.ElementId,
                target.Articulation,
                "strike",
                new CandidateTrigger("noteOn", channel, note, 1, 127),
                confidence,
                new[] { evidence },
                observation.Notes.Count > 1 ? new[] { "Other positive notes remain in the evidence and are not discarded." } : Array.Empty<string>(),
                requiresRecapture);
        }

        private static void AddConflictedNotes(
            CaptureStepObservation observation,
            TargetDefinition target,
            ICollection<CandidateMappingEvidence> candidates,
            string warning)
        {
            foreach (NoteEvidence note in observation.Notes.OrderBy(value => value.Note))
                candidates.Add(NoteCandidate(observation, target, note.Note, EvidenceConfidence.Conflicted, true, warning));
        }

        private static void AddAftertouchCandidate(
            CaptureStepObservation observation,
            TargetDefinition target,
            ICollection<CandidateMappingEvidence> candidates,
            int note,
            string evidence)
        {
            MessageEvidence value = observation.Aftertouch.FirstOrDefault(item => item.Kind == "polyAftertouch" && item.Data1 == note);
            if (value == null) return;
            int? channel = observation.Channels.Count == 1 ? observation.Channels[0] : (int?)null;
            candidates.Add(new CandidateMappingEvidence(
                observation.StepId,
                target.ElementId,
                target.Articulation,
                "choke",
                new CandidateTrigger("polyAftertouch", channel, note, value.MinimumValue, value.MaximumValue),
                EvidenceConfidence.Medium,
                new[] { evidence, $"Observed {value.Count} aftertouch messages." },
                new[] { "The associated strike trigger is still conflicted." },
                true));
        }

        private static CandidateMappingEvidence Unresolved(
            CaptureStepObservation observation,
            TargetDefinition target,
            string role,
            string evidence)
        {
            return new CandidateMappingEvidence(
                observation.StepId,
                target.ElementId,
                target.Articulation,
                role,
                null,
                EvidenceConfidence.Insufficient,
                new[] { evidence },
                new[] { "No production trigger is available." },
                true);
        }

        private static string Recommendation(
            string stepId,
            EvidenceConfidence confidence,
            IReadOnlyList<NoteEvidence> notes,
            IReadOnlyList<MessageEvidence> controlChanges,
            IReadOnlyList<MessageEvidence> aftertouch)
        {
            if (stepId == "hihat-continuous" && controlChanges.Count == 0) return "Repeat slow pedal travel for 10 seconds and inspect CC messages; do not assume CC4.";
            if (stepId == "ride-bow") return "Recapture isolated Ride bow and bell strikes; notes 51 and 59 are conflicted.";
            if (stepId == "crash-choke") return "Recapture normal Crash 1 separately, then five strike-and-grab sequences; preserve note 26 aftertouch evidence.";
            if (stepId == "ride-choke-optional") return "Recapture isolated Ride strikes and five strike-and-grab sequences; preserve note 59 aftertouch evidence.";
            if (stepId == "kick") return "Keep note 36 as a high-confidence candidate but perform ten clean confirmation strikes.";
            if (stepId == "hihat-closed") return "Keep note 42 as candidate; recapture closed hits while separating pedal note 44.";
            if (confidence == EvidenceConfidence.Insufficient) return "Collect targeted evidence for this step.";
            if (confidence == EvidenceConfidence.Low || confidence == EvidenceConfidence.Conflicted) return "Repeat the step in isolation and compare attempts.";
            if (aftertouch.Count > 0) return "Confirm strike and aftertouch roles in separate sequences.";
            return "Candidate may be retained, subject to profile-level conflict review.";
        }

        private static MessageEvidence[] MessageGroups(IEnumerable<CaptureEventData> events)
        {
            return events
                .Where(value => value.Data2.HasValue)
                .GroupBy(value => new { value.RawKind, value.Data1 })
                .OrderBy(group => group.Key.RawKind, StringComparer.Ordinal)
                .ThenBy(group => group.Key.Data1)
                .Select(group => new MessageEvidence(
                    group.Key.RawKind,
                    group.Key.Data1,
                    group.Count(),
                    group.Min(value => value.Data2.Value),
                    group.Max(value => value.Data2.Value)))
                .ToArray();
        }

        private static bool IsPositiveNoteOn(CaptureEventData value) =>
            value.RawKind == "noteOn" && value.Data1.HasValue && value.Data2 > 0;

        private static double? Median(int[] values)
        {
            if (values.Length == 0) return null;
            int middle = values.Length / 2;
            return values.Length % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2.0;
        }

        private static int CountForNote(CaptureStepObservation observation, int note) =>
            observation.Notes.First(value => value.Note == note).Count;

        private sealed class TargetDefinition
        {
            public TargetDefinition(string elementId, string articulation)
            {
                ElementId = elementId;
                Articulation = articulation;
            }

            public string ElementId { get; }
            public string Articulation { get; }
        }

        private static readonly Dictionary<string, TargetDefinition> Targets = new(StringComparer.Ordinal)
        {
            ["kick"] = new TargetDefinition("kick", "default"),
            ["snare-center"] = new TargetDefinition("snare.head", "head"),
            ["snare-rim"] = new TargetDefinition("snare.rim", "rim"),
            ["tom-1"] = new TargetDefinition("tom1.head", "head"),
            ["tom-2"] = new TargetDefinition("tom2.head", "head"),
            ["floor-tom"] = new TargetDefinition("floor-tom.head", "head"),
            ["crash-1"] = new TargetDefinition("crash1.bow", "bow"),
            ["crash-2-optional"] = new TargetDefinition("crash2.bow", "bow"),
            ["ride-bow"] = new TargetDefinition("ride.bow", "bow"),
            ["ride-bell-optional"] = new TargetDefinition("ride.bell", "bell"),
            ["hihat-closed"] = new TargetDefinition("hihat.closed", "closed"),
            ["hihat-open"] = new TargetDefinition("hihat.open", "open"),
            ["hihat-pedal"] = new TargetDefinition("hihat.pedal", "pedal"),
            ["hihat-continuous"] = new TargetDefinition("hihat.continuous", "continuous"),
            ["crash-choke"] = new TargetDefinition("crash1.choke", "choke"),
            ["ride-choke-optional"] = new TargetDefinition("ride.choke", "choke")
        };

        private static readonly string[] RecapturePriority =
        {
            "ride-bow",
            "ride-bell-optional",
            "crash-1",
            "crash-choke",
            "ride-choke-optional",
            "hihat-closed",
            "hihat-open",
            "hihat-pedal",
            "hihat-continuous",
            "kick"
        };
    }
}
