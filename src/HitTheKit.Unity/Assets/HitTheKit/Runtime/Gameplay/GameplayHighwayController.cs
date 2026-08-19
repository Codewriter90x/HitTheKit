using System;
using System.Collections.Generic;
using System.Globalization;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Matching;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using HitTheKit.Unity.MainMenu;

namespace HitTheKit.Unity.Gameplay
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameplayHighwayController : MonoBehaviour
    {
        private const double HighwayLookAheadSeconds = 4.0;
        private const double KitPreparationSeconds = 1.35;
        private const float PulseDurationSeconds = 0.18f;
        private const int PracticeLeadInBeats = 2;

        [SerializeField] private UIDocument document;
        [SerializeField] private ChartTimelinePrototype chartTimeline;
        [SerializeField] private DspSongClockPrototype songClock;
        [SerializeField] private HitMatchingPrototype matching;
        [SerializeField] private CoreMidiGameplayInput midiInput;
        [SerializeField] private GameplaySessionCoordinator sessionCoordinator;
        [SerializeField] private AudioSource drumFeedbackSource;
        [SerializeField] private Texture2D arcadeNeonBackground;
        [SerializeField] private Texture2D concertStageBackground;
        [SerializeField] private Texture2D precisionGridBackground;

        private readonly Dictionary<DrumPad, VisualElement> targets =
            new Dictionary<DrumPad, VisualElement>();
        private readonly Dictionary<DrumPad, float> pulseDeadlines =
            new Dictionary<DrumPad, float>();

        private VisualElement root;
        private VisualElement background;
        private VisualElement progressFill;
        private Label scoreLabel;
        private Label trackNameLabel;
        private Label trackMetaLabel;
        private Label sessionKickerLabel;
        private Label comboLabel;
        private Label accuracyLabel;
        private Label positionLabel;
        private Label judgmentLabel;
        private Label kitGuidanceLabel;
        private Label environmentTitleLabel;
        private Label environmentSubtitleLabel;
        private Label currentInputLabel;
        private Label deviceLabel;
        private Label keyGuideCymbalsLabel;
        private Label keyGuideTomsLabel;
        private Label keyGuideSnareHiHatLabel;
        private Label keyGuideFloorKickLabel;
        private Label impactCueLabel;
        private Button menuButton;
        private Button pauseButton;
        private Button resumeButton;
        private Button restartButton;
        private Button resultRestartButton;
        private Button resultMenuButton;
        private Button resultApplyCalibrationButton;
        private Button practicePreviousSectionButton;
        private Button practiceNextSectionButton;
        private Button practiceLoopSectionButton;
        private Button practiceSetAButton;
        private Button practiceSetBButton;
        private Button practiceClearButton;
        private VisualElement pauseOverlay;
        private VisualElement resultsOverlay;
        private VisualElement countdownOverlay;
        private Label countdownLabel;
        private Label resultRankLabel;
        private Label resultScoreLabel;
        private Label resultAccuracyLabel;
        private Label resultComboLabel;
        private Label resultBreakdownLabel;
        private Label resultPracticeLabel;
        private Label resultCalibrationLabel;
        private Label practiceSectionLabel;
        private Label practiceStatusLabel;
        private GameplayHighwaySurface surface;
        private GameplayKitSurface kitSurface;
        private bool showInstructionalKit;
        private HitMatchingPrototype subscribedMatching;
        private readonly GameplayScoreTracker scoreTracker = new GameplayScoreTracker();
        private readonly GameplayPracticeTimer practiceTimer = new GameplayPracticeTimer();
        private readonly TimingCalibrationAdvisor keyboardCalibration = new TimingCalibrationAdvisor();
        private readonly TimingCalibrationAdvisor midiCalibration = new TimingCalibrationAdvisor();
        private readonly PracticePerformanceAnalyzer performanceAnalyzer = new PracticePerformanceAnalyzer();
        private DrumInputSource lastCalibrationSource = DrumInputSource.Keyboard;
        private DrumPad? latestPulsePad;
        private bool invalidConfigurationLogged;
        private IReadOnlyDictionary<DrumPad, AudioClip> drumClips;
        private AudioClip mistakeClip;
        private AudioSource metronomeSource;
        private AudioClip metronomeClip;
        private bool metronomeScheduled;
        private bool metronomeSeekedWhilePaused;
        private bool resultRecorded;
        private readonly GameplayPracticeLoop practiceLoop = new GameplayPracticeLoop();
        private IReadOnlyList<GameplayPracticeRange> practiceSections = Array.Empty<GameplayPracticeRange>();
        private int selectedPracticeSectionIndex;

        public event Action<GameplayPresentationTheme> ThemeChanged;

        public GameplayPresentationTheme Theme { get; private set; }
        public GameplayHighwaySurface Surface => surface;
        public GameplayKitSurface KitSurface => kitSurface;
        public bool IsInstructionalKitVisible => showInstructionalKit;
        public Texture2D ActiveBackground => BackgroundFor(Theme);
        public string EnvironmentTitle => GameplayEnvironmentProfile.For(Theme).Title;
        public string EnvironmentSubtitle => GameplayEnvironmentProfile.For(Theme).Subtitle;
        public int TargetCount => targets.Count;
        public bool IsViewBound => root != null && surface != null && kitSurface != null;
        public bool IsReturningToMenu { get; private set; }
        public GameplayRunState RunState { get; private set; } = GameplayRunState.Countdown;
        public GameplayScoreSnapshot ScoreSnapshot => scoreTracker.Snapshot;
        public int CountdownBeat { get; private set; }
        public string SessionTitle => CurrentSession.Title;
        public GameplaySessionKind SessionKind => CurrentSession.Kind;
        public GameplaySessionDefinition CurrentSession =>
            sessionCoordinator?.Session ?? GameplaySessionContext.Current;
        public double CurrentAttemptPracticeSeconds => practiceTimer.CurrentAttemptSeconds;
        public GameplayPracticeRange ActivePracticeRange => practiceLoop.Range;
        public IReadOnlyList<GameplayPracticeRange> PracticeSections => practiceSections;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            PlayerPreferencesRuntime.Current.ApplyAudio();
        }

        private void OnEnable()
        {
            BindView();
            BindNavigation();
            Subscribe();
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                LogInvalidConfigurationOnce();
                enabled = false;
                return;
            }

            BindView();
            BindNavigation();
            Subscribe();
            InitializeAudioFeedback();
            InitializePracticeLab();
            SetTheme(CurrentSession.Theme);
            RefreshSessionCopy();
        }

        private void Update()
        {
            HandleShortcuts();
            TrackPracticeTime();
            TryScheduleMetronome();
            UpdatePracticeLoop();
            UpdatePulseState();
            RefreshPresentation();
        }

        private void OnDisable()
        {
            FlushPracticeTime();
            if (menuButton != null) menuButton.clicked -= ReturnToMainMenu;
            UnbindRunControls();
            Unsubscribe();
        }

        private void OnDestroy()
        {
            FlushPracticeTime();
            ReleaseAudioFeedback();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushPracticeTime();
        }

        private void OnApplicationQuit() => FlushPracticeTime();

        private void SetTheme(GameplayPresentationTheme theme)
        {
            if (!Enum.IsDefined(typeof(GameplayPresentationTheme), theme))
            {
                throw new ArgumentOutOfRangeException(nameof(theme));
            }

            Theme = theme;
            if (root == null) BindView();
            if (root == null) return;

            root.RemoveFromClassList("theme--arcade-neon");
            root.RemoveFromClassList("theme--concert-stage");
            root.RemoveFromClassList("theme--precision-grid");
            root.AddToClassList(ThemeClass(theme));

            GameplayEnvironmentProfile environment = GameplayEnvironmentProfile.For(theme);
            showInstructionalKit = environment.ShowsInstructionalKit;
            if (environmentTitleLabel != null) environmentTitleLabel.text = environment.Title;
            if (environmentSubtitleLabel != null) environmentSubtitleLabel.text = environment.Subtitle;

            if (background != null)
            {
                Texture2D texture = BackgroundFor(theme);
                background.style.backgroundImage = texture == null
                    ? new StyleBackground(StyleKeyword.None)
                    : new StyleBackground(texture);
            }

            surface?.SetTheme(theme);
            if (impactCueLabel != null)
                impactCueLabel.style.top = Length.Percent(environment.StrikeRatio * 100f);
            if (kitSurface != null)
            {
                kitSurface.SetTheme(theme);
                kitSurface.style.display = showInstructionalKit ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (kitGuidanceLabel != null)
            {
                kitGuidanceLabel.style.display = showInstructionalKit ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showInstructionalKit) kitGuidanceLabel.text = string.Empty;
            }
            ThemeChanged?.Invoke(theme);
        }

        private void BindView()
        {
            if (document == null) document = GetComponent<UIDocument>();
            VisualElement candidateRoot = document?.rootVisualElement?.Q<VisualElement>("gameplay-app");
            if (candidateRoot == null || ReferenceEquals(root, candidateRoot)) return;

            root = candidateRoot;
            background = root.Q<VisualElement>("gameplay-background");
            progressFill = root.Q<VisualElement>("song-progress-fill");
            scoreLabel = root.Q<Label>("score-value");
            trackNameLabel = root.Q<Label>("track-name");
            trackMetaLabel = root.Q<Label>("track-meta");
            sessionKickerLabel = root.Q<Label>("session-kicker");
            comboLabel = root.Q<Label>("combo-value");
            accuracyLabel = root.Q<Label>("accuracy-value");
            positionLabel = root.Q<Label>("song-position");
            judgmentLabel = root.Q<Label>("judgment-label");
            kitGuidanceLabel = root.Q<Label>("kit-guidance-label");
            environmentTitleLabel = root.Q<Label>("environment-title");
            environmentSubtitleLabel = root.Q<Label>("environment-subtitle");
            currentInputLabel = root.Q<Label>("current-input");
            deviceLabel = root.Q<Label>("device-status");
            keyGuideCymbalsLabel = root.Q<Label>("key-guide-cymbals");
            keyGuideTomsLabel = root.Q<Label>("key-guide-toms");
            keyGuideSnareHiHatLabel = root.Q<Label>("key-guide-snare-hihat");
            keyGuideFloorKickLabel = root.Q<Label>("key-guide-floor-kick");
            menuButton = root.Q<Button>("menu-button");
            pauseButton = root.Q<Button>("pause-button");
            resumeButton = root.Q<Button>("resume-button");
            restartButton = root.Q<Button>("restart-button");
            resultRestartButton = root.Q<Button>("result-restart-button");
            resultMenuButton = root.Q<Button>("result-menu-button");
            resultApplyCalibrationButton = root.Q<Button>("result-apply-calibration");
            practicePreviousSectionButton = root.Q<Button>("practice-previous-section");
            practiceNextSectionButton = root.Q<Button>("practice-next-section");
            practiceLoopSectionButton = root.Q<Button>("practice-loop-section");
            practiceSetAButton = root.Q<Button>("practice-set-a");
            practiceSetBButton = root.Q<Button>("practice-set-b");
            practiceClearButton = root.Q<Button>("practice-clear");
            pauseOverlay = root.Q<VisualElement>("pause-overlay");
            resultsOverlay = root.Q<VisualElement>("results-overlay");
            countdownOverlay = root.Q<VisualElement>("countdown-overlay");
            countdownLabel = root.Q<Label>("countdown-label");
            resultRankLabel = root.Q<Label>("result-rank");
            resultScoreLabel = root.Q<Label>("result-score");
            resultAccuracyLabel = root.Q<Label>("result-accuracy");
            resultComboLabel = root.Q<Label>("result-combo");
            resultBreakdownLabel = root.Q<Label>("result-breakdown");
            resultPracticeLabel = root.Q<Label>("result-practice");
            resultCalibrationLabel = root.Q<Label>("result-calibration");
            practiceSectionLabel = root.Q<Label>("practice-section-label");
            practiceStatusLabel = root.Q<Label>("practice-status");
            PlayerPreferencesSnapshot preferences = PlayerPreferencesRuntime.Current.Snapshot;
            root.EnableInClassList("gameplay--high-contrast", preferences.HighContrast);
            root.EnableInClassList("gameplay--reduced-motion", preferences.ReducedMotion);
            if (keyGuideCymbalsLabel != null) keyGuideCymbalsLabel.text = $"{preferences.CrashKey}/{preferences.RideKey}  PIATTI";
            if (keyGuideTomsLabel != null) keyGuideTomsLabel.text = $"{preferences.Tom1Key}/{preferences.Tom2Key}  TOM";
            if (keyGuideSnareHiHatLabel != null) keyGuideSnareHiHatLabel.text = $"{preferences.SnareKey}/{preferences.HiHatKey}  RULLANTE / HI-HAT";
            if (keyGuideFloorKickLabel != null) keyGuideFloorKickLabel.text = $"{preferences.FloorTomKey}/{preferences.KickKey}  TIMPANO / GRANCASSA";

            VisualElement highwayHost = root.Q<VisualElement>("highway-host");
            VisualElement kitVisualHost = root.Q<VisualElement>("kit-visual-host");
            VisualElement targetsHost = root.Q<VisualElement>("targets-host");
            VisualElement kickHost = root.Q<VisualElement>("kick-target-host");
            if (highwayHost == null || kitVisualHost == null || targetsHost == null || kickHost == null)
            {
                Debug.LogError("Gameplay highway UXML is missing a required host element.", this);
                enabled = false;
                return;
            }

            highwayHost.Clear();
            surface = new GameplayHighwaySurface { name = "gameplay-highway-surface" };
            surface.AddToClassList("highway-surface");
            highwayHost.Add(surface);
            impactCueLabel = new Label("COLPISCI ORA") { name = "impact-cue" };
            impactCueLabel.AddToClassList("impact-cue");
            highwayHost.Add(impactCueLabel);
            kitVisualHost.Clear();
            kitSurface = new GameplayKitSurface();
            kitVisualHost.Add(kitSurface);
            BuildTargets(targetsHost, kickHost);

            BindRunControls();
            SetTheme(CurrentSession.Theme);
            RefreshSessionCopy();
        }

        private void BindNavigation()
        {
            if (menuButton == null) return;
            menuButton.clicked -= ReturnToMainMenu;
            menuButton.clicked += ReturnToMainMenu;
        }

        private void BuildTargets(VisualElement targetsHost, VisualElement kickHost)
        {
            targets.Clear();
            targetsHost.Clear();
            kickHost.Clear();

            IReadOnlyList<GameplayLaneDefinition> lanes = GameplayHighwayLanes.All;
            for (int index = 0; index < GameplayHighwayLanes.HighwayLaneCount; index++)
            {
                GameplayLaneDefinition lane = lanes[index];
                VisualElement target = CreateTarget(lane, isKick: false);
                targetsHost.Add(target);
                targets.Add(lane.Pad, target);
            }

            GameplayLaneDefinition kick = GameplayHighwayLanes.Find(DrumPad.Kick);
            VisualElement kickTarget = CreateTarget(kick, isKick: true);
            kickHost.Add(kickTarget);
            targets.Add(kick.Pad, kickTarget);
        }

        private static VisualElement CreateTarget(GameplayLaneDefinition lane, bool isKick)
        {
            var target = new VisualElement { name = $"target-{lane.Id}" };
            target.AddToClassList(isKick ? "kick-target" : "lane-target");
            target.AddToClassList($"lane--{lane.Id}");
            target.style.borderTopColor = lane.Color;
            target.style.borderRightColor = lane.Color;
            target.style.borderBottomColor = lane.Color;
            target.style.borderLeftColor = lane.Color;

            var header = new VisualElement();
            header.AddToClassList("target-header");
            var key = new Label(lane.Key);
            key.AddToClassList("target-key");
            var name = new Label(lane.Label);
            name.AddToClassList("target-name");
            header.Add(key);
            header.Add(name);
            target.Add(header);

            var subtitle = new Label(lane.Subtitle);
            subtitle.AddToClassList("target-subtitle");
            target.Add(subtitle);
            return target;
        }

        private void RefreshPresentation()
        {
            if (surface == null) return;

            double position = 0;
            bool scheduled = songClock != null && songClock.Clock != null && songClock.Clock.IsScheduled;
            if (scheduled) position = songClock.PositionSeconds;
            IReadOnlyList<TimelineNote> upcoming = chartTimeline?.UpcomingNotes ?? Array.Empty<TimelineNote>();
            float pulse = latestPulsePad.HasValue && pulseDeadlines.TryGetValue(latestPulsePad.Value, out float deadline)
                ? Mathf.Clamp01((deadline - Time.unscaledTime) / PulseDurationSeconds)
                : 0;
            surface.SetFrame(upcoming, position, HighwayLookAheadSeconds, latestPulsePad, pulse);
            if (showInstructionalKit)
                kitSurface?.SetFrame(upcoming, position, KitPreparationSeconds, latestPulsePad, pulse);
            if (showInstructionalKit && kitGuidanceLabel != null && kitSurface != null)
                kitGuidanceLabel.text = kitSurface.GuidanceText;

            HitMatchingSnapshot snapshot = matching?.Snapshot;
            GameplayScoreSnapshot score = scoreTracker.Snapshot;
            scoreLabel.text = score.Score.ToString("N0", CultureInfo.InvariantCulture);
            comboLabel.text = score.Combo.ToString(CultureInfo.InvariantCulture);
            accuracyLabel.text = $"{score.Accuracy:0.0}%";
            float duration = songClock?.GeneratedClip?.length ?? 0;
            double displayedPosition = duration <= 0 ? position : Math.Min(position, duration);
            positionLabel.text = FormatPosition(displayedPosition, duration);
            deviceLabel.text = midiInput == null ? "TASTIERA" : midiInput.StatusMessage;

            UpdateRunState(position, snapshot);

            if (progressFill != null)
            {
                float progress = duration <= 0 ? 0 : Mathf.Clamp01((float)(position / duration));
                progressFill.style.width = Length.Percent(progress * 100f);
            }
        }

        private void HandleInputProcessed(DrumInputEvent input, HitResult result)
        {
            latestPulsePad = input.Pad;
            pulseDeadlines[input.Pad] = Time.unscaledTime + PulseDurationSeconds;
            lastCalibrationSource = input.Source == DrumInputSource.Midi
                ? DrumInputSource.Midi
                : DrumInputSource.Keyboard;
            string timing = string.Empty;
            if (result?.DeltaSeconds != null)
            {
                AdvisorFor(lastCalibrationSource).Add(result.DeltaSeconds.Value);
                if (PlayerPreferencesRuntime.Current.Snapshot.ShowTimingDiagnostics)
                    timing = $"  ·  {FormatDelta(result.DeltaSeconds.Value)}";
            }
            currentInputLabel.text = $"{GameplayHighwayLanes.Find(input.Pad).Label}  ·  VELOCITY {input.Velocity}{timing}";

            GameplayAudioFeedbackDecision audioDecision = GameplayAudioFeedbackPolicy.ForInput(input, result);
            if (audioDecision.PlayDrum) PlayDrum(input.Pad, input.Velocity);
            if (audioDecision.PlayMistake) PlayMistake();

            if (result == null)
            {
                scoreTracker.Apply(null);
                SetJudgment("NO MATCH", "judgment--miss");
                return;
            }

            string judgment = result.Grade.ToString().ToUpperInvariant();
            if (PlayerPreferencesRuntime.Current.Snapshot.ShowTimingDiagnostics && result.DeltaSeconds.HasValue)
                judgment += $" · {FormatDelta(result.DeltaSeconds.Value)}";
            SetJudgment(judgment, $"judgment--{result.Grade.ToString().ToLowerInvariant()}");
        }

        private void HandleHitResolved(HitResult result)
        {
            if (result == null) return;
            performanceAnalyzer.Record(result.Note.Pad, result.Grade);
            scoreTracker.Apply(result);
            if (result.Grade == HitGrade.Miss)
            {
                SetJudgment("MISS", "judgment--miss");
                if (GameplayAudioFeedbackPolicy.ShouldPlayMiss(result)) PlayMistake();
            }
        }

        private void SetJudgment(string text, string className)
        {
            if (judgmentLabel == null) return;
            judgmentLabel.text = text;
            judgmentLabel.RemoveFromClassList("judgment--perfect");
            judgmentLabel.RemoveFromClassList("judgment--good");
            judgmentLabel.RemoveFromClassList("judgment--early");
            judgmentLabel.RemoveFromClassList("judgment--late");
            judgmentLabel.RemoveFromClassList("judgment--miss");
            judgmentLabel.AddToClassList(className);
        }

        private void UpdatePulseState()
        {
            if (targets.Count == 0) return;
            float now = Time.unscaledTime;
            foreach (KeyValuePair<DrumPad, VisualElement> pair in targets)
            {
                bool active = pulseDeadlines.TryGetValue(pair.Key, out float deadline) && deadline > now;
                pair.Value.EnableInClassList("target--active", active);
            }
        }

        private void HandleShortcuts()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P)) TogglePause();
        }

        public void TogglePause()
        {
            if (RunState == GameplayRunState.Results) return;
            if (RunState == GameplayRunState.Paused)
            {
                songClock.ResumePlayback();
                if (metronomeScheduled && metronomeSource != null)
                {
                    if (metronomeSeekedWhilePaused)
                    {
                        metronomeSource.Play();
                        metronomeSeekedWhilePaused = false;
                    }
                    else
                    {
                        metronomeSource.UnPause();
                    }
                }
                RunState = songClock.PositionSeconds < 0 ? GameplayRunState.Countdown : GameplayRunState.Playing;
                SetDisplayed(pauseOverlay, false);
            }
            else
            {
                FlushPracticeTime();
                songClock.PausePlayback();
                if (metronomeScheduled) metronomeSource?.Pause();
                RunState = GameplayRunState.Paused;
                SetDisplayed(pauseOverlay, true);
            }
        }

        public void RestartRun()
        {
            practiceLoop.Clear();
            RefreshPracticeStatus();
            FlushPracticeTime();
            practiceTimer.ResetAttempt();
            resultRecorded = false;
            scoreTracker.Reset();
            keyboardCalibration.Reset();
            midiCalibration.Reset();
            performanceAnalyzer.Reset();
            if (metronomeSource != null) metronomeSource.Stop();
            metronomeScheduled = false;
            pulseDeadlines.Clear();
            latestPulsePad = null;
            matching.RestartSession();
            songClock.RestartPlayback();
            RunState = GameplayRunState.Countdown;
            SetDisplayed(pauseOverlay, false);
            SetDisplayed(resultsOverlay, false);
            SetJudgment("READY", "judgment--good");
        }

        public void SelectPreviousPracticeSection()
        {
            if (practiceSections.Count == 0) return;
            selectedPracticeSectionIndex = Math.Max(0, selectedPracticeSectionIndex - 1);
            RefreshPracticeStatus();
        }

        public void SelectNextPracticeSection()
        {
            if (practiceSections.Count == 0) return;
            selectedPracticeSectionIndex = Math.Min(practiceSections.Count - 1, selectedPracticeSectionIndex + 1);
            RefreshPracticeStatus();
        }

        public void LoopSelectedPracticeSection()
        {
            if (practiceSections.Count == 0) return;
            GameplayPracticeRange selected = practiceSections[selectedPracticeSectionIndex];
            double duration = songClock?.Clock?.DurationSeconds ?? selected.EndSeconds;
            double end = Math.Min(selected.EndSeconds, duration);
            if (selected.StartSeconds >= end)
            {
                if (practiceStatusLabel != null) practiceStatusLabel.text = "SEZIONE FUORI DALLA DURATA AUDIO";
                return;
            }
            practiceLoop.Select(new GameplayPracticeRange(selected.StartSeconds, end, selected.Label));
            RestartPracticePass();
            RefreshPracticeStatus();
        }

        public void SetPracticePointA()
        {
            practiceLoop.SetStart(CurrentClampedSongPosition());
            RefreshPracticeStatus();
        }

        public void SetPracticePointB()
        {
            try
            {
                practiceLoop.SetEnd(CurrentClampedSongPosition());
                RestartPracticePass();
            }
            catch (InvalidOperationException)
            {
                if (practiceStatusLabel != null) practiceStatusLabel.text = "IMPOSTA PRIMA IL PUNTO A";
                return;
            }
            catch (ArgumentOutOfRangeException)
            {
                if (practiceStatusLabel != null) practiceStatusLabel.text = "IL PUNTO B DEVE ESSERE DOPO A";
                return;
            }
            RefreshPracticeStatus();
        }

        public void ClearPracticeLoop()
        {
            if (!practiceLoop.IsEnabled && !practiceLoop.PendingStartSeconds.HasValue) return;
            RestartRun();
        }

        private void InitializePracticeLab()
        {
            GameplaySessionDefinition session = CurrentSession;
            practiceSections = GameplayPracticeSections.Create(session.Bars, session.BeatsPerBar, session.Bpm);
            selectedPracticeSectionIndex = 0;
            RefreshPracticeStatus();
        }

        private void UpdatePracticeLoop()
        {
            if (!practiceLoop.IsEnabled || RunState == GameplayRunState.Paused ||
                RunState == GameplayRunState.Results || songClock?.Clock == null || !songClock.Clock.IsScheduled)
                return;
            if (practiceLoop.ShouldRestart(Math.Max(0, songClock.PositionSeconds))) RestartPracticePass();
        }

        private void RestartPracticePass()
        {
            GameplayPracticeRange range = practiceLoop.Range;
            if (range == null || matching == null || songClock?.Clock == null) return;

            FlushPracticeTime();
            resultRecorded = false;
            scoreTracker.Reset();
            keyboardCalibration.Reset();
            midiCalibration.Reset();
            performanceAnalyzer.Reset();
            pulseDeadlines.Clear();
            latestPulsePad = null;
            matching.RestartSession(range.StartSeconds, range.EndSeconds);

            double leadIn = PracticeLeadInBeats * 60.0 / CurrentSession.Bpm;
            double playbackStart = Math.Max(0, range.StartSeconds - leadIn);
            songClock.SeekPlayback(playbackStart);
            SeekMetronome(playbackStart);
            RunState = songClock.Clock.IsPaused ? GameplayRunState.Paused : GameplayRunState.Playing;
            SetDisplayed(resultsOverlay, false);
            SetJudgment("PRACTICE", "judgment--good");
        }

        private double CurrentClampedSongPosition()
        {
            if (songClock?.Clock == null || !songClock.Clock.IsScheduled) return 0;
            double maximum = Math.Max(0, songClock.Clock.DurationSeconds - 0.001);
            return Math.Min(maximum, Math.Max(0, songClock.PositionSeconds));
        }

        private void SeekMetronome(double positionSeconds)
        {
            if (!metronomeScheduled || metronomeSource == null || metronomeClip == null) return;
            bool paused = songClock.Clock.IsPaused;
            metronomeSource.Stop();
            metronomeSource.time = Mathf.Clamp(
                (float)positionSeconds,
                0,
                Math.Max(0, metronomeClip.length - 0.001f));
            if (paused)
            {
                metronomeSeekedWhilePaused = true;
            }
            else
            {
                metronomeSource.Play();
                metronomeSeekedWhilePaused = false;
            }
        }

        private void RefreshPracticeStatus()
        {
            if (practiceSections.Count > 0 && practiceSectionLabel != null)
                practiceSectionLabel.text = practiceSections[selectedPracticeSectionIndex].Label;
            if (practiceStatusLabel == null) return;
            if (practiceLoop.IsEnabled)
            {
                practiceStatusLabel.text = $"ATTIVO · {practiceLoop.Range.Label} · " +
                    $"{FormatSeconds(practiceLoop.Range.StartSeconds)} → {FormatSeconds(practiceLoop.Range.EndSeconds)}";
            }
            else if (practiceLoop.PendingStartSeconds.HasValue)
            {
                practiceStatusLabel.text = $"A = {FormatSeconds(practiceLoop.PendingStartSeconds.Value)} · ORA IMPOSTA B";
            }
            else
            {
                practiceStatusLabel.text = "Loop disattivato · scegli una sezione o imposta A e B";
            }
        }

        private static string FormatSeconds(double seconds) =>
            TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss", CultureInfo.InvariantCulture);

        private void UpdateRunState(double position, HitMatchingSnapshot snapshot)
        {
            if (RunState == GameplayRunState.Paused || RunState == GameplayRunState.Results) return;
            if (position < 0)
            {
                RunState = GameplayRunState.Countdown;
                CountdownBeat = GameplayCountIn.RemainingBeat(position, songClock.Bpm, songClock.CountInBeats);
                if (countdownLabel != null)
                {
                    countdownLabel.text = CountdownBeat.ToString(CultureInfo.InvariantCulture);
                    SetDisplayed(countdownOverlay, true);
                }
                return;
            }

            CountdownBeat = 0;
            SetDisplayed(countdownOverlay, false);
            RunState = GameplayRunState.Playing;
            if (snapshot != null && snapshot.IsComplete && songClock.HasCompleted) ShowResults(snapshot);
        }

        private void ShowResults(HitMatchingSnapshot matchingSnapshot)
        {
            if (resultRecorded) return;
            RunState = GameplayRunState.Results;
            FlushPracticeTime();
            GameplayScoreSnapshot score = scoreTracker.Snapshot;
            RecordCompletedSession(matchingSnapshot, score);
            resultRecorded = true;
            resultRankLabel.text = score.Rank;
            resultScoreLabel.text = score.Score.ToString("N0", CultureInfo.InvariantCulture);
            resultAccuracyLabel.text = $"{score.Accuracy:0.0}%";
            resultComboLabel.text = score.MaxCombo.ToString(CultureInfo.InvariantCulture);
            resultBreakdownLabel.text =
                $"PERFECT {matchingSnapshot.PerfectCount}   GOOD {matchingSnapshot.GoodCount}   " +
                $"EARLY/LATE {matchingSnapshot.EarlyCount + matchingSnapshot.LateCount}   MISS {matchingSnapshot.MissCount}";
            RenderPracticeRecommendation();
            RenderCalibrationRecommendation();
            SetDisplayed(resultsOverlay, true);
        }

        private void RefreshSessionCopy()
        {
            if (sessionCoordinator == null) return;
            if (trackNameLabel != null) trackNameLabel.text = sessionCoordinator.Title;
            if (trackMetaLabel != null) trackMetaLabel.text = sessionCoordinator.Metadata;
            if (resultMenuButton != null) resultMenuButton.text = sessionCoordinator.Session.ReturnButtonLabel;
            if (sessionKickerLabel != null) sessionKickerLabel.text = sessionCoordinator.Session.Kicker;
        }

        private void InitializeAudioFeedback()
        {
            if (drumFeedbackSource == null)
            {
                var feedbackObject = new GameObject("PlayerDrumFeedback");
                feedbackObject.transform.SetParent(transform, false);
                drumFeedbackSource = feedbackObject.AddComponent<AudioSource>();
            }

            drumFeedbackSource.playOnAwake = false;
            drumFeedbackSource.loop = false;
            drumFeedbackSource.spatialBlend = 0;
            drumClips = GeneratedDrumFeedbackFactory.CreateKit();
            mistakeClip = GeneratedDrumFeedbackFactory.CreateMistake();
        }

        private void TryScheduleMetronome()
        {
            if (metronomeScheduled || songClock == null || songClock.Clock == null ||
                !songClock.Clock.IsScheduled || !PlayerPreferencesRuntime.Current.Snapshot.MetronomeEnabled)
                return;

            if (metronomeSource == null)
            {
                var metronomeObject = new GameObject("PracticeMetronome");
                metronomeObject.transform.SetParent(transform, false);
                metronomeSource = metronomeObject.AddComponent<AudioSource>();
                metronomeSource.playOnAwake = false;
                metronomeSource.loop = false;
                metronomeSource.spatialBlend = 0;
                metronomeSource.volume = 0.38f;
            }

            if (metronomeClip == null)
                metronomeClip = GeneratedClickTrackFactory.Create(
                    CurrentSession.Bpm,
                    CurrentSession.Bars,
                    CurrentSession.BeatsPerBar,
                    GeneratedClickTrackFactory.DefaultSampleRate);
            metronomeSource.clip = metronomeClip;
            metronomeSource.PlayScheduled(songClock.StartDspTime);
            metronomeScheduled = true;
            metronomeSeekedWhilePaused = false;
        }

        private void PlayDrum(DrumPad pad, int velocity)
        {
            if (drumFeedbackSource == null || drumClips == null || !drumClips.TryGetValue(pad, out AudioClip clip)) return;
            drumFeedbackSource.PlayOneShot(clip, Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(velocity / 127f)));
        }

        private void PlayMistake()
        {
            if (drumFeedbackSource != null && mistakeClip != null) drumFeedbackSource.PlayOneShot(mistakeClip, 0.30f);
        }

        private void ReleaseAudioFeedback()
        {
            if (drumClips != null)
            {
                foreach (AudioClip clip in drumClips.Values)
                {
                    if (clip != null) Destroy(clip);
                }
                drumClips = null;
            }

            if (mistakeClip != null)
            {
                Destroy(mistakeClip);
                mistakeClip = null;
            }
            if (metronomeSource != null)
            {
                metronomeSource.Stop();
                metronomeSource.clip = null;
            }
            if (metronomeClip != null)
            {
                Destroy(metronomeClip);
                metronomeClip = null;
            }
        }

        private void BindRunControls()
        {
            UnbindRunControls();
            if (pauseButton != null) pauseButton.clicked += TogglePause;
            if (resumeButton != null) resumeButton.clicked += TogglePause;
            if (restartButton != null) restartButton.clicked += RestartRun;
            if (resultRestartButton != null) resultRestartButton.clicked += RestartRun;
            if (resultMenuButton != null) resultMenuButton.clicked += ReturnToMainMenu;
            if (resultApplyCalibrationButton != null) resultApplyCalibrationButton.clicked += ApplyCalibrationRecommendation;
            if (practicePreviousSectionButton != null) practicePreviousSectionButton.clicked += SelectPreviousPracticeSection;
            if (practiceNextSectionButton != null) practiceNextSectionButton.clicked += SelectNextPracticeSection;
            if (practiceLoopSectionButton != null) practiceLoopSectionButton.clicked += LoopSelectedPracticeSection;
            if (practiceSetAButton != null) practiceSetAButton.clicked += SetPracticePointA;
            if (practiceSetBButton != null) practiceSetBButton.clicked += SetPracticePointB;
            if (practiceClearButton != null) practiceClearButton.clicked += ClearPracticeLoop;
        }

        private void UnbindRunControls()
        {
            if (pauseButton != null) pauseButton.clicked -= TogglePause;
            if (resumeButton != null) resumeButton.clicked -= TogglePause;
            if (restartButton != null) restartButton.clicked -= RestartRun;
            if (resultRestartButton != null) resultRestartButton.clicked -= RestartRun;
            if (resultMenuButton != null) resultMenuButton.clicked -= ReturnToMainMenu;
            if (resultApplyCalibrationButton != null) resultApplyCalibrationButton.clicked -= ApplyCalibrationRecommendation;
            if (practicePreviousSectionButton != null) practicePreviousSectionButton.clicked -= SelectPreviousPracticeSection;
            if (practiceNextSectionButton != null) practiceNextSectionButton.clicked -= SelectNextPracticeSection;
            if (practiceLoopSectionButton != null) practiceLoopSectionButton.clicked -= LoopSelectedPracticeSection;
            if (practiceSetAButton != null) practiceSetAButton.clicked -= SetPracticePointA;
            if (practiceSetBButton != null) practiceSetBButton.clicked -= SetPracticePointB;
            if (practiceClearButton != null) practiceClearButton.clicked -= ClearPracticeLoop;
        }

        private TimingCalibrationAdvisor AdvisorFor(DrumInputSource source) =>
            source == DrumInputSource.Midi ? midiCalibration : keyboardCalibration;

        private void RenderCalibrationRecommendation()
        {
            if (resultCalibrationLabel == null || resultApplyCalibrationButton == null) return;
            TimingCalibrationAdvisor advisor = AdvisorFor(lastCalibrationSource);
            TimingCalibrationSnapshot calibration = advisor.Snapshot;
            string source = lastCalibrationSource == DrumInputSource.Midi ? "MIDI" : "TASTIERA";
            if (!calibration.HasRecommendation)
            {
                resultCalibrationLabel.text = $"CALIBRAZIONE {source}: {calibration.SampleCount}/{TimingCalibrationAdvisor.MinimumSamples} COLPI VALIDI";
                resultApplyCalibrationButton.SetEnabled(false);
                return;
            }

            double current = PlayerPreferencesRuntime.Current.Snapshot.OffsetFor(lastCalibrationSource);
            double suggested = advisor.RecommendOffsetSeconds(current);
            resultCalibrationLabel.text =
                $"CALIBRAZIONE {source}: MEDIANA {FormatDelta(calibration.MedianDeltaSeconds)} · " +
                $"OFFSET {FormatOffset(current)} → {FormatOffset(suggested)}";
            resultApplyCalibrationButton.SetEnabled(Math.Abs(suggested - current) >= 0.001);
        }

        private void RenderPracticeRecommendation()
        {
            if (resultPracticeLabel == null) return;
            PadPerformanceSnapshot weakest = performanceAnalyzer.WeakestPad();
            if (weakest == null)
            {
                resultPracticeLabel.text = "FOCUS: NESSUN DATO SUFFICIENTE";
                return;
            }

            string tendency = performanceAnalyzer.EarlyCount > performanceAnalyzer.LateCount
                ? "TENDENZA IN ANTICIPO"
                : performanceAnalyzer.LateCount > performanceAnalyzer.EarlyCount
                    ? "TENDENZA IN RITARDO"
                    : "TIMING BILANCIATO";
            resultPracticeLabel.text =
                $"FOCUS PROSSIMO: {PadLabel(weakest.Pad)} · {weakest.Accuracy:0.0}% · {tendency}";
        }

        private static string PadLabel(DrumPad pad)
        {
            switch (pad)
            {
                case DrumPad.Kick: return "GRANCASSA";
                case DrumPad.Snare: return "RULLANTE";
                case DrumPad.HiHat: return "HI-HAT";
                case DrumPad.Tom1: return "TOM 1";
                case DrumPad.Tom2: return "TOM 2";
                case DrumPad.FloorTom: return "TIMPANO";
                case DrumPad.Crash: return "CRASH";
                case DrumPad.Ride: return "RIDE";
                default: throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        private void ApplyCalibrationRecommendation()
        {
            TimingCalibrationAdvisor advisor = AdvisorFor(lastCalibrationSource);
            double current = PlayerPreferencesRuntime.Current.Snapshot.OffsetFor(lastCalibrationSource);
            double suggested = advisor.RecommendOffsetSeconds(current);
            PlayerPreferencesRuntime.Current.SetInputOffset(lastCalibrationSource, suggested);
            RenderCalibrationRecommendation();
        }

        private static string FormatDelta(double seconds)
        {
            int milliseconds = Mathf.RoundToInt((float)(seconds * 1000));
            string direction = milliseconds < 0 ? "EARLY" : milliseconds > 0 ? "LATE" : "ON TIME";
            return $"{(milliseconds > 0 ? "+" : string.Empty)}{milliseconds} ms {direction}";
        }

        private static string FormatOffset(double seconds)
        {
            int milliseconds = Mathf.RoundToInt((float)(seconds * 1000));
            return $"{(milliseconds > 0 ? "+" : string.Empty)}{milliseconds} ms";
        }

        private static void SetDisplayed(VisualElement element, bool displayed)
        {
            if (element != null) element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ReturnToMainMenu()
        {
            if (IsReturningToMenu || !Application.CanStreamedLevelBeLoaded(MainMenuRoutes.MainMenuScene)) return;
            FlushPracticeTime();
            IsReturningToMenu = true;
            SceneManager.LoadSceneAsync(MainMenuRoutes.MainMenuScene, LoadSceneMode.Single);
        }

        private void TrackPracticeTime()
        {
            if (practiceTimer.Tick(RunState, Application.isFocused, Time.unscaledDeltaTime)) FlushPracticeTime();
        }

        private void FlushPracticeTime()
        {
            double seconds = practiceTimer.DrainPending();
            if (seconds <= 0) return;
            GameplayProgressRuntime.Current.AddPracticeTime(seconds);
        }

        private void RecordCompletedSession(HitMatchingSnapshot matchingSnapshot, GameplayScoreSnapshot score)
        {
            GameplaySessionDefinition session = CurrentSession;
            var result = new GameplaySessionResult(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                session.Kind,
                session.LessonId,
                session.SpeedMultiplier,
                practiceTimer.CurrentAttemptSeconds,
                score.Score,
                score.Accuracy,
                score.MaxCombo,
                matchingSnapshot.PerfectCount,
                matchingSnapshot.GoodCount,
                matchingSnapshot.EarlyCount,
                matchingSnapshot.LateCount,
                matchingSnapshot.MissCount);
            GameplayProgressRuntime.Current.RecordCompletedSession(result);
        }

        private void Subscribe()
        {
            if (matching == null || ReferenceEquals(subscribedMatching, matching)) return;
            Unsubscribe();
            matching.InputProcessed += HandleInputProcessed;
            matching.HitResolved += HandleHitResolved;
            subscribedMatching = matching;
        }

        private void Unsubscribe()
        {
            if (subscribedMatching == null) return;
            subscribedMatching.InputProcessed -= HandleInputProcessed;
            subscribedMatching.HitResolved -= HandleHitResolved;
            subscribedMatching = null;
        }

        private bool HasRequiredReferences()
        {
            return document != null && chartTimeline != null && songClock != null && matching != null &&
                   sessionCoordinator != null && sessionCoordinator.IsConfigured && IsViewBound;
        }

        private void LogInvalidConfigurationOnce()
        {
            if (invalidConfigurationLogged) return;
            invalidConfigurationLogged = true;
            Debug.LogError("Gameplay highway requires a configured session, UIDocument, chart timeline, DSP clock, matching session, and complete UXML.", this);
        }

        private Texture2D BackgroundFor(GameplayPresentationTheme theme)
        {
            switch (theme)
            {
                case GameplayPresentationTheme.ArcadeNeon: return arcadeNeonBackground;
                case GameplayPresentationTheme.ConcertStage: return concertStageBackground;
                case GameplayPresentationTheme.PrecisionGrid: return precisionGridBackground;
                default: return null;
            }
        }

        private static string ThemeClass(GameplayPresentationTheme theme)
        {
            switch (theme)
            {
                case GameplayPresentationTheme.ArcadeNeon: return "theme--arcade-neon";
                case GameplayPresentationTheme.ConcertStage: return "theme--concert-stage";
                case GameplayPresentationTheme.PrecisionGrid: return "theme--precision-grid";
                default: throw new ArgumentOutOfRangeException(nameof(theme));
            }
        }

        private static float CalculateAccuracy(HitMatchingSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ResolvedNoteCount == 0) return 100;
            float points = snapshot.PerfectCount + snapshot.GoodCount * 0.75f +
                           (snapshot.EarlyCount + snapshot.LateCount) * 0.5f;
            return points / snapshot.ResolvedNoteCount * 100f;
        }

        private static string FormatPosition(double positionSeconds, double durationSeconds)
        {
            int position = Mathf.Max(0, Mathf.FloorToInt((float)positionSeconds));
            int duration = Mathf.Max(0, Mathf.CeilToInt((float)durationSeconds));
            return $"{position / 60:00}:{position % 60:00}  /  {duration / 60:00}:{duration % 60:00}";
        }
    }
}
