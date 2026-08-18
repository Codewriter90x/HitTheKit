using System;
using System.Collections.Generic;
using System.IO;
using HitTheKit.Unity.Devices;

namespace HitTheKit.Unity.DeviceSetup
{
    public enum DeviceSetupState
    {
        Welcome,
        DeviceSelection,
        ProfileSelection,
        KitStructure,
        GuidedMapping,
        ConflictReview,
        ConfigurationReview,
        TestKit,
        Completed
    }

    public enum CaptureFeedbackState
    {
        Waiting,
        Receiving,
        NeedsMoreSamples,
        ReadyToConfirm,
        Conflict,
        Skipped,
        Completed,
        Disconnected,
        Unsupported
    }

    public enum ConfigurationMappingOrigin
    {
        KnownProfile,
        WizardCapture,
        UserOverride,
        Unresolved,
        Skipped
    }

    public sealed class DeviceSetupTransitionResult
    {
        private DeviceSetupTransitionResult(bool succeeded, DeviceSetupState state, string message)
        {
            Succeeded = succeeded;
            State = state;
            Message = message;
        }

        public bool Succeeded { get; }
        public DeviceSetupState State { get; }
        public string Message { get; }
        public static DeviceSetupTransitionResult Success(DeviceSetupState state, string message = null) => new DeviceSetupTransitionResult(true, state, message);
        public static DeviceSetupTransitionResult Invalid(DeviceSetupState state, string message) => new DeviceSetupTransitionResult(false, state, message);
    }

    public sealed class DeviceSetupConflict
    {
        public DeviceSetupConflict(string stepId, string message)
        {
            StepId = stepId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string StepId { get; }
        public string Message { get; }
    }

    public sealed class DeviceSetupSnapshot
    {
        internal DeviceSetupSnapshot(
            DeviceSetupState state,
            DeviceDiscoverySnapshot discovery,
            DrumDeviceDescriptor selectedDevice,
            DeviceProfileOption selectedProfile,
            KitSetupDefinition setup,
            KitMappingWizardSession wizard,
            CaptureFeedbackState feedback,
            int capturedEventCount,
            string lastEvent,
            IReadOnlyList<string> eventMonitor,
            IReadOnlyList<DeviceSetupConflict> conflicts,
            UserKitConfiguration configuration,
            string message,
            string highlightedElementId,
            MidiKitMappingStatus? lastTestStatus)
        {
            State = state;
            Discovery = discovery;
            SelectedDevice = selectedDevice;
            SelectedProfile = selectedProfile;
            Setup = setup;
            CurrentStep = wizard?.CurrentStep;
            CurrentStepIndex = wizard?.CurrentStepIndex ?? 0;
            TotalSteps = wizard?.Steps.Count ?? 0;
            Feedback = feedback;
            CapturedEventCount = capturedEventCount;
            LastEvent = lastEvent;
            EventMonitor = CopyStrings(eventMonitor);
            Conflicts = CopyConflicts(conflicts);
            Configuration = configuration;
            CurrentCandidateMapping = wizard?.CandidateMappingFor(CurrentStep?.TargetElement.Id) != null
                ? selectedProfile?.CandidateFor(CurrentStep?.TargetElement.Id)
                : null;
            ReviewIssues = CopyReviewIssues(wizard?.ReviewIssues);
            Message = message;
            HighlightedElementId = highlightedElementId;
            LastTestStatus = lastTestStatus;
        }

        public DeviceSetupState State { get; }
        public DeviceDiscoverySnapshot Discovery { get; }
        public DrumDeviceDescriptor SelectedDevice { get; }
        public DeviceProfileOption SelectedProfile { get; }
        public KitSetupDefinition Setup { get; }
        public KitMappingWizardStep CurrentStep { get; }
        public int CurrentStepIndex { get; }
        public int TotalSteps { get; }
        public CaptureFeedbackState Feedback { get; }
        public int CapturedEventCount { get; }
        public string LastEvent { get; }
        public IReadOnlyList<string> EventMonitor { get; }
        public IReadOnlyList<DeviceSetupConflict> Conflicts { get; }
        public UserKitConfiguration Configuration { get; }
        public DeviceProfileMappingCandidate CurrentCandidateMapping { get; }
        public IReadOnlyList<KitMappingReviewIssue> ReviewIssues { get; }
        public string Message { get; }
        public string HighlightedElementId { get; }
        public MidiKitMappingStatus? LastTestStatus { get; }

