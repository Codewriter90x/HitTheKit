using System;
using HitTheKit.Unity.Devices;

namespace HitTheKit.Unity.DeviceSetup
{
    public sealed class DeviceSetupPresenter : IDisposable
    {
        private readonly DeviceSetupFlow flow;
        private readonly DeviceSetupView view;
        private readonly IGuidedMidiCaptureSource capture;
        private readonly ISimulatedGuidedMidiCaptureControl simulation;
        private readonly ILocalizedTextProvider text;
        private readonly Action<string> selectedDeviceSaved;

        public DeviceSetupPresenter(
            DeviceSetupFlow flow,
            DeviceSetupView view,
            IGuidedMidiCaptureSource capture,
            ISimulatedGuidedMidiCaptureControl simulation,
            ILocalizedTextProvider text,
            Action<string> selectedDeviceSaved = null)
        {
            this.flow = flow ?? throw new ArgumentNullException(nameof(flow));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.capture = capture ?? throw new ArgumentNullException(nameof(capture));
            this.simulation = simulation;
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.selectedDeviceSaved = selectedDeviceSaved;
        }

        public DeviceSetupTransitionResult LastTransition { get; private set; }

        public void Initialize()
        {
            view.StartRequested += Start;
            view.RefreshRequested += Refresh;
            view.DeviceSelected += SelectDevice;
            view.ProfileSelected += SelectProfile;
            view.ConfigureFromScratchRequested += ConfigureFromScratch;
            view.PresetSelected += SelectPreset;
            view.CustomSetupSelected += SelectCustomSetup;
            view.BeginWizardRequested += BeginWizard;
            view.SimulateRequested += SimulateCurrent;
            view.AcceptRequested += Accept;
            view.RetryRequested += Retry;
            view.SkipRequested += Skip;
            view.BackRequested += Back;
            view.SaveDraftRequested += SaveDraft;
            view.ResolveConflictRequested += Retry;
            view.KeepUnresolvedRequested += KeepUnresolved;
            view.TestRequested += Test;
            view.CompleteRequested += Complete;
            view.LanguageChanged += ChangeLanguage;
            capture.MessageReceived += OnMessage;
            capture.ConnectionChanged += OnConnectionChanged;
            view.Relocalize();
            Render();
        }

        public void Dispose()
        {
            view.StartRequested -= Start;
            view.RefreshRequested -= Refresh;
            view.DeviceSelected -= SelectDevice;
            view.ProfileSelected -= SelectProfile;
            view.ConfigureFromScratchRequested -= ConfigureFromScratch;
            view.PresetSelected -= SelectPreset;
            view.CustomSetupSelected -= SelectCustomSetup;
            view.BeginWizardRequested -= BeginWizard;
            view.SimulateRequested -= SimulateCurrent;
            view.AcceptRequested -= Accept;
            view.RetryRequested -= Retry;
            view.SkipRequested -= Skip;
            view.BackRequested -= Back;
            view.SaveDraftRequested -= SaveDraft;
            view.ResolveConflictRequested -= Retry;
            view.KeepUnresolvedRequested -= KeepUnresolved;
            view.TestRequested -= Test;
            view.CompleteRequested -= Complete;
            view.LanguageChanged -= ChangeLanguage;
            capture.MessageReceived -= OnMessage;
            capture.ConnectionChanged -= OnConnectionChanged;
            capture.Stop();
        }

        public void Start() => Apply(flow.Start());
        public void Refresh() => Apply(flow.RefreshDevices());
        public void SelectDevice(string id)
        {
            DeviceSetupTransitionResult result = flow.SelectDevice(id);
            if (result.Succeeded)
            {
                capture.SelectDevice(id);
                selectedDeviceSaved?.Invoke(id);
            }
            Apply(result);
        }
        public void SelectProfile(string id) => Apply(flow.SelectProfile(id));
        public void ConfigureFromScratch() => Apply(flow.ConfigureFromScratch());
        public void SelectPreset(string id) => Apply(flow.ChoosePreset(id));
        public void SelectCustomSetup(KitSetupDefinition setup) => Apply(flow.ChooseCustomSetup(setup));

        public void BeginWizard()
        {
            Apply(flow.BeginGuidedMapping());
            StartCaptureForCurrentStep();
        }

        public void SimulateCurrent()
        {
            if (simulation == null) return;
            if (flow.State == DeviceSetupState.TestKit && !capture.IsCapturing) capture.Start("test-kit");
            if (flow.State == DeviceSetupState.GuidedMapping && !capture.IsCapturing) StartCaptureForCurrentStep();
            if (flow.State == DeviceSetupState.GuidedMapping && !simulation.HasDataFor(flow.Snapshot.CurrentStep?.Id))
            {
                capture.Stop();
                LastTransition = flow.SetSimulationUnsupported(flow.Snapshot.CurrentStep?.Id);
                Render();
                return;
            }
            simulation.EmitAll();
            Render();
        }

        public void Accept()
        {
            capture.Stop();
            Apply(flow.AcceptCurrentCapture());
            StartCaptureForCurrentStep();
        }

        public void Retry()
        {
            capture.Stop();
            Apply(flow.RetryCurrentStep());
            StartCaptureForCurrentStep();
        }

        public void Skip()
        {
            capture.Stop();
            Apply(flow.SkipCurrentStep());
            StartCaptureForCurrentStep();
        }

        public void Back()
        {
            capture.Stop();
            Apply(flow.Back());
            StartCaptureForCurrentStep();
        }

        public void SaveDraft() => Apply(flow.SaveDraft());
        public void KeepUnresolved() => Apply(flow.KeepConflictUnresolved());
        public void Test() => Apply(flow.TestConfiguration());
        public void Complete() => Apply(flow.Complete());

        public void ChangeLanguage(DeviceSetupLanguage language)
        {
            text.Language = language;
            view.Relocalize();
            Render();
        }

        private void OnMessage(RawMidiMessage message)
        {
            LastTransition = flow.ProcessCapturedMessage(message);
            Render();
        }

        private void OnConnectionChanged(DeviceConnectionState state)
        {
            if (state == DeviceConnectionState.Connected && flow.Snapshot.Feedback != CaptureFeedbackState.Disconnected)
            {
                Render();
                return;
            }
            LastTransition = state == DeviceConnectionState.Disconnected ? flow.SetDisconnected() : flow.ResumeConnection();
            Render();
            if (state == DeviceConnectionState.Connected && LastTransition.Succeeded && !capture.IsCapturing) StartCaptureForCurrentStep();
        }

        private void StartCaptureForCurrentStep()
        {
            if (flow.State == DeviceSetupState.GuidedMapping &&
                flow.Snapshot.CurrentStep != null &&
                flow.Snapshot.Feedback != CaptureFeedbackState.Disconnected &&
                !capture.IsCapturing)
                capture.Start(flow.Snapshot.CurrentStep.Id);
        }

        private void Apply(DeviceSetupTransitionResult result)
        {
            LastTransition = result;
            Render();
        }

        private void Render() => view.Render(flow.Snapshot);
    }
}
