using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Devices
{
    public sealed class KitSetupDefinition
    {
        private readonly IReadOnlyList<KitElement> elements;

        public KitSetupDefinition(string id, string displayName, IReadOnlyList<KitElement> elements)
        {
            KitElement.EnsureStableId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name must not be empty.", nameof(displayName));
            }

            KitElement[] copy = ElectronicDrumProfile.CopyReferences(elements, nameof(elements));
            ElectronicDrumProfile.ValidateElementsAndMappings(copy, Array.Empty<MidiMappingEntry>(), true);
            Id = id;
            DisplayName = displayName;
            this.elements = Array.AsReadOnly(copy);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<KitElement> Elements => elements;

        public static KitSetupDefinition CreateCustom(
            string id,
            string displayName,
            bool kickPresent,
            bool snarePresent,
            bool snareRim,
            int tomCount,
            int crashCount,
            bool ridePresent,
            bool rideBell,
            bool hiHatPresent,
            bool hiHatHalfOpen,
            bool hiHatPedal,
            bool crashChokes)
        {
            if (tomCount < 0 || tomCount > 4) throw new ArgumentOutOfRangeException(nameof(tomCount));
            if (crashCount < 0 || crashCount > 3) throw new ArgumentOutOfRangeException(nameof(crashCount));
            if (snareRim && !snarePresent) throw new ArgumentException("Snare rim requires a snare.", nameof(snareRim));
            if ((hiHatHalfOpen || hiHatPedal) && !hiHatPresent)
            {
                throw new ArgumentException("Hi-hat articulations require a hi-hat.", nameof(hiHatPresent));
            }
            if (rideBell && !ridePresent) throw new ArgumentException("Ride bell requires a ride.", nameof(rideBell));
            if (crashChokes && crashCount == 0)
            {
                throw new ArgumentException("Crash choke requires at least one crash.", nameof(crashChokes));
            }

            var result = new List<KitElement>();
            if (kickPresent) result.Add(Element("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick"));
            if (snarePresent)
            {
                result.Add(Element("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare head"));
                if (snareRim) result.Add(Element("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true));
            }
            if (hiHatPresent)
            {
                result.Add(Element("hihat.closed", KitPiece.HiHat, KitArticulation.Closed, "Closed hi-hat"));
                if (hiHatHalfOpen) result.Add(Element("hihat.halfopen", KitPiece.HiHat, KitArticulation.HalfOpen, "Half-open hi-hat", true));
                result.Add(Element("hihat.open", KitPiece.HiHat, KitArticulation.Open, "Open hi-hat"));
                if (hiHatPedal) result.Add(Element("hihat.pedal", KitPiece.HiHat, KitArticulation.Pedal, "Hi-hat pedal", true));
            }

            KitPiece[] tomPieces = { KitPiece.Tom1, KitPiece.Tom2, KitPiece.Tom3, KitPiece.Tom4 };
            for (int index = 0; index < tomCount; index++)
            {
                string number = (index + 1).ToString();
                result.Add(Element("tom" + number + ".head", tomPieces[index], KitArticulation.Head, "Tom " + number));
            }

            KitPiece[] crashPieces = { KitPiece.Crash1, KitPiece.Crash2, KitPiece.Crash3 };
            for (int index = 0; index < crashCount; index++)
            {
                string number = (index + 1).ToString();
                result.Add(Element("crash" + number + ".bow", crashPieces[index], KitArticulation.Bow, "Crash " + number));
                if (crashChokes)
                {
                    result.Add(Element("crash" + number + ".choke", crashPieces[index], KitArticulation.Choke, "Crash " + number + " choke", true));
                }
            }
            if (ridePresent)
            {
                result.Add(Element("ride.bow", KitPiece.Ride, KitArticulation.Bow, "Ride bow"));
                if (rideBell) result.Add(Element("ride.bell", KitPiece.Ride, KitArticulation.Bell, "Ride bell", true));
            }
            return new KitSetupDefinition(id, displayName, result);
        }

        public static KitSetupDefinition Minimal3Piece()
        {
            return new KitSetupDefinition("setup.minimal-3-piece", "Minimal 3-piece", new[]
            {
                Element("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick"),
                Element("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare head"),
                Element("hihat.closed", KitPiece.HiHat, KitArticulation.Closed, "Closed hi-hat"),
                Element("hihat.open", KitPiece.HiHat, KitArticulation.Open, "Open hi-hat")
            });
        }

        public static KitSetupDefinition Standard5Piece()
        {
            return new KitSetupDefinition("setup.standard-5-piece", "Standard 5-piece", new[]
            {
                Element("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick"),
                Element("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare head"),
                Element("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true),
                Element("hihat.closed", KitPiece.HiHat, KitArticulation.Closed, "Closed hi-hat"),
                Element("hihat.open", KitPiece.HiHat, KitArticulation.Open, "Open hi-hat"),
                Element("hihat.pedal", KitPiece.HiHat, KitArticulation.Pedal, "Hi-hat pedal", true),
                Element("tom1.head", KitPiece.Tom1, KitArticulation.Head, "Tom 1"),
                Element("tom2.head", KitPiece.Tom2, KitArticulation.Head, "Tom 2"),
                Element("floortom.head", KitPiece.FloorTom, KitArticulation.Head, "Floor tom"),
                Element("crash1.bow", KitPiece.Crash1, KitArticulation.Bow, "Crash 1"),
                Element("ride.bow", KitPiece.Ride, KitArticulation.Bow, "Ride bow")
            });
        }

        public static KitSetupDefinition ExtendedElectronicKit()
        {
            var elements = new List<KitElement>(Standard5Piece().Elements)
            {
                Element("hihat.halfopen", KitPiece.HiHat, KitArticulation.HalfOpen, "Half-open hi-hat", true),
                Element("tom1.rim", KitPiece.Tom1, KitArticulation.Rim, "Tom 1 rim", true),
                Element("tom2.rim", KitPiece.Tom2, KitArticulation.Rim, "Tom 2 rim", true),
                Element("crash1.edge", KitPiece.Crash1, KitArticulation.Edge, "Crash 1 edge", true),
                Element("crash1.choke", KitPiece.Crash1, KitArticulation.Choke, "Crash 1 choke", true),
                Element("crash2.bow", KitPiece.Crash2, KitArticulation.Bow, "Crash 2", true),
                Element("crash2.choke", KitPiece.Crash2, KitArticulation.Choke, "Crash 2 choke", true),
                Element("ride.edge", KitPiece.Ride, KitArticulation.Edge, "Ride edge", true),
                Element("ride.bell", KitPiece.Ride, KitArticulation.Bell, "Ride bell", true)
            };
            return new KitSetupDefinition("setup.extended-electronic", "Extended electronic kit", elements);
        }

        private static KitElement Element(
            string id,
            KitPiece piece,
            KitArticulation articulation,
            string displayName,
            bool optional = false)
        {
            return new KitElement(id, piece, articulation, displayName, optional);
        }
    }

    public enum KitMappingConflictPolicy
    {
        RejectOverlap
    }

    public sealed class KitMappingWizardStep
    {
        private readonly IReadOnlyList<RawMidiMessageKind> expectedMessageKinds;

        internal KitMappingWizardStep(
            string id,
            KitElement targetElement,
            string instructionKey,
            string fallbackDisplayText,
            bool required,
            RawMidiMessageKind[] expectedMessageKinds,
            int captureCount,
            KitMappingConflictPolicy conflictPolicy)
        {
            Id = id;
            TargetElement = targetElement;
            InstructionKey = instructionKey;
            FallbackDisplayText = fallbackDisplayText;
            Required = required;
            this.expectedMessageKinds = Array.AsReadOnly(expectedMessageKinds);
            CaptureCount = captureCount;
            ConflictPolicy = conflictPolicy;
        }

        public string Id { get; }
        public KitElement TargetElement { get; }
        public string InstructionKey { get; }
        public string FallbackDisplayText { get; }
        public bool Required { get; }
        public IReadOnlyList<RawMidiMessageKind> ExpectedMessageKinds => expectedMessageKinds;
        public int CaptureCount { get; }
        public KitMappingConflictPolicy ConflictPolicy { get; }
    }

    public enum KitMappingWizardCaptureStatus
    {
        Accepted,
        NeedsMoreSamples,
        Conflict,
        Ignored,
        Completed
    }

    public sealed class KitMappingWizardCaptureResult
    {
        internal KitMappingWizardCaptureResult(KitMappingWizardCaptureStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public KitMappingWizardCaptureStatus Status { get; }
        public string Message { get; }
    }

    public sealed class KitMappingWizardSeed
    {
        private readonly IReadOnlyList<MidiMappingEntry> candidateMappings;
        private readonly IReadOnlyList<KitMappingReviewIssue> reviewIssues;

        public KitMappingWizardSeed(
            IReadOnlyList<MidiMappingEntry> candidateMappings,
            IReadOnlyList<KitMappingReviewIssue> reviewIssues)
        {
            this.candidateMappings = Array.AsReadOnly(
                ElectronicDrumProfile.CopyReferences(candidateMappings ?? Array.Empty<MidiMappingEntry>(), nameof(candidateMappings)));
            this.reviewIssues = Array.AsReadOnly(
                ElectronicDrumProfile.CopyReferences(reviewIssues ?? Array.Empty<KitMappingReviewIssue>(), nameof(reviewIssues)));
        }

        public IReadOnlyList<MidiMappingEntry> CandidateMappings => candidateMappings;
        public IReadOnlyList<KitMappingReviewIssue> ReviewIssues => reviewIssues;
    }

    public sealed class KitMappingWizardSession
    {
        private readonly KitSetupDefinition setup;
        private readonly string configurationId;
        private readonly string displayName;
        private readonly IReadOnlyList<KitMappingWizardStep> steps;
        private readonly IReadOnlyList<MidiMappingEntry> initialCandidateMappings;
        private readonly IReadOnlyList<KitMappingReviewIssue> initialReviewIssues;
        private readonly List<MidiMappingEntry> mappings = new List<MidiMappingEntry>();
        private readonly List<KitMappingReviewIssue> reviewIssues = new List<KitMappingReviewIssue>();
        private readonly List<string> disabledElementIds = new List<string>();
        private readonly List<RawMidiMessage> samples = new List<RawMidiMessage>();
        private MidiTrigger pendingTrigger;
        private int currentIndex;

        public KitMappingWizardSession(
            KitSetupDefinition setup,
            string configurationId,
            string displayName)
            : this(setup, configurationId, displayName, null)
        {
        }

        public KitMappingWizardSession(
            KitSetupDefinition setup,
            string configurationId,
            string displayName,
            KitMappingWizardSeed seed)
        {
            this.setup = setup ?? throw new ArgumentNullException(nameof(setup));
            KitElement.EnsureStableId(configurationId, nameof(configurationId));
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name must not be empty.", nameof(displayName));
            }
            this.configurationId = configurationId;
            this.displayName = displayName;
            steps = Array.AsReadOnly(BuildSteps(setup.Elements));
            initialCandidateMappings = Array.AsReadOnly(ValidateSeedMappings(seed?.CandidateMappings));
            initialReviewIssues = Array.AsReadOnly(
                ElectronicDrumProfile.CopyReferences(seed?.ReviewIssues ?? Array.Empty<KitMappingReviewIssue>(), "reviewIssues"));
            RestoreInitialSeed();
        }

        public IReadOnlyList<KitMappingWizardStep> Steps => steps;
        public int CurrentStepIndex => currentIndex;
        public KitMappingWizardStep CurrentStep => currentIndex < steps.Count ? steps[currentIndex] : null;
        public bool IsCompleted => currentIndex >= steps.Count;
        public bool HasPendingCapture => pendingTrigger != null;
        public int CurrentCaptureCount => samples.Count;
        public IReadOnlyList<KitMappingReviewIssue> ReviewIssues => reviewIssues.AsReadOnly();

        public MidiMappingEntry CandidateMappingFor(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId)) return null;
            return mappings.Find(mapping =>
                string.Equals(mapping.ElementId, elementId, StringComparison.Ordinal) &&
                mapping.VerificationState == MidiMappingVerificationState.RequiresConfirmation);
        }

        public KitMappingWizardCaptureResult Capture(RawMidiMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (IsCompleted) return Result(KitMappingWizardCaptureStatus.Completed, "All steps are complete.");
            if (pendingTrigger != null)
            {
                return Result(KitMappingWizardCaptureStatus.Ignored, "Accept or retry the pending capture first.");
            }

            KitMappingWizardStep step = CurrentStep;
            if (!IsExpected(step, message))
            {
                return Result(KitMappingWizardCaptureStatus.Ignored, "The MIDI message is not valid for this step.");
            }
            if (samples.Count > 0 && !IsCoherent(samples[0], message))
            {
                samples.Clear();
                return Result(KitMappingWizardCaptureStatus.Conflict, "Samples did not identify the same trigger; retry the step.");
            }

            samples.Add(message);
            if (samples.Count < step.CaptureCount)
            {
                return Result(KitMappingWizardCaptureStatus.NeedsMoreSamples, "More consistent samples are required.");
            }

            pendingTrigger = BuildTrigger(samples);
            return Result(KitMappingWizardCaptureStatus.NeedsMoreSamples, "Capture is stable and ready to accept.");
        }

        public KitMappingWizardCaptureResult Accept()
        {
            if (IsCompleted) return Result(KitMappingWizardCaptureStatus.Completed, "All steps are complete.");
            if (pendingTrigger == null)
            {
                return Result(KitMappingWizardCaptureStatus.NeedsMoreSamples, "The current step has no stable capture.");
            }

            MidiMappingEntry candidate = CandidateMappingFor(CurrentStep.TargetElement.Id);
            if (candidate != null && !candidate.Trigger.HasSameDefinition(pendingTrigger))
            {
                AddReviewIssue(new KitMappingReviewIssue(
                    CurrentStep.TargetElement.Id,
                    KitMappingReviewIssueKind.Conflict,
                    $"Captured trigger differs from candidate mapping '{candidate.Id}'.",
                    CurrentStep.Required));
                return Result(
                    KitMappingWizardCaptureStatus.Conflict,
                    "The captured trigger differs from the candidate; retry or keep the element unresolved.");
            }

            foreach (MidiMappingEntry mapping in mappings)
            {
                if (mapping.Trigger.Overlaps(pendingTrigger) &&
                    !string.Equals(mapping.ElementId, CurrentStep.TargetElement.Id, StringComparison.Ordinal))
                {
                    AddReviewIssue(new KitMappingReviewIssue(
                        CurrentStep.TargetElement.Id,
                        KitMappingReviewIssueKind.Conflict,
                        $"Captured trigger overlaps mapping '{mapping.Id}' for '{mapping.ElementId}'.",
                        CurrentStep.Required));
                    return Result(
                        KitMappingWizardCaptureStatus.Conflict,
                        $"The trigger overlaps mapping '{mapping.Id}' for '{mapping.ElementId}'.");
                }
            }

            mappings.RemoveAll(mapping => string.Equals(mapping.ElementId, CurrentStep.TargetElement.Id, StringComparison.Ordinal));
            mappings.Add(new MidiMappingEntry(
                "wizard." + CurrentStep.TargetElement.Id.Replace('.', '-'),
                pendingTrigger,
                CurrentStep.TargetElement.Id,
                0,
                MidiMappingSource.WizardCapture,
                true,
                "Confirmed by guided capture.",
                MidiMappingVerificationState.Confirmed));
            reviewIssues.RemoveAll(issue => string.Equals(issue.ElementId, CurrentStep.TargetElement.Id, StringComparison.Ordinal));
            Advance();
            return Result(IsCompleted ? KitMappingWizardCaptureStatus.Completed : KitMappingWizardCaptureStatus.Accepted,
                IsCompleted ? "All steps are complete." : "Capture accepted.");
        }

        public void Retry()
        {
            ClearCapture();
        }

        public bool Back()
        {
            if (currentIndex == 0) return false;
            currentIndex--;
            string targetId = CurrentStep.TargetElement.Id;
            mappings.RemoveAll(mapping => string.Equals(mapping.ElementId, targetId, StringComparison.Ordinal));
            disabledElementIds.RemoveAll(id => string.Equals(id, targetId, StringComparison.Ordinal));
            RestoreSeedForElement(targetId);
            ClearCapture();
            return true;
        }

        public bool SkipOptional()
        {
            if (IsCompleted || CurrentStep.Required) return false;
            string targetId = CurrentStep.TargetElement.Id;
            mappings.RemoveAll(mapping => string.Equals(mapping.ElementId, targetId, StringComparison.Ordinal));
            reviewIssues.RemoveAll(issue => string.Equals(issue.ElementId, targetId, StringComparison.Ordinal));
            disabledElementIds.Add(CurrentStep.TargetElement.Id);
            Advance();
            return true;
        }

        public void Reset()
        {
            mappings.Clear();
            reviewIssues.Clear();
            disabledElementIds.Clear();
            currentIndex = 0;
            ClearCapture();
            RestoreInitialSeed();
        }

        public UserKitConfiguration ExportDraft()
        {
            return BuildConfiguration(false);
        }

        public UserKitConfiguration FinalizeConfiguration()
        {
            if (!IsCompleted)
            {
                throw new InvalidOperationException("All wizard steps must be completed or optional steps skipped.");
            }
            if (UserKitConfigurationInvariantValidator.TryGetViolation(
                    setup.Elements,
                    mappings,
                    reviewIssues,
                    disabledElementIds,
                    true,
                    out string completionViolation))
            {
                throw new InvalidOperationException(completionViolation);
            }
            return BuildConfiguration(true);
        }

        private UserKitConfiguration BuildConfiguration(bool complete)
        {
            return new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                configurationId,
                displayName,
                null,
                null,
                null,
                setup.Elements,
                mappings,
                disabledElementIds,
                null,
                complete,
                reviewIssues);
        }

        private MidiMappingEntry[] ValidateSeedMappings(IReadOnlyList<MidiMappingEntry> source)
        {
            if (source == null) return Array.Empty<MidiMappingEntry>();
            var result = new MidiMappingEntry[source.Count];
            var elementIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < source.Count; index++)
            {
                MidiMappingEntry mapping = source[index] ?? throw new ArgumentException("Candidate mapping is null.", nameof(source));
                if (!HasSetupElement(mapping.ElementId))
                {
                    throw new ArgumentException($"Candidate mapping '{mapping.Id}' targets an element outside the selected setup.", nameof(source));
                }
                if (mapping.VerificationState != MidiMappingVerificationState.RequiresConfirmation)
                {
                    throw new ArgumentException($"Candidate mapping '{mapping.Id}' must require confirmation.", nameof(source));
                }
                if (!elementIds.Add(mapping.ElementId))
                {
                    throw new ArgumentException($"Multiple candidate mappings target '{mapping.ElementId}'.", nameof(source));
                }
                for (int previous = 0; previous < index; previous++)
                {
                    if (result[previous].Trigger.Overlaps(mapping.Trigger))
                    {
                        throw new ArgumentException(
                            $"Candidate mapping '{mapping.Id}' overlaps '{result[previous].Id}' and must be represented as a review issue.",
                            nameof(source));
                    }
                }
                result[index] = mapping;
            }
            return result;
        }

        private bool HasSetupElement(string elementId)
        {
            for (int index = 0; index < setup.Elements.Count; index++)
            {
                if (string.Equals(setup.Elements[index].Id, elementId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private void RestoreInitialSeed()
        {
            for (int index = 0; index < initialCandidateMappings.Count; index++) mappings.Add(initialCandidateMappings[index]);
            for (int index = 0; index < initialReviewIssues.Count; index++) reviewIssues.Add(initialReviewIssues[index]);
        }

        private void RestoreSeedForElement(string elementId)
        {
            for (int index = 0; index < initialCandidateMappings.Count; index++)
            {
                MidiMappingEntry mapping = initialCandidateMappings[index];
                if (string.Equals(mapping.ElementId, elementId, StringComparison.Ordinal)) mappings.Add(mapping);
            }
            for (int index = 0; index < initialReviewIssues.Count; index++)
            {
                KitMappingReviewIssue issue = initialReviewIssues[index];
                if (string.Equals(issue.ElementId, elementId, StringComparison.Ordinal)) AddReviewIssue(issue);
            }
        }

        private void AddReviewIssue(KitMappingReviewIssue issue)
        {
            reviewIssues.RemoveAll(existing =>
                string.Equals(existing.ElementId, issue.ElementId, StringComparison.Ordinal) && existing.Kind == issue.Kind);
            reviewIssues.Add(issue);
        }

        private void Advance()
        {
            currentIndex++;
            ClearCapture();
        }

        private void ClearCapture()
        {
            samples.Clear();
            pendingTrigger = null;
        }

        private static bool IsExpected(KitMappingWizardStep step, RawMidiMessage message)
        {
            RawMidiMessageKind kind = message.SemanticKind;
            for (int index = 0; index < step.ExpectedMessageKinds.Count; index++)
            {
                if (step.ExpectedMessageKinds[index] == kind)
                {
                    return kind != RawMidiMessageKind.NoteOn || message.Value > 0;
                }
            }
            return false;
        }

        private static bool IsCoherent(RawMidiMessage first, RawMidiMessage current)
        {
            return first.SemanticKind == current.SemanticKind &&
                   first.Channel == current.Channel &&
                   first.Data1 == current.Data1;
        }

        private static MidiTrigger BuildTrigger(IReadOnlyList<RawMidiMessage> messages)
        {
            RawMidiMessage first = messages[0];
            int minimum = first.Value;
            int maximum = first.Value;
            for (int index = 1; index < messages.Count; index++)
            {
                minimum = Math.Min(minimum, messages[index].Value);
                maximum = Math.Max(maximum, messages[index].Value);
            }

            if (first.SemanticKind == RawMidiMessageKind.NoteOn)
            {
                minimum = 1;
                maximum = 127;
            }
            else if (first.SemanticKind == RawMidiMessageKind.NoteOff ||
                     first.SemanticKind == RawMidiMessageKind.PolyAftertouch ||
                     first.SemanticKind == RawMidiMessageKind.ChannelAftertouch)
            {
                minimum = 0;
                maximum = 127;
            }

            return new MidiTrigger(first.SemanticKind, first.Channel, first.Data1, minimum, maximum);
        }

        private static KitMappingWizardStep[] BuildSteps(IReadOnlyList<KitElement> elements)
        {
            var result = new KitMappingWizardStep[elements.Count];
            for (int index = 0; index < elements.Count; index++)
            {
                KitElement element = elements[index];
                RawMidiMessageKind[] expected;
                int count;
                if (element.Articulation == KitArticulation.Pedal)
                {
                    expected = new[] { RawMidiMessageKind.ControlChange, RawMidiMessageKind.NoteOn };
                    count = 2;
                }
                else if (element.Articulation == KitArticulation.Choke)
                {
                    expected = new[]
                    {
                        RawMidiMessageKind.PolyAftertouch,
                        RawMidiMessageKind.ChannelAftertouch,
                        RawMidiMessageKind.NoteOff
                    };
                    count = 1;
                }
                else
                {
                    expected = new[] { RawMidiMessageKind.NoteOn };
                    count = element.Piece == KitPiece.Kick ? 5 : 2;
                }

                result[index] = new KitMappingWizardStep(
                    "map." + element.Id.Replace('.', '-'),
                    element,
                    "kitMapping." + element.Id.Replace('.', '-'),
                    InstructionFor(element),
                    !element.IsOptional,
                    expected,
                    count,
                    KitMappingConflictPolicy.RejectOverlap);
            }
            return result;
        }

        private static string InstructionFor(KitElement element)
        {
            switch (element.Piece)
            {
                case KitPiece.Kick: return "Hit the kick drum.";
                case KitPiece.Snare when element.Articulation == KitArticulation.Rim: return "Hit the rim of the snare.";
                case KitPiece.Snare: return "Hit the center of the snare.";
                case KitPiece.HiHat when element.Articulation == KitArticulation.Pedal:
                    return "Press the hi-hat pedal without striking the cymbal.";
                case KitPiece.HiHat when element.Articulation == KitArticulation.Open: return "Hit the open hi-hat.";
                case KitPiece.HiHat when element.Articulation == KitArticulation.Closed: return "Hit the closed hi-hat.";
                case KitPiece.Crash1 when element.Articulation == KitArticulation.Choke: return "Choke Crash 1.";
                case KitPiece.Crash2 when element.Articulation == KitArticulation.Choke: return "Choke Crash 2.";
                case KitPiece.Ride when element.Articulation == KitArticulation.Bell: return "Hit the bell of the ride.";
                case KitPiece.Ride: return "Hit the bow of the ride.";
                default: return "Hit " + element.DisplayName + ".";
            }
        }

        private static KitMappingWizardCaptureResult Result(KitMappingWizardCaptureStatus status, string message)
        {
            return new KitMappingWizardCaptureResult(status, message);
        }
    }
}
