using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using HitTheKit.Core;
using UnityEngine;

namespace HitTheKit.Unity.Charts
{
    public sealed class ChartLoader
    {
        public const int SupportedVersion = 1;
        private static readonly string[] difficultyIds =
        {
            "easy",
            "medium",
            "hard",
            "advanced",
            "full",
            "expert"
        };
        private static readonly IReadOnlyList<string> readOnlyDifficultyIds =
            Array.AsReadOnly(difficultyIds);

        public static IReadOnlyList<string> SupportedDifficulties => readOnlyDifficultyIds;

        public IReadOnlyList<string> GetAvailableDifficulties(string json)
        {
            ChartDocumentDto document = ParseDocument(json);
            var available = new List<string>();
            for (int index = 0; index < difficultyIds.Length; index++)
            {
                string difficulty = difficultyIds[index];
                RejectExplicitNullDifficulty(json, difficulty);
                if (SelectDifficulty(document.difficulties, difficulty) != null)
                    available.Add(difficulty);
            }

            if (available.Count == 0)
                throw new ChartLoadException("Chart must contain at least one supported difficulty.");
            return new ReadOnlyCollection<string>(available);
        }

        public LoadedChart Load(string json, string difficulty)
        {
            if (string.IsNullOrWhiteSpace(difficulty))
            {
                throw new ArgumentException("Difficulty must not be null or empty.", nameof(difficulty));
            }

            ChartDocumentDto document = ParseDocument(json);
            if (!IsSupportedDifficulty(difficulty))
                throw new ChartLoadException($"Difficulty '{difficulty}' is not supported.");
            RejectExplicitNullDifficulty(json, difficulty);

            ChartNoteDto[] noteDtos = SelectDifficulty(document.difficulties, difficulty);
            if (noteDtos == null)
            {
                throw new ChartLoadException($"Difficulty '{difficulty}' does not exist or has a null note list.");
            }

            var notes = new List<IndexedChartNote>(noteDtos.Length);
            for (int index = 0; index < noteDtos.Length; index++)
            {
                ChartNoteDto noteDto = noteDtos[index];
                if (noteDto == null)
                {
                    throw new ChartLoadException($"Chart note at index {index} is null.");
                }

                if (!IsFinite(noteDto.time) || noteDto.time < 0)
                {
                    throw new ChartLoadException(
                        $"Chart note at index {index} requires a finite, non-negative time.");
                }

                DrumPad pad = ParsePad(noteDto.pad, index);
                notes.Add(new IndexedChartNote(new ChartNote(noteDto.time, pad), index));
            }

            notes.Sort((left, right) =>
            {
                int byTime = left.Note.TimeSeconds.CompareTo(right.Note.TimeSeconds);
                return byTime != 0 ? byTime : left.OriginalIndex.CompareTo(right.OriginalIndex);
            });

            return new LoadedChart(SupportedVersion, difficulty, document.offsetSeconds, notes);
        }

        private static ChartNoteDto[] SelectDifficulty(ChartDifficultiesDto difficulties, string difficulty)
        {
            switch (difficulty)
            {
                case "easy": return difficulties.easy;
                case "medium": return difficulties.medium;
                case "hard": return difficulties.hard;
                case "advanced": return difficulties.advanced;
                case "full": return difficulties.full;
                case "expert": return difficulties.expert;
                default: return null;
            }
        }

        private static ChartDocumentDto ParseDocument(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Chart JSON must not be null or empty.", nameof(json));

            ChartDocumentDto document;
            try
            {
                document = JsonUtility.FromJson<ChartDocumentDto>(json);
            }
            catch (ArgumentException exception)
            {
                throw new ChartLoadException("Chart JSON is malformed.", exception);
            }

            if (document == null) throw new ChartLoadException("Chart JSON did not contain a document.");
            if (document.version == int.MinValue) throw new ChartLoadException("Chart version is required.");
            if (document.version != SupportedVersion)
                throw new ChartLoadException(
                    $"Chart version {document.version} is not supported; expected {SupportedVersion}.");
            if (!IsFinite(document.offsetSeconds))
                throw new ChartLoadException("Chart offsetSeconds is required and must be finite.");
            if (document.difficulties == null)
                throw new ChartLoadException("Chart difficulties are required.");
            return document;
        }

        private static bool IsSupportedDifficulty(string difficulty)
        {
            for (int index = 0; index < difficultyIds.Length; index++)
            {
                if (string.Equals(difficultyIds[index], difficulty, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void RejectExplicitNullDifficulty(string json, string difficulty)
        {
            if (Regex.IsMatch(json, $"\\\"{difficulty}\\\"\\s*:\\s*null(?:\\s*[,}}])"))
                throw new ChartLoadException($"Difficulty '{difficulty}' has a null note list.");
        }

        private static DrumPad ParsePad(string pad, int index)
        {
            switch (pad)
            {
                case "kick": return DrumPad.Kick;
                case "snare": return DrumPad.Snare;
                case "hiHat": return DrumPad.HiHat;
                case "tom1": return DrumPad.Tom1;
                case "tom2": return DrumPad.Tom2;
                case "floorTom": return DrumPad.FloorTom;
                case "crash": return DrumPad.Crash;
                case "ride": return DrumPad.Ride;
                default:
                    throw new ChartLoadException(
                        $"Chart note at index {index} has unknown pad identifier '{pad ?? "<null>"}'.");
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        [Serializable]
        private sealed class ChartDocumentDto
        {
            public int version = int.MinValue;
            public double offsetSeconds = double.NaN;
            public ChartDifficultiesDto difficulties;
        }

        [Serializable]
        private sealed class ChartDifficultiesDto
        {
            public ChartNoteDto[] easy;
            public ChartNoteDto[] medium;
            public ChartNoteDto[] hard;
            public ChartNoteDto[] advanced;
            public ChartNoteDto[] full;
            public ChartNoteDto[] expert;
        }

        [Serializable]
        private sealed class ChartNoteDto
        {
            public double time = double.NaN;
            public string pad;
        }
    }
}