        private static IReadOnlyList<string> CopyStrings(IReadOnlyList<string> source)
        {
            var result = new string[source?.Count ?? 0];
            for (int index = 0; index < result.Length; index++) result[index] = source[index];
            return Array.AsReadOnly(result);
        }

        private static IReadOnlyList<DeviceSetupConflict> CopyConflicts(IReadOnlyList<DeviceSetupConflict> source)
        {
            var result = new DeviceSetupConflict[source?.Count ?? 0];
            for (int index = 0; index < result.Length; index++) result[index] = source[index];
            return Array.AsReadOnly(result);
        }

        private static IReadOnlyList<KitMappingReviewIssue> CopyReviewIssues(IReadOnlyList<KitMappingReviewIssue> source)
        {
            var result = new KitMappingReviewIssue[source?.Count ?? 0];
            for (int index = 0; index < result.Length; index++) result[index] = source[index];
            return Array.AsReadOnly(result);
        }
    }

    public sealed class DeviceSetupFlow
    {
        private const int EventMonitorCapacity = 10;
        private readonly IDrumDeviceDiscovery discovery;
        private readonly IUserKitConfigurationStore store;
        private readonly List<string> eventMonitor = new List<string>();
        private readonly List<DeviceSetupConflict> conflicts = new List<DeviceSetupConflict>();
        private DeviceDiscoverySnapshot discoverySnapshot;
        private DrumDeviceDescriptor selectedDevice;
        private DeviceProfileOption selectedProfile;
        private KitSetupDefinition setup;
        private KitMappingWizardSession wizard;
        private CaptureFeedbackState feedback = CaptureFeedbackState.Waiting;
        private int capturedEventCount;
        private string lastEvent;
        private string message;
        private UserKitConfiguration configuration;
        private string highlightedElementId;
        private MidiKitMappingStatus? lastTestStatus;

        public DeviceSetupFlow(IDrumDeviceDiscovery discovery, IUserKitConfigurationStore store)
        {
            this.discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            State = DeviceSetupState.Welcome;
            discoverySnapshot = new DeviceDiscoverySnapshot(DeviceDiscoveryState.Idle, Array.Empty<DrumDeviceDescriptor>());
        }

        public DeviceSetupState State { get; private set; }
        public DeviceSetupSnapshot Snapshot => new DeviceSetupSnapshot(
            State, discoverySnapshot, selectedDevice, selectedProfile, setup, wizard, feedback,
            capturedEventCount, lastEvent, eventMonitor, conflicts, configuration, message,
            highlightedElementId, lastTestStatus);

        public DeviceSetupTransitionResult Start()
        {
            if (State != DeviceSetupState.Welcome) return Invalid("Setup has already started.");
            State = DeviceSetupState.DeviceSelection;
            return RefreshDevices();
        }

        public DeviceSetupTransitionResult RefreshDevices()
        {
            if (State != DeviceSetupState.DeviceSelection) return Invalid("Device refresh is available only during device selection.");
            discoverySnapshot = discovery.Refresh();
            selectedDevice = null;
            selectedProfile = null;
            message = discoverySnapshot.Message ??
                (discoverySnapshot.Devices.Count == 0 ? "No MIDI input devices were found. Connect a device and refresh." : null);
            return Success(message);
        }

        public DeviceSetupTransitionResult SelectDevice(string deviceId)
        {
            if (State != DeviceSetupState.DeviceSelection) return Invalid("A device cannot be selected in the current state.");
            DrumDeviceDescriptor device = FindDevice(deviceId);
            if (device == null) return Invalid("Device was not found.");
            if (device.ConnectionState != DeviceConnectionState.Connected) return Invalid("Device is disconnected.");
            selectedDevice = device;
            selectedProfile = null;
            State = device.Profiles.Count > 0 ? DeviceSetupState.ProfileSelection : DeviceSetupState.KitStructure;
            message = null;
            return Success();
        }

