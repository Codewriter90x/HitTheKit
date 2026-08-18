using System;
using System.Text.RegularExpressions;

namespace HitTheKit.Unity.Devices
{
    public enum KitPiece
    {
        Kick,
        Snare,
        HiHat,
        Tom1,
        Tom2,
        FloorTom,
        Crash1,
        Crash2,
        Ride,
        Tom3,
        Tom4,
        Crash3
    }

    public enum KitArticulation
    {
        Default,
        Head,
        Rim,
        Bow,
        Edge,
        Bell,
        Closed,
        HalfOpen,
        Open,
        Pedal,
        Choke
    }

    [Flags]
    public enum ElectronicDrumCapability
    {
        None = 0,
        MultiZoneSnare = 1 << 0,
        MultiZoneToms = 1 << 1,
        HiHatContinuousController = 1 << 2,
        HiHatPedalNote = 1 << 3,
        CrashChoke = 1 << 4,
        RideBell = 1 << 5,
        RideEdge = 1 << 6
    }

    public enum MidiMappingSource
    {
        BuiltInProfile,
        WizardCapture,
        UserOverride
    }

    public enum MidiMappingVerificationState
    {
        Confirmed,
        RequiresConfirmation
    }

    public enum KitMappingReviewIssueKind
    {
        Conflict,
        Insufficient
    }

    public sealed class KitMappingReviewIssue
    {
        public KitMappingReviewIssue(
            string elementId,
            KitMappingReviewIssueKind kind,
            string description,
            bool blocksCompletion)
        {
            KitElement.EnsureStableId(elementId, nameof(elementId));
            if (!Enum.IsDefined(typeof(KitMappingReviewIssueKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Review issue description is required.", nameof(description));
            }

            ElementId = elementId;
            Kind = kind;
            Description = description;
            BlocksCompletion = blocksCompletion;
        }

        public string ElementId { get; }
        public KitMappingReviewIssueKind Kind { get; }
        public string Description { get; }
        public bool BlocksCompletion { get; }
    }

    public static class KitElementDefinitionValidator
    {
        public static bool IsValid(KitPiece piece, KitArticulation articulation)
        {
            if (!Enum.IsDefined(typeof(KitPiece), piece) ||
                !Enum.IsDefined(typeof(KitArticulation), articulation))
            {
                return false;
            }

            if (articulation == KitArticulation.Default)
            {
                return true;
            }

            switch (piece)
            {
                case KitPiece.Snare:
                case KitPiece.Tom1:
                case KitPiece.Tom2:
                case KitPiece.Tom3:
                case KitPiece.Tom4:
                case KitPiece.FloorTom:
                    return articulation == KitArticulation.Head || articulation == KitArticulation.Rim;
                case KitPiece.HiHat:
                    return articulation == KitArticulation.Bow ||
                           articulation == KitArticulation.Edge ||
                           articulation == KitArticulation.Closed ||
                           articulation == KitArticulation.HalfOpen ||
                           articulation == KitArticulation.Open ||
                           articulation == KitArticulation.Pedal ||
                           articulation == KitArticulation.Choke;
                case KitPiece.Crash1:
                case KitPiece.Crash2:
                case KitPiece.Crash3:
                    return articulation == KitArticulation.Bow ||
                           articulation == KitArticulation.Edge ||
                           articulation == KitArticulation.Choke;
                case KitPiece.Ride:
                    return articulation == KitArticulation.Bow ||
                           articulation == KitArticulation.Edge ||
                           articulation == KitArticulation.Bell ||
                           articulation == KitArticulation.Choke;
                default:
                    return false;
            }
        }

        public static void EnsureValid(KitPiece piece, KitArticulation articulation)
        {
            if (!IsValid(piece, articulation))
            {
                throw new ArgumentException($"Articulation '{articulation}' is not valid for kit piece '{piece}'.");
            }
        }
    }

    public sealed class KitElement
    {
        private static readonly Regex StableIdPattern =
            new Regex("^[a-z][a-z0-9-]*(\\.[a-z0-9][a-z0-9-]*)*$", RegexOptions.CultureInvariant);

        public KitElement(
            string id,
            KitPiece piece,
            KitArticulation articulation,
            string displayName,
            bool isOptional = false)
        {
            EnsureStableId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name must not be empty.", nameof(displayName));
            }

            KitElementDefinitionValidator.EnsureValid(piece, articulation);
            Id = id;
            Piece = piece;
            Articulation = articulation;
            DisplayName = displayName;
            IsOptional = isOptional;
        }

        public string Id { get; }
        public KitPiece Piece { get; }
        public KitArticulation Articulation { get; }
        public string DisplayName { get; }
        public bool IsOptional { get; }

        internal static void EnsureStableId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !StableIdPattern.IsMatch(value))
            {
                throw new ArgumentException(
                    "Stable identifiers must use lowercase letters, digits, dots or hyphens.",
                    parameterName);
            }
        }
    }

    public sealed class MidiMappingEntry
    {
        public MidiMappingEntry(
            string id,
            MidiTrigger trigger,
            string elementId,
            int priority = 0,
            MidiMappingSource source = MidiMappingSource.BuiltInProfile,
            bool enabled = true,
            string notes = null,
            MidiMappingVerificationState verificationState = MidiMappingVerificationState.Confirmed)
        {
            KitElement.EnsureStableId(id, nameof(id));
            KitElement.EnsureStableId(elementId, nameof(elementId));
            if (!Enum.IsDefined(typeof(MidiMappingSource), source))
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }
            if (!Enum.IsDefined(typeof(MidiMappingVerificationState), verificationState))
            {
                throw new ArgumentOutOfRangeException(nameof(verificationState));
            }

            Id = id;
            Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
            ElementId = elementId;
            Priority = priority;
            Source = source;
            Enabled = enabled;
            Notes = notes;
            VerificationState = verificationState;
        }

        public string Id { get; }
        public MidiTrigger Trigger { get; }
        public string ElementId { get; }
        public int Priority { get; }
        public MidiMappingSource Source { get; }
        public bool Enabled { get; }
        public string Notes { get; }
        public MidiMappingVerificationState VerificationState { get; }
    }

    public sealed class MidiDeviceIdentity
    {
        public MidiDeviceIdentity(
            string portName,
            string manufacturer = null,
            string model = null,
            string productId = null,
            string vendorId = null,
            string serial = null,
            string platformIdentifier = null)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("Port name must not be empty.", nameof(portName));
            }

            PortName = portName;
            Manufacturer = EmptyToNull(manufacturer);
            Model = EmptyToNull(model);
            ProductId = EmptyToNull(productId);
            VendorId = EmptyToNull(vendorId);
            Serial = EmptyToNull(serial);
            PlatformIdentifier = EmptyToNull(platformIdentifier);
        }

        public string PortName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public string ProductId { get; }
        public string VendorId { get; }
        public string Serial { get; }
        public string PlatformIdentifier { get; }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
