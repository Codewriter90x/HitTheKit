using System;
using System.IO;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    [DefaultExecutionOrder(-2000)]
    public sealed class GameplaySessionCoordinator : MonoBehaviour
    {
        [SerializeField] private DspSongClockPrototype songClock;
        [SerializeField] private ChartTimelinePrototype chartTimeline;
        [SerializeField] private TextAsset firstPulseChart;
        [SerializeField] private TextAsset backbeatChart;
        [SerializeField] private TextAsset timekeeperChart;
        [SerializeField] private TextAsset firstGrooveChart;
        private TextAsset generatedLessonChart;
        private TextAsset runtimeChartAsset;
        private TextAsset authoringChartAsset;

        public GameplaySessionDefinition Session { get; private set; }
        public bool IsConfigured { get; private set; }
        public string Title => Session?.Title ?? string.Empty;
        public string Subtitle => Session?.Subtitle ?? string.Empty;
        public string Metadata => Session?.Metadata ?? string.Empty;

        private void Awake()
        {
            ConfigureCurrentSession();
        }

        public void ConfigureCurrentSession()
        {
            if (songClock == null) throw new InvalidOperationException("Gameplay session requires a song clock.");
            if (chartTimeline == null) throw new InvalidOperationException("Gameplay session requires a chart timeline.");

            Session = GameplaySessionContext.Current;
            TextAsset chart = ChartFor(Session.Chart);
            if (chart == null) throw new InvalidOperationException($"Chart '{Session.Chart}' is missing.");
            chartTimeline.Configure(chart, Session.Difficulty, Session.ChartPlaybackSpeed);
            songClock.Configure(
                Session.Bpm,
                Session.Bars,
                Session.BeatsPerBar,
                Session.CountInBeats,
                Session.UseGeneratedSong,
                Session.AudioFilePath,
                Session.UseGeneratedSong ? 1.0 : Session.ChartPlaybackSpeed);
            IsConfigured = true;
        }

        private TextAsset ChartFor(GameplaySessionChart chart)
        {
            switch (chart)
            {
                case GameplaySessionChart.DemoSong: return chartTimeline.ChartAsset;
                case GameplaySessionChart.FirstPulse: return firstPulseChart;
                case GameplaySessionChart.Backbeat: return backbeatChart;
                case GameplaySessionChart.Timekeeper: return timekeeperChart;
                case GameplaySessionChart.FirstGroove: return firstGrooveChart;
                case GameplaySessionChart.SchoolLesson:
                    if (!Session.LessonId.HasValue)
                        throw new InvalidOperationException("A school lesson chart requires a lesson ID.");
                    generatedLessonChart = new TextAsset(GameplayLessonChartBuilder.BuildJson(Session.LessonId.Value));
                    generatedLessonChart.name = $"Generated lesson {Session.LessonId.Value}";
                    return generatedLessonChart;
                case GameplaySessionChart.ExternalFile:
                    if (string.IsNullOrWhiteSpace(Session.ChartFilePath) || !File.Exists(Session.ChartFilePath))
                        throw new InvalidOperationException("The selected song chart file is unavailable.");
                    var info = new FileInfo(Session.ChartFilePath);
                    if (info.Length <= 0 || info.Length > 4 * 1024 * 1024)
                        throw new InvalidOperationException("The selected song chart must be between 1 byte and 4 MiB.");
                    runtimeChartAsset = new TextAsset(File.ReadAllText(Session.ChartFilePath));
                    runtimeChartAsset.name = $"Imported song {Session.SongId}";
                    return runtimeChartAsset;
                case GameplaySessionChart.AuthoringEmpty:
                    authoringChartAsset = new TextAsset(
                        "{\n  \"version\": 1,\n  \"offsetSeconds\": 0,\n  \"difficulties\": {\n    \"" +
                        Session.Difficulty + "\": []\n  }\n}\n");
                    authoringChartAsset.name = $"New chart for {Session.SongId}";
                    return authoringChartAsset;
                default: throw new ArgumentOutOfRangeException(nameof(chart));
            }
        }

        private void OnDestroy()
        {
            if (generatedLessonChart != null)
            {
                Destroy(generatedLessonChart);
                generatedLessonChart = null;
            }
            if (runtimeChartAsset != null)
            {
                Destroy(runtimeChartAsset);
                runtimeChartAsset = null;
            }
            if (authoringChartAsset != null)
            {
                Destroy(authoringChartAsset);
                authoringChartAsset = null;
            }
        }
    }
}