        public DeviceSetupTransitionResult SelectProfile(string profileId)
        {
            if (State != DeviceSetupState.ProfileSelection || selectedDevice == null) return Invalid("Profile selection is not active.");
            DeviceProfileOption profile = null;
            for (int index = 0; index < selectedDevice.Profiles.Count; index++)
                if (selectedDevice.Profiles[index].Id == profileId) profile = selectedDevice.Profiles[index];
            if (profile == null) return Invalid("Profile was not found.");
            if (!profile.CanUseAsStartingPoint) return Invalid("Profile cannot be used as a starting point.");
            selectedProfile = profile;
            State = DeviceSetupState.KitStructure;
            message = profile.RequiresConfirmation ? "Candidate mappings are prefilled as evidence only and must be confirmed." : null;
            return Success(message);
        }

        public DeviceSetupTransitionResult ConfigureFromScratch()
        {
            if (State != DeviceSetupState.ProfileSelection) return Invalid("Configure from scratch is available only during profile selection.");
            selectedProfile = null;
            State = DeviceSetupState.KitStructure;
            message = "No profile mappings will be assumed.";
            return Success(message);
        }

        public DeviceSetupTransitionResult ChooseCustomSetup(KitSetupDefinition definition)
        {
            if (State != DeviceSetupState.KitStructure) return Invalid("Kit structure cannot be changed in the current state.");
            setup = definition ?? throw new ArgumentNullException(nameof(definition));
            message = null;
            return Success();
        }

        public DeviceSetupTransitionResult ChoosePreset(string presetId)
        {
            if (State != DeviceSetupState.KitStructure) return Invalid("Kit structure cannot be changed in the current state.");
            switch (presetId)
            {
                case "minimal": setup = KitSetupDefinition.Minimal3Piece(); break;
                case "standard": setup = KitSetupDefinition.Standard5Piece(); break;
                case "extended": setup = KitSetupDefinition.ExtendedElectronicKit(); break;
                default: return Invalid("Unknown kit preset.");
            }
            return Success();
        }

        public DeviceSetupTransitionResult BeginGuidedMapping()
        {
            if (State != DeviceSetupState.KitStructure || setup == null) return Invalid("Select a valid kit structure first.");
            wizard = new KitMappingWizardSession(
                setup,
                DeviceSetupConfigurationRuntime.ConfigurationId,
                "Device Setup Kit",
                BuildWizardSeed());
            configuration = null;
            conflicts.Clear();
            for (int index = 0; index < wizard.ReviewIssues.Count; index++)
            {
                KitMappingReviewIssue issue = wizard.ReviewIssues[index];
                if (issue.Kind == KitMappingReviewIssueKind.Conflict)
                    conflicts.Add(new DeviceSetupConflict(issue.ElementId, issue.Description));
            }
            eventMonitor.Clear();
            capturedEventCount = 0;
            feedback = CaptureFeedbackState.Waiting;
            State = DeviceSetupState.GuidedMapping;
            return Success();
        }

        public DeviceSetupTransitionResult SetSimulationUnsupported(string stepId)
        {
            if (State != DeviceSetupState.GuidedMapping || wizard == null) return Invalid("No guided capture is active.");
            if (!string.Equals(wizard.CurrentStep?.Id, stepId, StringComparison.Ordinal))
                return Invalid("Unsupported simulation does not match the current step.");
            capturedEventCount = 0;
            feedback = CaptureFeedbackState.Unsupported;
            return Success($"No simulated events are defined for step '{stepId}'.");
        }

        public DeviceSetupTransitionResult ProcessCapturedMessage(RawMidiMessage rawMessage)
        {
            if (rawMessage == null) throw new ArgumentNullException(nameof(rawMessage));
            if (State == DeviceSetupState.TestKit) return ProcessTestMessage(rawMessage);
            if (State != DeviceSetupState.GuidedMapping || wizard == null) return Invalid("Capture is not active.");
            if (wizard.HasPendingCapture) return Success(message);
            lastEvent = Format(rawMessage);
            eventMonitor.Add(lastEvent);
            while (eventMonitor.Count > EventMonitorCapacity) eventMonitor.RemoveAt(0);
            KitMappingWizardCaptureResult result = wizard.Capture(rawMessage);
            capturedEventCount = wizard.CurrentCaptureCount;
            message = result.Message;
            switch (result.Status)
            {
                case KitMappingWizardCaptureStatus.Conflict:
                    feedback = CaptureFeedbackState.Conflict;
                    AddConflict(wizard.CurrentStep?.Id, result.Message);
                    break;
                case KitMappingWizardCaptureStatus.NeedsMoreSamples:
                    feedback = wizard.HasPendingCapture ? CaptureFeedbackState.ReadyToConfirm : CaptureFeedbackState.NeedsMoreSamples;
                    break;
                case KitMappingWizardCaptureStatus.Completed:
                    feedback = CaptureFeedbackState.Completed;
                    break;
                default:
                    feedback = CaptureFeedbackState.Receiving;
                    break;
            }
            return Success(message);
        }

