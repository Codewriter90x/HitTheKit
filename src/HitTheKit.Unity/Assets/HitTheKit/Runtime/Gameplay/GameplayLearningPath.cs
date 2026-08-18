using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HitTheKit.Unity.Gameplay
{
    public enum GameplayLessonId
    {
        FirstPulse,
        Backbeat,
        Timekeeper,
        FirstGroove,
        GrooveBase8,
        SnareVariation,
        GhostNotesBase,
        OneBarFill,
        HandCoordination,
        HandsAndFeet,
        Groove16th,
        SwingFeel,
        HandDoubles,
        Paradiddle,
        SyncopatedKick,
        LinearGroove,
        HalfTime,
        ShuffleRock,
        FunkStarter,
        RockPerformance,
        DynamicControl,
        ReadingChallenge,
        FillImprovisation,
        FinalConcert
    }

    public enum GameplayLessonDiscipline
    {
        Pulse,
        Technique,
        Coordination,
        Reading,
        Musicality,
        Performance
    }

    public sealed class GameplayLessonDefinition
    {
        internal GameplayLessonDefinition(
            GameplayLessonId id,
            int number,
            int moduleNumber,
            string chapterItalian,
            string chapterEnglish,
            GameplayLessonDiscipline discipline,
            string italianTitle,
            string englishTitle,
            string italianDescription,
            string englishDescription,
            string italianObjective,
            string englishObjective,
            string exercisePattern,
            string focus,
            double bpm,
            int bars,
            int practiceMinutes,
            bool isPlayable,
            bool isModuleAssessment)
        {
            Id = id;
            Number = number;
            ModuleNumber = moduleNumber;
            ChapterItalian = chapterItalian;
            ChapterEnglish = chapterEnglish;
            Discipline = discipline;
            ItalianTitle = italianTitle;
            EnglishTitle = englishTitle;
            ItalianDescription = italianDescription;
            EnglishDescription = englishDescription;
            ItalianObjective = italianObjective;
            EnglishObjective = englishObjective;
            ExercisePattern = exercisePattern;
            Focus = focus;
            Bpm = bpm;
            Bars = bars;
            PracticeMinutes = practiceMinutes;
            IsPlayable = isPlayable;
            IsModuleAssessment = isModuleAssessment;
        }

        public GameplayLessonId Id { get; }
        public int Number { get; }
        public int ModuleNumber { get; }
        public string ChapterItalian { get; }
        public string ChapterEnglish { get; }
        public GameplayLessonDiscipline Discipline { get; }
        public string ItalianTitle { get; }
        public string EnglishTitle { get; }
        public string ItalianDescription { get; }
        public string EnglishDescription { get; }
        public string ItalianObjective { get; }
        public string EnglishObjective { get; }
        public string ExercisePattern { get; }
        public string Focus { get; }
        public double Bpm { get; }
        public int Bars { get; }
        public int PracticeMinutes { get; }
        public bool IsPlayable { get; }
        public bool IsModuleAssessment { get; }

        public string DisciplineName(bool italian)
        {
            switch (Discipline)
            {
                case GameplayLessonDiscipline.Pulse: return italian ? "Tempo" : "Pulse";
                case GameplayLessonDiscipline.Technique: return italian ? "Tecnica" : "Technique";
                case GameplayLessonDiscipline.Coordination: return italian ? "Coordinazione" : "Coordination";
                case GameplayLessonDiscipline.Reading: return italian ? "Lettura" : "Reading";
                case GameplayLessonDiscipline.Musicality: return italian ? "Musicalità" : "Musicality";
                case GameplayLessonDiscipline.Performance: return italian ? "Repertorio" : "Performance";
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static class GameplayStudySpeeds
    {
        private static readonly double[] values = { 0.5, 0.75, 1.0 };
        private static readonly IReadOnlyList<double> readOnlyValues = Array.AsReadOnly(values);

        public static IReadOnlyList<double> All => readOnlyValues;

        public static bool IsSupported(double value)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (Math.Abs(values[index] - value) < 0.0001) return true;
            }
            return false;
        }
    }

    public static class GameplayLearningPath
    {
        private static readonly GameplayLessonDefinition[] lessons =
        {
            Lesson(GameplayLessonId.FirstPulse, 1, 1, "Fondamentali", "Fundamentals", GameplayLessonDiscipline.Pulse,
                "Primo battito", "First pulse",
                "Trova il tempo con la sola grancassa, senza fretta.",
                "Find the pulse using only the kick drum, without rushing.",
                "Mantieni quattro quarti regolari per quattro battute.",
                "Hold four even quarter notes for four bars.",
                "1  2  3  4", "KICK", 64, 4, 4, true, false),
            Lesson(GameplayLessonId.Backbeat, 2, 1, "Fondamentali", "Fundamentals", GameplayLessonDiscipline.Coordination,
                "Cuore del groove", "Heart of the groove",
                "Alterna grancassa e rullante su una base lenta.",
                "Alternate kick and snare over a slow backing track.",
                "Distingui il pulse della cassa dal backbeat su 2 e 4.",
                "Separate the kick pulse from the backbeat on 2 and 4.",
                "K  S  K  S", "KICK + SNARE", 68, 4, 5, true, false),
            Lesson(GameplayLessonId.Timekeeper, 3, 1, "Fondamentali", "Fundamentals", GameplayLessonDiscipline.Pulse,
                "Tempo costante", "Steady time",
                "Aggiungi il charleston e mantieni il movimento regolare.",
                "Add the hi-hat and keep the motion even.",
                "Suona ottavi uniformi senza spostare cassa e rullante.",
                "Play even eighth notes without moving kick and snare.",
                "H&H&H&H& · S su 2/4", "HI-HAT + BACKBEAT", 72, 4, 6, true, false),
            Lesson(GameplayLessonId.FirstGroove, 4, 1, "Fondamentali", "Fundamentals", GameplayLessonDiscipline.Performance,
                "Il tuo primo groove", "Your first groove",
                "Unisci grancassa, rullante e charleston in un pattern completo.",
                "Combine kick, snare and hi-hat into a complete pattern.",
                "Completa otto battute e chiudi con il crash senza fermarti.",
                "Complete eight bars and finish with a crash without stopping.",
                "GROOVE 8TH · CRASH FINALE", "FIRST GROOVE", 76, 8, 7, true, true),

            Lesson(GameplayLessonId.HandCoordination, 5, 2, "Tecnica di base", "Basic technique", GameplayLessonDiscipline.Technique,
                "Mani alternate", "Alternating hands",
                "Costruisci colpi singoli regolari con sticking alternato.",
                "Build even single strokes with alternating sticking.",
                "Mantieni altezza e suono uniformi fra mano destra e sinistra.",
                "Keep stroke height and sound even between right and left hand.",
                "R L R L R L R L", "SINGLE STROKES", 70, 4, 6, true, false),
            Lesson(GameplayLessonId.HandDoubles, 6, 2, "Tecnica di base", "Basic technique", GameplayLessonDiscipline.Technique,
                "Doppi orchestrati", "Orchestrated doubles",
                "Suona due colpi per mano spostandoti fra rullante e tom.",
                "Play two strokes per hand while moving between snare and tom.",
                "Conserva la stessa distanza fra i colpi durante il cambio superficie.",
                "Keep equal spacing while moving between playing surfaces.",
                "R R L L · R R L L", "DOUBLE STROKES", 70, 4, 6, true, false),
            Lesson(GameplayLessonId.Paradiddle, 7, 2, "Tecnica di base", "Basic technique", GameplayLessonDiscipline.Technique,
                "Primo paradiddle", "First paradiddle",
                "Impara il movimento RLRR LRLL orchestrato sul kit.",
                "Learn the RLRR LRLL motion orchestrated around the kit.",
                "Riconosci gli accenti naturali senza accelerare i doppi.",
                "Hear the natural accents without rushing the doubles.",
                "R L R R · L R L L", "PARADIDDLE", 70, 4, 7, true, false),
            Lesson(GameplayLessonId.OneBarFill, 8, 2, "Tecnica di base", "Basic technique", GameplayLessonDiscipline.Performance,
                "Fill di una battuta", "One-bar fill",
                "Chiudi il groove con un fill semplice e controllato.",
                "Finish the groove with a simple controlled fill.",
                "Rientra sul primo movimento con crash e grancassa.",
                "Return on beat one with crash and kick.",
                "3 BATTUTE GROOVE · 1 FILL", "TOMS + SNARE", 76, 8, 8, true, true),

            Lesson(GameplayLessonId.GrooveBase8, 9, 3, "Coordinazione", "Coordination", GameplayLessonDiscipline.Coordination,
                "Groove base in ottavi", "Eighth-note groove",
                "Consolida il movimento regolare su un groove più lungo.",
                "Build consistency over a longer eighth-note groove.",
                "Mantieni l'hi-hat stabile mentre la grancassa cambia posizione.",
                "Keep the hi-hat stable while the kick pattern changes.",
                "K: 1 · 2& · 3  /  S: 2 · 4", "EIGHTH GROOVE", 78, 8, 8, true, false),
            Lesson(GameplayLessonId.SnareVariation, 10, 3, "Coordinazione", "Coordination", GameplayLessonDiscipline.Coordination,
                "Backbeat e anticipo", "Backbeat and anticipation",
                "Aggiungi un colpo di rullante in levare senza perdere il backbeat.",
                "Add an offbeat snare while preserving the backbeat.",
                "Separa l'accento aggiunto dai colpi principali su 2 e 4.",
                "Separate the added accent from the main hits on 2 and 4.",
                "S: 2 · 4 · 4&", "SNARE VARIATION", 80, 8, 8, true, false),
            Lesson(GameplayLessonId.HandsAndFeet, 11, 3, "Coordinazione", "Coordination", GameplayLessonDiscipline.Coordination,
                "Mani e pedale", "Hands and feet",
                "Coordina la grancassa con un ostinato semplice.",
                "Coordinate the kick with a simple ostinato.",
                "Mantieni le mani automatiche mentre il piede cambia figura.",
                "Keep the hands automatic while the foot pattern changes.",
                "H IN OTTAVI · K: 1 2& 3&", "KICK + HANDS", 80, 8, 9, true, false),
            Lesson(GameplayLessonId.Groove16th, 12, 3, "Coordinazione", "Coordination", GameplayLessonDiscipline.Performance,
                "Groove in sedicesimi", "Sixteenth-note groove",
                "Aumenta la densità mantenendo il movimento rilassato.",
                "Increase density while keeping the motion relaxed.",
                "Completa il modulo con sedicesimi regolari e backbeat stabile.",
                "Complete the module with even sixteenths and a stable backbeat.",
                "H: 1E&A · S: 2/4", "16TH NOTE GROOVE", 72, 8, 10, true, true),

            Lesson(GameplayLessonId.GhostNotesBase, 13, 4, "Controllo e dinamica", "Control and dynamics", GameplayLessonDiscipline.Musicality,
                "Prime ghost note", "First ghost notes",
                "Introduci colpi leggeri fra gli accenti principali.",
                "Introduce soft notes between the main accents.",
                "Distingui chiaramente note fantasma e backbeat con la velocity.",
                "Separate ghost notes and backbeat clearly through velocity.",
                "g S g · g S g", "SNARE DYNAMICS", 76, 8, 10, false, false),
            Lesson(GameplayLessonId.SwingFeel, 14, 4, "Controllo e dinamica", "Control and dynamics", GameplayLessonDiscipline.Musicality,
                "Prime sensazioni swing", "First swing feel",
                "Scopri una suddivisione meno rigida e più elastica.",
                "Explore a looser, more elastic subdivision.",
                "Mantieni la terzina interna senza trasformarla in ottavi dritti.",
                "Keep the internal triplet without flattening it into straight eighths.",
                "1 - A 2 - A 3 - A 4 - A", "SWING FEEL", 76, 8, 10, false, false),
            Lesson(GameplayLessonId.SyncopatedKick, 15, 4, "Controllo e dinamica", "Control and dynamics", GameplayLessonDiscipline.Coordination,
                "Grancassa sincopata", "Syncopated kick",
                "Sposta la grancassa sui levare mantenendo saldo il tempo.",
                "Move the kick to offbeats while holding steady time.",
                "Esegui le sincopi senza anticipare il rullante.",
                "Play syncopations without anticipating the snare.",
                "K: 1 · 2& · 3A", "SYNCOPATION", 78, 8, 10, false, false),
            Lesson(GameplayLessonId.LinearGroove, 16, 4, "Controllo e dinamica", "Control and dynamics", GameplayLessonDiscipline.Performance,
                "Groove lineare", "Linear groove",
                "Costruisci un groove in cui nessun arto colpisce insieme.",
                "Build a groove where no two limbs strike together.",
                "Mantieni il flusso continuo attraverso mani e piede.",
                "Keep continuous motion across hands and foot.",
                "H K S H · K H S H", "LINEAR CONTROL", 76, 8, 11, false, true),

            Lesson(GameplayLessonId.HalfTime, 17, 5, "Linguaggi musicali", "Musical styles", GameplayLessonDiscipline.Musicality,
                "Rock half-time", "Half-time rock",
                "Sposta il rullante sul terzo movimento e senti uno spazio più ampio.",
                "Move the snare to beat three and feel the wider space.",
                "Conserva il pulse anche quando il groove sembra rallentare.",
                "Keep the pulse when the groove appears to slow down.",
                "S SU 3", "HALF-TIME", 82, 8, 10, false, false),
            Lesson(GameplayLessonId.ShuffleRock, 18, 5, "Linguaggi musicali", "Musical styles", GameplayLessonDiscipline.Musicality,
                "Shuffle rock", "Rock shuffle",
                "Trasforma la suddivisione ternaria in un groove rock.",
                "Turn triplet subdivision into a rock groove.",
                "Fai respirare la nota centrale senza perdere il backbeat.",
                "Let the middle triplet breathe without losing the backbeat.",
                "1-A 2-A 3-A 4-A", "SHUFFLE", 84, 8, 10, false, false),
            Lesson(GameplayLessonId.FunkStarter, 19, 5, "Linguaggi musicali", "Musical styles", GameplayLessonDiscipline.Musicality,
                "Primo funk", "First funk",
                "Combina sedicesimi, sincopi e spazi in un groove essenziale.",
                "Combine sixteenths, syncopation and space in a basic funk groove.",
                "Dai priorità al pocket, non alla quantità di note.",
                "Prioritize pocket over note density.",
                "16TH · SPACE · BACKBEAT", "FUNK POCKET", 88, 8, 11, false, false),
            Lesson(GameplayLessonId.RockPerformance, 20, 5, "Linguaggi musicali", "Musical styles", GameplayLessonDiscipline.Performance,
                "Set rock", "Rock set",
                "Alterna groove dritto, half-time e fill in una base completa.",
                "Combine straight groove, half-time and fills in a full backing track.",
                "Attraversa le sezioni senza interrompere il tempo.",
                "Move through sections without interrupting time.",
                "VERSE · CHORUS · FILL", "ROCK PERFORMANCE", 88, 12, 12, false, true),

            Lesson(GameplayLessonId.DynamicControl, 21, 6, "Musicista sul palco", "Stage musician", GameplayLessonDiscipline.Musicality,
                "Controllo dinamico", "Dynamic control",
                "Costruisci crescendo, accenti e contrasto fra le sezioni.",
                "Build crescendos, accents and contrast between sections.",
                "Usa la dinamica per dare forma al brano, non solo volume.",
                "Use dynamics to shape the song, not merely add volume.",
                "P · MF · F · CRESCENDO", "DYNAMICS", 84, 8, 12, false, false),
            Lesson(GameplayLessonId.ReadingChallenge, 22, 6, "Musicista sul palco", "Stage musician", GameplayLessonDiscipline.Reading,
                "Leggi e suona", "Read and play",
                "Affronta brevi pattern nuovi dopo una preparazione limitata.",
                "Perform short unseen patterns after limited preparation.",
                "Riconosci quarti, ottavi, sedicesimi e pause prima di suonare.",
                "Recognize quarters, eighths, sixteenths and rests before playing.",
                "4 BATTUTE · 30 SEC PREP", "SIGHT READING", 80, 4, 10, false, false),
            Lesson(GameplayLessonId.FillImprovisation, 23, 6, "Musicista sul palco", "Stage musician", GameplayLessonDiscipline.Performance,
                "Crea il tuo fill", "Create your fill",
                "Rispondi a una frase musicale con un fill personale ma controllato.",
                "Answer a musical phrase with a personal but controlled fill.",
                "Concludi sempre in tempo sul primo movimento successivo.",
                "Always resolve on the following beat one.",
                "3 GROOVE · 1 FILL LIBERO", "IMPROVISATION", 84, 8, 12, false, false),
            Lesson(GameplayLessonId.FinalConcert, 24, 6, "Musicista sul palco", "Stage musician", GameplayLessonDiscipline.Performance,
                "Primo concerto", "First concert",
                "Suona un set originale completo usando tutte le competenze del corso.",
                "Perform a complete original set using every course skill.",
                "Mantieni tempo, controllo e musicalità lungo tre sezioni consecutive.",
                "Maintain time, control and musicality across three consecutive sections.",
                "3 BRANI · 1 SET", "FINAL PERFORMANCE", 90, 16, 15, false, true)
        };

        private static readonly IReadOnlyList<GameplayLessonDefinition> readOnlyLessons =
            Array.AsReadOnly(lessons);

        public static IReadOnlyList<GameplayLessonDefinition> All => readOnlyLessons;

        public static GameplayLessonDefinition Find(GameplayLessonId id)
        {
            for (int index = 0; index < lessons.Length; index++)
            {
                if (lessons[index].Id == id) return lessons[index];
            }

            throw new ArgumentOutOfRangeException(nameof(id));
        }

        public static int IndexOf(GameplayLessonId id)
        {
            for (int index = 0; index < lessons.Length; index++)
            {
                if (lessons[index].Id == id) return index;
            }
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        private static GameplayLessonDefinition Lesson(
            GameplayLessonId id,
            int number,
            int moduleNumber,
            string chapterItalian,
            string chapterEnglish,
            GameplayLessonDiscipline discipline,
            string italianTitle,
            string englishTitle,
            string italianDescription,
            string englishDescription,
            string italianObjective,
            string englishObjective,
            string exercisePattern,
            string focus,
            double bpm,
            int bars,
            int practiceMinutes,
            bool playable,
            bool moduleAssessment) => new GameplayLessonDefinition(
                id, number, moduleNumber, chapterItalian, chapterEnglish, discipline,
                italianTitle, englishTitle, italianDescription, englishDescription,
                italianObjective, englishObjective, exercisePattern, focus, bpm, bars,
                practiceMinutes, playable, moduleAssessment);
    }

    public static class GameplayLearningProgress
    {
        public const double PassAccuracy = 80.0;
        public const double MasteryAccuracy = 90.0;

        public static int PlayableCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < GameplayLearningPath.All.Count; index++)
                    if (GameplayLearningPath.All[index].IsPlayable) count++;
                return count;
            }
        }

        public static int CompletedCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < GameplayLearningPath.All.Count; index++)
                    if (IsCompleted(GameplayLearningPath.All[index].Id)) count++;
                return count;
            }
        }

        public static double? AverageMasteryAccuracy
        {
            get
            {
                double total = 0;
                int count = 0;
                for (int index = 0; index < GameplayLearningPath.All.Count; index++)
                {
                    double? value = BestAccuracy(GameplayLearningPath.All[index].Id, 1.0);
                    if (!value.HasValue) continue;
                    total += value.Value;
                    count++;
                }
                return count == 0 ? (double?)null : total / count;
            }
        }

        public static bool IsCompleted(GameplayLessonId lessonId)
        {
            double? accuracy = BestAccuracy(lessonId, 1.0);
            return accuracy.HasValue && accuracy.Value >= PassAccuracy;
        }

        public static bool IsMastered(GameplayLessonId lessonId)
        {
            double? accuracy = BestAccuracy(lessonId, 1.0);
            return accuracy.HasValue && accuracy.Value >= MasteryAccuracy;
        }

        public static bool IsUnlocked(GameplayLessonId lessonId)
        {
            GameplayLessonDefinition lesson = GameplayLearningPath.Find(lessonId);
            if (!lesson.IsPlayable) return false;
            int index = GameplayLearningPath.IndexOf(lessonId);
            return index == 0 || IsCompleted(GameplayLearningPath.All[index - 1].Id);
        }

        public static double? BestAccuracy(GameplayLessonId lessonId, double speedMultiplier)
        {
            ValidateSpeed(speedMultiplier);
            return GameplayProgressRuntime.Current.BestAccuracy(lessonId, speedMultiplier);
        }

        public static void RecordResult(GameplayLessonId lessonId, double speedMultiplier, double accuracy)
        {
            GameplayLessonDefinition lesson = GameplayLearningPath.Find(lessonId);
            if (!lesson.IsPlayable) throw new InvalidOperationException("Results can only be recorded for playable lessons.");
            ValidateSpeed(speedMultiplier);
            if (double.IsNaN(accuracy) || double.IsInfinity(accuracy) || accuracy < 0 || accuracy > 100)
                throw new ArgumentOutOfRangeException(nameof(accuracy));
            GameplayProgressRuntime.Current.RecordLessonResult(lessonId, speedMultiplier, accuracy);
        }

        public static void Reset() => GameplayProgressRuntime.Current.Reset();

        private static void ValidateSpeed(double speedMultiplier)
        {
            if (!GameplayStudySpeeds.IsSupported(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
        }
    }

    public static class GameplayLessonChartBuilder
    {
        private sealed class LessonNote
        {
            public LessonNote(double beat, string pad)
            {
                Beat = beat;
                Pad = pad;
            }

            public double Beat { get; }
            public string Pad { get; }
        }

        public static string BuildJson(GameplayLessonId lessonId)
        {
            GameplayLessonDefinition lesson = GameplayLearningPath.Find(lessonId);
            if (!lesson.IsPlayable) throw new InvalidOperationException($"Lesson '{lessonId}' has no playable chart.");

            var notes = new List<LessonNote>();
            for (int bar = 0; bar < lesson.Bars; bar++) AddBar(lessonId, bar, lesson.Bars, notes);

            double secondsPerBeat = 60.0 / lesson.Bpm;
            var json = new StringBuilder(256 + notes.Count * 42);
            json.Append("{\n  \"version\": 1,\n  \"offsetSeconds\": 0.0,\n  \"difficulties\": {\n    \"easy\": [\n");
            for (int index = 0; index < notes.Count; index++)
            {
                LessonNote note = notes[index];
                json.Append("      { \"time\": ")
                    .Append((note.Beat * secondsPerBeat).ToString("0.000000", CultureInfo.InvariantCulture))
                    .Append(", \"pad\": \"").Append(note.Pad).Append("\" }");
                if (index + 1 < notes.Count) json.Append(',');
                json.Append('\n');
            }
            json.Append("    ]\n  }\n}\n");
            return json.ToString();
        }

        private static void AddBar(GameplayLessonId lessonId, int bar, int bars, List<LessonNote> notes)
        {
            double start = bar * 4.0;
            switch (lessonId)
            {
                case GameplayLessonId.FirstPulse:
                    AddQuarters(notes, start, "kick");
                    break;
                case GameplayLessonId.Backbeat:
                    Add(notes, start, 0, "kick"); Add(notes, start, 1, "snare");
                    Add(notes, start, 2, "kick"); Add(notes, start, 3, "snare");
                    break;
                case GameplayLessonId.Timekeeper:
                    AddEighths(notes, start, "hiHat"); AddBackbeat(notes, start, false);
                    break;
                case GameplayLessonId.FirstGroove:
                    AddEighths(notes, start, "hiHat"); AddBackbeat(notes, start, true);
                    if (bar == 0 || bar == bars - 1) Add(notes, start, 0, "crash");
                    break;
                case GameplayLessonId.HandCoordination:
                    AddEighths(notes, start, "snare");
                    break;
                case GameplayLessonId.HandDoubles:
                    for (int step = 0; step < 16; step++)
                        Add(notes, start, step / 4.0, step % 4 < 2 ? "snare" : "tom1");
                    break;
                case GameplayLessonId.Paradiddle:
                    string[] stickingPads = { "snare", "tom1", "snare", "snare", "tom1", "snare", "tom1", "tom1" };
                    for (int step = 0; step < 16; step++) Add(notes, start, step / 4.0, stickingPads[step % 8]);
                    break;
                case GameplayLessonId.OneBarFill:
                    if (bar % 4 == 3)
                    {
                        string[] fill = { "snare", "snare", "tom1", "tom1", "tom2", "tom2", "floorTom", "floorTom" };
                        for (int step = 0; step < 8; step++) Add(notes, start, step / 2.0, fill[step]);
                    }
                    else
                    {
                        AddEighths(notes, start, "hiHat"); AddBackbeat(notes, start, true);
                    }
                    if (bar > 0 && bar % 4 == 0) Add(notes, start, 0, "crash");
                    break;
                case GameplayLessonId.GrooveBase8:
                    AddEighths(notes, start, "hiHat"); Add(notes, start, 0, "kick");
                    Add(notes, start, 1, "snare"); Add(notes, start, 1.5, "kick");
                    Add(notes, start, 2, "kick"); Add(notes, start, 3, "snare");
                    break;
                case GameplayLessonId.SnareVariation:
                    AddEighths(notes, start, "hiHat"); AddBackbeat(notes, start, true);
                    Add(notes, start, 3.5, "snare");
                    break;
                case GameplayLessonId.HandsAndFeet:
                    AddEighths(notes, start, "hiHat"); Add(notes, start, 0, "kick");
                    Add(notes, start, 1, "snare"); Add(notes, start, 1.5, "kick");
                    Add(notes, start, 2.5, "kick"); Add(notes, start, 3, "snare");
                    break;
                case GameplayLessonId.Groove16th:
                    for (int step = 0; step < 16; step++) Add(notes, start, step / 4.0, "hiHat");
                    Add(notes, start, 0, "kick"); Add(notes, start, 1, "snare");
                    Add(notes, start, 2.5, "kick"); Add(notes, start, 3, "snare");
                    if (bar == bars - 1) Add(notes, start, 3.75, "crash");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lessonId), lessonId, "No chart pattern is defined.");
            }
        }

        private static void AddBackbeat(List<LessonNote> notes, double start, bool variedKick)
        {
            Add(notes, start, 0, "kick"); Add(notes, start, 1, "snare");
            Add(notes, start, 2, "kick"); Add(notes, start, 3, "snare");
            if (variedKick) Add(notes, start, 2.5, "kick");
        }

        private static void AddQuarters(List<LessonNote> notes, double start, string pad)
        {
            for (int beat = 0; beat < 4; beat++) Add(notes, start, beat, pad);
        }

        private static void AddEighths(List<LessonNote> notes, double start, string pad)
        {
            for (int eighth = 0; eighth < 8; eighth++) Add(notes, start, eighth / 2.0, pad);
        }

        private static void Add(List<LessonNote> notes, double start, double beat, string pad) =>
            notes.Add(new LessonNote(start + beat, pad));
    }

    public static class GameplayCountIn
    {
        public static int RemainingBeat(double songPositionSeconds, double bpm, int countInBeats)
        {
            if (bpm <= 0 || double.IsNaN(bpm) || double.IsInfinity(bpm))
                throw new ArgumentOutOfRangeException(nameof(bpm));
            if (countInBeats <= 0) throw new ArgumentOutOfRangeException(nameof(countInBeats));
            if (songPositionSeconds >= 0) return 0;
            double beatSeconds = 60.0 / bpm;
            return Math.Min(countInBeats, Math.Max(1, (int)Math.Ceiling(-songPositionSeconds / beatSeconds)));
        }
    }
}
