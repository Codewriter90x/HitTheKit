using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class ChartWaveformModel
    {
        private readonly float[] peaks;

        public ChartWaveformModel(float[] sourcePeaks, double durationSeconds)
        {
            if (sourcePeaks == null || sourcePeaks.Length < 2)
                throw new ArgumentException("A waveform requires at least two peak values.", nameof(sourcePeaks));
            if (!IsFinite(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            peaks = new float[sourcePeaks.Length];
            for (int index = 0; index < peaks.Length; index++)
            {
                float value = sourcePeaks[index];
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentException("Waveform peaks must be finite.", nameof(sourcePeaks));
                peaks[index] = Mathf.Clamp01(Mathf.Abs(value));
            }
            DurationSeconds = durationSeconds;
            ResetZoom();
        }

        public double DurationSeconds { get; }
        public double ViewStartSeconds { get; private set; }
        public double ViewEndSeconds { get; private set; }
        public double SelectedTimeSeconds { get; private set; }
        public int PeakCount => peaks.Length;
        public float PeakAt(int index) => peaks[index];

        public double Scrub(double normalizedPosition)
        {
            if (!IsFinite(normalizedPosition)) throw new ArgumentOutOfRangeException(nameof(normalizedPosition));
            double clamped = Math.Max(0, Math.Min(1, normalizedPosition));
            SelectedTimeSeconds = ViewStartSeconds + (ViewEndSeconds - ViewStartSeconds) * clamped;
            return SelectedTimeSeconds;
        }

        public void Select(double timeSeconds)
        {
            if (!IsFinite(timeSeconds)) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
            SelectedTimeSeconds = Math.Max(0, Math.Min(DurationSeconds, timeSeconds));
        }

        public void Zoom(double factor)
        {
            if (!IsFinite(factor) || factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
            double current = ViewEndSeconds - ViewStartSeconds;
            double minimum = Math.Min(DurationSeconds, 0.5);
            double next = Math.Max(minimum, Math.Min(DurationSeconds, current / factor));
            double anchor = SelectedTimeSeconds;
            double ratio = current > 0 ? (anchor - ViewStartSeconds) / current : 0.5;
            double start = anchor - next * ratio;
            start = Math.Max(0, Math.Min(DurationSeconds - next, start));
            ViewStartSeconds = start;
            ViewEndSeconds = start + next;
        }

        public void ResetZoom()
        {
            ViewStartSeconds = 0;
            ViewEndSeconds = DurationSeconds;
            SelectedTimeSeconds = Math.Max(0, Math.Min(DurationSeconds, SelectedTimeSeconds));
        }

        public static ChartWaveformModel FromInterleaved(
            float[] samples,
            int channels,
            double durationSeconds,
            int pointCount = 512)
        {
            if (samples == null || samples.Length == 0) throw new ArgumentException("Samples are required.", nameof(samples));
            if (channels <= 0 || samples.Length % channels != 0) throw new ArgumentOutOfRangeException(nameof(channels));
            if (pointCount < 2 || pointCount > 4096) throw new ArgumentOutOfRangeException(nameof(pointCount));
            var result = new float[pointCount];
            int frames = samples.Length / channels;
            for (int point = 0; point < pointCount; point++)
            {
                int startFrame = (int)((long)point * frames / pointCount);
                int endFrame = Math.Max(startFrame + 1, (int)((long)(point + 1) * frames / pointCount));
                endFrame = Math.Min(frames, endFrame);
                float peak = 0;
                for (int frame = startFrame; frame < endFrame; frame++)
                    for (int channel = 0; channel < channels; channel++)
                        peak = Math.Max(peak, Math.Abs(samples[frame * channels + channel]));
                result[point] = peak;
            }
            return new ChartWaveformModel(result, durationSeconds);
        }

        public static ChartWaveformModel FromAudioClip(
            AudioClip clip,
            int pointCount = 512,
            int framesPerRead = 4096)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (clip.samples <= 0 || clip.channels <= 0 || clip.length <= 0)
                throw new ArgumentException("Audio clip is empty.", nameof(clip));
            if (pointCount < 2 || pointCount > 4096) throw new ArgumentOutOfRangeException(nameof(pointCount));
            if (framesPerRead <= 0 || framesPerRead > 32768)
                throw new ArgumentOutOfRangeException(nameof(framesPerRead));

            int boundedRead = Math.Min(framesPerRead, clip.samples);
            var buffer = new float[boundedRead * clip.channels];
            var result = new float[pointCount];
            for (int point = 0; point < pointCount; point++)
            {
                int startFrame = (int)((long)point * clip.samples / pointCount);
                int endFrame = Math.Max(startFrame + 1, (int)((long)(point + 1) * clip.samples / pointCount));
                endFrame = Math.Min(clip.samples, endFrame);
                float peak = 0;
                for (int offset = startFrame; offset < endFrame; offset += boundedRead)
                {
                    int frames = Math.Min(boundedRead, endFrame - offset);
                    if (!clip.GetData(buffer, offset))
                        throw new InvalidOperationException("Audio samples could not be read.");
                    int sampleCount = frames * clip.channels;
                    for (int index = 0; index < sampleCount; index++)
                        peak = Math.Max(peak, Math.Abs(buffer[index]));
                }
                result[point] = peak;
            }
            return new ChartWaveformModel(result, clip.length);
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class ChartWaveformView : VisualElement
    {
        private bool dragging;
        private ChartWaveformModel model;

        public ChartWaveformView()
        {
            focusable = true;
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
        }

        public event Action<double> Scrubbed;
        public ChartWaveformModel Model => model;

        public void SetModel(ChartWaveformModel value)
        {
            model = value;
            MarkDirtyRepaint();
        }

        public void SetSelectedTime(double value)
        {
            model?.Select(value);
            MarkDirtyRepaint();
        }

        public void Zoom(double factor)
        {
            model?.Zoom(factor);
            MarkDirtyRepaint();
        }

        public void ResetZoom()
        {
            model?.ResetZoom();
            MarkDirtyRepaint();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || model == null) return;
            dragging = true;
            this.CapturePointer(evt.pointerId);
            Scrub(evt.localPosition.x);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || model == null) return;
            Scrub(evt.localPosition.x);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            dragging = false;
            if (this.HasPointerCapture(evt.pointerId)) this.ReleasePointer(evt.pointerId);
        }

        private void Scrub(float x)
        {
            double width = Math.Max(1, contentRect.width);
            double selected = model.Scrub(x / width);
            MarkDirtyRepaint();
            Scrubbed?.Invoke(selected);
        }

        private void Draw(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            Rect rect = contentRect;
            painter.fillColor = new Color(0.01f, 0.04f, 0.075f, 0.98f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width, rect.height));
            painter.LineTo(new Vector2(0, rect.height));
            painter.ClosePath();
            painter.Fill();
            if (model == null || rect.width <= 1 || rect.height <= 1) return;

            float center = rect.height * 0.5f;
            painter.strokeColor = new Color(0.22f, 0.55f, 0.68f, 0.45f);
            painter.lineWidth = 1;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, center));
            painter.LineTo(new Vector2(rect.width, center));
            painter.Stroke();

            double viewDuration = model.ViewEndSeconds - model.ViewStartSeconds;
            int first = Math.Max(0, (int)Math.Floor(model.ViewStartSeconds / model.DurationSeconds * (model.PeakCount - 1)));
            int last = Math.Min(model.PeakCount - 1, (int)Math.Ceiling(model.ViewEndSeconds / model.DurationSeconds * (model.PeakCount - 1)));
            painter.strokeColor = new Color(0.24f, 0.86f, 1f, 0.9f);
            painter.lineWidth = Math.Max(1, rect.width / Math.Max(1, last - first + 1));
            for (int index = first; index <= last; index++)
            {
                double time = index / (double)(model.PeakCount - 1) * model.DurationSeconds;
                float x = (float)((time - model.ViewStartSeconds) / viewDuration * rect.width);
                float amplitude = model.PeakAt(index) * (rect.height * 0.42f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, center - amplitude));
                painter.LineTo(new Vector2(x, center + amplitude));
                painter.Stroke();
            }

            if (model.SelectedTimeSeconds >= model.ViewStartSeconds &&
                model.SelectedTimeSeconds <= model.ViewEndSeconds)
            {
                float x = (float)((model.SelectedTimeSeconds - model.ViewStartSeconds) / viewDuration * rect.width);
                painter.strokeColor = new Color(1f, 0.67f, 0.18f, 1f);
                painter.lineWidth = 2.5f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, rect.height));
                painter.Stroke();
            }
        }
    }
}