        public DeviceSetupTransitionResult AcceptCurrentCapture()
        {
            if (State != DeviceSetupState.GuidedMapping || wizard == null) return Invalid("No guided capture is active.");
            if (feedback != CaptureFeedbackState.ReadyToConfirm) return Invalid("Only a stable connected capture can be accepted.");
            string acceptedElementId = wizard.CurrentStep?.TargetElement.Id;
            KitMappingWizardCaptureResult result = wizard.Accept();
            message = result.Message;
            if (result.Status == KitMappingWizardCaptureStatus.Conflict)
            {
                feedback = CaptureFeedbackState.Conflict;
                AddConflict(wizard.CurrentStep?.Id, result.Message);
                State = DeviceSetupState.ConflictReview;
                return Success(message);
            }
            conflicts.RemoveAll(conflict => string.Equals(conflict.StepId, acceptedElementId, StringComparison.Ordinal));
            capturedEventCount = 0;
            feedback = wizard.IsCompleted ? CaptureFeedbackState.Completed : CaptureFeedbackState.Waiting;
            if (wizard.IsCompleted)
            {
                configuration = wizard.FinalizeConfiguration();
                State = DeviceSetupState.ConfigurationReview;
            }
            return Success(message);
        }

        public DeviceSetupTransitionResult RetryCurrentStep()
        {
            if ((State != DeviceSetupState.GuidedMapping && State != DeviceSetupState.ConflictReview) || wizard == null)
                return Invalid("No step can be retried now.");
            wizard.Retry();
            conflicts.RemoveAll(conflict => conflict.StepId == wizard.CurrentStep?.Id);
            capturedEventCount = 0;
            feedback = CaptureFeedbackState.Waiting;
            State = DeviceSetupState.GuidedMapping;
            return Success();
        }

        public DeviceSetupTransitionResult SkipCurrentStep()
        {
            if (State != DeviceSetupState.GuidedMapping || wizard == null) return Invalid("No step can be skipped now.");
            if (!wizard.SkipOptional()) return Invalid("Required steps cannot be skipped.");
            feedback = CaptureFeedbackState.Skipped;
            capturedEventCount = 0;
            if (wizard.IsCompleted)
            {
                configuration = wizard.FinalizeConfiguration();
                State = DeviceSetupState.ConfigurationReview;
            }
            return Success();
        }

        public DeviceSetupTransitionResult Back()
        {
            switch (State)
            {
                case DeviceSetupState.ProfileSelection: State = DeviceSetupState.DeviceSelection; selectedDevice = null; selectedProfile = null; break;
                case DeviceSetupState.KitStructure: State = selectedDevice?.Profiles.Count > 0 ? DeviceSetupState.ProfileSelection : DeviceSetupState.DeviceSelection; break;
                case DeviceSetupState.GuidedMapping:
                    if (wizard != null && wizard.Back()) { feedback = CaptureFeedbackState.Waiting; capturedEventCount = 0; }
                    else State = DeviceSetupState.KitStructure;
                    break;
                case DeviceSetupState.ConflictReview: return RetryCurrentStep();
                case DeviceSetupState.ConfigurationReview: State = DeviceSetupState.GuidedMapping; break;
                case DeviceSetupState.TestKit: State = DeviceSetupState.ConfigurationReview; break;
                default: return Invalid("Back is not available in the current state.");
            }
            return Success();
        }

        public DeviceSetupTransitionResult ResolveConflict(string stepId)
        {
            if (State != DeviceSetupState.ConflictReview) return Invalid("Conflict review is not active.");
            if (string.IsNullOrWhiteSpace(stepId)) return Invalid("Conflict step is required.");
            return RetryCurrentStep();
        }

