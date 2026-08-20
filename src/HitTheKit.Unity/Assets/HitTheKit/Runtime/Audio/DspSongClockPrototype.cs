using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace HitTheKit.Unity.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class DspSongClockPrototype : MonoBehaviour
    {
        [SerializeField] private double leadInSeconds = 2.0;
        [SerializeField] private int countInBeats = 4;
        [SerializeField] private double bpm = GeneratedClickTrackFactory.DefaultBpm;
        [SerializeField] private int bars = GeneratedClickTrackFactory.DefaultBars;
        [SerializeField] private int beatsPerBar = GeneratedClickTrackFactory.DefaultBeatsPerBar;
        [SerializeField] private int sampleRate = GeneratedClickTrackFactory.DefaultSampleRate;
        [SerializeField] private bool logLifecycle = true;
        [SerializeField] private bool useDemoSong = true;

        private AudioSource audioSource;
        private AudioClip generatedClip;
        private bool startedLogged;
        private bool completedLogged;
        private string externalAudioPath;
        private double audioPlaybackSpeed = 1.0;
        private UnityWebRequest audioRequest;
        private bool seekedWhilePaused;

        public DspSongClock Clock { get; private set; }
        public double StartDspTime => Clock != null && Clock.IsScheduled ? Clock.StartDspTime : double.NaN;
        public double PositionSeconds => Clock != null ? Clock.PositionSeconds : throw new InvalidOperationException("The prototype has not started.");
        public bool HasStarted => Clock != null && Clock.HasStarted;
        public bool HasCompleted => Clock != null && Clock.HasCompleted;
        public AudioClip GeneratedClip => generatedClip;
        public double Bpm => bpm;
        public int CountInBeats => countInBeats;
        public double CountInSeconds => countInBeats * 60.0 / bpm;
        public string ExternalAudioPath => externalAudioPath;
        public double AudioPlaybackSpeed => audioPlaybackSpeed;
        public bool IsPreviewing { get; private set; }
        public string LoadError { get; private set; }

        public void Configure(
            double selectedBpm,
            int selectedBars,
            int selectedBeatsPerBar,
            int selectedCountInBeats,
            bool demoSong,
            string selectedExternalAudioPath = null,
            double selectedAudioPlaybackSpeed = 1.0)
        {
            if (Clock != null && Clock.IsScheduled)
                throw new InvalidOperationException("A scheduled song clock cannot be reconfigured.");
            if (selectedBpm <= 0 || selectedBars <= 0 || selectedBeatsPerBar <= 0 || selectedCountInBeats <= 0)
                throw new ArgumentOutOfRangeException(nameof(selectedBpm), "Song and count-in values must be positive.");
            if (double.IsNaN(selectedAudioPlaybackSpeed) || double.IsInfinity(selectedAudioPlaybackSpeed) ||
                selectedAudioPlaybackSpeed <= 0 || selectedAudioPlaybackSpeed > 3)
                throw new ArgumentOutOfRangeException(nameof(selectedAudioPlaybackSpeed));

            bpm = selectedBpm;
            bars = selectedBars;
            beatsPerBar = selectedBeatsPerBar;
            countInBeats = selectedCountInBeats;
            leadInSeconds = selectedCountInBeats * 60.0 / selectedBpm;
            useDemoSong = demoSong;
            audioPlaybackSpeed = selectedAudioPlaybackSpeed;
            if (!string.IsNullOrWhiteSpace(selectedExternalAudioPath))
            {
                string path = Path.GetFullPath(selectedExternalAudioPath);
                string extension = Path.GetExtension(path);
                if (!string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("External song audio must use .ogg or .wav.", nameof(selectedExternalAudioPath));
                if (!File.Exists(path)) throw new FileNotFoundException("External song audio was not found.", path);
                externalAudioPath = path;
            }
            else
            {
                externalAudioPath = null;
            }
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0;
        }

        private IEnumerator Start()
        {
            if (double.IsNaN(leadInSeconds) || double.IsInfinity(leadInSeconds) || leadInSeconds < 0)
            {
                throw new InvalidOperationException("Lead-in must be finite and non-negative.");
            }
            if (countInBeats <= 0) throw new InvalidOperationException("Count-in beats must be positive.");

            if (!string.IsNullOrEmpty(externalAudioPath))
            {
                AudioType type = string.Equals(Path.GetExtension(externalAudioPath), ".ogg", StringComparison.OrdinalIgnoreCase)
                    ? AudioType.OGGVORBIS
                    : AudioType.WAV;
                audioRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(externalAudioPath).AbsoluteUri, type);
                yield return audioRequest.SendWebRequest();
                if (audioRequest.result != UnityWebRequest.Result.Success)
                {
                    LoadError = $"Could not load the selected song audio: {audioRequest.error}";
                    Debug.LogError(LoadError, this);
                    audioRequest.Dispose();
                    audioRequest = null;
                    yield break;
                }

                generatedClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                generatedClip.name = $"Imported song {Path.GetFileNameWithoutExtension(externalAudioPath)}";
                audioRequest.Dispose();
                audioRequest = null;
            }
            else
            {
                generatedClip = useDemoSong
                    ? GeneratedDemoSongFactory.Create(bpm, bars, beatsPerBar, sampleRate)
                    : GeneratedClickTrackFactory.Create(bpm, bars, beatsPerBar, sampleRate);
            }
            SchedulePlayback();

            if (logLifecycle && !Application.isBatchMode)
            {
                Debug.Log($"HitTheKit demo song scheduled at DSP time {StartDspTime:F3}.", this);
            }
        }

        public void PausePlayback()
        {
            if (Clock == null || !Clock.IsScheduled || Clock.IsPaused) return;
            Clock.Pause();
            audioSource.Pause();
        }

        public void ResumePlayback()
        {
            if (Clock == null || !Clock.IsPaused) return;
            Clock.Resume();
            if (seekedWhilePaused)
            {
                audioSource.Play();
                seekedWhilePaused = false;
            }
            else
            {
                audioSource.UnPause();
            }
        }

        public void RestartPlayback()
        {
            if (generatedClip == null) return;
            audioSource.Stop();
            SchedulePlayback();
            startedLogged = false;
            completedLogged = false;
        }

        public void SeekPlayback(double positionSeconds)
        {
            if (generatedClip == null || Clock == null || !Clock.IsScheduled)
                throw new InvalidOperationException("Song playback must be scheduled before seeking.");
            if (double.IsNaN(positionSeconds) || double.IsInfinity(positionSeconds) ||
                positionSeconds < 0 || positionSeconds >= Clock.DurationSeconds)
                throw new ArgumentOutOfRangeException(nameof(positionSeconds));

            bool wasPaused = Clock.IsPaused;
            double clipPosition = Math.Min(
                generatedClip.length - (1.0 / Math.Max(1, generatedClip.frequency)),
                positionSeconds * audioPlaybackSpeed);

            audioSource.Stop();
            audioSource.time = (float)Math.Max(0, clipPosition);
            Clock.Seek(positionSeconds);
            if (wasPaused)
            {
                seekedWhilePaused = true;
            }
            else
            {
                audioSource.Play();
                seekedWhilePaused = false;
            }
            startedLogged = positionSeconds >= 0;
            completedLogged = false;
        }

        public void PreviewFromSourceTime(double sourceTimeSeconds, double speed = 1.0)
        {
            if (generatedClip == null || audioSource == null)
                throw new InvalidOperationException("Song audio is not loaded.");
            if (double.IsNaN(sourceTimeSeconds) || double.IsInfinity(sourceTimeSeconds) ||
                sourceTimeSeconds < 0 || sourceTimeSeconds >= generatedClip.length)
                throw new ArgumentOutOfRangeException(nameof(sourceTimeSeconds));
            if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0 || speed > 3)
                throw new ArgumentOutOfRangeException(nameof(speed));
            audioSource.Stop();
            audioSource.pitch = (float)speed;
            audioSource.time = (float)sourceTimeSeconds;
            audioSource.Play();
            IsPreviewing = true;
        }

        public void StopPreview()
        {
            if (audioSource != null) audioSource.Stop();
            IsPreviewing = false;
        }

        private void SchedulePlayback()
        {
            IsPreviewing = false;
            audioSource.clip = generatedClip;
            audioSource.pitch = (float)audioPlaybackSpeed;
            var timeSource = new UnityDspTimeSource();
            double startDspTime = timeSource.Now + leadInSeconds;
            Clock = new DspSongClock(timeSource);
            Clock.Schedule(startDspTime, generatedClip.length / audioPlaybackSpeed);
            audioSource.PlayScheduled(startDspTime);
            seekedWhilePaused = false;
        }

        private void Update()
        {
            if (!logLifecycle || Application.isBatchMode || Clock == null) return;

            if (!startedLogged && Clock.HasStarted)
            {
                startedLogged = true;
                Debug.Log("HitTheKit demo song playback started.", this);
            }

            if (!completedLogged && Clock.HasCompleted)
            {
                completedLogged = true;
                Debug.Log("HitTheKit demo song playback completed.", this);
            }
        }

        private void OnDestroy()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }

            if (audioRequest != null)
            {
                audioRequest.Abort();
                audioRequest.Dispose();
                audioRequest = null;
            }

            if (generatedClip != null)
            {
                Destroy(generatedClip);
                generatedClip = null;
            }
        }
    }
}
