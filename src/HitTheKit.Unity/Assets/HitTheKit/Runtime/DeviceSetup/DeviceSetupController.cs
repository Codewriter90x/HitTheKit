using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using HitTheKit.Unity.MainMenu;
using HitTheKit.Unity.Gameplay;

namespace HitTheKit.Unity.DeviceSetup
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class DeviceSetupController : MonoBehaviour
    {
        [SerializeField] private DeviceSetupInputBackend inputBackend = DeviceSetupInputBackend.CoreMidiMacOS;
        [SerializeField] private SimulatedCaptureScenario simulatedScenario = SimulatedCaptureScenario.HampbackCapture2;
        [SerializeField] private PanelSettings panelSettingsOverride;
        private DeviceSetupPresenter presenter;
        private CoreMidiNativeSession coreMidiSession;
        private bool isReturningToMenu;

        public DeviceSetupFlow Flow { get; private set; }
        public SimulatedDrumDeviceDiscovery Discovery { get; private set; }
        public SimulatedGuidedMidiCaptureSource CaptureSource { get; private set; }
        public IDrumDeviceDiscovery ActiveDiscovery { get; private set; }
        public IGuidedMidiCaptureSource ActiveCaptureSource { get; private set; }
        public CoreMidiDrumDeviceDiscovery CoreMidiDiscovery { get; private set; }
        public CoreMidiGuidedMidiCaptureSource CoreMidiCaptureSource { get; private set; }
        public DeviceSetupInputBackend InputBackend => inputBackend;
        public IUserKitConfigurationStore Store { get; private set; }
        public DeviceSetupView View { get; private set; }
        public ILocalizedTextProvider Localization { get; private set; }
        public DeviceSetupPresenter Presenter => presenter;

        private void OnEnable()
        {
            TryInitialize();
        }

        private void Start()
        {
            TryInitialize();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) ReturnToMainMenu();
            CoreMidiCaptureSource?.Poll();
            if (CoreMidiDiscovery != null && CoreMidiDiscovery.HasDevicesChanged && Flow?.State == DeviceSetupState.DeviceSelection)
                presenter?.Refresh();
        }

        private void TryInitialize()
        {
            if (presenter != null) return;
            UIDocument document = GetComponent<UIDocument>();
            if (document.panelSettings == null && panelSettingsOverride != null) document.panelSettings = panelSettingsOverride;
            if (document.rootVisualElement == null) return;
            PlayerPreferencesSnapshot preferences = PlayerPreferencesRuntime.Current.Snapshot;
            PlayerPreferencesRuntime.Current.ApplyAudio();
            document.rootVisualElement.EnableInClassList("device-setup--high-contrast", preferences.HighContrast);
            document.rootVisualElement.EnableInClassList("device-setup--reduced-motion", preferences.ReducedMotion);
            ISimulatedGuidedMidiCaptureControl simulation = null;
            if (inputBackend == DeviceSetupInputBackend.CoreMidiMacOS)
            {
                coreMidiSession = new CoreMidiNativeSession(new CoreMidiNativeApi());
                CoreMidiDiscovery = new CoreMidiDrumDeviceDiscovery(coreMidiSession, DeviceSetupProfileCatalog.ForCoreMidiDevice);
                CoreMidiCaptureSource = new CoreMidiGuidedMidiCaptureSource(coreMidiSession);
                ActiveDiscovery = CoreMidiDiscovery;
                ActiveCaptureSource = CoreMidiCaptureSource;
            }
            else
            {
                Discovery = new SimulatedDrumDeviceDiscovery();
                CaptureSource = new SimulatedGuidedMidiCaptureSource(simulatedScenario);
                ActiveDiscovery = Discovery;
                ActiveCaptureSource = CaptureSource;
                simulation = CaptureSource;
            }
            Store = DeviceSetupConfigurationRuntime.CreateDefaultStore();
            Localization = new DictionaryLocalizedTextProvider(DeviceSetupLanguage.Italian);
            Flow = new DeviceSetupFlow(ActiveDiscovery, Store);
            View = new DeviceSetupView(
                document.rootVisualElement,
                Localization,
                simulation != null,
                () => ActiveCaptureSource?.ConnectionState ?? DeviceConnectionState.Disconnected);
            View.MainMenuRequested += ReturnToMainMenu;
            presenter = new DeviceSetupPresenter(
                Flow,
                View,
                ActiveCaptureSource,
                simulation,
                Localization,
                inputBackend == DeviceSetupInputBackend.CoreMidiMacOS
                    ? PlayerPreferencesRuntime.Current.SelectMidiDevice
                    : (System.Action<string>)null);
            presenter.Initialize();
        }

        private void OnDisable()
        {
            DisposeBackend();
        }

#if UNITY_INCLUDE_TESTS
        public void ReplaceBackendForTests(
            IDrumDeviceDiscovery discovery,
            IGuidedMidiCaptureSource capture,
            ISimulatedGuidedMidiCaptureControl simulation = null)
        {
            DisposeBackend();
            ActiveDiscovery = discovery ?? throw new System.ArgumentNullException(nameof(discovery));
            ActiveCaptureSource = capture ?? throw new System.ArgumentNullException(nameof(capture));
            Discovery = discovery as SimulatedDrumDeviceDiscovery;
            CaptureSource = capture as SimulatedGuidedMidiCaptureSource;
            Store = new InMemoryUserKitConfigurationStore();
            Localization = new DictionaryLocalizedTextProvider(DeviceSetupLanguage.Italian);
            Flow = new DeviceSetupFlow(ActiveDiscovery, Store);
            UIDocument document = GetComponent<UIDocument>();
            View = new DeviceSetupView(
                document.rootVisualElement,
                Localization,
                simulation != null,
                () => ActiveCaptureSource?.ConnectionState ?? DeviceConnectionState.Disconnected);
            View.MainMenuRequested += ReturnToMainMenu;
            presenter = new DeviceSetupPresenter(Flow, View, ActiveCaptureSource, simulation, Localization);
            presenter.Initialize();
        }
#endif

        private void DisposeBackend()
        {
            if (View != null) View.MainMenuRequested -= ReturnToMainMenu;
            presenter?.Dispose();
            CoreMidiCaptureSource?.Dispose();
            coreMidiSession?.Dispose();
            presenter = null;
            CoreMidiCaptureSource = null;
            CoreMidiDiscovery = null;
            coreMidiSession = null;
            ActiveCaptureSource = null;
            ActiveDiscovery = null;
            CaptureSource = null;
            Discovery = null;
        }

        public void ReturnToMainMenu()
        {
            if (!isReturningToMenu && Application.CanStreamedLevelBeLoaded(MainMenuRoutes.MainMenuScene))
            {
                isReturningToMenu = true;
                SceneManager.LoadSceneAsync(MainMenuRoutes.MainMenuScene, LoadSceneMode.Single);
            }
        }

        public void ConfigurePanelSettings(PanelSettings panelSettings)
        {
            panelSettingsOverride = panelSettings;
            UIDocument document = GetComponent<UIDocument>();
            if (document != null) document.panelSettings = panelSettings;
        }
    }

}
