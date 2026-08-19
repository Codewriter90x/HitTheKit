using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using HitTheKit.Core;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.DeviceSetup;

namespace HitTheKit.Unity.MainMenu
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private PanelSettings panelSettingsOverride;
        [SerializeField] private Texture2D stageBackground;
        [SerializeField] private MainMenuStageEnvironment stageEnvironment;
        [SerializeField] private CoreMidiGameplayInput menuMidiInput;

        private UIDocument document;
        private VisualElement app;
        private VisualElement mainHome;
        private VisualElement background;
        private VisualElement playOverlay;
        private VisualElement learnOverlay;
        private VisualElement settingsOverlay;
        private VisualElement onboardingOverlay;
        private VisualElement chartAudioImportOverlay;
        private VisualElement songAudioBindingOverlay;
        private Label eyebrow;
        private Label status;
        private Label learnHeading;
        private Label learnCopy;
        private Label settingsHeading;
        private Button playButton;
        private Button learnButton;
        private Button setupButton;
        private Button languageButton;
        private Button settingsButton;
        private Button exitButton;
        private ScrollView songList;
        private Label songLibraryHeading;
        private Label songLibraryCopy;
        private Label songLibraryCount;
        private Label songLibraryReadyCount;
        private Label songLibrarySource;
        private Label songLibraryDiagnostic;
        private Label songDetailNumber;
        private Label songDetailTitle;
        private Label songDetailArtist;
        private Label songDetailAlbum;
        private Label songDetailMetadata;
        private Label songDetailOrigin;
        private Label songDetailReadiness;
        private VisualElement songDifficultyButtons;
        private Label songDifficultyTitle;
        private Label songDifficultyNote;
        private Label songSpeedTitle;
        private Label songEffectiveBpm;
        private Label songSpeedNote;
        private Button songSpeedSixtyButton;
        private Button songSpeedSeventyButton;
        private Button songSpeedEightyButton;
        private Button songSpeedNinetyButton;
        private Button songSpeedFullButton;
        private Button songPlayButton;
        private Button songRecordButton;
        private Button songBindAudioButton;
        private Button songRefreshButton;
        private Button songFolderButton;
        private Button songBackButton;
        private Button songImportAudioButton;
        private Label chartAudioFileLabel;
        private TextField chartAudioTitleField;
        private TextField chartAudioArtistField;
        private TextField chartAudioBpmField;
        private TextField chartAudioBarsField;
        private TextField chartAudioBeatsField;
        private Label chartAudioErrorLabel;
        private Button chartAudioStartButton;
        private Button chartAudioCancelButton;
        private Label songAudioBindingSongLabel;
        private Label songAudioBindingFileLabel;
        private Label songAudioBindingErrorLabel;
        private Button songAudioBindingConfirmButton;
        private Button songAudioBindingCancelButton;
        private ScrollView learnList;
        private Label learnProgressCount;
        private Label learnProgressAccuracy;
        private Label learnProgressTime;
        private VisualElement learnProgressFill;
        private Label learnPassRule;
        private Label learnSelectedNumber;
        private Label learnSelectedTitle;
        private Label learnSelectedCopy;
        private Label learnSelectedFocus;
        private Label learnSelectedObjective;
        private Label learnSelectedPattern;
        private Label learnSelectedBest;
        private Label learnEffectiveBpm;
        private Label learnSpeedNote;
        private Button learnSpeedHalfButton;
        private Button learnSpeedThreeQuarterButton;
        private Button learnSpeedFullButton;
        private Button learnStartButton;
        private Button learnBackButton;
        private Button learnRailTrainButton;
        private Button learnRailSetupButton;
        private Button audioButton;
        private Button motionButton;
        private Button volumeDownButton;
        private Button volumeUpButton;
        private Label volumeValue;
        private Button timingDiagnosticsButton;
        private Button metronomeButton;
        private Button highContrastButton;
        private Button keyboardOffsetDownButton;
        private Button keyboardOffsetUpButton;
        private Button keyboardOffsetResetButton;
        private Label keyboardOffsetValue;
        private Button midiOffsetDownButton;
        private Button midiOffsetUpButton;
        private Button midiOffsetResetButton;
        private Label midiOffsetValue;
        private Label calibrationHelp;
        private Button bindingKickButton;
        private Button bindingSnareButton;
        private Button bindingHiHatButton;
        private Button bindingTom1Button;
        private Button bindingTom2Button;
        private Button bindingFloorTomButton;
        private Button bindingCrashButton;
        private Button bindingRideButton;
        private Label bindingStatus;
        private Button fullscreenButton;
        private Button resolutionPreviousButton;
        private Button resolutionNextButton;
        private Label resolutionValue;
        private Button resetPreferencesButton;
        private Button resetLocalDataButton;
        private Label resetStatus;
        private Button onboardingKeyboardButton;
        private Button onboardingMidiButton;
        private Button onboardingSkipButton;
        private Label settingsThemeHeading;
        private Label settingsThemeDescription;
        private Button settingsThemeArcadeButton;
        private Button settingsThemeConcertButton;
        private Button settingsThemePrecisionButton;
        private Label settingsProgressSummary;
        private Label settingsBackupPath;
        private Label settingsBackupStatus;
        private Button settingsExportProgressButton;
        private Button settingsImportProgressButton;
        private Button settingsBackButton;
        private bool isBound;
        private bool playVisible;
        private bool learnVisible;
        private bool settingsVisible;
        private bool onboardingVisible;
        private int learnOpenedFrame = -1;
        private int playOpenedFrame = -1;
        private SongLibrarySnapshot songLibrary;
        private HtkSongInboxResult packageInbox;
        private string selectedSongId;
        private string selectedSongDifficulty;
        private string pendingChartAudioPath;
        private string pendingSongAudioPath;
        private string pendingSongAudioId;
        private double selectedSongSpeed = 1.0;
        private GameplayLessonId selectedLesson = GameplayLessonId.FirstPulse;
        private double selectedStudySpeed = 1.0;
        private readonly DoubleKickConfirmGesture doubleKickConfirm = new DoubleKickConfirmGesture();
        private GameplayMidiState lastRenderedMidiState = (GameplayMidiState)(-1);
        private string backupStatusMessage = string.Empty;
        private DrumPad? pendingKeyBinding;
        private bool resetConfirmationPending;
        private bool resetDataConfirmationPending;
        private static readonly Vector2Int[] WindowSizes =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440)
        };

        public event Action<MainMenuDestination> DestinationRequested;

        public MainMenuDestination SelectedDestination { get; private set; } = MainMenuDestination.Play;
        public MainMenuLanguage Language { get; private set; } = MainMenuLanguage.Italian;
        public bool IsViewBound => isBound;
        public bool IsSongLibraryVisible => playVisible;
        public bool IsLearnOverlayVisible => learnVisible;
        public bool IsSettingsOverlayVisible => settingsVisible;
        public bool IsNavigationPending { get; private set; }
        public bool AudioMuted { get; private set; }
        public bool ReducedMotion { get; private set; }
        public GameplayPresentationTheme SelectedGameplayTheme => GameplaySettingsRuntime.Current.Theme;
        public string RequestedSceneName { get; private set; }
        public GameplayLessonId SelectedLesson => selectedLesson;
        public double SelectedStudySpeed => selectedStudySpeed;
        public double SelectedSongSpeed => selectedSongSpeed;
        public string SelectedSongDifficulty => selectedSongDifficulty;
        public string SelectedSongId => selectedSongId;
        public SongLibrarySnapshot SongLibrary => songLibrary;
        public MainMenuStageEnvironment StageEnvironment => stageEnvironment;
        public bool IsChartAudioImportVisible =>
            chartAudioImportOverlay != null && chartAudioImportOverlay.resolvedStyle.display != DisplayStyle.None;
        public string PendingChartAudioPath => pendingChartAudioPath;
        public bool IsSongAudioBindingVisible =>
            songAudioBindingOverlay != null && songAudioBindingOverlay.resolvedStyle.display != DisplayStyle.None;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            PlayerPreferencesSnapshot preferences = PlayerPreferencesRuntime.Current.Snapshot;
            Language = preferences.Language == PlayerLanguage.English
                ? MainMenuLanguage.English
                : MainMenuLanguage.Italian;
            AudioMuted = preferences.AudioMuted;
            ReducedMotion = preferences.ReducedMotion;
            PlayerPreferencesRuntime.Current.ApplyAudio();
            if (!Application.isEditor) PlayerPreferencesRuntime.Current.ApplyDisplay();
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void Start()
        {
            TryBind();
            if (playVisible) FocusSelectedSongRow();
            else if (learnVisible) FocusSelectedLessonRow();
            else playButton?.Focus();
        }

        private void Update()
        {
            if (!isBound || IsNavigationPending) return;
            if (onboardingVisible) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (playVisible) MoveSongSelection(-1);
                else if (!learnVisible) MoveSelection(-1);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (playVisible) MoveSongSelection(1);
                else if (!learnVisible) MoveSelection(1);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.G)) SelectDestination(MainMenuDestination.Play, true);
            if (UnityEngine.Input.GetKeyDown(KeyCode.I)) SelectDestination(MainMenuDestination.Learn, true);
            if (UnityEngine.Input.GetKeyDown(KeyCode.C)) SelectDestination(MainMenuDestination.DeviceSetup, true);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                ConfirmCurrentAction();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
            RefreshInputStatus();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Configure(
            PanelSettings panelSettings,
            Texture2D backgroundTexture,
            MainMenuStageEnvironment environment = null)
        {
            panelSettingsOverride = panelSettings;
            stageBackground = backgroundTexture;
            stageEnvironment = environment;
            document = GetComponent<UIDocument>();
            if (document != null) document.panelSettings = panelSettings;
        }

        public void SelectDestination(MainMenuDestination destination, bool focus = false)
        {
            if (!Enum.IsDefined(typeof(MainMenuDestination), destination))
                throw new ArgumentOutOfRangeException(nameof(destination));
            SelectedDestination = destination;
            ApplySelection(playButton, destination == MainMenuDestination.Play);
            ApplySelection(learnButton, destination == MainMenuDestination.Learn);
            ApplySelection(setupButton, destination == MainMenuDestination.DeviceSetup);
            stageEnvironment?.PulseDestination(destination);
            if (!focus) return;
            Button button = ButtonFor(destination);
            button?.Focus();
        }

        public void ActivateSelected()
        {
            switch (SelectedDestination)
            {
                case MainMenuDestination.Play:
                    ShowSongLibrary();
                    break;
                case MainMenuDestination.Learn:
                    ShowLearn();
                    break;
                case MainMenuDestination.DeviceSetup:
                    Navigate(MainMenuRoutes.DeviceSetupScene, MainMenuDestination.DeviceSetup);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ShowSongLibrary()
        {
            playVisible = true;
            learnVisible = false;
            playOpenedFrame = Time.frameCount;
            SetSection(mainHome, false);
            SetSection(learnOverlay, false);
            SetSection(playOverlay, true);
            RefreshSongLibrary();
            FocusSelectedSongRow();
        }

        public void CloseSongLibrary()
        {
            playVisible = false;
            SetSection(playOverlay, false);
            SetSection(mainHome, true);
            playButton?.Focus();
        }

        public void ShowLearn()
        {
            playVisible = false;
            learnVisible = true;
            learnOpenedFrame = Time.frameCount;
            SetSection(mainHome, false);
            SetSection(playOverlay, false);
            SetSection(learnOverlay, true);
            RenderLearningPath();
            FocusSelectedLessonRow();
        }

        public void CloseLearn()
        {
            learnVisible = false;
            GameplaySessionContext.SelectFreePlay();
            SetSection(learnOverlay, false);
            SetSection(mainHome, true);
            learnButton?.Focus();
        }

        public bool ConfirmCurrentAction()
        {
            if (!isBound || IsNavigationPending || settingsVisible || onboardingVisible) return false;
            if (playVisible)
            {
                if (Time.frameCount == playOpenedFrame) return false;
                return StartSelectedSong();
            }
            if (learnVisible)
            {
                if (Time.frameCount == learnOpenedFrame) return false;
                if (!GameplayLearningProgress.IsUnlocked(selectedLesson)) return false;
                StartSelectedLesson();
                return true;
            }

            ActivateSelected();
            return true;
        }

        public bool ProcessDrumInput(DrumInputEvent input, double monotonicSeconds)
        {
            if (!doubleKickConfirm.Register(input.Pad, input.Velocity, monotonicSeconds)) return false;
            return ConfirmCurrentAction();
        }

        public void ToggleLanguage()
        {
            Language = Language == MainMenuLanguage.Italian ? MainMenuLanguage.English : MainMenuLanguage.Italian;
            PlayerPreferencesRuntime.Current.SetLanguage(
                Language == MainMenuLanguage.Italian ? PlayerLanguage.Italian : PlayerLanguage.English);
            RenderCopy();
        }

        public void ToggleSettings()
        {
            settingsVisible = !settingsVisible;
            SetOverlay(settingsOverlay, settingsVisible);
            if (settingsVisible) audioButton?.Focus();
            else ButtonFor(SelectedDestination)?.Focus();
        }

        private void TryBind()
        {
            if (isBound) return;
            if (document == null) document = GetComponent<UIDocument>();
            if (document.panelSettings == null && panelSettingsOverride != null)
                document.panelSettings = panelSettingsOverride;
            VisualElement root = document.rootVisualElement;
            if (root == null) return;

            app = Required<VisualElement>(root, "main-menu-app");
            mainHome = Required<VisualElement>(root, "main-home");
            background = Required<VisualElement>(root, "main-menu-background");
            playOverlay = Required<VisualElement>(root, "play-overlay");
            learnOverlay = Required<VisualElement>(root, "learn-overlay");
            settingsOverlay = Required<VisualElement>(root, "settings-overlay");
            onboardingOverlay = Required<VisualElement>(root, "onboarding-overlay");
            eyebrow = Required<Label>(root, "menu-eyebrow");
            status = Required<Label>(root, "input-status");
            learnHeading = Required<Label>(root, "learn-heading");
            learnCopy = Required<Label>(root, "learn-copy");
            settingsHeading = Required<Label>(root, "settings-heading");
            playButton = Required<Button>(root, "menu-play");
            learnButton = Required<Button>(root, "menu-learn");
            setupButton = Required<Button>(root, "menu-setup");
            languageButton = Required<Button>(root, "language-button");
            settingsButton = Required<Button>(root, "settings-button");
            exitButton = Required<Button>(root, "exit-button");
            songList = Required<ScrollView>(root, "song-library-list");
            songLibraryHeading = Required<Label>(root, "song-library-heading");
            songLibraryCopy = Required<Label>(root, "song-library-copy");
            songLibraryCount = Required<Label>(root, "song-library-count");
            songLibraryReadyCount = Required<Label>(root, "song-library-ready-count");
            songLibrarySource = Required<Label>(root, "song-library-source");
            songLibraryDiagnostic = Required<Label>(root, "song-library-diagnostic");
            songDetailNumber = Required<Label>(root, "song-detail-number");
            songDetailTitle = Required<Label>(root, "song-detail-title");
            songDetailArtist = Required<Label>(root, "song-detail-artist");
            songDetailAlbum = Required<Label>(root, "song-detail-album");
            songDetailMetadata = Required<Label>(root, "song-detail-metadata");
            songDetailOrigin = Required<Label>(root, "song-detail-origin");
            songDetailReadiness = Required<Label>(root, "song-detail-readiness");
            songDifficultyButtons = Required<VisualElement>(root, "song-difficulty-buttons");
            songDifficultyTitle = Required<Label>(root, "song-difficulty-title");
            songDifficultyNote = Required<Label>(root, "song-difficulty-note");
            songSpeedTitle = Required<Label>(root, "song-speed-title");
            songEffectiveBpm = Required<Label>(root, "song-effective-bpm");
            songSpeedNote = Required<Label>(root, "song-speed-note");
            songSpeedSixtyButton = Required<Button>(root, "song-speed-sixty");
            songSpeedSeventyButton = Required<Button>(root, "song-speed-seventy");
            songSpeedEightyButton = Required<Button>(root, "song-speed-eighty");
            songSpeedNinetyButton = Required<Button>(root, "song-speed-ninety");
            songSpeedFullButton = Required<Button>(root, "song-speed-full");
            songPlayButton = Required<Button>(root, "song-play-button");
            songRecordButton = Required<Button>(root, "song-record-button");
            songBindAudioButton = Required<Button>(root, "song-bind-audio-button");
            songRefreshButton = Required<Button>(root, "song-refresh-button");
            songFolderButton = Required<Button>(root, "song-folder-button");
            songBackButton = Required<Button>(root, "song-back-button");
            songImportAudioButton = Required<Button>(root, "song-import-audio-button");
            chartAudioImportOverlay = Required<VisualElement>(root, "chart-audio-import-overlay");
            chartAudioFileLabel = Required<Label>(root, "chart-audio-file");
            chartAudioTitleField = Required<TextField>(root, "chart-audio-title");
            chartAudioArtistField = Required<TextField>(root, "chart-audio-artist");
            chartAudioBpmField = Required<TextField>(root, "chart-audio-bpm");
            chartAudioBarsField = Required<TextField>(root, "chart-audio-bars");
            chartAudioBeatsField = Required<TextField>(root, "chart-audio-beats");
            chartAudioErrorLabel = Required<Label>(root, "chart-audio-error");
            chartAudioStartButton = Required<Button>(root, "chart-audio-start");
            chartAudioCancelButton = Required<Button>(root, "chart-audio-cancel");
            songAudioBindingOverlay = Required<VisualElement>(root, "song-audio-binding-overlay");
            songAudioBindingSongLabel = Required<Label>(root, "song-audio-binding-song");
            songAudioBindingFileLabel = Required<Label>(root, "song-audio-binding-file");
            songAudioBindingErrorLabel = Required<Label>(root, "song-audio-binding-error");
            songAudioBindingConfirmButton = Required<Button>(root, "song-audio-binding-confirm");
            songAudioBindingCancelButton = Required<Button>(root, "song-audio-binding-cancel");
            learnList = Required<ScrollView>(root, "learn-list");
            learnProgressCount = Required<Label>(root, "learn-progress-count");
            learnProgressAccuracy = Required<Label>(root, "learn-progress-accuracy");
            learnProgressTime = Required<Label>(root, "learn-progress-time");
            learnProgressFill = Required<VisualElement>(root, "learn-progress-fill");
            learnPassRule = Required<Label>(root, "learn-pass-rule");
            learnSelectedNumber = Required<Label>(root, "learn-selected-number");
            learnSelectedTitle = Required<Label>(root, "learn-selected-title");
            learnSelectedCopy = Required<Label>(root, "learn-selected-copy");
            learnSelectedFocus = Required<Label>(root, "learn-selected-focus");
            learnSelectedObjective = Required<Label>(root, "learn-selected-objective");
            learnSelectedPattern = Required<Label>(root, "learn-selected-pattern");
            learnSelectedBest = Required<Label>(root, "learn-selected-best");
            learnEffectiveBpm = Required<Label>(root, "learn-effective-bpm");
            learnSpeedNote = Required<Label>(root, "learn-speed-note");
            learnSpeedHalfButton = Required<Button>(root, "learn-speed-half");
            learnSpeedThreeQuarterButton = Required<Button>(root, "learn-speed-three-quarter");
            learnSpeedFullButton = Required<Button>(root, "learn-speed-full");
            learnStartButton = Required<Button>(root, "learn-start-button");
            learnBackButton = Required<Button>(root, "learn-back-button");
            learnRailTrainButton = Required<Button>(root, "learn-rail-train");
            learnRailSetupButton = Required<Button>(root, "learn-rail-setup");
            audioButton = Required<Button>(root, "audio-button");
            motionButton = Required<Button>(root, "motion-button");
            volumeDownButton = Required<Button>(root, "volume-down");
            volumeUpButton = Required<Button>(root, "volume-up");
            volumeValue = Required<Label>(root, "volume-value");
            timingDiagnosticsButton = Required<Button>(root, "timing-diagnostics-button");
            metronomeButton = Required<Button>(root, "metronome-button");
            highContrastButton = Required<Button>(root, "high-contrast-button");
            keyboardOffsetDownButton = Required<Button>(root, "keyboard-offset-down");
            keyboardOffsetUpButton = Required<Button>(root, "keyboard-offset-up");
            keyboardOffsetResetButton = Required<Button>(root, "keyboard-offset-reset");
            keyboardOffsetValue = Required<Label>(root, "keyboard-offset-value");
            midiOffsetDownButton = Required<Button>(root, "midi-offset-down");
            midiOffsetUpButton = Required<Button>(root, "midi-offset-up");
            midiOffsetResetButton = Required<Button>(root, "midi-offset-reset");
            midiOffsetValue = Required<Label>(root, "midi-offset-value");
            calibrationHelp = Required<Label>(root, "calibration-help");
            bindingKickButton = Required<Button>(root, "binding-kick");
            bindingSnareButton = Required<Button>(root, "binding-snare");
            bindingHiHatButton = Required<Button>(root, "binding-hihat");
            bindingTom1Button = Required<Button>(root, "binding-tom1");
            bindingTom2Button = Required<Button>(root, "binding-tom2");
            bindingFloorTomButton = Required<Button>(root, "binding-floor-tom");
            bindingCrashButton = Required<Button>(root, "binding-crash");
            bindingRideButton = Required<Button>(root, "binding-ride");
            bindingStatus = Required<Label>(root, "binding-status");
            fullscreenButton = Required<Button>(root, "fullscreen-button");
            resolutionPreviousButton = Required<Button>(root, "resolution-previous");
            resolutionNextButton = Required<Button>(root, "resolution-next");
            resolutionValue = Required<Label>(root, "resolution-value");
            resetPreferencesButton = Required<Button>(root, "reset-preferences");
            resetLocalDataButton = Required<Button>(root, "reset-local-data");
            resetStatus = Required<Label>(root, "reset-status");
            onboardingKeyboardButton = Required<Button>(root, "onboarding-keyboard");
            onboardingMidiButton = Required<Button>(root, "onboarding-midi");
            onboardingSkipButton = Required<Button>(root, "onboarding-skip");
            settingsThemeHeading = Required<Label>(root, "settings-theme-heading");
            settingsThemeDescription = Required<Label>(root, "settings-theme-description");
            settingsThemeArcadeButton = Required<Button>(root, "settings-theme-arcade");
            settingsThemeConcertButton = Required<Button>(root, "settings-theme-concert");
            settingsThemePrecisionButton = Required<Button>(root, "settings-theme-precision");
            settingsProgressSummary = Required<Label>(root, "settings-progress-summary");
            settingsBackupPath = Required<Label>(root, "settings-backup-path");
            settingsBackupStatus = Required<Label>(root, "settings-backup-status");
            settingsExportProgressButton = Required<Button>(root, "settings-export-progress");
            settingsImportProgressButton = Required<Button>(root, "settings-import-progress");
            settingsBackButton = Required<Button>(root, "settings-back-button");

            stageEnvironment?.Initialize();
            if (stageEnvironment != null && stageEnvironment.IsReady)
            {
                background.style.display = DisplayStyle.None;
                stageEnvironment.ApplyTheme(GameplaySettingsRuntime.Current.Theme);
            }
            else if (stageBackground != null)
            {
                background.style.display = DisplayStyle.Flex;
                background.style.backgroundImage = new StyleBackground(stageBackground);
            }
            playButton.clicked += HandlePlay;
            learnButton.clicked += HandleLearn;
            setupButton.clicked += HandleSetup;
            languageButton.clicked += ToggleLanguage;
            settingsButton.clicked += ToggleSettings;
            exitButton.clicked += HandleExit;
            songSpeedSixtyButton.clicked += SelectSixtySongSpeed;
            songSpeedSeventyButton.clicked += SelectSeventySongSpeed;
            songSpeedEightyButton.clicked += SelectEightySongSpeed;
            songSpeedNinetyButton.clicked += SelectNinetySongSpeed;
            songSpeedFullButton.clicked += SelectFullSongSpeed;
            songPlayButton.clicked += StartSelectedSongFromButton;
            songRecordButton.clicked += StartChartCreatorFromButton;
            songBindAudioButton.clicked += PickAudioForSelectedSong;
            songRefreshButton.clicked += RefreshSongLibrary;
            songFolderButton.clicked += OpenUserSongFolder;
            songBackButton.clicked += CloseSongLibrary;
            songImportAudioButton.clicked += PickChartAuthoringAudio;
            chartAudioStartButton.clicked += ImportAudioAndStartChartCreatorFromButton;
            chartAudioCancelButton.clicked += CancelChartAuthoringAudio;
            songAudioBindingConfirmButton.clicked += ConfirmSongAudioBindingFromButton;
            songAudioBindingCancelButton.clicked += CancelSongAudioBinding;
            learnSpeedHalfButton.clicked += SelectHalfSpeed;
            learnSpeedThreeQuarterButton.clicked += SelectThreeQuarterSpeed;
            learnSpeedFullButton.clicked += SelectFullSpeed;
            learnStartButton.clicked += StartSelectedLesson;
            learnBackButton.clicked += CloseLearn;
            learnRailTrainButton.clicked += HandlePlay;
            learnRailSetupButton.clicked += HandleSetup;
            audioButton.clicked += ToggleAudio;
            motionButton.clicked += ToggleMotion;
            volumeDownButton.clicked += DecreaseVolume;
            volumeUpButton.clicked += IncreaseVolume;
            timingDiagnosticsButton.clicked += ToggleTimingDiagnostics;
            metronomeButton.clicked += ToggleMetronome;
            highContrastButton.clicked += ToggleHighContrast;
            keyboardOffsetDownButton.clicked += DecreaseKeyboardOffset;
            keyboardOffsetUpButton.clicked += IncreaseKeyboardOffset;
            keyboardOffsetResetButton.clicked += ResetKeyboardOffset;
            midiOffsetDownButton.clicked += DecreaseMidiOffset;
            midiOffsetUpButton.clicked += IncreaseMidiOffset;
            midiOffsetResetButton.clicked += ResetMidiOffset;
            bindingKickButton.clicked += BeginKickBinding;
            bindingSnareButton.clicked += BeginSnareBinding;
            bindingHiHatButton.clicked += BeginHiHatBinding;
            bindingTom1Button.clicked += BeginTom1Binding;
            bindingTom2Button.clicked += BeginTom2Binding;
            bindingFloorTomButton.clicked += BeginFloorTomBinding;
            bindingCrashButton.clicked += BeginCrashBinding;
            bindingRideButton.clicked += BeginRideBinding;
            fullscreenButton.clicked += ToggleFullscreen;
            resolutionPreviousButton.clicked += SelectPreviousResolution;
            resolutionNextButton.clicked += SelectNextResolution;
            resetPreferencesButton.clicked += ResetPreferences;
            resetLocalDataButton.clicked += ResetLocalData;
            onboardingKeyboardButton.clicked += CompleteKeyboardOnboarding;
            onboardingMidiButton.clicked += CompleteMidiOnboarding;
            onboardingSkipButton.clicked += CompleteKeyboardOnboarding;
            settingsThemeArcadeButton.clicked += SelectArcadeTheme;
            settingsThemeConcertButton.clicked += SelectConcertTheme;
            settingsThemePrecisionButton.clicked += SelectPrecisionTheme;
            settingsExportProgressButton.clicked += ExportProgressBackup;
            settingsImportProgressButton.clicked += ImportProgressBackup;
            settingsBackButton.clicked += ToggleSettings;
            app.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            app.RegisterCallback<KeyDownEvent>(HandleBindingKeyDown, TrickleDown.TrickleDown);
            learnStartButton.pickingMode = PickingMode.Position;
            learnStartButton.focusable = true;
            learnStartButton.tabIndex = 0;
            songPlayButton.pickingMode = PickingMode.Position;
            songPlayButton.focusable = true;
            songPlayButton.tabIndex = 0;
            if (menuMidiInput != null) menuMidiInput.HitReceived += HandleMenuDrumInput;

            isBound = true;
            ApplyPreferenceClasses();
            GameplaySessionDefinition session = GameplaySessionContext.Current;
            if (session.ReturnTarget == GameplayReturnTarget.LearningPath && session.LessonId.HasValue)
            {
                selectedLesson = session.LessonId.Value;
                selectedStudySpeed = session.SpeedMultiplier;
            }
            RenderCopy();
            settingsVisible = false;
            SetOverlay(settingsOverlay, false);
            SetOverlay(chartAudioImportOverlay, false);
            SetOverlay(songAudioBindingOverlay, false);
            onboardingVisible = !PlayerPreferencesRuntime.Current.Snapshot.FirstRunCompleted;
            SetOverlay(onboardingOverlay, onboardingVisible);
            if (session.ReturnTarget == GameplayReturnTarget.LearningPath)
            {
                SelectDestination(MainMenuDestination.Learn);
                ShowLearn();
            }
            else if (session.ReturnTarget == GameplayReturnTarget.SongLibrary)
            {
                selectedSongId = session.SongId;
                selectedSongDifficulty = session.Difficulty;
                selectedSongSpeed = session.SpeedMultiplier;
                SelectDestination(MainMenuDestination.Play);
                ShowSongLibrary();
            }
            else
            {
                SelectDestination(MainMenuDestination.Play);
                playVisible = false;
                learnVisible = false;
                SetSection(playOverlay, false);
                SetSection(learnOverlay, false);
                SetSection(mainHome, true);
            }
            RefreshInputStatus(true);
        }

        private void Unbind()
        {
            if (!isBound) return;
            playButton.clicked -= HandlePlay;
            learnButton.clicked -= HandleLearn;
            setupButton.clicked -= HandleSetup;
            languageButton.clicked -= ToggleLanguage;
            settingsButton.clicked -= ToggleSettings;
            exitButton.clicked -= HandleExit;
            songSpeedSixtyButton.clicked -= SelectSixtySongSpeed;
            songSpeedSeventyButton.clicked -= SelectSeventySongSpeed;
            songSpeedEightyButton.clicked -= SelectEightySongSpeed;
            songSpeedNinetyButton.clicked -= SelectNinetySongSpeed;
            songSpeedFullButton.clicked -= SelectFullSongSpeed;
            songPlayButton.clicked -= StartSelectedSongFromButton;
            songRecordButton.clicked -= StartChartCreatorFromButton;
            songBindAudioButton.clicked -= PickAudioForSelectedSong;
            songRefreshButton.clicked -= RefreshSongLibrary;
            songFolderButton.clicked -= OpenUserSongFolder;
            songBackButton.clicked -= CloseSongLibrary;
            songImportAudioButton.clicked -= PickChartAuthoringAudio;
            chartAudioStartButton.clicked -= ImportAudioAndStartChartCreatorFromButton;
            chartAudioCancelButton.clicked -= CancelChartAuthoringAudio;
            songAudioBindingConfirmButton.clicked -= ConfirmSongAudioBindingFromButton;
            songAudioBindingCancelButton.clicked -= CancelSongAudioBinding;
            learnSpeedHalfButton.clicked -= SelectHalfSpeed;
            learnSpeedThreeQuarterButton.clicked -= SelectThreeQuarterSpeed;
            learnSpeedFullButton.clicked -= SelectFullSpeed;
            learnStartButton.clicked -= StartSelectedLesson;
            learnBackButton.clicked -= CloseLearn;
            learnRailTrainButton.clicked -= HandlePlay;
            learnRailSetupButton.clicked -= HandleSetup;
            audioButton.clicked -= ToggleAudio;
            motionButton.clicked -= ToggleMotion;
            volumeDownButton.clicked -= DecreaseVolume;
            volumeUpButton.clicked -= IncreaseVolume;
            timingDiagnosticsButton.clicked -= ToggleTimingDiagnostics;
            metronomeButton.clicked -= ToggleMetronome;
            highContrastButton.clicked -= ToggleHighContrast;
            keyboardOffsetDownButton.clicked -= DecreaseKeyboardOffset;
            keyboardOffsetUpButton.clicked -= IncreaseKeyboardOffset;
            keyboardOffsetResetButton.clicked -= ResetKeyboardOffset;
            midiOffsetDownButton.clicked -= DecreaseMidiOffset;
            midiOffsetUpButton.clicked -= IncreaseMidiOffset;
            midiOffsetResetButton.clicked -= ResetMidiOffset;
            bindingKickButton.clicked -= BeginKickBinding;
            bindingSnareButton.clicked -= BeginSnareBinding;
            bindingHiHatButton.clicked -= BeginHiHatBinding;
            bindingTom1Button.clicked -= BeginTom1Binding;
            bindingTom2Button.clicked -= BeginTom2Binding;
            bindingFloorTomButton.clicked -= BeginFloorTomBinding;
            bindingCrashButton.clicked -= BeginCrashBinding;
            bindingRideButton.clicked -= BeginRideBinding;
            fullscreenButton.clicked -= ToggleFullscreen;
            resolutionPreviousButton.clicked -= SelectPreviousResolution;
            resolutionNextButton.clicked -= SelectNextResolution;
            resetPreferencesButton.clicked -= ResetPreferences;
            resetLocalDataButton.clicked -= ResetLocalData;
            onboardingKeyboardButton.clicked -= CompleteKeyboardOnboarding;
            onboardingMidiButton.clicked -= CompleteMidiOnboarding;
            onboardingSkipButton.clicked -= CompleteKeyboardOnboarding;
            settingsThemeArcadeButton.clicked -= SelectArcadeTheme;
            settingsThemeConcertButton.clicked -= SelectConcertTheme;
            settingsThemePrecisionButton.clicked -= SelectPrecisionTheme;
            settingsExportProgressButton.clicked -= ExportProgressBackup;
            settingsImportProgressButton.clicked -= ImportProgressBackup;
            settingsBackButton.clicked -= ToggleSettings;
            app.UnregisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            app.UnregisterCallback<KeyDownEvent>(HandleBindingKeyDown, TrickleDown.TrickleDown);
            if (menuMidiInput != null) menuMidiInput.HitReceived -= HandleMenuDrumInput;
            isBound = false;
        }

        private void HandlePlay()
        {
            SelectDestination(MainMenuDestination.Play);
            ActivateSelected();
        }

        private void HandleLearn()
        {
            SelectDestination(MainMenuDestination.Learn);
            ActivateSelected();
        }

        private void HandleSetup()
        {
            GameplaySessionContext.SelectFreePlay();
            playVisible = false;
            learnVisible = false;
            SelectDestination(MainMenuDestination.DeviceSetup);
            ActivateSelected();
        }

        public void RefreshSongLibrary()
        {
            try
            {
                packageInbox = new HtkSongPackageService().ImportInbox(SongLibraryRuntime.UserRoot);
            }
            catch (Exception exception)
            {
                packageInbox = null;
                Debug.LogWarning($"Could not scan .htksong packages ({exception.GetType().Name}).", this);
            }
            songLibrary = SongLibraryRuntime.Discover();
            string importedSongId = packageInbox?.ImportedSongIds.Count > 0
                ? packageInbox.ImportedSongIds[packageInbox.ImportedSongIds.Count - 1]
                : null;
            if (songLibrary.Songs.Count == 0)
            {
                selectedSongId = null;
            }
            else if (!string.IsNullOrEmpty(importedSongId) &&
                     songLibrary.Songs.Any(song => string.Equals(song.Id, importedSongId, StringComparison.Ordinal)))
            {
                selectedSongId = importedSongId;
            }
            else if (string.IsNullOrEmpty(selectedSongId) ||
                     songLibrary.Songs.All(song => !string.Equals(song.Id, selectedSongId, StringComparison.Ordinal)))
            {
                SongLibraryEntry firstPlayable = songLibrary.Songs.FirstOrDefault(song => song.IsPlayable);
                selectedSongId = (firstPlayable ?? songLibrary.Songs[0]).Id;
            }
            EnsureSelectedDifficulty(SelectedSong());
            RenderSongLibrary();
        }

        public void SelectSong(string songId)
        {
            if (songLibrary == null) throw new InvalidOperationException("The song library has not been loaded.");
            SongLibraryEntry song = songLibrary.Songs.FirstOrDefault(
                item => string.Equals(item.Id, songId, StringComparison.Ordinal));
            if (song == null) throw new ArgumentOutOfRangeException(nameof(songId));
            selectedSongId = song.Id;
            EnsureSelectedDifficulty(song);
            RenderSongLibrary();
            FocusSelectedSongRow();
        }

        public bool StartSelectedSong()
        {
            SongLibraryEntry song = SelectedSong();
            if (song == null || !song.IsPlayable)
            {
                RenderSongLibrary();
                return false;
            }

            GameplaySessionContext.SelectSong(song, selectedSongSpeed, selectedSongDifficulty);
            Navigate(MainMenuRoutes.GameplayScene, MainMenuDestination.Play);
            return IsNavigationPending;
        }

        private void StartSelectedSongFromButton() => StartSelectedSong();

        public bool StartChartCreator()
        {
            SongLibraryEntry song = SelectedSong();
            if (song == null || (!song.IsPlayable && !song.CanAuthorChart))
            {
                RenderSongLibrary();
                return false;
            }

            GameplaySessionContext.SelectChartCreator(song, selectedSongSpeed, selectedSongDifficulty);
            Navigate(MainMenuRoutes.GameplayScene, MainMenuDestination.Play);
            return IsNavigationPending;
        }

        private void StartChartCreatorFromButton() => StartChartCreator();

        private void PickChartAuthoringAudio()
        {
            try
            {
                string path = new MacOsChartAuthoringAudioPicker().PickAudioFile();
                if (!string.IsNullOrWhiteSpace(path)) BeginChartAuthoringAudio(path);
            }
            catch (Exception exception)
            {
                songLibraryDiagnostic.text = (Language == MainMenuLanguage.Italian
                    ? "IMPORT AUDIO NON RIUSCITO · "
                    : "AUDIO IMPORT FAILED · ") + exception.Message;
            }
        }

        public void BeginChartAuthoringAudio(string sourceAudioPath)
        {
            string path = ChartAuthoringAudioImporter.ValidateSource(sourceAudioPath);
            pendingChartAudioPath = path;
            chartAudioFileLabel.text = Path.GetFileName(path);
            chartAudioTitleField.value = Path.GetFileNameWithoutExtension(path).Replace('_', ' ').Replace('-', ' ');
            chartAudioArtistField.value = string.Empty;
            chartAudioBpmField.value = string.Empty;
            chartAudioBarsField.value = string.Empty;
            chartAudioBeatsField.value = string.Empty;
            chartAudioErrorLabel.text = Language == MainMenuLanguage.Italian
                ? "Inserisci dati verificati per questa precisa versione audio."
                : "Enter values verified for this exact audio version.";
            SetOverlay(chartAudioImportOverlay, true);
            chartAudioTitleField.Focus();
        }

        public bool ImportAudioAndStartChartCreator(string libraryRoot = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pendingChartAudioPath))
                    throw new InvalidOperationException("Select an audio file first.");
                if (!TryParseDouble(chartAudioBpmField.value, out double bpm))
                    throw new InvalidOperationException("BPM must be a positive number.");
                if (!int.TryParse(chartAudioBarsField.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int bars))
                    throw new InvalidOperationException("Bars must be a positive whole number.");
                if (!int.TryParse(chartAudioBeatsField.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int beats))
                    throw new InvalidOperationException("Beats per bar must be a positive whole number.");

                var request = new ChartAuthoringAudioRequest(
                    pendingChartAudioPath,
                    chartAudioTitleField.value,
                    chartAudioArtistField.value,
                    bpm,
                    bars,
                    beats);
                ChartAuthoringAudioImportResult imported = new ChartAuthoringAudioImporter().Import(
                    request,
                    string.IsNullOrWhiteSpace(libraryRoot) ? SongLibraryRuntime.UserRoot : libraryRoot,
                    DateTimeOffset.UtcNow);

                pendingChartAudioPath = null;
                SetOverlay(chartAudioImportOverlay, false);
                GameplaySessionContext.SelectChartCreator(imported.Song, 1.0, "easy");
                Navigate(MainMenuRoutes.GameplayScene, MainMenuDestination.Play);
                return IsNavigationPending;
            }
            catch (Exception exception)
            {
                chartAudioErrorLabel.text = (Language == MainMenuLanguage.Italian
                    ? "CONTROLLA I DATI · "
                    : "CHECK THE DETAILS · ") + exception.Message;
                return false;
            }
        }

        private void ImportAudioAndStartChartCreatorFromButton() => ImportAudioAndStartChartCreator();

        private void CancelChartAuthoringAudio()
        {
            pendingChartAudioPath = null;
            SetOverlay(chartAudioImportOverlay, false);
        }

        private void PickAudioForSelectedSong()
        {
            try
            {
                SongLibraryEntry song = SelectedSong();
                if (song == null || !song.CanBindAudio)
                    throw new InvalidOperationException("Select an imported chart awaiting local audio.");
                string path = new MacOsChartAuthoringAudioPicker().PickAudioFile();
                if (!string.IsNullOrWhiteSpace(path)) BeginSongAudioBinding(path);
            }
            catch (Exception exception)
            {
                songLibraryDiagnostic.text = (Language == MainMenuLanguage.Italian
                    ? "ASSOCIAZIONE AUDIO NON RIUSCITA · "
                    : "AUDIO BINDING FAILED · ") + exception.Message;
            }
        }

        public void BeginSongAudioBinding(string sourceAudioPath)
        {
            SongLibraryEntry song = SelectedSong();
            if (song == null || !song.CanBindAudio)
                throw new InvalidOperationException("Select an imported chart awaiting local audio.");
            pendingSongAudioPath = ChartAuthoringAudioImporter.ValidateSource(sourceAudioPath);
            pendingSongAudioId = song.Id;
            songAudioBindingSongLabel.text = $"{song.Title.ToUpperInvariant()} · {song.Artist.ToUpperInvariant()}";
            songAudioBindingFileLabel.text = Path.GetFileName(pendingSongAudioPath);
            songAudioBindingErrorLabel.text = Language == MainMenuLanguage.Italian
                ? "Conferma di avere il diritto di usare questa copia audio locale."
                : "Confirm that you may use this local audio copy.";
            SetOverlay(songAudioBindingOverlay, true);
        }

        public bool ConfirmSongAudioBinding(string libraryRoot = null)
        {
            try
            {
                SongLibraryEntry song = songLibrary?.Songs.FirstOrDefault(value =>
                    string.Equals(value.Id, pendingSongAudioId, StringComparison.Ordinal));
                if (song == null || string.IsNullOrWhiteSpace(pendingSongAudioPath))
                    throw new InvalidOperationException("Choose a song and an audio file first.");
                new SongAudioBindingService().Bind(
                    song,
                    pendingSongAudioPath,
                    string.IsNullOrWhiteSpace(libraryRoot) ? SongLibraryRuntime.UserRoot : libraryRoot);
                selectedSongId = song.Id;
                pendingSongAudioPath = null;
                pendingSongAudioId = null;
                SetOverlay(songAudioBindingOverlay, false);
                RefreshSongLibrary();
                songLibraryDiagnostic.text = Language == MainMenuLanguage.Italian
                    ? "AUDIO LOCALE ASSOCIATO · BRANO PRONTO"
                    : "LOCAL AUDIO BOUND · SONG READY";
                return SelectedSong()?.IsPlayable == true;
            }
            catch (Exception exception)
            {
                songAudioBindingErrorLabel.text = (Language == MainMenuLanguage.Italian
                    ? "ASSOCIAZIONE NON RIUSCITA · "
                    : "BINDING FAILED · ") + exception.Message;
                return false;
            }
        }

        private void ConfirmSongAudioBindingFromButton() => ConfirmSongAudioBinding();

        private void CancelSongAudioBinding()
        {
            pendingSongAudioPath = null;
            pendingSongAudioId = null;
            SetOverlay(songAudioBindingOverlay, false);
        }

        private static bool TryParseDouble(string value, out double result)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) && result > 0)
                return true;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result > 0;
        }

        public void SelectSongSpeed(double speedMultiplier)
        {
            if (!GameplaySongSpeeds.IsSupported(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
            selectedSongSpeed = speedMultiplier;
            RenderSongLibrary();
        }

        private void SelectSixtySongSpeed() => SelectSongSpeed(0.6);
        private void SelectSeventySongSpeed() => SelectSongSpeed(0.7);
        private void SelectEightySongSpeed() => SelectSongSpeed(0.8);
        private void SelectNinetySongSpeed() => SelectSongSpeed(0.9);
        private void SelectFullSongSpeed() => SelectSongSpeed(1.0);

        public void SelectSongDifficulty(string difficulty)
        {
            SongLibraryEntry song = SelectedSong();
            if (song == null || !ContainsDifficulty(song, difficulty))
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            selectedSongDifficulty = difficulty;
            RenderSongLibrary();
        }

        private void OpenUserSongFolder()
        {
            try
            {
                Directory.CreateDirectory(SongLibraryRuntime.UserRoot);
                Application.OpenURL(new Uri(SongLibraryRuntime.UserRoot).AbsoluteUri);
                songLibraryDiagnostic.text = Language == MainMenuLanguage.Italian
                    ? "CARTELLA APERTA · CREA HTKSongs/<brano>/song.json"
                    : "FOLDER OPENED · CREATE HTKSongs/<song>/song.json";
            }
            catch (Exception exception)
            {
                songLibraryDiagnostic.text = (Language == MainMenuLanguage.Italian
                    ? "IMPOSSIBILE APRIRE LA CARTELLA: "
                    : "COULD NOT OPEN THE FOLDER: ") + exception.Message;
            }
        }

        public void StartLesson(GameplayLessonId lesson, double speedMultiplier = 1.0)
        {
            if (!GameplayLearningProgress.IsUnlocked(lesson))
                throw new InvalidOperationException($"Lesson '{lesson}' is locked.");
            GameplaySessionContext.SelectLesson(lesson, speedMultiplier);
            Navigate(MainMenuRoutes.GameplayScene, MainMenuDestination.Learn);
        }

        public void SelectLearningLesson(GameplayLessonId lesson)
        {
            GameplayLearningPath.Find(lesson);
            selectedLesson = lesson;
            RenderLearningPath();
            FocusSelectedLessonRow();
        }

        public void SelectStudySpeed(double speedMultiplier)
        {
            if (!GameplayStudySpeeds.IsSupported(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
            selectedStudySpeed = speedMultiplier;
            RenderLearningPath();
        }

        private void SelectHalfSpeed() => SelectStudySpeed(0.5);
        private void SelectThreeQuarterSpeed() => SelectStudySpeed(0.75);
        private void SelectFullSpeed() => SelectStudySpeed(1.0);
        private void StartSelectedLesson() => StartLesson(selectedLesson, selectedStudySpeed);

        private void HandleMenuDrumInput(DrumInputEvent input) =>
            ProcessDrumInput(input, Time.unscaledTimeAsDouble);

        private void HandleExit()
        {
#if UNITY_EDITOR
            status.text = Language == MainMenuLanguage.Italian
                ? "USCITA DISPONIBILE NELLA BUILD"
                : "EXIT IS AVAILABLE IN A BUILD";
#else
            Application.Quit();
#endif
        }

        private void HandleEscape()
        {
            if (IsSettingsOverlayVisible) ToggleSettings();
            else if (IsSongLibraryVisible) CloseSongLibrary();
            else if (IsLearnOverlayVisible) CloseLearn();
        }

        private void ToggleAudio()
        {
            AudioMuted = !AudioMuted;
            PlayerPreferencesRuntime.Current.SetAudioMuted(AudioMuted);
            PlayerPreferencesRuntime.Current.ApplyAudio();
            RenderSettings();
        }

        private void ToggleMotion()
        {
            ReducedMotion = !ReducedMotion;
            PlayerPreferencesRuntime.Current.SetReducedMotion(ReducedMotion);
            app.EnableInClassList("main-menu--reduced-motion", ReducedMotion);
            stageEnvironment?.SetReducedMotion(ReducedMotion);
            RenderSettings();
        }

        private void DecreaseVolume() => ChangeVolume(-0.1f);
        private void IncreaseVolume() => ChangeVolume(0.1f);
        private void ChangeVolume(float delta)
        {
            float value = Mathf.Clamp01(PlayerPreferencesRuntime.Current.Snapshot.MasterVolume + delta);
            PlayerPreferencesRuntime.Current.SetMasterVolume(value);
            PlayerPreferencesRuntime.Current.ApplyAudio();
            RenderSettings();
        }

        private void ToggleTimingDiagnostics()
        {
            PlayerPreferencesSnapshot value = PlayerPreferencesRuntime.Current.Snapshot;
            PlayerPreferencesRuntime.Current.SetShowTimingDiagnostics(!value.ShowTimingDiagnostics);
            RenderSettings();
        }

        private void ToggleMetronome()
        {
            PlayerPreferencesSnapshot value = PlayerPreferencesRuntime.Current.Snapshot;
            PlayerPreferencesRuntime.Current.SetMetronomeEnabled(!value.MetronomeEnabled);
            RenderSettings();
        }

        private void ToggleHighContrast()
        {
            PlayerPreferencesSnapshot value = PlayerPreferencesRuntime.Current.Snapshot;
            PlayerPreferencesRuntime.Current.SetHighContrast(!value.HighContrast);
            ApplyPreferenceClasses();
            RenderSettings();
        }

        private void DecreaseKeyboardOffset() => ChangeOffset(DrumInputSource.Keyboard, -0.005);
        private void IncreaseKeyboardOffset() => ChangeOffset(DrumInputSource.Keyboard, 0.005);
        private void ResetKeyboardOffset() => SetOffset(DrumInputSource.Keyboard, 0);
        private void DecreaseMidiOffset() => ChangeOffset(DrumInputSource.Midi, -0.005);
        private void IncreaseMidiOffset() => ChangeOffset(DrumInputSource.Midi, 0.005);
        private void ResetMidiOffset() => SetOffset(DrumInputSource.Midi, 0);

        private void ChangeOffset(DrumInputSource source, double delta)
        {
            double current = PlayerPreferencesRuntime.Current.Snapshot.OffsetFor(source);
            SetOffset(source, Math.Max(-PlayerPreferencesService.MaximumOffsetSeconds,
                Math.Min(PlayerPreferencesService.MaximumOffsetSeconds, current + delta)));
        }

        private void SetOffset(DrumInputSource source, double value)
        {
            PlayerPreferencesRuntime.Current.SetInputOffset(source, value);
            RenderSettings();
        }

        private void BeginKickBinding() => BeginKeyBinding(DrumPad.Kick);
        private void BeginSnareBinding() => BeginKeyBinding(DrumPad.Snare);
        private void BeginHiHatBinding() => BeginKeyBinding(DrumPad.HiHat);
        private void BeginTom1Binding() => BeginKeyBinding(DrumPad.Tom1);
        private void BeginTom2Binding() => BeginKeyBinding(DrumPad.Tom2);
        private void BeginFloorTomBinding() => BeginKeyBinding(DrumPad.FloorTom);
        private void BeginCrashBinding() => BeginKeyBinding(DrumPad.Crash);
        private void BeginRideBinding() => BeginKeyBinding(DrumPad.Ride);

        private void BeginKeyBinding(DrumPad pad)
        {
            pendingKeyBinding = pad;
            bindingStatus.text = Language == MainMenuLanguage.Italian
                ? $"Premi ora il nuovo tasto per {PadLabel(pad, true)}. ESC annulla."
                : $"Press the new key for {PadLabel(pad, false)} now. ESC cancels.";
            RenderKeyBindings();
            app?.Focus();
        }

        private void HandleBindingKeyDown(KeyDownEvent evt)
        {
            if (!pendingKeyBinding.HasValue) return;
            if (evt.keyCode == KeyCode.Escape)
            {
                pendingKeyBinding = null;
                RenderSettings();
                evt.StopImmediatePropagation();
                return;
            }
            if (evt.keyCode == KeyCode.None) return;
            try
            {
                PlayerPreferencesRuntime.Current.SetKeyBinding(pendingKeyBinding.Value, evt.keyCode);
                pendingKeyBinding = null;
                bindingStatus.text = Language == MainMenuLanguage.Italian
                    ? "Tasto salvato."
                    : "Key saved.";
            }
            catch (Exception exception)
            {
                bindingStatus.text = Language == MainMenuLanguage.Italian
                    ? $"Tasto non disponibile: {exception.Message}"
                    : $"Key unavailable: {exception.Message}";
            }
            RenderKeyBindings();
            evt.StopImmediatePropagation();
        }

        private void ToggleFullscreen()
        {
            PlayerPreferencesSnapshot value = PlayerPreferencesRuntime.Current.Snapshot;
            PlayerPreferencesRuntime.Current.SetDisplay(!value.Fullscreen, value.WindowWidth, value.WindowHeight);
            PlayerPreferencesRuntime.Current.ApplyDisplay();
            RenderSettings();
        }

        private void SelectPreviousResolution() => ChangeResolution(-1);
        private void SelectNextResolution() => ChangeResolution(1);

        private void ChangeResolution(int direction)
        {
            PlayerPreferencesSnapshot value = PlayerPreferencesRuntime.Current.Snapshot;
            int index = 0;
            for (int candidate = 0; candidate < WindowSizes.Length; candidate++)
                if (WindowSizes[candidate].x == value.WindowWidth && WindowSizes[candidate].y == value.WindowHeight)
                    index = candidate;
            index = (index + direction + WindowSizes.Length) % WindowSizes.Length;
            Vector2Int size = WindowSizes[index];
            PlayerPreferencesRuntime.Current.SetDisplay(false, size.x, size.y);
            PlayerPreferencesRuntime.Current.ApplyDisplay();
            RenderSettings();
        }

        private void ResetPreferences()
        {
            if (!resetConfirmationPending)
            {
                resetConfirmationPending = true;
                resetStatus.text = Language == MainMenuLanguage.Italian
                    ? "Premi di nuovo per confermare. I progressi non saranno cancellati."
                    : "Press again to confirm. Progress will not be deleted.";
                return;
            }

            PlayerPreferencesRuntime.Current.Reset();
            PlayerPreferencesRuntime.Current.SetFirstRunCompleted(true);
            PlayerPreferencesRuntime.Current.ApplyAudio();
            AudioMuted = false;
            ReducedMotion = false;
            pendingKeyBinding = null;
            resetConfirmationPending = false;
            resetStatus.text = Language == MainMenuLanguage.Italian
                ? "Impostazioni ripristinate."
                : "Settings restored.";
            ApplyPreferenceClasses();
            RenderSettings();
        }

        private void ResetLocalData()
        {
            if (!resetDataConfirmationPending)
            {
                resetDataConfirmationPending = true;
                resetStatus.text = Language == MainMenuLanguage.Italian
                    ? "Premi di nuovo per cancellare progressi e configurazione batteria. I brani personali restano invariati."
                    : "Press again to delete progress and drum configuration. Personal songs are kept.";
                return;
            }

            try
            {
                GameplayProgressRuntime.Current.Reset();
                DeviceSetupConfigurationRuntime.DeleteSavedConfiguration();
                resetStatus.text = Language == MainMenuLanguage.Italian
                    ? "Progressi e configurazione batteria cancellati."
                    : "Progress and drum configuration deleted.";
            }
            catch (Exception exception)
            {
                resetStatus.text = Language == MainMenuLanguage.Italian
                    ? $"Cancellazione incompleta: {exception.Message}"
                    : $"Deletion incomplete: {exception.Message}";
            }
            resetDataConfirmationPending = false;
            RenderSettings();
        }

        private void CompleteKeyboardOnboarding()
        {
            PlayerPreferencesRuntime.Current.SetFirstRunCompleted(true);
            onboardingVisible = false;
            SetOverlay(onboardingOverlay, false);
            playButton?.Focus();
        }

        private void CompleteMidiOnboarding()
        {
            PlayerPreferencesRuntime.Current.SetFirstRunCompleted(true);
            onboardingVisible = false;
            SetOverlay(onboardingOverlay, false);
            HandleSetup();
        }

        private void ApplyPreferenceClasses()
        {
            if (app == null) return;
            PlayerPreferencesSnapshot value = PlayerPreferencesRuntime.Current.Snapshot;
            app.EnableInClassList("main-menu--reduced-motion", value.ReducedMotion);
            app.EnableInClassList("main-menu--high-contrast", value.HighContrast);
            stageEnvironment?.SetReducedMotion(value.ReducedMotion);
        }

        private void RenderKeyBindings()
        {
            PlayerPreferencesSnapshot preferences = PlayerPreferencesRuntime.Current.Snapshot;
            RenderKeyBinding(bindingKickButton, DrumPad.Kick, preferences.KickKey);
            RenderKeyBinding(bindingSnareButton, DrumPad.Snare, preferences.SnareKey);
            RenderKeyBinding(bindingHiHatButton, DrumPad.HiHat, preferences.HiHatKey);
            RenderKeyBinding(bindingTom1Button, DrumPad.Tom1, preferences.Tom1Key);
            RenderKeyBinding(bindingTom2Button, DrumPad.Tom2, preferences.Tom2Key);
            RenderKeyBinding(bindingFloorTomButton, DrumPad.FloorTom, preferences.FloorTomKey);
            RenderKeyBinding(bindingCrashButton, DrumPad.Crash, preferences.CrashKey);
            RenderKeyBinding(bindingRideButton, DrumPad.Ride, preferences.RideKey);
        }

        private void RenderKeyBinding(Button button, DrumPad pad, KeyCode key)
        {
            if (button == null) return;
            button.text = $"{PadLabel(pad, Language == MainMenuLanguage.Italian)} · {key}";
            button.EnableInClassList("keybinding-button--waiting", pendingKeyBinding == pad);
        }

        private static string PadLabel(DrumPad pad, bool italian)
        {
            switch (pad)
            {
                case DrumPad.Kick: return italian ? "GRANCASSA" : "KICK";
                case DrumPad.Snare: return italian ? "RULLANTE" : "SNARE";
                case DrumPad.HiHat: return "HI-HAT";
                case DrumPad.Tom1: return "TOM 1";
                case DrumPad.Tom2: return "TOM 2";
                case DrumPad.FloorTom: return italian ? "TIMPANO" : "FLOOR TOM";
                case DrumPad.Crash: return "CRASH";
                case DrumPad.Ride: return "RIDE";
                default: throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        public void SelectGameplayTheme(GameplayPresentationTheme theme)
        {
            try
            {
                GameplaySettingsRuntime.Current.SelectTheme(theme);
                stageEnvironment?.ApplyTheme(theme);
                backupStatusMessage = Language == MainMenuLanguage.Italian
                    ? "AMBIENTE SALVATO. SARÀ USATO DALLA PROSSIMA SESSIONE."
                    : "ENVIRONMENT SAVED. IT WILL BE USED BY THE NEXT SESSION.";
            }
            catch (Exception exception)
            {
                backupStatusMessage = Language == MainMenuLanguage.Italian
                    ? $"IMPOSSIBILE SALVARE L'AMBIENTE: {exception.Message}"
                    : $"COULD NOT SAVE THE ENVIRONMENT: {exception.Message}";
            }

            RenderSettings();
        }

        private void SelectArcadeTheme() => SelectGameplayTheme(GameplayPresentationTheme.ArcadeNeon);
        private void SelectConcertTheme() => SelectGameplayTheme(GameplayPresentationTheme.ConcertStage);
        private void SelectPrecisionTheme() => SelectGameplayTheme(GameplayPresentationTheme.PrecisionGrid);

        private void MoveSelection(int direction)
        {
            int count = Enum.GetValues(typeof(MainMenuDestination)).Length;
            int next = ((int)SelectedDestination + direction + count) % count;
            SelectDestination((MainMenuDestination)next, true);
        }

        private void MoveSongSelection(int direction)
        {
            if (songLibrary == null || songLibrary.Songs.Count == 0) return;
            int current = SongIndex(SelectedSong());
            int next = (current + direction + songLibrary.Songs.Count) % songLibrary.Songs.Count;
            SelectSong(songLibrary.Songs[next].Id);
        }

        private void Navigate(string sceneName, MainMenuDestination destination)
        {
            if (IsNavigationPending) return;
            DestinationRequested?.Invoke(destination);
            RequestedSceneName = sceneName;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                status.text = $"SCENA NON DISPONIBILE: {sceneName}";
                return;
            }
            IsNavigationPending = true;
            SetButtonsEnabled(false);
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private void RenderCopy()
        {
            MainMenuContent copy = MainMenuContent.For(Language);
            eyebrow.text = copy.Eyebrow;
            status.text = copy.Status;
            RenderDestination(playButton, copy.Destination(MainMenuDestination.Play));
            RenderDestination(learnButton, copy.Destination(MainMenuDestination.Learn));
            RenderDestination(setupButton, copy.Destination(MainMenuDestination.DeviceSetup));
            languageButton.text = Language == MainMenuLanguage.Italian ? "IT / EN" : "EN / IT";
            settingsButton.text = copy.Settings;
            settingsHeading.text = copy.Settings;
            exitButton.text = copy.Exit;
            learnHeading.text = copy.LearnHeading;
            learnCopy.text = copy.LearnCopy;
            if (songLibrary != null) RenderSongLibrary();
            RenderLearningPath();
            learnBackButton.text = copy.Back;
            settingsBackButton.text = copy.Back;
            RenderSettings();
            SelectDestination(SelectedDestination);
            RefreshInputStatus(true);
        }

        private void RenderSongLibrary()
        {
            if (songLibrary == null) return;
            bool italian = Language == MainMenuLanguage.Italian;
            songLibraryHeading.text = italian ? "SCEGLI IL BRANO" : "CHOOSE A SONG";
            songLibraryCopy.text = italian
                ? "La scaletta viene rilevata automaticamente dalle cartelle Songs."
                : "The setlist is discovered automatically from Songs folders.";
            songLibraryCount.text = $"{songLibrary.Songs.Count} " +
                                    (italian ? "BRANI RILEVATI" : "SONGS DETECTED");
            int ready = songLibrary.Songs.Count(song => song.IsPlayable);
            songLibraryReadyCount.text = $"{ready} " +
                                         (italian ? "PRONTI A SUONARE" : "READY TO PLAY");
            songLibrarySource.text = italian
                ? "BUNDLED + CARTELLA UTENTE"
                : "BUNDLED + USER FOLDER";
            songImportAudioButton.text = italian ? "IMPORTA AUDIO E CREA" : "IMPORT AUDIO & CREATE";

            songList.Clear();
            for (int index = 0; index < songLibrary.Songs.Count; index++)
            {
                SongLibraryEntry song = songLibrary.Songs[index];
                int displayNumber = index + 1;
                var row = new Button(() => SelectSong(song.Id)) { name = $"song-row-{song.Id}" };
                row.AddToClassList("song-row");
                row.EnableInClassList("song-row--selected", string.Equals(song.Id, selectedSongId, StringComparison.Ordinal));
                row.EnableInClassList("song-row--missing", !song.IsPlayable);
                row.Add(SongLabel($"{displayNumber:00}", "song-row-index"));
                var copy = new VisualElement();
                copy.AddToClassList("song-row-copy");
                copy.Add(SongLabel(song.Title.ToUpperInvariant(), "song-row-title"));
                copy.Add(SongLabel(song.Artist.ToUpperInvariant(), "song-row-artist"));
                copy.Add(SongLabel(SongMetadataLabel(song, italian), "song-row-meta"));
                row.Add(copy);
                row.Add(SongLabel(
                    song.IsPlayable
                        ? (italian ? "PRONTO" : "READY")
                        : (italian ? "NON DISPONIBILE" : "UNAVAILABLE"),
                    "song-row-state"));
                songList.Add(row);
            }

            SongLibraryEntry selected = SelectedSong();
            songDifficultyTitle.text = italian ? "DIFFICOLTÀ CHART" : "CHART DIFFICULTY";
            songSpeedTitle.text = italian ? "VELOCITÀ" : "SPEED";
            if (selected == null)
            {
                songDetailNumber.text = "—";
                songDetailTitle.text = italian ? "NESSUN BRANO" : "NO SONGS";
                songDetailArtist.text = string.Empty;
                songDetailAlbum.text = string.Empty;
                songDetailMetadata.text = string.Empty;
                songDetailOrigin.text = string.Empty;
                songDifficultyButtons.Clear();
                songDifficultyNote.text = italian ? "NESSUNA DIFFICOLTÀ" : "NO DIFFICULTY";
                songEffectiveBpm.text = "—";
                songSpeedNote.text = italian
                    ? "Seleziona un brano disponibile."
                    : "Select an available song.";
                songDetailReadiness.text = italian
                    ? "AGGIUNGI UNA CARTELLA CON song.json."
                    : "ADD A FOLDER CONTAINING song.json.";
                songPlayButton.SetEnabled(false);
                songRecordButton.SetEnabled(false);
                songBindAudioButton.SetEnabled(false);
                songBindAudioButton.style.display = DisplayStyle.None;
                songPlayButton.text = italian ? "NESSUN BRANO" : "NO SONGS";
                songRecordButton.text = italian ? "REGISTRA CHART" : "RECORD CHART";
                SetSongSpeedControlsEnabled(false);
                RenderSongSpeedSelection();
                return;
            }

            int selectedIndex = SongIndex(selected) + 1;
            songDetailNumber.text = $"{selectedIndex:00}";
            songDetailTitle.text = selected.Title.ToUpperInvariant();
            songDetailArtist.text = selected.Artist.ToUpperInvariant();
            string album = string.IsNullOrWhiteSpace(selected.Album)
                ? (italian ? "NON VERIFICATO" : "UNVERIFIED")
                : selected.Album.ToUpperInvariant();
            songDetailAlbum.text = "ALBUM · " + album +
                                   (selected.Year.HasValue ? $" · {selected.Year.Value}" : string.Empty);
            songDetailMetadata.text = SongMetadataLabel(selected, italian);
            songDetailOrigin.text = selected.Origin == SongLibraryOrigin.Bundled
                ? (italian ? "ORIGINE · LIBRERIA INCLUSA" : "SOURCE · BUNDLED LIBRARY")
                : (italian ? "ORIGINE · CARTELLA UTENTE" : "SOURCE · USER FOLDER");
            RenderSongDifficulties(selected, italian);
            songEffectiveBpm.text = selected.Bpm.HasValue
                ? $"{selected.Bpm.Value:0.#} BPM  →  {selected.Bpm.Value * selectedSongSpeed:0.#} BPM"
                : "—";
            bool canRecordChart = selected.IsPlayable || selected.CanAuthorChart;
            songSpeedNote.text = canRecordChart
                ? selected.IsPlayable
                    ? (italian
                        ? "Audio e chart rallentano insieme."
                        : "Audio and chart slow down together.")
                    : (italian
                        ? "L'audio rallenta mentre registri la prima chart."
                        : "Audio slows down while you record the first chart.")
                : (italian
                    ? "Disponibile quando audio e chart sono pronti."
                    : "Available when audio and chart are ready.");
            SetSongSpeedControlsEnabled(canRecordChart);
            RenderSongSpeedSelection();
            songDetailReadiness.EnableInClassList("song-detail-readiness--missing", !selected.IsPlayable);
            if (selected.IsPlayable)
            {
                songDetailReadiness.text = italian
                    ? "✓ AUDIO E CHART PRONTI · PREPARAZIONE DI ALMENO 6 SECONDI"
                    : "✓ AUDIO AND CHART READY · AT LEAST 6 SECONDS TO GET READY";
                songPlayButton.text = italian ? "SUONA ORA" : "PLAY NOW";
            }
            else
            {
                songDetailReadiness.text = selected.CanAuthorChart
                    ? (italian
                        ? "✓ AUDIO PRONTO · CHART DA REGISTRARE"
                        : "✓ AUDIO READY · CHART TO RECORD")
                    : SongAvailabilityLabel(selected, italian);
                songPlayButton.text = italian ? "CONTENUTI NON DISPONIBILI" : "CONTENT UNAVAILABLE";
            }
            songPlayButton.SetEnabled(selected.IsPlayable);
            songRecordButton.SetEnabled(canRecordChart);
            songRecordButton.text = selected.CanAuthorChart && !selected.IsPlayable
                ? (italian ? "REGISTRA PRIMA CHART" : "RECORD FIRST CHART")
                : (italian ? "REGISTRA CHART" : "RECORD CHART");
            songBindAudioButton.style.display = selected.CanBindAudio ? DisplayStyle.Flex : DisplayStyle.None;
            songBindAudioButton.SetEnabled(selected.CanBindAudio);
            songBindAudioButton.text = italian ? "ASSOCIA AUDIO LOCALE" : "BIND LOCAL AUDIO";
            songRefreshButton.text = italian ? "AGGIORNA LIBRERIA" : "REFRESH LIBRARY";
            songFolderButton.text = italian ? "APRI CARTELLA BRANI" : "OPEN SONG FOLDER";
            songBackButton.text = italian ? "INDIETRO" : "BACK";
            if (packageInbox?.ImportedSongIds.Count > 0)
            {
                songLibraryDiagnostic.text = italian
                    ? $"PACCHETTO .htksong IMPORTATO · {packageInbox.ImportedSongIds.Count}"
                    : $".htksong PACKAGE IMPORTED · {packageInbox.ImportedSongIds.Count}";
            }
            else if (packageInbox?.Diagnostics.Count > 0)
            {
                songLibraryDiagnostic.text = italian
                    ? $"{packageInbox.Diagnostics.Count} PACCHETTI .htksong IGNORATI"
                    : $"{packageInbox.Diagnostics.Count} .htksong PACKAGES IGNORED";
            }
            else
            {
                songLibraryDiagnostic.text = songLibrary.Diagnostics.Count == 0
                    ? (italian
                        ? "CATALOGO: CARTELLE + PACCHETTI .htksong"
                        : "CATALOG: FOLDERS + .htksong PACKAGES")
                    : $"{songLibrary.Diagnostics.Count} " + (italian ? "CARTELLE IGNORATE" : "FOLDERS IGNORED");
            }
        }

        private void SetSongSpeedControlsEnabled(bool enabled)
        {
            songSpeedSixtyButton.SetEnabled(enabled);
            songSpeedSeventyButton.SetEnabled(enabled);
            songSpeedEightyButton.SetEnabled(enabled);
            songSpeedNinetyButton.SetEnabled(enabled);
            songSpeedFullButton.SetEnabled(enabled);
        }

        private void RenderSongSpeedSelection()
        {
            songSpeedSixtyButton.EnableInClassList(
                "song-speed-button--selected", Math.Abs(selectedSongSpeed - 0.6) < 0.001);
            songSpeedSeventyButton.EnableInClassList(
                "song-speed-button--selected", Math.Abs(selectedSongSpeed - 0.7) < 0.001);
            songSpeedEightyButton.EnableInClassList(
                "song-speed-button--selected", Math.Abs(selectedSongSpeed - 0.8) < 0.001);
            songSpeedNinetyButton.EnableInClassList(
                "song-speed-button--selected", Math.Abs(selectedSongSpeed - 0.9) < 0.001);
            songSpeedFullButton.EnableInClassList(
                "song-speed-button--selected", Math.Abs(selectedSongSpeed - 1.0) < 0.001);
        }

        private void RenderSongDifficulties(SongLibraryEntry song, bool italian)
        {
            songDifficultyButtons.Clear();
            if (song.AvailableDifficulties.Count == 0)
            {
                songDifficultyNote.text = italian
                    ? "NESSUNA CHART DISPONIBILE"
                    : "NO CHART AVAILABLE";
                return;
            }

            EnsureSelectedDifficulty(song);
            for (int index = 0; index < song.AvailableDifficulties.Count; index++)
            {
                string difficulty = song.AvailableDifficulties[index];
                var button = new Button(() => SelectSongDifficulty(difficulty))
                {
                    name = $"song-difficulty-{difficulty}",
                    text = DifficultyLabel(difficulty, italian)
                };
                button.AddToClassList("song-difficulty-button");
                button.EnableInClassList(
                    "song-difficulty-button--selected",
                    string.Equals(difficulty, selectedSongDifficulty, StringComparison.Ordinal));
                button.SetEnabled(song.IsPlayable);
                songDifficultyButtons.Add(button);
            }
            songDifficultyNote.text = DifficultyTeachingLabel(italian);
        }

        private void EnsureSelectedDifficulty(SongLibraryEntry song)
        {
            if (song == null || song.AvailableDifficulties.Count == 0)
            {
                selectedSongDifficulty = null;
                return;
            }
            if (!ContainsDifficulty(song, selectedSongDifficulty))
                selectedSongDifficulty = song.AvailableDifficulties[0];
        }

        private static bool ContainsDifficulty(SongLibraryEntry song, string difficulty)
        {
            if (song == null || string.IsNullOrWhiteSpace(difficulty)) return false;
            for (int index = 0; index < song.AvailableDifficulties.Count; index++)
            {
                if (string.Equals(song.AvailableDifficulties[index], difficulty, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static string DifficultyLabel(string difficulty, bool italian)
        {
            switch (difficulty)
            {
                case "easy": return italian ? "FACILE" : "EASY";
                case "medium": return italian ? "MEDIO" : "MEDIUM";
                case "hard": return italian ? "DIFFICILE" : "HARD";
                case "advanced": return italian ? "AVANZATO" : "ADVANCED";
                case "full": return italian ? "BRANO COMPLETO" : "FULL SONG";
                case "expert": return italian ? "ESPERTO" : "EXPERT";
                default: return difficulty.ToUpperInvariant();
            }
        }

        private static string DifficultyTeachingLabel(bool italian)
        {
            return italian ? "LIVELLI LETTI DALLA CHART" : "LEVELS READ FROM THE CHART";
        }

        private static Label SongLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static string SongMetadataLabel(SongLibraryEntry song, bool italian)
        {
            string difficulty = string.IsNullOrWhiteSpace(song.DifficultyHint)
                ? (italian ? "DIFFICOLTÀ DA VALUTARE" : "DIFFICULTY UNRATED")
                : song.DifficultyHint.ToUpperInvariant();
            string tempo = song.Bpm.HasValue
                ? $"{song.Bpm.Value:0} BPM"
                : (italian ? "BPM NON VERIFICATO" : "BPM UNVERIFIED");
            return $"{difficulty} · {tempo}";
        }

        private static string SongAvailabilityLabel(SongLibraryEntry song, bool italian)
        {
            if (song.MissingFiles.Count > 0)
            {
                string files = string.Join(" + ", song.MissingFiles);
                return (italian ? "BINDING LOCALE INCOMPLETO · " : "INCOMPLETE LOCAL BINDING · ") + files;
            }

            string audio = song.AudioAvailability == SongAudioAvailability.Missing
                ? (italian ? "AUDIO MANCANTE" : "AUDIO MISSING")
                : (italian ? "AUDIO DISPONIBILE" : "AUDIO AVAILABLE");
            string chart = song.ChartAvailability == SongChartAvailability.Unavailable
                ? (italian ? "CHART NON DISPONIBILE" : "CHART UNAVAILABLE")
                : (italian ? "CHART DISPONIBILE" : "CHART AVAILABLE");
            return $"{audio} · {chart}";
        }

        private void RenderSettings()
        {
            MainMenuContent copy = MainMenuContent.For(Language);
            bool italian = Language == MainMenuLanguage.Italian;
            PlayerPreferencesSnapshot preferences = PlayerPreferencesRuntime.Current.Snapshot;
            AudioMuted = preferences.AudioMuted;
            ReducedMotion = preferences.ReducedMotion;
            audioButton.text = $"{copy.Audio}: {(AudioMuted ? "OFF" : "ON")}";
            motionButton.text = $"{copy.ReducedMotion}: {(ReducedMotion ? "ON" : "OFF")}";
            volumeValue.text = $"{(italian ? "VOLUME" : "VOLUME")} {Mathf.RoundToInt(preferences.MasterVolume * 100)}%";
            timingDiagnosticsButton.text = italian
                ? $"DIAGNOSTICA TIMING: {(preferences.ShowTimingDiagnostics ? "ON" : "OFF")}"
                : $"TIMING DIAGNOSTICS: {(preferences.ShowTimingDiagnostics ? "ON" : "OFF")}";
            metronomeButton.text = italian
                ? $"METRONOMO: {(preferences.MetronomeEnabled ? "ON" : "OFF")}"
                : $"METRONOME: {(preferences.MetronomeEnabled ? "ON" : "OFF")}";
            highContrastButton.text = italian
                ? $"ALTO CONTRASTO: {(preferences.HighContrast ? "ON" : "OFF")}"
                : $"HIGH CONTRAST: {(preferences.HighContrast ? "ON" : "OFF")}";
            keyboardOffsetValue.text = $"{(italian ? "TASTIERA" : "KEYBOARD")} {FormatOffset(preferences.KeyboardOffsetSeconds)}";
            midiOffsetValue.text = $"MIDI {FormatOffset(preferences.MidiOffsetSeconds)}";
            calibrationHelp.text = italian
                ? "Valori positivi compensano un input registrato in ritardo. Il risultato suggerisce una correzione dopo almeno 8 colpi."
                : "Positive values compensate late input. Results suggest a correction after at least 8 hits.";
            RenderKeyBindings();
            if (!pendingKeyBinding.HasValue && string.IsNullOrEmpty(bindingStatus.text))
                bindingStatus.text = italian
                    ? "Seleziona un pezzo, poi premi un tasto non ancora assegnato."
                    : "Select a piece, then press a key that is not already assigned.";
            fullscreenButton.text = italian
                ? $"SCHERMO INTERO: {(preferences.Fullscreen ? "ON" : "OFF")}"
                : $"FULLSCREEN: {(preferences.Fullscreen ? "ON" : "OFF")}";
            resolutionValue.text = $"{preferences.WindowWidth} × {preferences.WindowHeight}";
            resolutionPreviousButton.SetEnabled(!preferences.Fullscreen);
            resolutionNextButton.SetEnabled(!preferences.Fullscreen);
            resetPreferencesButton.text = italian ? "RIPRISTINA IMPOSTAZIONI" : "RESET SETTINGS";
            resetLocalDataButton.text = italian
                ? "CANCELLA PROGRESSI E CONFIGURAZIONE"
                : "DELETE PROGRESS AND CONFIGURATION";
            settingsThemeHeading.text = italian ? "AMBIENTE DI GIOCO" : "GAMEPLAY ENVIRONMENT";
            settingsThemeArcadeButton.text = "ARCADE NEON";
            settingsThemeConcertButton.text = "CONCERT STAGE";
            settingsThemePrecisionButton.text = "PRECISION GRID";
            GameplayPresentationTheme selectedTheme = GameplaySettingsRuntime.Current.Theme;
            stageEnvironment?.ApplyTheme(selectedTheme);
            settingsThemeArcadeButton.EnableInClassList(
                "theme-settings-button--selected",
                selectedTheme == GameplayPresentationTheme.ArcadeNeon);
            settingsThemeConcertButton.EnableInClassList(
                "theme-settings-button--selected",
                selectedTheme == GameplayPresentationTheme.ConcertStage);
            settingsThemePrecisionButton.EnableInClassList(
                "theme-settings-button--selected",
                selectedTheme == GameplayPresentationTheme.PrecisionGrid);
            settingsThemeDescription.text = ThemeDescription(selectedTheme, italian);
            GameplayProgressSnapshot progress = GameplayProgressRuntime.Current.Snapshot;
            string duration = GameplayDurationFormatter.Format(progress.TotalPracticeSeconds);
            settingsProgressSummary.text = italian
                ? $"{duration} DI ALLENAMENTO · {progress.CompletedSessionCount} SESSIONI"
                : $"{duration} PRACTICED · {progress.CompletedSessionCount} SESSIONS";
            settingsBackupPath.text = (italian ? "FILE: " : "FILE: ") + GameplayProgressRuntime.DefaultBackupPath;
            settingsExportProgressButton.text = italian ? "ESPORTA BACKUP" : "EXPORT BACKUP";
            settingsImportProgressButton.text = italian ? "IMPORTA E SOSTITUISCI" : "IMPORT AND REPLACE";
            settingsBackupStatus.text = string.IsNullOrEmpty(backupStatusMessage)
                ? GameplayProgressRuntime.Current.LastError
                : backupStatusMessage;
        }

        private static string FormatOffset(double seconds)
        {
            int milliseconds = Mathf.RoundToInt((float)(seconds * 1000));
            return $"{(milliseconds > 0 ? "+" : string.Empty)}{milliseconds} ms";
        }

        private static string ThemeDescription(GameplayPresentationTheme theme, bool italian)
        {
            switch (theme)
            {
                case GameplayPresentationTheme.ArcadeNeon:
                    return italian
                        ? "Pista olografica, colori neon e note a diamante."
                        : "Holographic runway, neon colors and diamond notes.";
                case GameplayPresentationTheme.ConcertStage:
                    return italian
                        ? "Palco live, luci calde e note circolari da concerto."
                        : "Live stage, warm lights and concert-style circular notes.";
                case GameplayPresentationTheme.PrecisionGrid:
                    return italian
                        ? "Batteria didattica colorata, zone evidenziate e guida per ogni colpo."
                        : "Color-coded training kit with highlighted zones and guidance for every hit.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme));
            }
        }

        private void ExportProgressBackup()
        {
            GameplayProgressOperationResult result = GameplayProgressRuntime.ExportDefaultBackup();
            backupStatusMessage = result.Succeeded
                ? (Language == MainMenuLanguage.Italian ? "BACKUP ESPORTATO. COPIA IL FILE SUL NUOVO MAC." : "BACKUP EXPORTED. COPY THE FILE TO THE NEW MAC.")
                : result.Message;
            RenderSettings();
        }

        private void ImportProgressBackup()
        {
            GameplayProgressOperationResult result = GameplayProgressRuntime.ImportDefaultBackup();
            backupStatusMessage = result.Succeeded
                ? (Language == MainMenuLanguage.Italian ? "BACKUP IMPORTATO. PROGRESSI AGGIORNATI." : "BACKUP IMPORTED. PROGRESS UPDATED.")
                : result.Message;
            RenderLearningPath();
            RenderSettings();
        }

        private static void RenderDestination(Button button, MainMenuDestinationContent content)
        {
            button.Q<Label>("choice-title").text = content.Title;
            button.Q<Label>("choice-subtitle").text = content.Subtitle;
        }

        private void RenderLearningPath()
        {
            bool italian = Language == MainMenuLanguage.Italian;
            GameplayLessonDefinition selected = GameplayLearningPath.Find(selectedLesson);
            learnList.Clear();
            string previousChapter = null;
            foreach (GameplayLessonDefinition lesson in GameplayLearningPath.All)
            {
                string chapter = italian ? lesson.ChapterItalian : lesson.ChapterEnglish;
                if (!string.Equals(previousChapter, chapter, StringComparison.Ordinal))
                {
                    var chapterLabel = new Label(
                        (italian ? $"MODULO {lesson.ModuleNumber} · " : $"MODULE {lesson.ModuleNumber} · ") +
                        chapter.ToUpperInvariant());
                    chapterLabel.AddToClassList("lesson-chapter");
                    learnList.Add(chapterLabel);
                    previousChapter = chapter;
                }

                bool completed = GameplayLearningProgress.IsCompleted(lesson.Id);
                bool unlocked = GameplayLearningProgress.IsUnlocked(lesson.Id);
                double? best = GameplayLearningProgress.BestAccuracy(lesson.Id, 1.0);
                var row = new Button(() => SelectLearningLesson(lesson.Id)) { name = $"learn-lesson-{lesson.Number:00}" };
                row.AddToClassList("lesson-row");
                row.EnableInClassList("lesson-row--selected", lesson.Id == selectedLesson);
                row.EnableInClassList("lesson-row--completed", completed);
                row.EnableInClassList("lesson-row--locked", !unlocked);
                row.Add(LessonLabel($"{lesson.Number:00}", "lesson-row-number"));
                var copy = new VisualElement();
                copy.AddToClassList("lesson-row-copy");
                copy.Add(LessonLabel(
                    (italian ? lesson.ItalianTitle : lesson.EnglishTitle).ToUpperInvariant(),
                    "lesson-row-title"));
                copy.Add(LessonLabel(
                    $"{lesson.DisciplineName(italian).ToUpperInvariant()} · {lesson.Focus} · " +
                    $"{lesson.Bpm:0} BPM · {lesson.PracticeMinutes} MIN",
                    "lesson-row-meta"));
                row.Add(copy);
                row.Add(LessonLabel(best.HasValue ? $"{best.Value:0.0}%" : "—", "lesson-row-accuracy"));
                string state = GameplayLearningProgress.IsMastered(lesson.Id)
                    ? (italian ? "PADRONANZA" : "MASTERED")
                    : completed
                        ? (italian ? "SUPERATA" : "PASSED")
                    : unlocked
                        ? lesson.IsModuleAssessment
                            ? (italian ? "PROVA MODULO" : "MODULE TEST")
                            : (italian ? "DISPONIBILE" : "AVAILABLE")
                        : lesson.IsPlayable
                            ? (italian ? "BLOCCATA" : "LOCKED")
                            : (italian ? "PROGRAMMA" : "SYLLABUS");
                row.Add(LessonLabel(state, "lesson-row-state"));
                row.SetEnabled(unlocked);
                learnList.Add(row);
            }

            int completedCount = GameplayLearningProgress.CompletedCount;
            learnProgressCount.text = $"{completedCount} / {GameplayLearningProgress.PlayableCount} " +
                                      (italian ? "LEZIONI ATTIVE" : "ACTIVE LESSONS");
            double? average = GameplayLearningProgress.AverageMasteryAccuracy;
            learnProgressAccuracy.text = (italian ? "PRECISIONE MEDIA " : "AVERAGE ACCURACY ") +
                                         (average.HasValue ? $"{average.Value:0.0}%" : "—");
            learnProgressTime.text = (italian ? "ALLENAMENTO " : "PRACTICE ") +
                                     GameplayDurationFormatter.Format(GameplayProgressRuntime.Current.Snapshot.TotalPracticeSeconds);
            learnProgressFill.style.width = Length.Percent(
                GameplayLearningProgress.PlayableCount == 0
                    ? 0
                    : completedCount * 100f / GameplayLearningProgress.PlayableCount);
            learnPassRule.text = italian
                ? "PASSA CON 80% A 1,0× · PADRONANZA CON 90% · LE PROVE CHIUDONO OGNI MODULO"
                : "PASS AT 80% ON 1.0× · MASTER AT 90% · MODULE TESTS CLOSE EACH TERM";

            learnSelectedNumber.text = $"{selected.Number:00}";
            learnSelectedTitle.text = (italian ? selected.ItalianTitle : selected.EnglishTitle).ToUpperInvariant();
            learnSelectedCopy.text = italian ? selected.ItalianDescription : selected.EnglishDescription;
            learnSelectedFocus.text =
                $"{selected.DisciplineName(italian).ToUpperInvariant()} · {selected.Focus} · " +
                $"{selected.Bpm:0} BPM · {selected.PracticeMinutes} MIN";
            learnSelectedObjective.text = (italian ? "OBIETTIVO · " : "OUTCOME · ") +
                                          (italian ? selected.ItalianObjective : selected.EnglishObjective);
            learnSelectedPattern.text = (italian ? "ESERCIZIO · " : "EXERCISE · ") + selected.ExercisePattern;
            double? selectedBest = GameplayLearningProgress.BestAccuracy(selected.Id, selectedStudySpeed);
            learnSelectedBest.text = (italian ? "MIGLIORE A QUESTA VELOCITÀ: " : "BEST AT THIS SPEED: ") +
                                     (selectedBest.HasValue ? $"{selectedBest.Value:0.0}%" : "—");
            learnEffectiveBpm.text = $"{selected.Bpm:0} BPM  →  {selected.Bpm * selectedStudySpeed:0} BPM";
            learnSpeedNote.text = italian
                ? "La precisione viene salvata per ogni velocità. Per superare la lezione serve 1,0×."
                : "Accuracy is tracked per speed. Passing the lesson requires 1.0×.";
            learnSpeedHalfButton.EnableInClassList("study-speed-button--selected", Math.Abs(selectedStudySpeed - 0.5) < 0.001);
            learnSpeedThreeQuarterButton.EnableInClassList("study-speed-button--selected", Math.Abs(selectedStudySpeed - 0.75) < 0.001);
            learnSpeedFullButton.EnableInClassList("study-speed-button--selected", Math.Abs(selectedStudySpeed - 1.0) < 0.001);
            bool canStart = GameplayLearningProgress.IsUnlocked(selected.Id);
            learnStartButton.SetEnabled(canStart);
            learnStartButton.text = canStart
                ? (italian ? $"INIZIA · {selected.Bpm * selectedStudySpeed:0} BPM" : $"START · {selected.Bpm * selectedStudySpeed:0} BPM")
                : selected.IsPlayable
                    ? (italian ? "COMPLETA LA LEZIONE PRECEDENTE" : "COMPLETE THE PREVIOUS LESSON")
                    : (italian ? "LEZIONE IN ARRIVO" : "LESSON COMING SOON");
        }

        private static Label LessonLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private void FocusSelectedLessonRow()
        {
            GameplayLessonDefinition selected = GameplayLearningPath.Find(selectedLesson);
            learnList?.Q<Button>($"learn-lesson-{selected.Number:00}")?.Focus();
        }

        private SongLibraryEntry SelectedSong()
        {
            if (songLibrary == null || string.IsNullOrEmpty(selectedSongId)) return null;
            return songLibrary.Songs.FirstOrDefault(
                song => string.Equals(song.Id, selectedSongId, StringComparison.Ordinal));
        }

        private int SongIndex(SongLibraryEntry song)
        {
            if (song == null || songLibrary == null) return 0;
            for (int index = 0; index < songLibrary.Songs.Count; index++)
            {
                if (ReferenceEquals(songLibrary.Songs[index], song) ||
                    string.Equals(songLibrary.Songs[index].Id, song.Id, StringComparison.Ordinal))
                    return index;
            }
            return 0;
        }

        private void FocusSelectedSongRow()
        {
            if (songList == null || string.IsNullOrEmpty(selectedSongId)) return;
            songList.Q<Button>($"song-row-{selectedSongId}")?.Focus();
        }

        private void HandleGeometryChanged(GeometryChangedEvent evt)
        {
            app.EnableInClassList("main-menu--compact", evt.newRect.width < 1050f);
            app.EnableInClassList("main-menu--short", evt.newRect.height < 780f);
        }

        private void ApplySelection(Button button, bool selected)
        {
            if (button == null) return;
            button.EnableInClassList("menu-choice--selected", selected);
            button.Q<Label>("choice-state").text = selected ? MainMenuContent.For(Language).Select : string.Empty;
        }

        private Button ButtonFor(MainMenuDestination destination)
        {
            switch (destination)
            {
                case MainMenuDestination.Play: return playButton;
                case MainMenuDestination.Learn: return learnButton;
                case MainMenuDestination.DeviceSetup: return setupButton;
                default: return null;
            }
        }

        private static void SetOverlay(VisualElement overlay, bool visible)
        {
            if (overlay != null) overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetSection(VisualElement section, bool visible)
        {
            if (section != null) section.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshInputStatus(bool force = false)
        {
            if (status == null) return;
            GameplayMidiState state = menuMidiInput == null ? GameplayMidiState.Unavailable : menuMidiInput.State;
            if (!force && state == lastRenderedMidiState) return;
            lastRenderedMidiState = state;
            bool italian = Language == MainMenuLanguage.Italian;
            if (state == GameplayMidiState.Connected)
            {
                status.text = italian
                    ? "INPUT ATTIVO: TASTIERA / CORE MIDI · DOPPIO KICK = CONFERMA"
                    : "ACTIVE INPUT: KEYBOARD / CORE MIDI · DOUBLE KICK = CONFIRM";
            }
            else
            {
                status.text = italian
                    ? "INPUT ATTIVO: TASTIERA · CORE MIDI NON COLLEGATO"
                    : "ACTIVE INPUT: KEYBOARD · CORE MIDI NOT CONNECTED";
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            playButton.SetEnabled(enabled);
            learnButton.SetEnabled(enabled);
            setupButton.SetEnabled(enabled);
        }

        private static T Required<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null) throw new InvalidOperationException($"Main menu UXML is missing required element '{name}'.");
            return element;
        }
    }
}
