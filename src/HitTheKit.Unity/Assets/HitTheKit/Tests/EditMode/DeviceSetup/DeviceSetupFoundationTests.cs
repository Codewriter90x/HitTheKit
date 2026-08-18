using System;
using System.IO;
using System.Linq;
using HitTheKit.Unity.DeviceSetup;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class DeviceSetupFoundationTests
    {
        [Test]
        public void Flow_starts_at_welcome_and_rejects_invalid_transition()
        {
            DeviceSetupFlow flow = Flow();
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.Welcome));
            Assert.That(flow.SelectDevice("device.hampback").Succeeded, Is.False);
            Assert.That(flow.Start().Succeeded, Is.True);
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.DeviceSelection));
        }

        [Test]
        public void Hampback_candidate_is_visible_but_never_auto_selected()
        {
            DeviceSetupFlow flow = StartedFlow();
            Assert.That(flow.SelectDevice("device.hampback").Succeeded, Is.True);
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.ProfileSelection));
            Assert.That(flow.Snapshot.SelectedProfile, Is.Null);
            DeviceProfileOption option = flow.Snapshot.SelectedDevice.Profiles.Single();
            Assert.That(option.Status, Does.Contain("not verified"));
            Assert.That(option.ProductionReady, Is.False);
            Assert.That(option.AutoSelectable, Is.False);
            Assert.That(option.RequiresConfirmation, Is.True);
            Assert.That(option.UnresolvedMappings, Is.Not.Empty);
        }

        [Test]
        public void Candidate_can_only_be_used_as_confirmation_starting_point()
        {
            DeviceSetupFlow flow = StartedFlow();
            flow.SelectDevice("device.hampback");
            Assert.That(flow.SelectProfile("dream-edrum-hampback-candidate-001").Succeeded, Is.True);
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.KitStructure));
            Assert.That(flow.Snapshot.Message, Does.Contain("confirmed"));
        }

        [Test]
        public void Candidate_prefill_enters_real_draft_and_never_auto_completes()
        {
            var store = new InMemoryUserKitConfigurationStore();
            DeviceSetupFlow flow = StartedFlow(store);
            flow.SelectDevice("device.hampback");
            flow.SelectProfile("dream-edrum-hampback-candidate-001");
            flow.ChoosePreset("standard");
            flow.BeginGuidedMapping();

            DeviceProfileMappingCandidate candidate = flow.Snapshot.CurrentCandidateMapping;
            Assert.That(candidate, Is.Not.Null);
            Assert.That(candidate.ElementId, Is.EqualTo("kick.default"));
            Assert.That(candidate.Kind, Is.EqualTo(RawMidiMessageKind.NoteOn));
            Assert.That(candidate.Channel, Is.EqualTo(9));
            Assert.That(candidate.Data1, Is.EqualTo(36));
            Assert.That(candidate.RequiresConfirmation, Is.True);
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Waiting));
            Assert.That(flow.AcceptCurrentCapture().Succeeded, Is.False);

            Assert.That(flow.SaveDraft().Succeeded, Is.True);
            UserKitConfiguration draft = flow.Snapshot.Configuration;
            MidiMappingEntry kick = draft.Mappings.Single(mapping => mapping.ElementId == "kick.default");
            Assert.That(kick.Trigger.Data1, Is.EqualTo(36));
            Assert.That(kick.Source, Is.EqualTo(MidiMappingSource.BuiltInProfile));
            Assert.That(kick.VerificationState, Is.EqualTo(MidiMappingVerificationState.RequiresConfirmation));
            Assert.That(kick.Notes, Does.Contain("Candidate profile"));
            Assert.That(draft.IsComplete, Is.False);
            Assert.That(flow.TestConfiguration().Succeeded, Is.False);
            Assert.That(store.List().Single().Mappings.Single(mapping => mapping.ElementId == "kick.default").VerificationState,
                Is.EqualTo(MidiMappingVerificationState.RequiresConfirmation));
        }

        [Test]
        public void Candidate_review_state_is_limited_to_elements_in_the_selected_setup()
        {
            DeviceSetupFlow flow = StartedFlow();
            flow.SelectDevice("device.hampback");
            flow.SelectProfile("dream-edrum-hampback-candidate-001");
            flow.ChoosePreset("standard");
            flow.BeginGuidedMapping();

            Assert.That(flow.Snapshot.ReviewIssues.Any(issue =>
                issue.ElementId == "ride.bow" && issue.Kind == KitMappingReviewIssueKind.Conflict && issue.BlocksCompletion), Is.True);
            Assert.That(flow.Snapshot.SelectedProfile.ReviewIssues.Any(issue =>
                issue.ElementId == "hihat.continuous" && issue.Kind == KitMappingReviewIssueKind.Insufficient), Is.True);
            Assert.That(flow.Snapshot.ReviewIssues.Any(issue => issue.ElementId == "hihat.continuous"), Is.False);

            flow.SaveDraft();
            Assert.That(flow.Snapshot.Configuration.ReviewIssues.Any(issue => issue.ElementId == "ride.bow"), Is.True);
            Assert.That(flow.Snapshot.Configuration.ReviewIssues.All(issue =>
                flow.Snapshot.Configuration.Elements.Any(element => element.Id == issue.ElementId)), Is.True);
        }

        [Test]
        public void Coherent_capture_confirms_candidate_without_duplicate_or_source_mutation()
        {
            DeviceSetupFlow flow = StartedFlow();
            flow.SelectDevice("device.hampback");
            flow.SelectProfile("dream-edrum-hampback-candidate-001");
            DeviceProfileMappingCandidate source = flow.Snapshot.SelectedProfile.CandidateFor("kick.default");
            flow.ChoosePreset("minimal");
            flow.BeginGuidedMapping();

            CaptureAndAccept(flow, 36);
            flow.SaveDraft();
            MidiMappingEntry[] kickMappings = flow.Snapshot.Configuration.Mappings
                .Where(mapping => mapping.ElementId == "kick.default").ToArray();
            Assert.That(kickMappings, Has.Length.EqualTo(1));
            Assert.That(kickMappings[0].Source, Is.EqualTo(MidiMappingSource.WizardCapture));
            Assert.That(kickMappings[0].VerificationState, Is.EqualTo(MidiMappingVerificationState.Confirmed));
            Assert.That(source.Data1, Is.EqualTo(36));
            Assert.That(source.RequiresConfirmation, Is.True);
        }

        [Test]
        public void Different_capture_creates_real_conflict_without_overwriting_candidate()
        {
            DeviceSetupFlow flow = StartedFlow();
            flow.SelectDevice("device.hampback");
            flow.SelectProfile("dream-edrum-hampback-candidate-001");
            flow.ChoosePreset("minimal");
            flow.BeginGuidedMapping();

            for (int index = 0; index < flow.Snapshot.CurrentStep.CaptureCount; index++)
                flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 37, 80 + index));
            Assert.That(flow.AcceptCurrentCapture().Succeeded, Is.True);
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.ConflictReview));
            Assert.That(flow.Snapshot.ReviewIssues.Any(issue =>
                issue.ElementId == "kick.default" && issue.Kind == KitMappingReviewIssueKind.Conflict), Is.True);
            flow.KeepConflictUnresolved();
            MidiMappingEntry kick = flow.Snapshot.Configuration.Mappings.Single(mapping => mapping.ElementId == "kick.default");
            Assert.That(kick.Trigger.Data1, Is.EqualTo(36));
            Assert.That(kick.VerificationState, Is.EqualTo(MidiMappingVerificationState.RequiresConfirmation));
        }

        [Test]
        public void Configure_from_scratch_does_not_select_candidate()
        {
            DeviceSetupFlow flow = StartedFlow();
            flow.SelectDevice("device.hampback");
            Assert.That(flow.ConfigureFromScratch().Succeeded, Is.True);
            Assert.That(flow.Snapshot.SelectedProfile, Is.Null);
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.KitStructure));
        }

        [Test]
        public void Discovery_supports_zero_one_and_duplicate_names_without_automatic_choice()
        {
            var discovery = new SimulatedDrumDeviceDiscovery();
            discovery.ReplaceDevices(Array.Empty<DrumDeviceDescriptor>());
            DeviceSetupFlow empty = new DeviceSetupFlow(discovery, new InMemoryUserKitConfigurationStore());
            empty.Start();
            Assert.That(empty.Snapshot.Discovery.Devices, Is.Empty);

            var duplicate = new DrumDeviceDescriptor("device.duplicate-a", "Same", "A", "Port A", DeviceConnectionState.Connected, Array.Empty<DeviceProfileOption>());
            var duplicate2 = new DrumDeviceDescriptor("device.duplicate-b", "Same", "B", "Port B", DeviceConnectionState.Connected, Array.Empty<DeviceProfileOption>());
            discovery.ReplaceDevices(new[] { duplicate, duplicate2 });
            empty.RefreshDevices();
            Assert.That(empty.Snapshot.SelectedDevice, Is.Null);
            Assert.That(empty.SelectDevice("device.duplicate-b").Succeeded, Is.True);
            Assert.That(empty.Snapshot.SelectedDevice.PortName, Is.EqualTo("Port B"));
        }

        [Test]
        public void Disconnected_device_cannot_be_selected()
        {
            var discovery = new SimulatedDrumDeviceDiscovery();
            discovery.Disconnect("device.hampback");
            DeviceSetupFlow flow = new DeviceSetupFlow(discovery, new InMemoryUserKitConfigurationStore());
            flow.Start();
            Assert.That(flow.SelectDevice("device.hampback").Succeeded, Is.False);
        }

        [Test]
        public void Refresh_after_back_drops_stale_device_selection()
        {
            var discovery = new SimulatedDrumDeviceDiscovery();
            var flow = new DeviceSetupFlow(discovery, new InMemoryUserKitConfigurationStore());
            flow.Start();
            flow.SelectDevice("device.hampback");
            flow.Back();
            discovery.ReplaceDevices(Array.Empty<DrumDeviceDescriptor>());

            Assert.That(flow.RefreshDevices().Succeeded, Is.True);
            Assert.That(flow.Snapshot.SelectedDevice, Is.Null);
            Assert.That(flow.Snapshot.SelectedProfile, Is.Null);
            Assert.That(flow.Snapshot.Discovery.Devices, Is.Empty);
        }

        [TestCase("minimal", 4)]
        [TestCase("standard", 11)]
        [TestCase("extended", 20)]
        public void Presets_create_real_wizard_structures(string preset, int expectedMinimum)
        {
            DeviceSetupFlow flow = StructureFlow();
            Assert.That(flow.ChoosePreset(preset).Succeeded, Is.True);
            Assert.That(flow.Snapshot.Setup.Elements.Count, Is.GreaterThanOrEqualTo(expectedMinimum));
        }

        [Test]
        public void Custom_structure_uses_domain_validation()
        {
            Assert.Throws<ArgumentException>(() => KitSetupDefinition.CreateCustom(
                "setup.invalid-ui", "Invalid", true, true, false, 0, 0, false, true, true, false, false, false));
            KitSetupDefinition valid = KitSetupDefinition.CreateCustom(
                "setup.valid-ui", "Valid", true, true, true, 2, 1, true, true, true, false, true, true);
            DeviceSetupFlow flow = StructureFlow();
            Assert.That(flow.ChooseCustomSetup(valid).Succeeded, Is.True);
        }

        [Test]
        public void Guided_capture_accept_retry_back_and_required_skip_contract()
        {
            DeviceSetupFlow flow = GuidedFlow();
            Assert.That(flow.SkipCurrentStep().Succeeded, Is.False);
            for (int index = 0; index < 4; index++)
            {
                flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 80 + index));
                Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.NeedsMoreSamples));
            }
            flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 100));
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.ReadyToConfirm));
            Assert.That(flow.RetryCurrentStep().Succeeded, Is.True);
            Assert.That(flow.Snapshot.CapturedEventCount, Is.Zero);
            CaptureAndAccept(flow, 36);
            Assert.That(flow.Snapshot.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(flow.Back().Succeeded, Is.True);
            Assert.That(flow.Snapshot.CurrentStepIndex, Is.Zero);
        }

        [Test]
        public void Optional_step_can_be_skipped_and_is_recorded()
        {
            var optional = new KitSetupDefinition("setup.optional-ui", "Optional", new[]
            {
                new KitElement("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true)
            });
            DeviceSetupFlow flow = StructureFlow();
            flow.ChooseCustomSetup(optional);
            flow.BeginGuidedMapping();
            Assert.That(flow.SkipCurrentStep().Succeeded, Is.True);
            Assert.That(flow.Snapshot.Configuration.DisabledElementIds, Is.EqualTo(new[] { "snare.rim" }));
        }

        [Test]
        public void Conflicting_samples_are_never_merged_silently()
        {
            DeviceSetupFlow flow = GuidedFlow();
            flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 80));
            flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 38, 90));
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Conflict));
            Assert.That(flow.Snapshot.Conflicts, Has.Count.EqualTo(1));
        }

        [Test]
        public void Overlapping_accepted_trigger_enters_conflict_review()
        {
            DeviceSetupFlow flow = GuidedFlow();
            CaptureAndAccept(flow, 36);
            CaptureAndAccept(flow, 36, acceptExpected: false);
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.ConflictReview));
            Assert.That(flow.KeepConflictUnresolved().Succeeded, Is.True);
            Assert.That(flow.Snapshot.Configuration.IsComplete, Is.False);
        }

        [Test]
        public void Disconnect_and_resume_preserve_current_step()
        {
            DeviceSetupFlow flow = GuidedFlow();
            int step = flow.Snapshot.CurrentStepIndex;
            Assert.That(flow.SetDisconnected().Succeeded, Is.True);
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Disconnected));
            Assert.That(flow.ResumeConnection().Succeeded, Is.True);
            Assert.That(flow.Snapshot.CurrentStepIndex, Is.EqualTo(step));
        }

        [Test]
        public void Disconnect_discards_pending_samples_and_blocks_accept_until_recaptured()
        {
            DeviceSetupFlow flow = GuidedFlow();
            for (int index = 0; index < flow.Snapshot.CurrentStep.CaptureCount; index++)
                flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 80 + index));
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.ReadyToConfirm));

            Assert.That(flow.SetDisconnected().Succeeded, Is.True);
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Disconnected));
            Assert.That(flow.Snapshot.CapturedEventCount, Is.Zero);
            Assert.That(flow.AcceptCurrentCapture().Succeeded, Is.False);
            Assert.That(flow.ResumeConnection().Succeeded, Is.True);
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Waiting));
            Assert.That(flow.AcceptCurrentCapture().Succeeded, Is.False);

            CaptureAndAccept(flow, 36);
            Assert.That(flow.Snapshot.CurrentStepIndex, Is.EqualTo(1));
        }

        [Test]
        public void Event_monitor_is_bounded_and_formats_velocity_zero_semantics()
        {
            DeviceSetupFlow flow = GuidedFlow();
            for (int index = 0; index < 12; index++) flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 0));
            Assert.That(flow.Snapshot.EventMonitor, Has.Count.EqualTo(10));
            Assert.That(flow.Snapshot.LastEvent, Does.Contain("equivalente NoteOff"));
            Assert.That(flow.Snapshot.CapturedEventCount, Is.Zero);
        }

        [Test]
        public void Guided_progress_counts_only_valid_hit_samples()
        {
            DeviceSetupFlow flow = GuidedFlow();
            flow.ProcessCapturedMessage(RawMidiMessage.NoteOff(9, 36));
            flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 0));
            Assert.That(flow.Snapshot.CapturedEventCount, Is.Zero);

            flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 90));
            Assert.That(flow.Snapshot.CapturedEventCount, Is.EqualTo(1));
            Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.NeedsMoreSamples));
        }

        [Test]
        public void In_memory_store_clones_and_rejects_duplicate_id()
        {
            var store = new InMemoryUserKitConfigurationStore();
            UserKitConfiguration configuration = CompleteMinimal();
            store.Save(configuration);
            Assert.That(store.TryLoad(configuration.ConfigurationId, out UserKitConfiguration loaded), Is.True);
            Assert.That(loaded, Is.Not.SameAs(configuration));
            Assert.That(store.List().Single(), Is.Not.SameAs(loaded));
            Assert.Throws<InvalidOperationException>(() => store.Save(configuration));
        }

        [Test]
        public void Draft_can_be_saved_but_not_used_by_mapping_engine()
        {
            var store = new InMemoryUserKitConfigurationStore();
            DeviceSetupFlow flow = GuidedFlow(store);
            CaptureAndAccept(flow, 36);
            Assert.That(flow.SaveDraft().Succeeded, Is.True);
            Assert.That(store.List().Single().IsComplete, Is.False);
            Assert.That(flow.TestConfiguration().Succeeded, Is.False);
        }

        [Test]
        public void Complete_configuration_reaches_test_and_highlights_mapping_source()
        {
            DeviceSetupFlow flow = GuidedFlow();
            CaptureAndAccept(flow, 36);
            CaptureAndAccept(flow, 38);
            CaptureAndAccept(flow, 42);
            CaptureAndAccept(flow, 46);
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.ConfigurationReview));
            Assert.That(flow.TestConfiguration().Succeeded, Is.True);
            flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 99));
            Assert.That(flow.Snapshot.HighlightedElementId, Is.EqualTo("kick.default"));
            Assert.That(flow.Snapshot.LastTestStatus, Is.EqualTo(MidiKitMappingStatus.Mapped));
            Assert.That(flow.Complete().Succeeded, Is.True);
        }

        [TestCase(DeviceSetupLanguage.Italian, "Configura")]
        [TestCase(DeviceSetupLanguage.English, "Configure")]
        public void Localization_supports_both_languages(DeviceSetupLanguage language, string expected)
        {
            var provider = new DictionaryLocalizedTextProvider(language);
            Assert.That(provider.Get("deviceSetup.title"), Does.StartWith(expected));
            Assert.That(provider.Get("missing.key", "Friendly fallback"), Is.EqualTo("Friendly fallback"));
        }

        [Test]
        public void Simulated_scenarios_are_deterministic_and_use_raw_messages()
        {
            var first = new SimulatedGuidedMidiCaptureSource(SimulatedCaptureScenario.HampbackCapture2);
            var second = new SimulatedGuidedMidiCaptureSource(SimulatedCaptureScenario.HampbackCapture2);
            var firstMessages = new System.Collections.Generic.List<string>();
            var secondMessages = new System.Collections.Generic.List<string>();
            first.MessageReceived += message => firstMessages.Add(DeviceSetupFlow.Format(message));
            second.MessageReceived += message => secondMessages.Add(DeviceSetupFlow.Format(message));
            first.Start("map.ride-bow");
            second.Start("map.ride-bow");
            first.EmitAll();
            second.EmitAll();
            Assert.That(firstMessages, Is.EqualTo(secondMessages));
        }

        [Test]
        public void Clean_scenario_covers_every_extended_wizard_step_without_fallback_conflicts()
        {
            var capture = new SimulatedGuidedMidiCaptureSource(SimulatedCaptureScenario.CleanStandardKit);
            DeviceSetupFlow flow = StructureFlow();
            flow.ChoosePreset("extended");
            flow.BeginGuidedMapping();
            capture.MessageReceived += message => flow.ProcessCapturedMessage(message);

            while (flow.State == DeviceSetupState.GuidedMapping)
            {
                string stepId = flow.Snapshot.CurrentStep.Id;
                capture.Start(stepId);
                Assert.That(capture.EmitAll(), Is.GreaterThan(0), stepId);
                Assert.That(flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.ReadyToConfirm), stepId);
                Assert.That(flow.AcceptCurrentCapture().Succeeded, Is.True, stepId);
                Assert.That(flow.Snapshot.Conflicts, Is.Empty, stepId);
            }

            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.ConfigurationReview));
            Assert.That(flow.Snapshot.Configuration.IsComplete, Is.True);
        }

        [Test]
        public void Hampback_capture_two_simulates_continuous_pedal_and_ride_choke_evidence()
        {
            var capture = new SimulatedGuidedMidiCaptureSource(SimulatedCaptureScenario.HampbackCapture2);
            var messages = new System.Collections.Generic.List<RawMidiMessage>();
            capture.MessageReceived += messages.Add;

            capture.Start("map.hihat-continuous");
            capture.EmitAll();
            Assert.That(messages.All(message => message.Kind == RawMidiMessageKind.ControlChange && message.Data1 == 4), Is.True);
            Assert.That(messages.Min(message => message.Value), Is.EqualTo(8));
            Assert.That(messages.Max(message => message.Value), Is.EqualTo(118));

            messages.Clear();
            capture.Start("map.ride-choke");
            capture.EmitAll();
            Assert.That(messages.Single().Kind, Is.EqualTo(RawMidiMessageKind.PolyAftertouch));
            Assert.That(messages.Single().Data1, Is.EqualTo(59));
        }

        [Test]
        public void Missing_continuous_scenario_produces_no_invented_cc()
        {
            var capture = new SimulatedGuidedMidiCaptureSource(SimulatedCaptureScenario.MissingHiHatContinuous);
            int received = 0;
            capture.MessageReceived += _ => received++;
            capture.Start("map.hihat-continuous");
            Assert.That(capture.EmitAll(), Is.Zero);
            Assert.That(received, Is.Zero);
        }

        [Test]
        public void Unknown_simulation_steps_never_fall_back_to_kick()
        {
            var capture = new SimulatedGuidedMidiCaptureSource(SimulatedCaptureScenario.CleanStandardKit);
            int received = 0;
            capture.MessageReceived += _ => received++;

            foreach (string stepId in new[] { "future-unknown-step", "Kick", " kick " })
            {
                capture.Start(stepId);
                Assert.That(capture.HasDataFor(stepId), Is.False, stepId);
                Assert.That(capture.EmitAll(), Is.Zero, stepId);
            }

            Assert.That(received, Is.Zero);
            Assert.Throws<ArgumentException>(() => capture.Start(null));
            Assert.Throws<ArgumentException>(() => capture.Start(string.Empty));
        }

        [Test]
        public void Reset_returns_flow_to_welcome()
        {
            DeviceSetupFlow flow = GuidedFlow();
            flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 80));
            flow.Reset();
            Assert.That(flow.State, Is.EqualTo(DeviceSetupState.Welcome));
            Assert.That(flow.Snapshot.EventMonitor, Is.Empty);
        }

        [Test]
        public void Atomic_store_round_trips_a_complete_configuration()
        {
            string directory = Path.Combine(Path.GetTempPath(), "hitthekit-device-setup-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "configuration.json");
            try
            {
                UserKitConfiguration source = CompleteMinimal();
                var store = new AtomicUserKitConfigurationStore(path);
                store.Save(source);

                Assert.That(store.TryLoad(source.ConfigurationId, out UserKitConfiguration loaded), Is.True);
                Assert.That(loaded.IsComplete, Is.True);
                Assert.That(loaded.Mappings.Count, Is.EqualTo(source.Mappings.Count));
                Assert.That(store.List(), Has.Count.EqualTo(1));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static DeviceSetupFlow Flow(InMemoryUserKitConfigurationStore store = null) =>
            new DeviceSetupFlow(new SimulatedDrumDeviceDiscovery(), store ?? new InMemoryUserKitConfigurationStore());

        private static DeviceSetupFlow StartedFlow(InMemoryUserKitConfigurationStore store = null)
        {
            DeviceSetupFlow flow = Flow(store);
            flow.Start();
            return flow;
        }

        private static DeviceSetupFlow StructureFlow()
        {
            DeviceSetupFlow flow = StartedFlow();
            flow.SelectDevice("device.unknown");
            return flow;
        }

        private static DeviceSetupFlow GuidedFlow(InMemoryUserKitConfigurationStore store = null)
        {
            DeviceSetupFlow flow = StartedFlow(store);
            flow.SelectDevice("device.unknown");
            flow.ChoosePreset("minimal");
            flow.BeginGuidedMapping();
            return flow;
        }

        private static UserKitConfiguration CompleteMinimal()
        {
            DeviceSetupFlow flow = GuidedFlow();
            CaptureAndAccept(flow, 36);
            CaptureAndAccept(flow, 38);
            CaptureAndAccept(flow, 42);
            CaptureAndAccept(flow, 46);
            return flow.Snapshot.Configuration;
        }

        private static void CaptureAndAccept(DeviceSetupFlow flow, int note, bool acceptExpected = true)
        {
            int captureCount = flow.Snapshot.CurrentStep.CaptureCount;
            for (int index = 0; index < captureCount; index++)
                flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, note, 80 + index));
            DeviceSetupTransitionResult result = flow.AcceptCurrentCapture();
            Assert.That(result.Succeeded, Is.EqualTo(acceptExpected || flow.State == DeviceSetupState.ConflictReview));
        }
    }
}
