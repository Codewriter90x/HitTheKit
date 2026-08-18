using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using UnityEngine;

namespace HitTheKit.Unity.Charts
{
    public sealed class ChartTimelinePrototype : MonoBehaviour
    {
        [SerializeField] private TextAsset chartAsset;
        [SerializeField] private string difficulty = "easy";
        [SerializeField] private DspSongClockPrototype songClock;
        [SerializeField] private double lookAheadSeconds = 1.0;
        [SerializeField] private bool logLifecycle = true;
        [SerializeField] private double playbackSpeed = 1.0;

        private IReadOnlyList<TimelineNote> upcomingNotes = Array.Empty<TimelineNote>();
        private bool firstUpcomingLogged;
        private bool completionLogged;

        public TextAsset ChartAsset => chartAsset;
        public string Difficulty => difficulty;
        public DspSongClockPrototype SongClock => songClock;
        public double LookAheadSeconds => lookAheadSeconds;
        public LoadedChart Chart { get; private set; }
        public ChartTimeline Timeline { get; private set; }
        public IReadOnlyList<TimelineNote> UpcomingNotes => upcomingNotes;
        public int ElapsedNoteCount { get; private set; }
        public double PlaybackSpeed => playbackSpeed;

        public void Configure(TextAsset asset, string selectedDifficulty, double selectedPlaybackSpeed = 1.0)
        {
            if (Chart != null || Timeline != null)
                throw new InvalidOperationException("A running chart timeline cannot be reconfigured.");
            chartAsset = asset != null ? asset : throw new ArgumentNullException(nameof(asset));
            difficulty = !string.IsNullOrWhiteSpace(selectedDifficulty)
                ? selectedDifficulty
                : throw new ArgumentException("Difficulty is required.", nameof(selectedDifficulty));
            if (!IsFinite(selectedPlaybackSpeed) || selectedPlaybackSpeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(selectedPlaybackSpeed));
            playbackSpeed = selectedPlaybackSpeed;
        }

        private void Start()
        {
            if (chartAsset == null) throw new InvalidOperationException("A chart TextAsset is required.");
            if (songClock == null) throw new InvalidOperationException("A DSP song clock prototype is required.");
            if (string.IsNullOrWhiteSpace(difficulty)) throw new InvalidOperationException("Difficulty is required.");
            if (!IsFinite(lookAheadSeconds) || lookAheadSeconds < 0)
            {
                throw new InvalidOperationException("Look-ahead must be finite and non-negative.");
            }

            Chart = new ChartLoader().Load(chartAsset.text, difficulty);
            Timeline = new ChartTimeline(Chart, playbackSpeed);
            RefreshTimeline();

            if (logLifecycle && !Application.isBatchMode)
            {
                Debug.Log($"HitTheKit chart '{difficulty}' loaded with {Chart.Notes.Count} notes.", this);
            }
        }

        public IReadOnlyList<ChartNote> CreateMatchingNotes()
        {
            if (Timeline == null) throw new InvalidOperationException("The chart timeline has not started.");
            var result = new ChartNote[Timeline.Notes.Count];
            for (int index = 0; index < result.Length; index++)
                result[index] = new ChartNote(Timeline.Notes[index].EffectiveTimeSeconds, Timeline.Notes[index].Note.Pad);
            return Array.AsReadOnly(result);
        }

        private void Update()
        {
            RefreshTimeline();
        }

        private void RefreshTimeline()
        {
            if (Timeline == null || songClock == null || songClock.Clock == null || !songClock.Clock.IsScheduled)
            {
                return;
            }

            double position = songClock.PositionSeconds;
            upcomingNotes = Timeline.GetUpcoming(position, lookAheadSeconds);
            ElapsedNoteCount = Timeline.GetElapsed(position).Count;

            if (!logLifecycle || Application.isBatchMode) return;

            if (!firstUpcomingLogged && upcomingNotes.Count > 0)
            {
                firstUpcomingLogged = true;
                Debug.Log("The first chart note entered the look-ahead window.", this);
            }

            if (!completionLogged && Timeline.Notes.Count > 0 && ElapsedNoteCount == Timeline.Notes.Count)
            {
                completionLogged = true;
                Debug.Log("HitTheKit chart timeline completed.", this);
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
