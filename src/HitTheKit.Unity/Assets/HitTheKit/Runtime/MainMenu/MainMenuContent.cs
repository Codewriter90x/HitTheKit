using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.MainMenu
{
    public enum MainMenuDestination
    {
        Play,
        Learn,
        DeviceSetup
    }

    public enum MainMenuLanguage
    {
        Italian,
        English
    }

    public sealed class MainMenuDestinationContent
    {
        public MainMenuDestinationContent(MainMenuDestination destination, string title, string subtitle)
        {
            Destination = destination;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle));
        }

        public MainMenuDestination Destination { get; }
        public string Title { get; }
        public string Subtitle { get; }
    }

    public sealed class MainMenuContent
    {
        private readonly IReadOnlyDictionary<MainMenuDestination, MainMenuDestinationContent> destinations;

        private MainMenuContent(
            string eyebrow,
            string status,
            string select,
            string learnHeading,
            string learnCopy,
            string practice,
            string back,
            string settings,
            string audio,
            string reducedMotion,
            string exit,
            IReadOnlyDictionary<MainMenuDestination, MainMenuDestinationContent> destinations)
        {
            Eyebrow = eyebrow;
            Status = status;
            Select = select;
            LearnHeading = learnHeading;
            LearnCopy = learnCopy;
            Practice = practice;
            Back = back;
            Settings = settings;
            Audio = audio;
            ReducedMotion = reducedMotion;
            Exit = exit;
            this.destinations = destinations;
        }

        public string Eyebrow { get; }
        public string Status { get; }
        public string Select { get; }
        public string LearnHeading { get; }
        public string LearnCopy { get; }
        public string Practice { get; }
        public string Back { get; }
        public string Settings { get; }
        public string Audio { get; }
        public string ReducedMotion { get; }
        public string Exit { get; }

        public MainMenuDestinationContent Destination(MainMenuDestination destination)
        {
            if (!destinations.TryGetValue(destination, out MainMenuDestinationContent value))
                throw new ArgumentOutOfRangeException(nameof(destination));
            return value;
        }

        public static MainMenuContent For(MainMenuLanguage language)
        {
            switch (language)
            {
                case MainMenuLanguage.Italian:
                    return new MainMenuContent(
                        "STAGE COMMAND",
                        "INPUT: TASTIERA / CORE MIDI",
                        "SELEZIONA",
                        "IMPARA",
                        "Parti da un solo colpo e costruisci il tuo primo groove, una lezione alla volta.",
                        "INIZIA",
                        "INDIETRO",
                        "IMPOSTAZIONI",
                        "AUDIO",
                        "RIDUCI MOVIMENTO",
                        "ESCI",
                        ItalianDestinations());
                case MainMenuLanguage.English:
                    return new MainMenuContent(
                        "STAGE COMMAND",
                        "INPUT: KEYBOARD / CORE MIDI",
                        "SELECT",
                        "LEARN",
                        "Start with one hit and build your first groove, one lesson at a time.",
                        "START",
                        "BACK",
                        "SETTINGS",
                        "AUDIO",
                        "REDUCE MOTION",
                        "EXIT",
                        EnglishDestinations());
                default:
                    throw new ArgumentOutOfRangeException(nameof(language));
            }
        }

        private static IReadOnlyDictionary<MainMenuDestination, MainMenuDestinationContent> ItalianDestinations()
        {
            return new Dictionary<MainMenuDestination, MainMenuDestinationContent>
            {
                [MainMenuDestination.Play] = new MainMenuDestinationContent(MainMenuDestination.Play, "GIOCA", "Scegli un brano e sali sul palco"),
                [MainMenuDestination.Learn] = new MainMenuDestinationContent(MainMenuDestination.Learn, "IMPARA", "Allenati con lezioni e pattern guidati"),
                [MainMenuDestination.DeviceSetup] = new MainMenuDestinationContent(MainMenuDestination.DeviceSetup, "CONFIGURA BATTERIA", "Collega e mappa il tuo kit elettronico")
            };
        }

        private static IReadOnlyDictionary<MainMenuDestination, MainMenuDestinationContent> EnglishDestinations()
        {
            return new Dictionary<MainMenuDestination, MainMenuDestinationContent>
            {
                [MainMenuDestination.Play] = new MainMenuDestinationContent(MainMenuDestination.Play, "PLAY", "Choose a song and take the stage"),
                [MainMenuDestination.Learn] = new MainMenuDestinationContent(MainMenuDestination.Learn, "LEARN", "Train with guided lessons and patterns"),
                [MainMenuDestination.DeviceSetup] = new MainMenuDestinationContent(MainMenuDestination.DeviceSetup, "SET UP DRUMS", "Connect and map your electronic kit")
            };
        }
    }

    public static class MainMenuRoutes
    {
        public const string MainMenuScene = "MainMenuPrototype";
        public const string GameplayScene = "GameplayPrototype";
        public const string DeviceSetupScene = "DeviceSetupPrototype";
    }
}