        public DeviceSetupTransitionResult KeepConflictUnresolved()
        {
            if (State != DeviceSetupState.ConflictReview) return Invalid("Conflict review is not active.");
            wizard?.Retry();
            feedback = CaptureFeedbackState.Conflict;
            State = DeviceSetupState.ConfigurationReview;
            configuration = wizard?.ExportDraft();
            return Success("Conflict remains unresolved in the draft.");
        }

        public DeviceSetupTransitionResult ReviewConfiguration()
        {
            if (wizard == null || (State != DeviceSetupState.GuidedMapping && State != DeviceSetupState.ConflictReview))
                return Invalid("Configuration review is not available.");
            configuration = wizard.IsCompleted ? wizard.FinalizeConfiguration() : wizard.ExportDraft();
            State = conflicts.Count > 0 ? DeviceSetupState.ConflictReview : DeviceSetupState.ConfigurationReview;
            return Success();
        }

        public DeviceSetupTransitionResult SaveDraft()
        {
            if (wizard == null) return Invalid("There is no configuration to save.");
            UserKitConfiguration draft = wizard.ExportDraft();
            try
            {
                store.Save(draft);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is IOException)
            {
                return Invalid(exception.Message);
            }
            configuration = draft;
            State = DeviceSetupState.ConfigurationReview;
            return Success("Draft saved. Complete the required mappings before gameplay can use it.");
        }

        public DeviceSetupTransitionResult TestConfiguration()
        {
            if (State != DeviceSetupState.ConfigurationReview || configuration == null)
                return Invalid("Review a configuration before testing it.");
            if (!configuration.IsComplete)
                return Invalid("Complete all required mappings before entering kit test mode.");
            State = DeviceSetupState.TestKit;
            highlightedElementId = null;
            lastTestStatus = null;
            return Success();
        }

        public DeviceSetupTransitionResult Complete()
        {
            if (State != DeviceSetupState.TestKit) return Invalid("Test the configuration before completing setup.");
            if (configuration == null || !configuration.IsComplete)
                return Invalid("Only a complete, tested configuration can be saved for gameplay.");
            try
            {
                store.Save(configuration);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is IOException)
            {
                return Invalid($"The kit configuration could not be saved: {exception.Message}");
            }
            State = DeviceSetupState.Completed;
            return Success("Configuration saved and ready for gameplay.");
        }

        public DeviceSetupTransitionResult SetDisconnected()
        {
            if (State != DeviceSetupState.GuidedMapping) return Invalid("No active capture to disconnect.");
            wizard?.Retry();
            capturedEventCount = 0;
            feedback = CaptureFeedbackState.Disconnected;
            return Success("Device disconnected. Current samples were discarded; reconnect to retry the step.");
        }

        public DeviceSetupTransitionResult ResumeConnection()
        {
            if (State != DeviceSetupState.GuidedMapping || feedback != CaptureFeedbackState.Disconnected)
                return Invalid("Capture is not waiting for reconnection.");
            feedback = CaptureFeedbackState.Waiting;
            return Success("Device reconnected.");
        }

        public void Reset()
        {
            State = DeviceSetupState.Welcome;
            selectedDevice = null;
            selectedProfile = null;
            setup = null;
            wizard = null;
            configuration = null;
            eventMonitor.Clear();
            conflicts.Clear();
            capturedEventCount = 0;
            lastEvent = null;
            highlightedElementId = null;
            lastTestStatus = null;
            feedback = CaptureFeedbackState.Waiting;
            message = null;
        }

        private DeviceSetupTransitionResult ProcessTestMessage(RawMidiMessage rawMessage)
        {
            MidiKitMappingResult result = new MidiKitMappingEngine().Map(rawMessage, configuration);
            lastTestStatus = result.Status;
            highlightedElementId = result.Hit?.ElementId;
            lastEvent = Format(rawMessage);
            eventMonitor.Add(lastEvent + " — " + result.Status + (result.Hit == null ? string.Empty : " / " + result.Hit.Source));
            while (eventMonitor.Count > EventMonitorCapacity) eventMonitor.RemoveAt(0);
            message = result.Message;
            return Success(message);
        }

