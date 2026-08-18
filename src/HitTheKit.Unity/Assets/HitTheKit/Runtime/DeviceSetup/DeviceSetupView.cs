using System;
using System.Collections.Generic;
using HitTheKit.Unity.Devices;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.DeviceSetup
{
    public sealed class DrumKitDiagram
    {
        private readonly VisualElement root;
        private readonly Dictionary<string, VisualElement> pieces = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private VisualElement activePiece;

        public DrumKitDiagram(VisualElement root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            Add("hihat", "Hi-Hat", 3, 34, 23, 15);
            Add("hihatPedal", "Hi-Hat Pedal", 8, 68, 13, 25);
            Add("crash1", "Crash 1", 15, 8, 22, 16);
            Add("crash2", "Crash 2", 67, 8, 27, 20);
            Add("ride", "Ride", 67, 8, 27, 20);
            Add("tom1", "Tom 1", 33, 29, 18, 17);
            Add("tom2", "Tom 2", 51, 29, 18, 17);
            Add("snare", "Snare", 22, 49, 22, 18);
            Add("floortom", "Floor Tom", 68, 50, 22, 22);
            Add("kick", "Kick", 41, 53, 20, 32);
        }

        public string HighlightedPiece { get; private set; }

        public void Highlight(string elementId)
        {
            HighlightedPiece = PieceKey(elementId);
            foreach (KeyValuePair<string, VisualElement> pair in pieces)
            {
                pair.Value.EnableInClassList("drum-piece--active", pair.Key == HighlightedPiece);
                pair.Value.EnableInClassList("drum-piece--ready", false);
            }
            activePiece = HighlightedPiece != null && pieces.TryGetValue(HighlightedPiece, out VisualElement piece) ? piece : null;
        }

        public void SetCaptureReady(bool ready)
        {
            activePiece?.EnableInClassList("drum-piece--ready", ready);
        }

        private void Add(string id, string label, float left, float top, float width, float height)
        {
            var piece = new VisualElement { name = "kit-piece-" + id };
            piece.AddToClassList("drum-piece");
            piece.style.left = new Length(left, LengthUnit.Percent);
            piece.style.top = new Length(top, LengthUnit.Percent);
            piece.style.width = new Length(width, LengthUnit.Percent);
            piece.style.height = new Length(height, LengthUnit.Percent);
            piece.Add(new Label(label));
            root.Add(piece);
            pieces.Add(id, piece);
        }

        private static string PieceKey(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId)) return null;
            if (elementId.StartsWith("hihat.pedal", StringComparison.Ordinal) ||
                elementId.StartsWith("hihat.continuous", StringComparison.Ordinal)) return "hihatPedal";
            if (elementId.StartsWith("hihat", StringComparison.Ordinal)) return "hihat";
            if (elementId.StartsWith("snare", StringComparison.Ordinal)) return "snare";
            if (elementId.StartsWith("floortom", StringComparison.Ordinal)) return "floortom";
            if (elementId.StartsWith("crash1", StringComparison.Ordinal)) return "crash1";
            if (elementId.StartsWith("crash2", StringComparison.Ordinal)) return "crash2";
            if (elementId.StartsWith("ride", StringComparison.Ordinal)) return "ride";
            if (elementId.StartsWith("tom1", StringComparison.Ordinal)) return "tom1";
            if (elementId.StartsWith("tom2", StringComparison.Ordinal)) return "tom2";
            return elementId.StartsWith("kick", StringComparison.Ordinal) ? "kick" : null;
        }
    }

    public sealed class DeviceSetupView
    {
        private readonly VisualElement root;
        private readonly ILocalizedTextProvider text;
        private readonly bool simulationEnabled;
        private readonly Func<DeviceConnectionState> connectionState;
        private readonly List<VisualElement> screens;
        private readonly Label title;
        private readonly Label message;
        private readonly Label guidedTitle;
        private readonly Label guidedTarget;
        private readonly Label guidedInstruction;
        private readonly Label guidedProgress;
        private readonly Label guidedStatus;
        private readonly Label guidedHelp;
        private readonly Label candidateHint;
        private readonly Label testStatus;
        private readonly Label connectionStatus;
        private readonly VisualElement deviceList;
        private readonly VisualElement profileList;
        private readonly VisualElement conflictList;
        private readonly VisualElement reviewList;
        private readonly VisualElement eventList;
        private readonly VisualElement testEventList;
        private readonly DropdownField language;
        private readonly ProgressBar hitProgress;
        private readonly Button acceptButton;
        private readonly DrumKitDiagram diagram;
        private readonly DrumKitDiagram testDiagram;

        public DeviceSetupView(
            VisualElement root,
            ILocalizedTextProvider text,
            bool simulationEnabled,
            Func<DeviceConnectionState> connectionState)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.simulationEnabled = simulationEnabled;
            this.connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
            title = Required<Label>("app-title");
            message = Required<Label>("flow-message");
            guidedTitle = Required<Label>("guided-title");
            guidedTarget = Required<Label>("guided-target");
            guidedInstruction = Required<Label>("guided-instruction");
            guidedProgress = Required<Label>("guided-progress");
            guidedStatus = Required<Label>("guided-status");
            guidedHelp = Required<Label>("guided-help");
            candidateHint = Required<Label>("candidate-hint");
            testStatus = Required<Label>("test-status");
            connectionStatus = Required<Label>("connection-status");
            deviceList = Required<VisualElement>("device-list");
            profileList = Required<VisualElement>("profile-list");
            conflictList = Required<VisualElement>("conflict-list");
            reviewList = Required<VisualElement>("review-list");
            eventList = Required<VisualElement>("event-list");
            testEventList = Required<VisualElement>("test-event-list");
            language = Required<DropdownField>("language-dropdown");
            hitProgress = Required<ProgressBar>("hit-progress");
            acceptButton = Required<Button>("accept-button");
            language.choices = new List<string> { "Italiano", "English" };
            language.index = text.Language == DeviceSetupLanguage.Italian ? 0 : 1;
            diagram = new DrumKitDiagram(Required<VisualElement>("diagram-host"));
            testDiagram = new DrumKitDiagram(Required<VisualElement>("test-diagram-host"));
            screens = root.Query<VisualElement>(className: "screen").ToList();
            WireStaticActions();
            Required<Button>("simulate-button").style.display = simulationEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            Required<Button>("test-simulate-button").style.display = simulationEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public event Action StartRequested;
        public event Action RefreshRequested;
        public event Action<string> DeviceSelected;
        public event Action<string> ProfileSelected;
        public event Action ConfigureFromScratchRequested;
        public event Action<string> PresetSelected;
        public event Action<KitSetupDefinition> CustomSetupSelected;
        public event Action BeginWizardRequested;
        public event Action SimulateRequested;
        public event Action AcceptRequested;
        public event Action RetryRequested;
        public event Action SkipRequested;
        public event Action BackRequested;
        public event Action SaveDraftRequested;
        public event Action ResolveConflictRequested;
        public event Action KeepUnresolvedRequested;
        public event Action TestRequested;
        public event Action CompleteRequested;
        public event Action<DeviceSetupLanguage> LanguageChanged;
        public event Action MainMenuRequested;

        public VisualElement Root => root;
        public DrumKitDiagram Diagram => diagram;
        public string VisibleTitle => title.text;

        public void Render(DeviceSetupSnapshot snapshot)
        {
            title.text = text.Get("deviceSetup.title");
            message.text = snapshot.State == DeviceSetupState.GuidedMapping ? string.Empty : snapshot.Message ?? string.Empty;
            foreach (VisualElement screen in screens) screen.style.display = DisplayStyle.None;
            Required<VisualElement>(ScreenName(snapshot.State)).style.display = DisplayStyle.Flex;
            RenderDevices(snapshot);
            RenderProfiles(snapshot);
            RenderKitStructure(snapshot);
            RenderGuided(snapshot);
            RenderConflicts(snapshot);
            RenderReview(snapshot);
            RenderEvents(snapshot.EventMonitor);
            RenderTestStatus(snapshot);
            diagram.Highlight(snapshot.State == DeviceSetupState.TestKit ? snapshot.HighlightedElementId : snapshot.CurrentStep?.TargetElement.Id);
            diagram.SetCaptureReady(snapshot.Feedback == CaptureFeedbackState.ReadyToConfirm);
            testDiagram.Highlight(snapshot.HighlightedElementId);
            bool disconnected = snapshot.Feedback == CaptureFeedbackState.Disconnected ||
                (!simulationEnabled && connectionState() == DeviceConnectionState.Disconnected);
            connectionStatus.text = disconnected
                ? text.Get("deviceSetup.midiWaiting")
                : simulationEnabled
                    ? text.Get("deviceSetup.simulationConnected")
                    : text.Get("deviceSetup.midiConnected");
            connectionStatus.EnableInClassList("status--error", disconnected);
        }

        public void Relocalize()
        {
            Required<Label>("welcome-heading").text = text.Get("deviceSetup.title");
            Required<Label>("welcome-subtitle").text = text.Get("deviceSetup.subtitle");
            Required<Label>("welcome-privacy").text = text.Get("deviceSetup.privacy");
            SetButton("start-button", "deviceSetup.start");
            SetButton("back-game-button", "deviceSetup.backToGame");
            SetButton("main-menu-button", "deviceSetup.backToGame");
            SetButton("refresh-button", "deviceSetup.refresh");
            SetButton("configure-new-button", "deviceSetup.configureNewKit");
            SetButton("begin-wizard-button", "deviceSetup.beginWizard");
            SetButton("simulate-button", "deviceSetup.simulate");
            SetButton("accept-button", "deviceSetup.continueWhenReady");
            SetButton("retry-button", "deviceSetup.retry");
            SetButton("skip-button", "deviceSetup.skip");
            SetButton("guided-back-button", "deviceSetup.back");
            SetButton("save-draft-button", "deviceSetup.saveDraft");
            SetButton("conflict-retry-button", "deviceSetup.retry");
            SetButton("keep-unresolved-button", "deviceSetup.back");
            SetButton("test-button", "deviceSetup.continueTest");
            SetButton("complete-button", "deviceSetup.finish");
            Required<Label>("device-heading").text = text.Get("deviceSetup.selectDevice");
            Required<Label>("profile-heading").text = text.Get("deviceSetup.profile");
            Required<Label>("structure-heading").text = text.Get("deviceSetup.structure");
            Required<Label>("structure-copy").text = text.Get("deviceSetup.structureCopy");
            Required<Label>("preset-minimal-title").text = text.Get("deviceSetup.presetMinimalTitle");
            Required<Label>("preset-minimal-copy").text = text.Get("deviceSetup.presetMinimalCopy");
            Required<Label>("preset-standard-title").text = text.Get("deviceSetup.presetStandardTitle");
            Required<Label>("preset-standard-copy").text = text.Get("deviceSetup.presetStandardCopy");
            Required<Label>("preset-extended-title").text = text.Get("deviceSetup.presetExtendedTitle");
            Required<Label>("preset-extended-copy").text = text.Get("deviceSetup.presetExtendedCopy");
            SetButton("preset-minimal", "deviceSetup.choosePreset");
            SetButton("preset-standard", "deviceSetup.choosePreset");
            SetButton("preset-extended", "deviceSetup.choosePreset");
            guidedTarget.text = text.Get("deviceSetup.hitNow");
            guidedHelp.text = text.Get("deviceSetup.guidedHelp");
            Required<Label>("conflict-heading").text = text.Get("deviceSetup.conflicts");
            Required<Label>("review-heading").text = text.Get("deviceSetup.review");
            Required<Label>("test-heading").text = text.Get("deviceSetup.testKit");
            Required<Label>("completed-heading").text = text.Get("deviceSetup.complete");
            Required<Label>("audio-routing-help").text = text.Get("deviceSetup.audioRouting");
        }

        private void WireStaticActions()
        {
            Required<Button>("start-button").clicked += () => StartRequested?.Invoke();
            Required<Button>("refresh-button").clicked += () => RefreshRequested?.Invoke();
            Required<Button>("configure-new-button").clicked += () => ConfigureFromScratchRequested?.Invoke();
            Required<Button>("preset-minimal").clicked += () => PresetSelected?.Invoke("minimal");
            Required<Button>("preset-standard").clicked += () => PresetSelected?.Invoke("standard");
            Required<Button>("preset-extended").clicked += () => PresetSelected?.Invoke("extended");
            Required<Button>("custom-apply-button").clicked += ApplyCustomSetup;
            Required<Button>("begin-wizard-button").clicked += () => BeginWizardRequested?.Invoke();
            Required<Button>("simulate-button").clicked += () => SimulateRequested?.Invoke();
            Required<Button>("test-simulate-button").clicked += () => SimulateRequested?.Invoke();
            Required<Button>("accept-button").clicked += () => AcceptRequested?.Invoke();
            Required<Button>("retry-button").clicked += () => RetryRequested?.Invoke();
            Required<Button>("skip-button").clicked += () => SkipRequested?.Invoke();
            Required<Button>("guided-back-button").clicked += () => BackRequested?.Invoke();
            Required<Button>("save-draft-button").clicked += () => SaveDraftRequested?.Invoke();
            Required<Button>("review-save-draft-button").clicked += () => SaveDraftRequested?.Invoke();
            Required<Button>("conflict-retry-button").clicked += () => ResolveConflictRequested?.Invoke();
            Required<Button>("keep-unresolved-button").clicked += () => KeepUnresolvedRequested?.Invoke();
            Required<Button>("test-button").clicked += () => TestRequested?.Invoke();
            Required<Button>("complete-button").clicked += () => CompleteRequested?.Invoke();
            Required<Button>("profile-back-button").clicked += () => BackRequested?.Invoke();
            Required<Button>("structure-back-button").clicked += () => BackRequested?.Invoke();
            Required<Button>("review-back-button").clicked += () => BackRequested?.Invoke();
            Required<Button>("back-game-button").clicked += () => MainMenuRequested?.Invoke();
            Required<Button>("main-menu-button").clicked += () => MainMenuRequested?.Invoke();
            language.RegisterValueChangedCallback(evt => LanguageChanged?.Invoke(evt.newValue == "English" ? DeviceSetupLanguage.English : DeviceSetupLanguage.Italian));
        }

        private void RenderDevices(DeviceSetupSnapshot snapshot)
        {
            deviceList.Clear();
            int profileCount = 0;
            foreach (DrumDeviceDescriptor device in snapshot.Discovery.Devices)
            {
                profileCount += device.Profiles.Count;
                var button = new Button(() => DeviceSelected?.Invoke(device.Id)) { name = "device-" + device.Id };
                button.AddToClassList("device-card");
                button.EnableInClassList("device-card--selected", snapshot.SelectedDevice != null && snapshot.SelectedDevice.Id == device.Id);
                var indicator = new VisualElement();
                indicator.AddToClassList("device-card-indicator");
                button.Add(indicator);
                var copy = new VisualElement();
                copy.AddToClassList("device-card-copy");
                var name = new Label(device.DisplayName);
                name.AddToClassList("device-card-name");
                copy.Add(name);
                var identity = new Label($"{device.Manufacturer}  ·  {device.PortName}");
                identity.AddToClassList("device-card-identity");
                copy.Add(identity);
                var connection = new Label($"{device.ConnectionState}  ·  {device.Profiles.Count} profile(s)");
                connection.AddToClassList("device-card-connection");
                copy.Add(connection);
                button.Add(copy);
                string profileState = device.Profiles.Count == 0
                    ? "NESSUN PROFILO"
                    : device.Profiles[0].Status.ToString().ToUpperInvariant();
                var badge = new Label(profileState);
                badge.AddToClassList("device-card-badge");
                badge.EnableInClassList("device-card-badge--warning", device.Profiles.Count == 0 ||
                    string.Equals(profileState, "CANDIDATE", StringComparison.Ordinal));
                button.Add(badge);
                button.SetEnabled(device.ConnectionState == DeviceConnectionState.Connected);
                deviceList.Add(button);
            }
            if (snapshot.Discovery.Devices.Count == 0)
                deviceList.Add(new Label(snapshot.Discovery.Message ?? text.Get("deviceSetup.noMidiDevices")));
            Required<Label>("device-count").text = snapshot.Discovery.Devices.Count.ToString();
            Required<Label>("device-profile-count").text = profileCount.ToString();
            Required<Label>("device-system-state").text = snapshot.Discovery.Devices.Count == 0 ? "IN ATTESA" : "PRONTO";
        }

        private void RenderProfiles(DeviceSetupSnapshot snapshot)
        {
            profileList.Clear();
            if (snapshot.SelectedDevice == null) return;
            foreach (DeviceProfileOption profile in snapshot.SelectedDevice.Profiles)
            {
                var card = new VisualElement();
                card.AddToClassList("profile-card");
                card.Add(new Label(profile.DisplayName) { name = "profile-name-" + profile.Id });
                card.Add(new Label($"Status: {profile.Status}\nOrigin: {profile.Origin}\n{profile.Reason}\nProduction-ready: {profile.ProductionReady}\nRequires confirmation: {profile.RequiresConfirmation}"));
                foreach (DeviceProfileMappingCandidate candidate in profile.CandidateMappings)
                    card.Add(new Label($"◌ {candidate.ElementId}: {candidate.TriggerLabel} · {candidate.Confidence} · confirmation required"));
                foreach (string unresolved in profile.UnresolvedMappings) card.Add(new Label("⚠ " + unresolved));
                var use = new Button(() => ProfileSelected?.Invoke(profile.Id)) { text = text.Get("deviceSetup.useKnownProfile") };
                use.name = "profile-use-" + profile.Id;
                use.SetEnabled(profile.CanUseAsStartingPoint);
                card.Add(use);
                profileList.Add(card);
            }
        }

        private void RenderKitStructure(DeviceSetupSnapshot snapshot)
        {
            string selectedId = snapshot.Setup?.Id;
            SetPresetState("minimal", string.Equals(selectedId, "setup.minimal-3-piece", StringComparison.Ordinal));
            SetPresetState("standard", string.Equals(selectedId, "setup.standard-5-piece", StringComparison.Ordinal));
            SetPresetState("extended", string.Equals(selectedId, "setup.extended-electronic", StringComparison.Ordinal));
            Required<Button>("begin-wizard-button").SetEnabled(snapshot.Setup != null);
        }

        private void RenderGuided(DeviceSetupSnapshot snapshot)
        {
            KitMappingWizardStep step = snapshot.CurrentStep;
            if (step == null)
            {
                guidedTitle.text = text.Get("deviceSetup.guidedMapping");
                guidedInstruction.text = string.Empty;
                guidedProgress.text = string.Empty;
                hitProgress.highValue = 1;
                hitProgress.value = 0;
                hitProgress.title = string.Empty;
            }
            else
            {
                string key = KeyFor(step.TargetElement.Id);
                guidedTitle.text = text.Get(key + ".title", step.TargetElement.DisplayName) + " / " + EnglishName(step.TargetElement.Id);
                guidedInstruction.text = text.Get(key + ".instruction", step.FallbackDisplayText);
                guidedProgress.text = $"{text.Get("deviceSetup.step")} {snapshot.CurrentStepIndex + 1}/{snapshot.TotalSteps} · " +
                    text.Get(step.Required ? "deviceSetup.required" : "deviceSetup.optional");
                hitProgress.highValue = step.CaptureCount;
                hitProgress.value = Math.Min(snapshot.CapturedEventCount, step.CaptureCount);
                hitProgress.title = string.Format(text.Get("deviceSetup.hitsProgress"), snapshot.CapturedEventCount, step.CaptureCount);
            }
            guidedStatus.text = CaptureStatus(snapshot);
            candidateHint.text = snapshot.CurrentCandidateMapping == null
                ? string.Empty
                : string.Format(text.Get("deviceSetup.candidateHint"), snapshot.CurrentCandidateMapping.TriggerLabel, snapshot.CurrentCandidateMapping.Confidence);
            guidedStatus.EnableInClassList(
                "status--error",
                snapshot.Feedback == CaptureFeedbackState.Conflict ||
                snapshot.Feedback == CaptureFeedbackState.Disconnected ||
                snapshot.Feedback == CaptureFeedbackState.Unsupported);
            Required<Button>("skip-button").SetEnabled(step != null && !step.Required);
            bool ready = snapshot.Feedback == CaptureFeedbackState.ReadyToConfirm;
            acceptButton.text = text.Get(ready ? "deviceSetup.confirmAndContinue" : "deviceSetup.continueWhenReady");
            acceptButton.SetEnabled(ready);
            acceptButton.EnableInClassList("capture-cta--ready", ready);
        }

        private string CaptureStatus(DeviceSetupSnapshot snapshot)
        {
            switch (snapshot.Feedback)
            {
                case CaptureFeedbackState.Waiting: return text.Get("deviceSetup.captureWaiting");
                case CaptureFeedbackState.Receiving:
                case CaptureFeedbackState.NeedsMoreSamples:
                    return string.Format(text.Get("deviceSetup.captureReceiving"), snapshot.CapturedEventCount, snapshot.CurrentStep?.CaptureCount ?? 0);
                case CaptureFeedbackState.ReadyToConfirm: return text.Get("deviceSetup.captureReady");
                case CaptureFeedbackState.Conflict: return text.Get("deviceSetup.captureConflict") + " " + snapshot.Message;
                case CaptureFeedbackState.Disconnected: return text.Get("deviceSetup.captureDisconnected");
                case CaptureFeedbackState.Unsupported: return text.Get("deviceSetup.captureUnsupported");
                case CaptureFeedbackState.Completed: return text.Get("deviceSetup.captureCompleted");
                default: return snapshot.Message ?? string.Empty;
            }
        }

        private void SetPresetState(string presetId, bool selected)
        {
            Required<VisualElement>("preset-card-" + presetId).EnableInClassList("preset-card--selected", selected);
            Required<Button>("preset-" + presetId).text = text.Get(selected ? "deviceSetup.presetSelected" : "deviceSetup.choosePreset");
        }

        private void RenderConflicts(DeviceSetupSnapshot snapshot)
        {
            conflictList.Clear();
            foreach (DeviceSetupConflict conflict in snapshot.Conflicts)
                conflictList.Add(new Label("⚠ " + conflict.StepId + "\n" + conflict.Message));
            if (snapshot.Conflicts.Count == 0) conflictList.Add(new Label("No unresolved conflicts."));
        }

        private void RenderReview(DeviceSetupSnapshot snapshot)
        {
            reviewList.Clear();
            if (snapshot.SelectedProfile != null)
            {
                reviewList.Add(new Label("Candidate evidence (confirmation required):"));
                foreach (DeviceProfileMappingCandidate candidate in snapshot.SelectedProfile.CandidateMappings)
                    reviewList.Add(new Label($"◌ {candidate.ElementId} · {candidate.TriggerLabel} · {candidate.Confidence} · Known profile candidate / confirmation required"));
                foreach (string unresolved in snapshot.SelectedProfile.UnresolvedMappings) reviewList.Add(new Label("⚠ " + unresolved + " — Unresolved"));
            }
            if (snapshot.Configuration != null)
            {
                foreach (var mapping in snapshot.Configuration.Mappings)
                {
                    string marker = mapping.VerificationState == MidiMappingVerificationState.Confirmed ? "✓" : "◌";
                    reviewList.Add(new Label($"{marker} {mapping.ElementId} · {mapping.Trigger.Kind} ch{(mapping.Trigger.Channel ?? -1) + 1} data {mapping.Trigger.Data1} · {mapping.Source} · {mapping.VerificationState}"));
                }
                foreach (KitMappingReviewIssue issue in snapshot.Configuration.ReviewIssues)
                    reviewList.Add(new Label($"⚠ {issue.ElementId} · {issue.Kind} · {issue.Description}"));
                foreach (string disabled in snapshot.Configuration.DisabledElementIds)
                    reviewList.Add(new Label("– " + disabled + " · Skipped"));
            }
        }

        private void RenderEvents(IReadOnlyList<string> events)
        {
            eventList.Clear();
            for (int index = Math.Max(0, events.Count - 10); index < events.Count; index++) eventList.Add(new Label(events[index]));
            if (events.Count == 0)
                eventList.Add(new Label(simulationEnabled
                    ? text.Get("deviceSetup.waitingSimulatedEvents")
                    : text.Get("deviceSetup.waitingMidiEvents")));
        }

        private void RenderTestStatus(DeviceSetupSnapshot snapshot)
        {
            testStatus.text = snapshot.State == DeviceSetupState.TestKit
                ? $"{snapshot.LastTestStatus?.ToString() ?? "Waiting"} · {snapshot.LastEvent ?? "No event"} · element {snapshot.HighlightedElementId ?? "none"}"
                : string.Empty;
            testEventList.Clear();
            for (int index = Math.Max(0, snapshot.EventMonitor.Count - 10); index < snapshot.EventMonitor.Count; index++)
                testEventList.Add(new Label(snapshot.EventMonitor[index]));
        }

        private T Required<T>(string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            return element ?? throw new InvalidOperationException($"Device Setup UI element '{name}' is missing.");
        }

        private void SetButton(string name, string key) => Required<Button>(name).text = text.Get(key);

        private void ApplyCustomSetup()
        {
            try
            {
                CustomSetupSelected?.Invoke(KitSetupDefinition.CreateCustom(
                    "setup.custom-ui",
                    "Custom UI kit",
                    true,
                    true,
                    Required<Toggle>("custom-snare-rim").value,
                    Required<IntegerField>("custom-toms").value,
                    Required<IntegerField>("custom-crashes").value,
                    Required<Toggle>("custom-ride").value,
                    Required<Toggle>("custom-ride-bell").value,
                    Required<Toggle>("custom-hihat").value,
                    false,
                    Required<Toggle>("custom-hihat-pedal").value,
                    Required<Toggle>("custom-chokes").value));
            }
            catch (ArgumentException exception)
            {
                message.text = exception.Message;
            }
        }

        private static string ScreenName(DeviceSetupState state) => "screen-" + state.ToString().ToLowerInvariant();

        private static string KeyFor(string elementId)
        {
            if (elementId == "kick.default") return "wizard.kick";
            if (elementId == "snare.head") return "wizard.snareHead";
            if (elementId == "snare.rim") return "wizard.snareRim";
            if (elementId == "ride.bow") return "wizard.rideBow";
            if (elementId == "ride.bell") return "wizard.rideBell";
            if (elementId == "hihat.closed") return "wizard.hihatClosed";
            if (elementId == "hihat.open") return "wizard.hihatOpen";
            if (elementId == "hihat.pedal") return "wizard.hihatPedal";
            return "wizard." + elementId.Replace('.', '-');
        }

        private static string EnglishName(string elementId)
        {
            if (elementId == "kick.default") return "Kick";
            if (elementId == "snare.head") return "Snare center";
            if (elementId == "snare.rim") return "Snare rim";
            if (elementId == "ride.bow") return "Ride bow";
            if (elementId == "ride.bell") return "Ride bell";
            if (elementId == "hihat.closed") return "Closed hi-hat";
            if (elementId == "hihat.open") return "Open hi-hat";
            if (elementId == "hihat.pedal") return "Hi-hat pedal";
            return elementId;
        }
    }
}
