using System;
using System.Linq;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class KitMappingWizardTests
    {
        [Test]
        public void Presets_describe_expected_elements()
        {
            Assert.That(KitSetupDefinition.Minimal3Piece().Elements.Select(element => element.Id),
                Is.EqualTo(new[] { "kick.default", "snare.head", "hihat.closed", "hihat.open" }));
            Assert.That(KitSetupDefinition.Standard5Piece().Elements.Select(element => element.Id),
                Does.Contain("floortom.head"));
            Assert.That(KitSetupDefinition.ExtendedElectronicKit().Elements.Select(element => element.Id),
                Does.Contain("ride.bell"));
        }

        [Test]
        public void Custom_setup_supports_documented_tom_and_crash_count_bounds()
        {
            KitSetupDefinition setup = KitSetupDefinition.CreateCustom(
                "setup.custom", "Custom", true, true, true, 4, 3, true, true, true, true, true, true);
            Assert.That(setup.Elements.Count(element =>
                element.Piece == KitPiece.Tom1 || element.Piece == KitPiece.Tom2 ||
                element.Piece == KitPiece.Tom3 || element.Piece == KitPiece.Tom4), Is.EqualTo(4));
            Assert.That(setup.Elements.Count(element =>
                element.Articulation == KitArticulation.Bow &&
                (element.Piece == KitPiece.Crash1 || element.Piece == KitPiece.Crash2 || element.Piece == KitPiece.Crash3)),
                Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => KitSetupDefinition.CreateCustom(
                "setup.invalid", "Invalid", true, true, false, 5, 0, false, false, false, false, false, false));
        }

        [Test]
        public void Custom_setup_rejects_articulations_without_their_parent_piece()
        {
            Assert.Throws<ArgumentException>(() => KitSetupDefinition.CreateCustom(
                "setup.invalid-snare", "Invalid", true, false, true, 0, 0, false, false, false, false, false, false));
            Assert.Throws<ArgumentException>(() => KitSetupDefinition.CreateCustom(
                "setup.invalid-hihat", "Invalid", true, true, false, 0, 0, false, false, false, true, false, false));
            Assert.Throws<ArgumentException>(() => KitSetupDefinition.CreateCustom(
                "setup.invalid-ride", "Invalid", true, true, false, 0, 0, false, true, false, false, false, false));
            Assert.Throws<ArgumentException>(() => KitSetupDefinition.CreateCustom(
                "setup.invalid-crash", "Invalid", true, true, false, 0, 0, false, false, false, false, false, true));
        }

        [Test]
        public void Step_order_is_deterministic_and_exposes_prompt_contract()
        {
            var first = Session(KitSetupDefinition.Minimal3Piece());
            var second = Session(KitSetupDefinition.Minimal3Piece());
            Assert.That(first.Steps.Select(step => step.Id), Is.EqualTo(second.Steps.Select(step => step.Id)));
            Assert.That(first.CurrentStep.FallbackDisplayText, Is.EqualTo("Hit the kick drum."));
            Assert.That(first.CurrentStep.Required, Is.True);
            Assert.That(first.CurrentStep.CaptureCount, Is.EqualTo(5));
        }

        [Test]
        public void Kick_requires_five_coherent_samples_and_explicit_accept()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            for (int index = 0; index < 4; index++)
            {
                Assert.That(session.Capture(RawMidiMessage.NoteOn(9, 36, 70 + index * 5)).Status,
                    Is.EqualTo(KitMappingWizardCaptureStatus.NeedsMoreSamples));
                Assert.That(session.HasPendingCapture, Is.False);
            }
            Assert.That(session.Capture(RawMidiMessage.NoteOn(9, 36, 100)).Status,
                Is.EqualTo(KitMappingWizardCaptureStatus.NeedsMoreSamples));
            Assert.That(session.HasPendingCapture, Is.True);
            Assert.That(session.Accept().Status, Is.EqualTo(KitMappingWizardCaptureStatus.Accepted));
            Assert.That(session.CurrentStep.TargetElement.Id, Is.EqualTo("snare.head"));
        }

        [Test]
        public void Note_off_and_velocity_zero_note_on_are_ignored_for_hit_step()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            Assert.That(session.Capture(RawMidiMessage.NoteOff(0, 36)).Status, Is.EqualTo(KitMappingWizardCaptureStatus.Ignored));
            Assert.That(session.Capture(RawMidiMessage.NoteOn(0, 36, 0)).Status, Is.EqualTo(KitMappingWizardCaptureStatus.Ignored));
        }

        [Test]
        public void Incoherent_sample_reports_conflict_and_resets_capture()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            session.Capture(RawMidiMessage.NoteOn(0, 36, 80));
            Assert.That(session.Capture(RawMidiMessage.NoteOn(0, 37, 80)).Status,
                Is.EqualTo(KitMappingWizardCaptureStatus.Conflict));
            Assert.That(session.HasPendingCapture, Is.False);
            Assert.That(session.Capture(RawMidiMessage.NoteOn(0, 37, 80)).Status,
                Is.EqualTo(KitMappingWizardCaptureStatus.NeedsMoreSamples));
        }

        [Test]
        public void Retry_clears_current_capture()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            for (int index = 0; index < session.CurrentStep.CaptureCount; index++)
                session.Capture(RawMidiMessage.NoteOn(0, 36, 80 + index));
            Assert.That(session.HasPendingCapture, Is.True);
            session.Retry();
            Assert.That(session.HasPendingCapture, Is.False);
            Assert.That(session.CurrentStepIndex, Is.Zero);
        }

        [Test]
        public void Back_returns_to_previous_step_and_removes_its_mapping()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            CaptureAndAccept(session, 36);
            Assert.That(session.Back(), Is.True);
            Assert.That(session.CurrentStepIndex, Is.Zero);
            Assert.That(session.ExportDraft().Mappings, Is.Empty);
            Assert.That(session.Back(), Is.False);
        }

        [Test]
        public void Skip_is_allowed_only_for_optional_steps()
        {
            var optionalSetup = new KitSetupDefinition("setup.optional", "Optional",
                new[] { new KitElement("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true) });
            KitMappingWizardSession optional = Session(optionalSetup);
            Assert.That(optional.SkipOptional(), Is.True);
            Assert.That(optional.IsCompleted, Is.True);
            Assert.That(optional.FinalizeConfiguration().DisabledElementIds, Is.EqualTo(new[] { "snare.rim" }));

            KitMappingWizardSession required = Session(KitSetupDefinition.Minimal3Piece());
            Assert.That(required.SkipOptional(), Is.False);
            Assert.Throws<InvalidOperationException>(() => required.FinalizeConfiguration());
        }

        [Test]
        public void Overlapping_trigger_for_different_element_is_not_silently_overwritten()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            CaptureAndAccept(session, 36);
            session.Capture(RawMidiMessage.NoteOn(0, 36, 80));
            session.Capture(RawMidiMessage.NoteOn(0, 36, 90));
            Assert.That(session.Accept().Status, Is.EqualTo(KitMappingWizardCaptureStatus.Conflict));
            Assert.That(session.CurrentStep.TargetElement.Id, Is.EqualTo("snare.head"));
        }

        [Test]
        public void Control_change_pedal_uses_consistent_controller_and_sample_range()
        {
            var setup = new KitSetupDefinition("setup.pedal", "Pedal",
                new[] { new KitElement("hihat.pedal", KitPiece.HiHat, KitArticulation.Pedal, "Hi-hat pedal") });
            KitMappingWizardSession session = Session(setup);
            session.Capture(RawMidiMessage.ControlChange(2, 4, 20));
            session.Capture(RawMidiMessage.ControlChange(2, 4, 100));
            Assert.That(session.Accept().Status, Is.EqualTo(KitMappingWizardCaptureStatus.Completed));
            MidiTrigger trigger = session.FinalizeConfiguration().Mappings[0].Trigger;
            Assert.That((trigger.Kind, trigger.Data1, trigger.MinimumValue, trigger.MaximumValue),
                Is.EqualTo((RawMidiMessageKind.ControlChange, (int?)4, 20, 100)));
        }

        [Test]
        public void Choke_accepts_one_supported_aftertouch_sample()
        {
            var setup = new KitSetupDefinition("setup.choke", "Choke",
                new[] { new KitElement("crash1.choke", KitPiece.Crash1, KitArticulation.Choke, "Crash choke") });
            KitMappingWizardSession session = Session(setup);
            Assert.That(session.CurrentStep.CaptureCount, Is.EqualTo(1));
            Assert.That(session.Capture(RawMidiMessage.PolyAftertouch(9, 49, 50)).Status,
                Is.EqualTo(KitMappingWizardCaptureStatus.NeedsMoreSamples));
            Assert.That(session.Accept().Status, Is.EqualTo(KitMappingWizardCaptureStatus.Completed));
        }

        [Test]
        public void Incomplete_session_exports_draft_but_cannot_finalize()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            Assert.That(session.ExportDraft().IsComplete, Is.False);
            Assert.Throws<InvalidOperationException>(() => session.FinalizeConfiguration());
        }

        [Test]
        public void Candidate_seed_is_real_draft_state_and_requires_matching_capture()
        {
            KitSetupDefinition setup = KitSetupDefinition.Minimal3Piece();
            var candidate = new MidiMappingEntry(
                "candidate.kick-default",
                new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 1, 127),
                "kick.default",
                source: MidiMappingSource.BuiltInProfile,
                notes: "Candidate; requires confirmation.",
                verificationState: MidiMappingVerificationState.RequiresConfirmation);
            var issue = new KitMappingReviewIssue(
                "hihat.open",
                KitMappingReviewIssueKind.Insufficient,
                "Open hi-hat evidence is insufficient.",
                false);
            var session = new KitMappingWizardSession(
                setup,
                "user.seeded-kit",
                "Seeded Kit",
                new KitMappingWizardSeed(new[] { candidate }, new[] { issue }));

            UserKitConfiguration draft = session.ExportDraft();
            Assert.That(draft.Mappings.Single().VerificationState,
                Is.EqualTo(MidiMappingVerificationState.RequiresConfirmation));
            Assert.That(draft.ReviewIssues.Single().ElementId, Is.EqualTo("hihat.open"));
            Assert.That(draft.IsComplete, Is.False);
            Assert.Throws<InvalidOperationException>(() => session.FinalizeConfiguration());

            for (int index = 0; index < session.CurrentStep.CaptureCount; index++)
                session.Capture(RawMidiMessage.NoteOn(9, 36, 80 + index * 5));
            Assert.That(session.Accept().Status, Is.EqualTo(KitMappingWizardCaptureStatus.Accepted));
            MidiMappingEntry confirmed = session.ExportDraft().Mappings.Single(mapping => mapping.ElementId == "kick.default");
            Assert.That(confirmed.Source, Is.EqualTo(MidiMappingSource.WizardCapture));
            Assert.That(confirmed.VerificationState, Is.EqualTo(MidiMappingVerificationState.Confirmed));
        }

        [Test]
        public void Candidate_seed_rejects_different_capture_without_replacement()
        {
            KitSetupDefinition setup = KitSetupDefinition.Minimal3Piece();
            var candidate = new MidiMappingEntry(
                "candidate.kick-default",
                new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 1, 127),
                "kick.default",
                verificationState: MidiMappingVerificationState.RequiresConfirmation);
            var session = new KitMappingWizardSession(
                setup,
                "user.seeded-kit",
                "Seeded Kit",
                new KitMappingWizardSeed(new[] { candidate }, Array.Empty<KitMappingReviewIssue>()));

            for (int index = 0; index < session.CurrentStep.CaptureCount; index++)
                session.Capture(RawMidiMessage.NoteOn(9, 37, 80 + index * 5));
            Assert.That(session.Accept().Status, Is.EqualTo(KitMappingWizardCaptureStatus.Conflict));
            Assert.That(session.ExportDraft().Mappings.Single().Trigger.Data1, Is.EqualTo(36));
            Assert.That(session.ReviewIssues.Any(issue =>
                issue.ElementId == "kick.default" && issue.Kind == KitMappingReviewIssueKind.Conflict), Is.True);
        }

        [Test]
        public void Completed_session_finalizes_and_round_trips_deterministically()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            CaptureAndAccept(session, 36);
            CaptureAndAccept(session, 38);
            CaptureAndAccept(session, 42);
            CaptureAndAccept(session, 46);
            Assert.That(session.IsCompleted, Is.True);
            UserKitConfiguration configuration = session.FinalizeConfiguration();
            Assert.That(configuration.IsComplete, Is.True);

            var serializer = new UserKitConfigurationSerializer();
            string first = serializer.Serialize(configuration);
            string second = serializer.Serialize(new UserKitConfigurationLoader().Load(first));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(serializer.Serialize(session.FinalizeConfiguration()), Is.EqualTo(first));
        }

        [Test]
        public void Wizard_and_direct_model_apply_the_same_required_mapping_gate()
        {
            KitSetupDefinition setup = KitSetupDefinition.Minimal3Piece();
            KitMappingWizardSession incomplete = Session(setup);

            Assert.Throws<InvalidOperationException>(() => incomplete.FinalizeConfiguration());
            Assert.Throws<ArgumentException>(() => new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.direct-incomplete",
                "Direct Incomplete",
                null,
                null,
                null,
                setup.Elements,
                Array.Empty<MidiMappingEntry>(),
                Array.Empty<string>()));

            KitMappingWizardSession complete = Session(setup);
            CaptureAndAccept(complete, 36);
            CaptureAndAccept(complete, 38);
            CaptureAndAccept(complete, 42);
            CaptureAndAccept(complete, 46);
            UserKitConfiguration finalized = complete.FinalizeConfiguration();
            string json = new UserKitConfigurationSerializer().Serialize(finalized);

            Assert.That(new UserKitConfigurationLoader().Load(json).IsComplete, Is.True);
        }

        [Test]
        public void Reset_returns_session_to_initial_state()
        {
            KitMappingWizardSession session = Session(KitSetupDefinition.Minimal3Piece());
            CaptureAndAccept(session, 36);
            session.Reset();
            Assert.That(session.CurrentStepIndex, Is.Zero);
            Assert.That(session.ExportDraft().Mappings, Is.Empty);
            Assert.That(session.IsCompleted, Is.False);
        }

        private static KitMappingWizardSession Session(KitSetupDefinition setup)
        {
            return new KitMappingWizardSession(setup, "user.wizard-kit", "Wizard Kit");
        }

        private static void CaptureAndAccept(KitMappingWizardSession session, int note)
        {
            int captureCount = session.CurrentStep.CaptureCount;
            for (int index = 0; index < captureCount; index++)
            {
                Assert.That(session.Capture(RawMidiMessage.NoteOn(0, note, 80 + index)).Status,
                    Is.EqualTo(KitMappingWizardCaptureStatus.NeedsMoreSamples));
            }
            KitMappingWizardCaptureStatus status = session.Accept().Status;
            Assert.That(status == KitMappingWizardCaptureStatus.Accepted || status == KitMappingWizardCaptureStatus.Completed,
                Is.True);
        }
    }
}