        private KitMappingWizardSeed BuildWizardSeed()
        {
            if (selectedProfile == null || selectedProfile.CandidateMappings.Count == 0 && selectedProfile.ReviewIssues.Count == 0)
                return null;

            var seedMappings = new List<MidiMappingEntry>();
            for (int index = 0; index < selectedProfile.CandidateMappings.Count; index++)
            {
                DeviceProfileMappingCandidate candidate = selectedProfile.CandidateMappings[index];
                if (!SetupContains(candidate.ElementId, out _)) continue;
                int minimum = candidate.Kind == RawMidiMessageKind.NoteOn ? 1 : 0;
                seedMappings.Add(new MidiMappingEntry(
                    "candidate." + candidate.ElementId.Replace('.', '-'),
                    new MidiTrigger(candidate.Kind, candidate.Channel, candidate.Data1, minimum, 127),
                    candidate.ElementId,
                    0,
                    MidiMappingSource.BuiltInProfile,
                    true,
                    $"Candidate profile '{selectedProfile.Id}' ({candidate.Confidence}); requires confirmation.",
                    MidiMappingVerificationState.RequiresConfirmation));
            }

            var issues = new List<KitMappingReviewIssue>();
            for (int index = 0; index < selectedProfile.ReviewIssues.Count; index++)
            {
                KitMappingReviewIssue source = selectedProfile.ReviewIssues[index];
                if (!SetupContains(source.ElementId, out KitElement element)) continue;
                issues.Add(new KitMappingReviewIssue(
                    source.ElementId,
                    source.Kind,
                    source.Description,
                    !element.IsOptional && source.BlocksCompletion));
            }
            return new KitMappingWizardSeed(seedMappings, issues);
        }

        private bool SetupContains(string elementId, out KitElement found)
        {
            for (int index = 0; index < setup.Elements.Count; index++)
            {
                if (string.Equals(setup.Elements[index].Id, elementId, StringComparison.Ordinal))
                {
                    found = setup.Elements[index];
                    return true;
                }
            }
            found = null;
            return false;
        }

        private DrumDeviceDescriptor FindDevice(string id)
        {
            DrumDeviceDescriptor found = null;
            for (int index = 0; index < discoverySnapshot.Devices.Count; index++)
            {
                DrumDeviceDescriptor candidate = discoverySnapshot.Devices[index];
                if (candidate.Id != id) continue;
                if (found != null) return null;
                found = candidate;
            }
            return found;
        }

        private void AddConflict(string stepId, string conflictMessage)
        {
            for (int index = 0; index < conflicts.Count; index++)
                if (conflicts[index].StepId == stepId) return;
            conflicts.Add(new DeviceSetupConflict(stepId, conflictMessage));
        }

        private DeviceSetupTransitionResult Success(string resultMessage = null)
        {
            if (resultMessage != null) message = resultMessage;
            return DeviceSetupTransitionResult.Success(State, resultMessage);
        }

        private DeviceSetupTransitionResult Invalid(string reason)
        {
            message = reason;
            return DeviceSetupTransitionResult.Invalid(State, reason);
        }

        public static string Format(RawMidiMessage message)
        {
            string channel = "ch" + (message.Channel + 1);
            switch (message.Kind)
            {
                case RawMidiMessageKind.NoteOn when message.Value == 0:
                    return $"NoteOn  {channel}  note{message.Data1}  velocity 0 — equivalente NoteOff";
                case RawMidiMessageKind.NoteOn:
                    return $"NoteOn  {channel}  note{message.Data1}  velocity{message.Value}";
                case RawMidiMessageKind.NoteOff:
                    return $"NoteOff {channel}  note{message.Data1}  velocity{message.Value}";
                case RawMidiMessageKind.ControlChange:
                    return $"CC      {channel}  cc{message.Data1}  value{message.Value}";
                case RawMidiMessageKind.PolyAftertouch:
                    return $"PolyAT  {channel}  note{message.Data1}  pressure{message.Value}";
                case RawMidiMessageKind.ChannelAftertouch:
                    return $"ChanAT  {channel}  pressure{message.Value}";
                case RawMidiMessageKind.PitchBend:
                    return $"Pitch   {channel}  value{message.Value}";
                case RawMidiMessageKind.ProgramChange:
                    return $"Program {channel}  program{message.Data1}";
                default: return message.Kind + " " + channel;
            }
        }
    }
}
