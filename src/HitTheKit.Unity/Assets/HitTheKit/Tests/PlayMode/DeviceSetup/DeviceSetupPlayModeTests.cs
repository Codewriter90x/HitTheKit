using System.Collections;
using System.Linq;
using HitTheKit.Unity.DeviceSetup;
using HitTheKit.Unity.Devices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Tests
{
    public sealed class DeviceSetupPlayModeTests
    {
        private const string SceneName = "DeviceSetupPrototype";

        [UnityTest]
        public IEnumerator Scene_loads_visible_ui_and_unique_foundation_components()
        {
            yield return LoadScene(simulated: false);
            Assert.That(Object.FindObjectsByType<DeviceSetupController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<SimulatedBackendRoot>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(GameObject.Find("Main Camera"), Is.Not.Null);
            Assert.That(GameObject.Find("EventSystem"), Is.Not.Null);
            UIDocument document = Object.FindFirstObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q("screen-welcome").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(document.rootVisualElement.Q<Button>("start-button").focusable, Is.True);
            DeviceSetupController controller = Object.FindFirstObjectByType<DeviceSetupController>();
            Assert.That(controller.InputBackend, Is.EqualTo(DeviceSetupInputBackend.CoreMidiMacOS));
            Assert.That(document.rootVisualElement.Q<Button>("simulate-button").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Hampback_candidate_starts_guided_flow_and_language_switches()
        {
            DeviceSetupController controller = null;
            yield return LoadScene(value => controller = value);
            controller.Presenter.Start();
            Assert.That(controller.View.Root.Q<Label>("device-count").text, Is.EqualTo("3"));
            Assert.That(controller.View.Root.Query<Button>(className: "device-card").ToList(), Has.Count.EqualTo(3));
            Assert.That(controller.View.Root.Q<VisualElement>(className: "soundcheck-kit-visual"), Is.Not.Null);
            controller.Presenter.SelectDevice("device.hampback");
            Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.ProfileSelection));
            Assert.That(controller.Flow.Snapshot.SelectedProfile, Is.Null);
            controller.Presenter.SelectProfile("dream-edrum-hampback-candidate-001");
            controller.Presenter.SelectPreset("minimal");
            Assert.That(controller.View.Root.Q("preset-card-minimal").ClassListContains("preset-card--selected"), Is.True);
            Assert.That(controller.View.Root.Q<Label>("preset-minimal-copy").text, Does.Contain("Grancassa"));
            controller.Presenter.BeginWizard();
            Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.GuidedMapping));
            Assert.That(controller.Flow.Snapshot.SelectedProfile.ProductionReady, Is.False);
            Assert.That(controller.View.Diagram.HighlightedPiece, Is.EqualTo("kick"));
            Assert.That(controller.Flow.Snapshot.CurrentCandidateMapping.Data1, Is.EqualTo(36));
            Assert.That(controller.Flow.Snapshot.SelectedProfile.ReviewIssues.Any(issue =>
                issue.ElementId == "hihat.continuous" && issue.Kind == KitMappingReviewIssueKind.Insufficient), Is.True);
            Assert.That(controller.Flow.Snapshot.ReviewIssues.Any(issue =>
                issue.ElementId == "hihat.continuous"), Is.False);

            controller.Presenter.SaveDraft();
            MidiMappingEntry kick = controller.Flow.Snapshot.Configuration.Mappings
                .Single(mapping => mapping.ElementId == "kick.default");
            Assert.That(kick.Trigger.Data1, Is.EqualTo(36));
            Assert.That(kick.VerificationState, Is.EqualTo(MidiMappingVerificationState.RequiresConfirmation));
            Assert.That(controller.Flow.Snapshot.Configuration.IsComplete, Is.False);
            controller.Presenter.Test();
            Assert.That(controller.Presenter.LastTransition.Succeeded, Is.False);
            Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.ConfigurationReview));

            controller.Presenter.ChangeLanguage(DeviceSetupLanguage.English);
            Assert.That(controller.View.VisibleTitle, Does.StartWith("Configure"));
            controller.Presenter.ChangeLanguage(DeviceSetupLanguage.Italian);
            Assert.That(controller.View.VisibleTitle, Does.StartWith("Configura"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Simulated_capture_updates_monitor_and_completes_test_kit_highlight()
        {
            DeviceSetupController controller = null;
            yield return LoadScene(value => controller = value);
            controller.Presenter.Start();
            controller.Presenter.SelectDevice("device.unknown");
            controller.Presenter.SelectPreset("minimal");
            controller.Presenter.BeginWizard();

            for (int index = 0; index < 4; index++)
            {
                controller.Presenter.SimulateCurrent();
                Assert.That(controller.Flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.ReadyToConfirm));
                Assert.That(controller.Flow.Snapshot.EventMonitor, Is.Not.Empty);
                controller.Presenter.Accept();
            }

            Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.ConfigurationReview));
            controller.Presenter.Test();
            Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.TestKit));
            controller.Presenter.SimulateCurrent();
            Assert.That(controller.Flow.Snapshot.LastTestStatus, Is.EqualTo(MidiKitMappingStatus.Mapped));
            Assert.That(controller.View.Diagram.HighlightedPiece, Is.EqualTo("kick"));
            Assert.That(controller.View.Root.Q<Label>("test-status").text, Does.Contain("velocity"));
            Assert.That(controller.View.Root.Q("test-event-list").childCount, Is.GreaterThan(0));
            controller.Presenter.Complete();
            Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.Completed));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Conflict_review_retry_and_draft_store_are_observable_without_hardware()
        {
            DeviceSetupController controller = null;
            yield return LoadScene(value => controller = value);
            controller.Presenter.Start();
            controller.Presenter.SelectDevice("device.unknown");
            controller.Presenter.SelectPreset("minimal");
            controller.Presenter.BeginWizard();

            controller.Flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 36, 80));
            controller.Flow.ProcessCapturedMessage(RawMidiMessage.NoteOn(9, 38, 90));
            controller.View.Render(controller.Flow.Snapshot);
            Assert.That(controller.Flow.Snapshot.Conflicts, Is.Not.Empty);
            controller.Presenter.Retry();
            Assert.That(controller.Flow.Snapshot.Conflicts, Is.Empty);
            controller.Presenter.SimulateCurrent();
            controller.Presenter.Accept();
            controller.Presenter.SaveDraft();
            Assert.That(controller.Store.List(), Has.Count.EqualTo(1));
            Assert.That(controller.Store.List()[0].IsComplete, Is.False);
        }

        [UnityTest]
        public IEnumerator Disconnect_reconnect_discards_pending_capture_and_restarts_once()
        {
            DeviceSetupController controller = null;
            yield return LoadScene(value => controller = value);
            controller.Presenter.Start();
            controller.Presenter.SelectDevice("device.unknown");
            controller.Presenter.SelectPreset("minimal");
            controller.Presenter.BeginWizard();
            controller.Presenter.SimulateCurrent();
            Assert.That(controller.Flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.ReadyToConfirm));

            controller.CaptureSource.Disconnect();
            Assert.That(controller.Flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Disconnected));
            Assert.That(controller.Flow.Snapshot.CapturedEventCount, Is.Zero);
            Assert.That(controller.Presenter.LastTransition.Succeeded, Is.True);
            controller.Presenter.Accept();
            Assert.That(controller.Presenter.LastTransition.Succeeded, Is.False);

            controller.CaptureSource.Reconnect();
            Assert.That(controller.CaptureSource.IsCapturing, Is.True);
            controller.Presenter.SimulateCurrent();
            Assert.That(controller.Flow.Snapshot.CapturedEventCount, Is.EqualTo(5));
            Assert.That(controller.Flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.ReadyToConfirm));
        }

        [UnityTest]
        public IEnumerator Unknown_simulation_data_keeps_ui_stable_and_accept_disabled()
        {
            DeviceSetupController controller = null;
            yield return LoadScene(value => controller = value);
            controller.Presenter.Start();
            controller.Presenter.SelectDevice("device.unknown");
            controller.Presenter.SelectPreset("minimal");
            controller.Presenter.BeginWizard();

            controller.CaptureSource.Stop();
            controller.CaptureSource.Start("future-unknown-step");
            Assert.That(controller.CaptureSource.HasDataFor("future-unknown-step"), Is.False);
            Assert.That(controller.CaptureSource.EmitAll(), Is.Zero);
            Assert.That(controller.Flow.Snapshot.EventMonitor, Is.Empty);

            Assert.That(controller.Flow.SetSimulationUnsupported(controller.Flow.Snapshot.CurrentStep.Id).Succeeded, Is.True);
            controller.View.Render(controller.Flow.Snapshot);
            Assert.That(controller.Flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Unsupported));
            controller.Presenter.Accept();
            Assert.That(controller.Presenter.LastTransition.Succeeded, Is.False);
            Assert.That(controller.Flow.Snapshot.CurrentStepIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CoreMidi_boundary_fake_drives_real_scene_flow_and_cleans_up()
        {
            DeviceSetupController controller = null;
            yield return LoadScene(value => controller = value, simulated: false);
            var capture = new FakeCoreMidiCapture();
            controller.ReplaceBackendForTests(new FakeCoreMidiDiscovery(), capture);
            controller.Presenter.Start();
            controller.Presenter.SelectDevice("coremidi.4242");
            controller.Presenter.SelectPreset("minimal");
            controller.Presenter.BeginWizard();
            Assert.That(capture.StartCalls, Is.EqualTo(1));
            Assert.That(capture.ConnectionState, Is.EqualTo(DeviceConnectionState.Connected));
            capture.Emit(RawMidiMessage.NoteOn(9, 36, 84, 1.0));
            capture.Emit(RawMidiMessage.NoteOn(9, 36, 90, 1.1));
            capture.Emit(RawMidiMessage.NoteOn(9, 36, 96, 1.2));
            capture.Emit(RawMidiMessage.NoteOn(9, 36, 102, 1.3));
            Assert.That(controller.Flow.Snapshot.CapturedEventCount, Is.EqualTo(4));
            Assert.That(controller.View.Root.Q<Button>("accept-button").enabledSelf, Is.False);
            capture.Emit(RawMidiMessage.NoteOn(9, 36, 108, 1.4));
            Assert.That(controller.Flow.Snapshot.EventMonitor, Has.Count.EqualTo(5));
            Assert.That(controller.Flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.ReadyToConfirm));
            Assert.That(controller.View.Root.Q("event-list").childCount, Is.EqualTo(5));
            Assert.That(controller.View.Root.Q<ProgressBar>("hit-progress").value, Is.EqualTo(5));
            Assert.That(controller.View.Root.Q<Button>("accept-button").enabledSelf, Is.True);
            Assert.That(controller.View.Root.Q<Button>("accept-button").text, Does.Contain("prosegui"));
            Assert.That(capture.IsCapturing, Is.True);
            capture.Emit(RawMidiMessage.NoteOn(9, 36, 112, 1.45));
            Assert.That(controller.Flow.Snapshot.EventMonitor, Has.Count.EqualTo(5));

            capture.Disconnect();
            Assert.That(controller.Flow.Snapshot.Feedback, Is.EqualTo(CaptureFeedbackState.Disconnected));
            capture.Reconnect();
            Assert.That(capture.IsCapturing, Is.True);
            capture.Emit(RawMidiMessage.NoteOn(9, 36, 90, 1.5));
            Assert.That(controller.Flow.Snapshot.CapturedEventCount, Is.EqualTo(1));

            var simulatedCapture = new SimulatedGuidedMidiCaptureSource();
            controller.ReplaceBackendForTests(
                new SimulatedDrumDeviceDiscovery(),
                simulatedCapture,
                simulatedCapture);
            Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.Welcome));
            Assert.That(capture.StopCalls, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Generated_CoreMidi_plugin_enumerates_zero_or_more_devices_in_real_scene()
        {
            DeviceSetupController controller = null;
            yield return LoadScene(value => controller = value, simulated: false);
            var api = new CoreMidiNativeApi();
            if (!api.IsAvailable)
            {
                Assert.That(api.AvailabilityMessage, Is.Not.Empty);
                yield break;
            }
            using (var session = new CoreMidiNativeSession(api))
            using (var capture = new CoreMidiGuidedMidiCaptureSource(session))
            {
                controller.ReplaceBackendForTests(
                    new CoreMidiDrumDeviceDiscovery(session, DeviceSetupProfileCatalog.ForCoreMidiDevice),
                    capture);
                controller.Presenter.Start();
                Assert.That(controller.Flow.State, Is.EqualTo(DeviceSetupState.DeviceSelection));
                Assert.That(controller.Flow.Snapshot.Discovery.State, Is.EqualTo(DeviceDiscoveryState.Ready));
                Assert.That(controller.Flow.Snapshot.Discovery.Devices.Count, Is.GreaterThanOrEqualTo(0));
            }
            LogAssert.NoUnexpectedReceived();
        }

        private sealed class FakeCoreMidiDiscovery : IDrumDeviceDiscovery
        {
            public DeviceDiscoverySnapshot Refresh() => new DeviceDiscoverySnapshot(
                DeviceDiscoveryState.Ready,
                new[]
                {
                    new DrumDeviceDescriptor("coremidi.4242", "USB Drum", "Synthetic", "USB Drum", DeviceConnectionState.Connected,
                        System.Array.Empty<DeviceProfileOption>())
                });
        }

        private sealed class FakeCoreMidiCapture : IGuidedMidiCaptureSource
        {
            public event System.Action<RawMidiMessage> MessageReceived;
            public event System.Action<DeviceConnectionState> ConnectionChanged;
            public DeviceConnectionState ConnectionState { get; private set; } = DeviceConnectionState.Disconnected;
            public bool IsCapturing { get; private set; }
            public string ActiveStepId { get; private set; }
            public int StartCalls { get; private set; }
            public int StopCalls { get; private set; }
            public void SelectDevice(string deviceId) { Assert.That(deviceId, Is.EqualTo("coremidi.4242")); }
            public void Start(string stepId)
            {
                StartCalls++;
                ActiveStepId = stepId;
                IsCapturing = true;
                if (ConnectionState == DeviceConnectionState.Connected) return;
                ConnectionState = DeviceConnectionState.Connected;
                ConnectionChanged?.Invoke(ConnectionState);
            }
            public void Stop() { StopCalls++; ActiveStepId = null; IsCapturing = false; }
            public void Emit(RawMidiMessage message) { if (IsCapturing) MessageReceived?.Invoke(message); }
            public void Disconnect() { IsCapturing = false; ConnectionState = DeviceConnectionState.Disconnected; ConnectionChanged?.Invoke(ConnectionState); }
            public void Reconnect() { ConnectionState = DeviceConnectionState.Connected; ConnectionChanged?.Invoke(ConnectionState); }
        }

        private static IEnumerator LoadScene(
            System.Action<DeviceSetupController> ready = null,
            bool simulated = true)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!operation.isDone && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(operation.isDone, Is.True, "Device Setup scene timed out while loading.");
            yield return null;
            DeviceSetupController controller = Object.FindFirstObjectByType<DeviceSetupController>();
            Assert.That(controller, Is.Not.Null);
            if (simulated)
            {
                var capture = new SimulatedGuidedMidiCaptureSource();
                controller.ReplaceBackendForTests(new SimulatedDrumDeviceDiscovery(), capture, capture);
            }
            Assert.That(controller.Flow, Is.Not.Null);
            ready?.Invoke(controller);
        }
    }
}
