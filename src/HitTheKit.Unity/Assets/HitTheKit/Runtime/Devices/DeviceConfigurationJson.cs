using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HitTheKit.Unity.Devices
{
    public sealed class ElectronicDrumProfileLoadException : Exception
    {
        public ElectronicDrumProfileLoadException(string message) : base(message) { }
        public ElectronicDrumProfileLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class UserKitConfigurationLoadException : Exception
    {
        public UserKitConfigurationLoadException(string message) : base(message) { }
        public UserKitConfigurationLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class ElectronicDrumProfileLoader
    {
        public const int SupportedSchemaVersion = 1;

        public ElectronicDrumProfile Load(string json)
        {
            ProfileDto dto = DeviceJson.Parse<ProfileDto>(json, "Profile", exception =>
                new ElectronicDrumProfileLoadException("Profile JSON is malformed.", exception));

            try
            {
                DeviceJson.RequireVersion(dto.schemaVersion, SupportedSchemaVersion, "Profile");
                DeviceJson.RequireText(dto.profileId, "profileId");
                DeviceJson.RequireText(dto.displayName, "displayName");
                if (dto.profileVersion == int.MinValue)
                {
                    throw new ArgumentException("profileVersion is required.");
                }
                if (dto.elements == null)
                {
                    throw new ArgumentException("elements is required.");
                }
                if (dto.mappings == null)
                {
                    throw new ArgumentException("mappings is required.");
                }

                KitElement[] elements = DeviceJson.ReadElements(dto.elements);
                MidiMappingEntry[] mappings = DeviceJson.ReadMappings(dto.mappings);
                return new ElectronicDrumProfile(
                    dto.schemaVersion,
                    dto.profileId,
                    dto.profileVersion,
                    dto.manufacturer,
                    dto.model,
                    dto.displayName,
                    dto.aliases ?? Array.Empty<string>(),
                    dto.portNamePatterns ?? Array.Empty<string>(),
                    elements,
                    mappings,
                    DeviceJson.ReadCapabilities(dto.capabilities ?? Array.Empty<string>()),
                    dto.notes,
                    dto.vendorId,
                    dto.productId);
            }
            catch (Exception exception) when (!(exception is ElectronicDrumProfileLoadException))
            {
                throw new ElectronicDrumProfileLoadException(
                    $"Profile validation failed: {exception.Message}", exception);
            }
        }
    }

    public sealed class UserKitConfigurationSerializer
    {
        public string Serialize(UserKitConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            UserConfigurationDto dto = DeviceJson.WriteConfiguration(configuration);
            string json = JsonUtility.ToJson(dto, true);
            json = Regex.Replace(
                json,
                "^[ \\t]*\\\"data1\\\": -2147483648,\\r?\\n",
                string.Empty,
                RegexOptions.Multiline | RegexOptions.CultureInvariant);
            return json + "\n";
        }
    }

    public sealed class UserKitConfigurationLoader
    {
        public const int SupportedSchemaVersion = 2;
        public const int LegacySchemaVersion = 1;

        public UserKitConfiguration Load(string json)
        {
            UserConfigurationDto dto = DeviceJson.Parse<UserConfigurationDto>(json, "Configuration", exception =>
                new UserKitConfigurationLoadException("Configuration JSON is malformed.", exception));
            try
            {
                if (dto.schemaVersion != LegacySchemaVersion && dto.schemaVersion != SupportedSchemaVersion)
                {
                    DeviceJson.RequireVersion(dto.schemaVersion, SupportedSchemaVersion, "Configuration");
                }
                DeviceJson.RequireText(dto.configurationId, "configurationId");
                DeviceJson.RequireText(dto.displayName, "displayName");
                if (dto.elements == null)
                {
                    throw new ArgumentException("elements is required.");
                }
                if (dto.mappings == null)
                {
                    throw new ArgumentException("mappings is required.");
                }

                ConfigurationJsonShape shape = ConfigurationJsonShape.Parse(json);
                bool isLegacy = dto.schemaVersion == LegacySchemaVersion;
                if (isLegacy)
                {
                    ValidateLegacyConfiguration(dto, shape);
                }
                else
                {
                    ValidateCurrentConfiguration(dto, shape);
                }

                if (dto.midiDeviceIdentity != null &&
                    string.IsNullOrWhiteSpace(dto.midiDeviceIdentity.portName) &&
                    HasIdentityMetadata(dto.midiDeviceIdentity))
                {
                    throw new ArgumentException("midiDeviceIdentity.portName is required when device identity is provided.");
                }
                MidiDeviceIdentity identity = dto.midiDeviceIdentity == null ||
                                              string.IsNullOrWhiteSpace(dto.midiDeviceIdentity.portName)
                    ? null
                    : new MidiDeviceIdentity(
                        dto.midiDeviceIdentity.portName,
                        dto.midiDeviceIdentity.manufacturer,
                        dto.midiDeviceIdentity.model,
                        dto.midiDeviceIdentity.productId,
                        dto.midiDeviceIdentity.vendorId,
                        dto.midiDeviceIdentity.serial,
                        dto.midiDeviceIdentity.platformIdentifier);

                KitElement[] elements = DeviceJson.ReadElements(dto.elements);
                MidiMappingEntry[] mappings = DeviceJson.ReadMappings(dto.mappings, !isLegacy);
                KitMappingReviewIssue[] reviewIssues = isLegacy
                    ? Array.Empty<KitMappingReviewIssue>()
                    : DeviceJson.ReadReviewIssues(dto.reviewIssues);

                return new UserKitConfiguration(
                    SupportedSchemaVersion,
                    dto.configurationId,
                    dto.displayName,
                    dto.baseProfileId,
                    dto.baseProfileVersion == int.MinValue ? (int?)null : dto.baseProfileVersion,
                    identity,
                    elements,
                    mappings,
                    dto.disabledElementIds ?? Array.Empty<string>(),
                    dto.userNotes,
                    dto.isComplete,
                    reviewIssues);
            }
            catch (Exception exception) when (!(exception is UserKitConfigurationLoadException))
            {
                throw new UserKitConfigurationLoadException(
                    $"Configuration validation failed: {exception.Message}", exception);
            }
        }

        private static void ValidateLegacyConfiguration(UserConfigurationDto dto, ConfigurationJsonShape shape)
        {
            if (!dto.isComplete)
            {
                throw new ArgumentException(
                    "Legacy schemaVersion 1 Draft configurations are ambiguous and cannot be migrated.");
            }
            if (shape.RootFields.Contains("reviewIssues") || shape.MappingFieldsContain("verificationState"))
            {
                throw new ArgumentException(
                    "Legacy schemaVersion 1 must not contain schemaVersion 2 candidate review fields.");
            }
        }

        private static void ValidateCurrentConfiguration(UserConfigurationDto dto, ConfigurationJsonShape shape)
        {
            shape.RequireRootField("isComplete");
            shape.RequireRootField("reviewIssues");
            if (dto.reviewIssues == null || shape.ReviewIssuesWasNull)
            {
                throw new ArgumentException("reviewIssues must be a non-null array in schemaVersion 2.");
            }

            shape.RequireMappingFields(
                "id",
                "elementId",
                "kind",
                "channel",
                "minimumValue",
                "maximumValue",
                "priority",
                "source",
                "enabled",
                "verificationState");
            shape.RequireReviewIssueFields(
                "elementId",
                "kind",
                "description",
                "blocksCompletion");
        }

        private static bool HasIdentityMetadata(MidiDeviceIdentityDto identity)
        {
            return !string.IsNullOrWhiteSpace(identity.manufacturer) ||
                   !string.IsNullOrWhiteSpace(identity.model) ||
                   !string.IsNullOrWhiteSpace(identity.productId) ||
                   !string.IsNullOrWhiteSpace(identity.vendorId) ||
                   !string.IsNullOrWhiteSpace(identity.serial) ||
                   !string.IsNullOrWhiteSpace(identity.platformIdentifier);
        }
    }

    internal sealed class ConfigurationJsonShape
    {
        private readonly IReadOnlyList<HashSet<string>> mappingFields;
        private readonly IReadOnlyList<HashSet<string>> reviewIssueFields;
        private readonly IReadOnlyList<HashSet<string>> mappingNullFields;
        private readonly IReadOnlyList<HashSet<string>> reviewIssueNullFields;

        private ConfigurationJsonShape(
            HashSet<string> rootFields,
            IReadOnlyList<HashSet<string>> mappingFields,
            IReadOnlyList<HashSet<string>> reviewIssueFields,
            IReadOnlyList<HashSet<string>> mappingNullFields,
            IReadOnlyList<HashSet<string>> reviewIssueNullFields,
            bool reviewIssuesWasNull)
        {
            RootFields = rootFields;
            this.mappingFields = mappingFields;
            this.reviewIssueFields = reviewIssueFields;
            this.mappingNullFields = mappingNullFields;
            this.reviewIssueNullFields = reviewIssueNullFields;
            ReviewIssuesWasNull = reviewIssuesWasNull;
        }

        internal HashSet<string> RootFields { get; }
        internal bool ReviewIssuesWasNull { get; }

        internal static ConfigurationJsonShape Parse(string json)
        {
            return new ShapeParser(json).Parse();
        }

        internal bool MappingFieldsContain(string field)
        {
            for (int index = 0; index < mappingFields.Count; index++)
            {
                if (mappingFields[index].Contains(field)) return true;
            }
            return false;
        }

        internal void RequireRootField(string field)
        {
            if (!RootFields.Contains(field))
            {
                throw new ArgumentException($"{field} is required in schemaVersion 2.");
            }
        }

        internal void RequireMappingFields(params string[] fields)
        {
            RequireFields(mappingFields, mappingNullFields, "Mapping", fields);
        }

        internal void RequireReviewIssueFields(params string[] fields)
        {
            RequireFields(reviewIssueFields, reviewIssueNullFields, "Review issue", fields);
        }

        private static void RequireFields(
            IReadOnlyList<HashSet<string>> objects,
            IReadOnlyList<HashSet<string>> nullFields,
            string label,
            IReadOnlyList<string> requiredFields)
        {
            for (int objectIndex = 0; objectIndex < objects.Count; objectIndex++)
            {
                for (int fieldIndex = 0; fieldIndex < requiredFields.Count; fieldIndex++)
                {
                    string field = requiredFields[fieldIndex];
                    if (!objects[objectIndex].Contains(field))
                    {
                        throw new ArgumentException(
                            $"{label} at index {objectIndex} requires {field} in schemaVersion 2.");
                    }
                    if (nullFields[objectIndex].Contains(field))
                    {
                        throw new ArgumentException(
                            $"{label} at index {objectIndex} requires non-null {field} in schemaVersion 2.");
                    }
                }
            }
        }

        private sealed class ShapeParser
        {
            private readonly string json;
            private int position;

            internal ShapeParser(string json)
            {
                this.json = json ?? throw new ArgumentNullException(nameof(json));
            }

            internal ConfigurationJsonShape Parse()
            {
                var rootFields = new HashSet<string>(StringComparer.Ordinal);
                var mappings = new List<HashSet<string>>();
                var issues = new List<HashSet<string>>();
                var mappingNulls = new List<HashSet<string>>();
                var issueNulls = new List<HashSet<string>>();
                bool reviewIssuesWasNull = false;

                SkipWhitespace();
                Expect('{');
                SkipWhitespace();
                if (!TryConsume('}'))
                {
                    while (true)
                    {
                        string name = ReadString();
                        if (!rootFields.Add(name))
                        {
                            throw new ArgumentException($"Configuration contains duplicate field '{name}'.");
                        }
                        SkipWhitespace();
                        Expect(':');
                        SkipWhitespace();
                        if (string.Equals(name, "mappings", StringComparison.Ordinal))
                        {
                            ReadObjectArray(mappings, mappingNulls, "mappings");
                        }
                        else if (string.Equals(name, "reviewIssues", StringComparison.Ordinal))
                        {
                            reviewIssuesWasNull = ReadObjectArray(issues, issueNulls, "reviewIssues");
                        }
                        else
                        {
                            SkipValue();
                        }

                        SkipWhitespace();
                        if (TryConsume('}')) break;
                        Expect(',');
                        SkipWhitespace();
                    }
                }
                SkipWhitespace();
                if (position != json.Length)
                {
                    throw new ArgumentException("Configuration JSON contains trailing data.");
                }
                return new ConfigurationJsonShape(
                    rootFields,
                    mappings.AsReadOnly(),
                    issues.AsReadOnly(),
                    mappingNulls.AsReadOnly(),
                    issueNulls.AsReadOnly(),
                    reviewIssuesWasNull);
            }

            private bool ReadObjectArray(
                ICollection<HashSet<string>> destination,
                ICollection<HashSet<string>> nullDestination,
                string label)
            {
                if (StartsWith("null"))
                {
                    position += 4;
                    return true;
                }

                Expect('[');
                SkipWhitespace();
                if (TryConsume(']')) return false;
                while (true)
                {
                    if (Peek() != '{')
                    {
                        throw new ArgumentException($"{label} entries must be JSON objects.");
                    }
                    destination.Add(ReadObjectFields(label, out HashSet<string> nullFields));
                    nullDestination.Add(nullFields);
                    SkipWhitespace();
                    if (TryConsume(']')) break;
                    Expect(',');
                    SkipWhitespace();
                }
                return false;
            }

            private HashSet<string> ReadObjectFields(string label, out HashSet<string> nullFields)
            {
                var fields = new HashSet<string>(StringComparer.Ordinal);
                nullFields = new HashSet<string>(StringComparer.Ordinal);
                Expect('{');
                SkipWhitespace();
                if (TryConsume('}')) return fields;
                while (true)
                {
                    string name = ReadString();
                    if (!fields.Add(name))
                    {
                        throw new ArgumentException($"{label} entry contains duplicate field '{name}'.");
                    }
                    SkipWhitespace();
                    Expect(':');
                    SkipWhitespace();
                    if (StartsWith("null")) nullFields.Add(name);
                    SkipValue();
                    SkipWhitespace();
                    if (TryConsume('}')) break;
                    Expect(',');
                    SkipWhitespace();
                }
                return fields;
            }

            private void SkipValue()
            {
                SkipWhitespace();
                char current = Peek();
                if (current == '"')
                {
                    ReadString();
                    return;
                }
                if (current == '{')
                {
                    SkipObject();
                    return;
                }
                if (current == '[')
                {
                    SkipArray();
                    return;
                }
                SkipPrimitive();
            }

            private void SkipObject()
            {
                Expect('{');
                SkipWhitespace();
                if (TryConsume('}')) return;
                while (true)
                {
                    ReadString();
                    SkipWhitespace();
                    Expect(':');
                    SkipWhitespace();
                    SkipValue();
                    SkipWhitespace();
                    if (TryConsume('}')) return;
                    Expect(',');
                    SkipWhitespace();
                }
            }

            private void SkipArray()
            {
                Expect('[');
                SkipWhitespace();
                if (TryConsume(']')) return;
                while (true)
                {
                    SkipValue();
                    SkipWhitespace();
                    if (TryConsume(']')) return;
                    Expect(',');
                    SkipWhitespace();
                }
            }

            private void SkipPrimitive()
            {
                int start = position;
                while (position < json.Length)
                {
                    char current = json[position];
                    if (char.IsWhiteSpace(current) || current == ',' || current == ']' || current == '}') break;
                    position++;
                }
                if (position == start)
                {
                    throw new ArgumentException($"Invalid JSON value at character {position}.");
                }
            }

            private string ReadString()
            {
                Expect('"');
                var value = new StringBuilder();
                while (position < json.Length)
                {
                    char current = json[position++];
                    if (current == '"') return value.ToString();
                    if (current != '\\')
                    {
                        value.Append(current);
                        continue;
                    }
                    if (position >= json.Length) break;
                    char escaped = json[position++];
                    switch (escaped)
                    {
                        case '"': value.Append('"'); break;
                        case '\\': value.Append('\\'); break;
                        case '/': value.Append('/'); break;
                        case 'b': value.Append('\b'); break;
                        case 'f': value.Append('\f'); break;
                        case 'n': value.Append('\n'); break;
                        case 'r': value.Append('\r'); break;
                        case 't': value.Append('\t'); break;
                        case 'u':
                            if (position + 4 > json.Length)
                            {
                                throw new ArgumentException("Configuration JSON contains an invalid unicode escape.");
                            }
                            string hex = json.Substring(position, 4);
                            if (!ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture, out ushort codePoint))
                            {
                                throw new ArgumentException("Configuration JSON contains an invalid unicode escape.");
                            }
                            value.Append((char)codePoint);
                            position += 4;
                            break;
                        default:
                            throw new ArgumentException("Configuration JSON contains an invalid string escape.");
                    }
                }
                throw new ArgumentException("Configuration JSON contains an unterminated string.");
            }

            private bool StartsWith(string value)
            {
                return position + value.Length <= json.Length &&
                       string.CompareOrdinal(json, position, value, 0, value.Length) == 0;
            }

            private char Peek()
            {
                if (position >= json.Length)
                {
                    throw new ArgumentException("Configuration JSON ended unexpectedly.");
                }
                return json[position];
            }

            private void Expect(char expected)
            {
                if (Peek() != expected)
                {
                    throw new ArgumentException(
                        $"Configuration JSON expected '{expected}' at character {position}.");
                }
                position++;
            }

            private bool TryConsume(char expected)
            {
                if (position >= json.Length || json[position] != expected) return false;
                position++;
                return true;
            }

            private void SkipWhitespace()
            {
                while (position < json.Length && char.IsWhiteSpace(json[position])) position++;
            }
        }
    }

    internal static class DeviceJson
    {
        internal static T Parse<T>(string json, string label, Func<Exception, Exception> wrap) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw wrap(new ArgumentException($"{label} JSON must not be null or empty."));
            }
            try
            {
                T result = JsonUtility.FromJson<T>(json);
                if (result == null)
                {
                    throw new ArgumentException($"{label} JSON did not contain an object.");
                }
                return result;
            }
            catch (Exception exception)
            {
                throw wrap(exception);
            }
        }

        internal static void RequireVersion(int actual, int supported, string label)
        {
            if (actual == int.MinValue)
            {
                throw new ArgumentException($"{label} schemaVersion is required.");
            }
            if (actual != supported)
            {
                throw new ArgumentException($"{label} schemaVersion {actual} is unsupported; expected {supported}.");
            }
        }

        internal static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} is required.");
            }
        }

        internal static KitElement[] ReadElements(ElementDto[] dtos)
        {
            var result = new KitElement[dtos.Length];
            for (int index = 0; index < dtos.Length; index++)
            {
                ElementDto dto = dtos[index] ?? throw new ArgumentException($"Element at index {index} is null.");
                result[index] = new KitElement(
                    dto.id,
                    ParsePiece(dto.piece),
                    ParseArticulation(dto.articulation),
                    dto.displayName,
                    dto.isOptional);
            }
            return result;
        }

        internal static MidiMappingEntry[] ReadMappings(
            MappingDto[] dtos,
            bool requireVerificationState = false)
        {
            var result = new MidiMappingEntry[dtos.Length];
            for (int index = 0; index < dtos.Length; index++)
            {
                MappingDto dto = dtos[index] ?? throw new ArgumentException($"Mapping at index {index} is null.");
                if (dto.minimumValue == int.MinValue || dto.maximumValue == int.MinValue)
                {
                    throw new ArgumentException($"Mapping at index {index} requires minimumValue and maximumValue.");
                }
                int? channel = dto.channel == -1 ? (int?)null : dto.channel;
                RawMidiMessageKind kind = ParseMessageKind(dto.kind);
                bool hasNoData1 = kind == RawMidiMessageKind.ChannelAftertouch || kind == RawMidiMessageKind.PitchBend;
                if (hasNoData1 && dto.data1 != int.MinValue)
                {
                    throw new ArgumentException($"Mapping at index {index} must omit data1 for this message kind.");
                }
                int? data1 = hasNoData1 ? (int?)null : dto.data1;
                result[index] = new MidiMappingEntry(
                    dto.id,
                    new MidiTrigger(kind, channel, data1, dto.minimumValue, dto.maximumValue),
                    dto.elementId,
                    dto.priority,
                    ParseMappingSource(dto.source),
                    dto.enabled,
                    dto.notes,
                    requireVerificationState
                        ? ParseVerificationState(dto.verificationState)
                        : MidiMappingVerificationState.Confirmed);
            }
            return result;
        }

        internal static KitMappingReviewIssue[] ReadReviewIssues(ReviewIssueDto[] dtos)
        {
            var result = new KitMappingReviewIssue[dtos.Length];
            for (int index = 0; index < dtos.Length; index++)
            {
                ReviewIssueDto dto = dtos[index] ?? throw new ArgumentException($"Review issue at index {index} is null.");
                try
                {
                    result[index] = new KitMappingReviewIssue(
                        dto.elementId,
                        ParseReviewIssueKind(dto.kind),
                        dto.description,
                        dto.blocksCompletion);
                }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException(
                        $"Review issue at index {index} is invalid: {exception.Message}",
                        exception);
                }
            }
            return result;
        }

        internal static ElectronicDrumCapability ReadCapabilities(string[] values)
        {
            ElectronicDrumCapability result = ElectronicDrumCapability.None;
            foreach (string value in values)
            {
                switch (value)
                {
                    case "multiZoneSnare": result |= ElectronicDrumCapability.MultiZoneSnare; break;
                    case "multiZoneToms": result |= ElectronicDrumCapability.MultiZoneToms; break;
                    case "hiHatContinuousController": result |= ElectronicDrumCapability.HiHatContinuousController; break;
                    case "hiHatPedalNote": result |= ElectronicDrumCapability.HiHatPedalNote; break;
                    case "crashChoke": result |= ElectronicDrumCapability.CrashChoke; break;
                    case "rideBell": result |= ElectronicDrumCapability.RideBell; break;
                    case "rideEdge": result |= ElectronicDrumCapability.RideEdge; break;
                    default: throw new ArgumentException($"Unknown capability '{value ?? "<null>"}'.");
                }
            }
            return result;
        }

        internal static UserConfigurationDto WriteConfiguration(UserKitConfiguration configuration)
        {
            var dto = new UserConfigurationDto
            {
                schemaVersion = configuration.SchemaVersion,
                configurationId = configuration.ConfigurationId,
                displayName = configuration.DisplayName,
                baseProfileId = configuration.BaseProfileId,
                baseProfileVersion = configuration.BaseProfileVersion ?? int.MinValue,
                userNotes = configuration.UserNotes,
                isComplete = configuration.IsComplete,
                disabledElementIds = Copy(configuration.DisabledElementIds),
                elements = new ElementDto[configuration.Elements.Count],
                mappings = new MappingDto[configuration.Mappings.Count],
                reviewIssues = new ReviewIssueDto[configuration.ReviewIssues.Count]
            };

            if (configuration.MidiDeviceIdentity != null)
            {
                MidiDeviceIdentity identity = configuration.MidiDeviceIdentity;
                dto.midiDeviceIdentity = new MidiDeviceIdentityDto
                {
                    portName = identity.PortName,
                    manufacturer = identity.Manufacturer,
                    model = identity.Model,
                    productId = identity.ProductId,
                    vendorId = identity.VendorId,
                    serial = identity.Serial,
                    platformIdentifier = identity.PlatformIdentifier
                };
            }

            for (int index = 0; index < configuration.Elements.Count; index++)
            {
                KitElement element = configuration.Elements[index];
                dto.elements[index] = new ElementDto
                {
                    id = element.Id,
                    piece = WritePiece(element.Piece),
                    articulation = WriteArticulation(element.Articulation),
                    displayName = element.DisplayName,
                    isOptional = element.IsOptional
                };
            }
            for (int index = 0; index < configuration.Mappings.Count; index++)
            {
                MidiMappingEntry mapping = configuration.Mappings[index];
                dto.mappings[index] = new MappingDto
                {
                    id = mapping.Id,
                    elementId = mapping.ElementId,
                    kind = WriteMessageKind(mapping.Trigger.Kind),
                    channel = mapping.Trigger.Channel ?? -1,
                    data1 = mapping.Trigger.Data1 ?? int.MinValue,
                    minimumValue = mapping.Trigger.MinimumValue,
                    maximumValue = mapping.Trigger.MaximumValue,
                    priority = mapping.Priority,
                    source = WriteMappingSource(mapping.Source),
                    enabled = mapping.Enabled,
                    notes = mapping.Notes,
                    verificationState = WriteVerificationState(mapping.VerificationState)
                };
            }
            for (int index = 0; index < configuration.ReviewIssues.Count; index++)
            {
                KitMappingReviewIssue issue = configuration.ReviewIssues[index];
                dto.reviewIssues[index] = new ReviewIssueDto
                {
                    elementId = issue.ElementId,
                    kind = WriteReviewIssueKind(issue.Kind),
                    description = issue.Description,
                    blocksCompletion = issue.BlocksCompletion
                };
            }
            return dto;
        }

        internal static KitPiece ParsePiece(string value)
        {
            switch (value)
            {
                case "kick": return KitPiece.Kick;
                case "snare": return KitPiece.Snare;
                case "hiHat": return KitPiece.HiHat;
                case "tom1": return KitPiece.Tom1;
                case "tom2": return KitPiece.Tom2;
                case "tom3": return KitPiece.Tom3;
                case "tom4": return KitPiece.Tom4;
                case "floorTom": return KitPiece.FloorTom;
                case "crash1": return KitPiece.Crash1;
                case "crash2": return KitPiece.Crash2;
                case "crash3": return KitPiece.Crash3;
                case "ride": return KitPiece.Ride;
                default: throw new ArgumentException($"Unknown kit piece '{value ?? "<null>"}'.");
            }
        }

        internal static KitArticulation ParseArticulation(string value)
        {
            switch (value)
            {
                case "default": return KitArticulation.Default;
                case "head": return KitArticulation.Head;
                case "rim": return KitArticulation.Rim;
                case "bow": return KitArticulation.Bow;
                case "edge": return KitArticulation.Edge;
                case "bell": return KitArticulation.Bell;
                case "closed": return KitArticulation.Closed;
                case "halfOpen": return KitArticulation.HalfOpen;
                case "open": return KitArticulation.Open;
                case "pedal": return KitArticulation.Pedal;
                case "choke": return KitArticulation.Choke;
                default: throw new ArgumentException($"Unknown articulation '{value ?? "<null>"}'.");
            }
        }

        internal static RawMidiMessageKind ParseMessageKind(string value)
        {
            switch (value)
            {
                case "noteOn": return RawMidiMessageKind.NoteOn;
                case "noteOff": return RawMidiMessageKind.NoteOff;
                case "controlChange": return RawMidiMessageKind.ControlChange;
                case "polyAftertouch": return RawMidiMessageKind.PolyAftertouch;
                case "channelAftertouch": return RawMidiMessageKind.ChannelAftertouch;
                case "pitchBend": return RawMidiMessageKind.PitchBend;
                case "programChange": return RawMidiMessageKind.ProgramChange;
                default: throw new ArgumentException($"Unknown MIDI message kind '{value ?? "<null>"}'.");
            }
        }

        internal static MidiMappingSource ParseMappingSource(string value)
        {
            switch (value)
            {
                case "builtInProfile": return MidiMappingSource.BuiltInProfile;
                case "wizardCapture": return MidiMappingSource.WizardCapture;
                case "userOverride": return MidiMappingSource.UserOverride;
                default: throw new ArgumentException($"Unknown mapping source '{value ?? "<null>"}'.");
            }
        }

        internal static MidiMappingVerificationState ParseVerificationState(string value)
        {
            switch (value)
            {
                case "confirmed": return MidiMappingVerificationState.Confirmed;
                case "requiresConfirmation": return MidiMappingVerificationState.RequiresConfirmation;
                default: throw new ArgumentException(
                    $"Unknown mapping verification state '{value ?? "<null>"}'.");
            }
        }

        internal static KitMappingReviewIssueKind ParseReviewIssueKind(string value)
        {
            switch (value)
            {
                case "conflict": return KitMappingReviewIssueKind.Conflict;
                case "insufficient": return KitMappingReviewIssueKind.Insufficient;
                default: throw new ArgumentException($"Unknown review issue kind '{value ?? "<null>"}'.");
            }
        }

        internal static string WritePiece(KitPiece value)
        {
            switch (value)
            {
                case KitPiece.Kick: return "kick";
                case KitPiece.Snare: return "snare";
                case KitPiece.HiHat: return "hiHat";
                case KitPiece.Tom1: return "tom1";
                case KitPiece.Tom2: return "tom2";
                case KitPiece.Tom3: return "tom3";
                case KitPiece.Tom4: return "tom4";
                case KitPiece.FloorTom: return "floorTom";
                case KitPiece.Crash1: return "crash1";
                case KitPiece.Crash2: return "crash2";
                case KitPiece.Crash3: return "crash3";
                case KitPiece.Ride: return "ride";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        internal static string WriteArticulation(KitArticulation value)
        {
            switch (value)
            {
                case KitArticulation.Default: return "default";
                case KitArticulation.Head: return "head";
                case KitArticulation.Rim: return "rim";
                case KitArticulation.Bow: return "bow";
                case KitArticulation.Edge: return "edge";
                case KitArticulation.Bell: return "bell";
                case KitArticulation.Closed: return "closed";
                case KitArticulation.HalfOpen: return "halfOpen";
                case KitArticulation.Open: return "open";
                case KitArticulation.Pedal: return "pedal";
                case KitArticulation.Choke: return "choke";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        internal static string WriteMessageKind(RawMidiMessageKind value)
        {
            switch (value)
            {
                case RawMidiMessageKind.NoteOn: return "noteOn";
                case RawMidiMessageKind.NoteOff: return "noteOff";
                case RawMidiMessageKind.ControlChange: return "controlChange";
                case RawMidiMessageKind.PolyAftertouch: return "polyAftertouch";
                case RawMidiMessageKind.ChannelAftertouch: return "channelAftertouch";
                case RawMidiMessageKind.PitchBend: return "pitchBend";
                case RawMidiMessageKind.ProgramChange: return "programChange";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        internal static string WriteMappingSource(MidiMappingSource value)
        {
            switch (value)
            {
                case MidiMappingSource.BuiltInProfile: return "builtInProfile";
                case MidiMappingSource.WizardCapture: return "wizardCapture";
                case MidiMappingSource.UserOverride: return "userOverride";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        internal static string WriteVerificationState(MidiMappingVerificationState value)
        {
            switch (value)
            {
                case MidiMappingVerificationState.Confirmed: return "confirmed";
                case MidiMappingVerificationState.RequiresConfirmation: return "requiresConfirmation";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        internal static string WriteReviewIssueKind(KitMappingReviewIssueKind value)
        {
            switch (value)
            {
                case KitMappingReviewIssueKind.Conflict: return "conflict";
                case KitMappingReviewIssueKind.Insufficient: return "insufficient";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static string[] Copy(IReadOnlyList<string> values)
        {
            var result = new string[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index];
            return result;
        }
    }

    [Serializable]
    internal sealed class ProfileDto
    {
        public int schemaVersion = int.MinValue;
        public string profileId;
        public int profileVersion = int.MinValue;
        public string manufacturer;
        public string model;
        public string displayName;
        public string[] aliases;
        public string[] portNamePatterns;
        public string vendorId;
        public string productId;
        public string[] capabilities;
        public string notes;
        public ElementDto[] elements;
        public MappingDto[] mappings;
    }

    [Serializable]
    internal sealed class UserConfigurationDto
    {
        public int schemaVersion = int.MinValue;
        public string configurationId;
        public string displayName;
        public string baseProfileId;
        public int baseProfileVersion = int.MinValue;
        public MidiDeviceIdentityDto midiDeviceIdentity;
        public ElementDto[] elements;
        public MappingDto[] mappings;
        public string[] disabledElementIds;
        public string userNotes;
        public bool isComplete;
        public ReviewIssueDto[] reviewIssues;
    }

    [Serializable]
    internal sealed class MidiDeviceIdentityDto
    {
        public string portName;
        public string manufacturer;
        public string model;
        public string productId;
        public string vendorId;
        public string serial;
        public string platformIdentifier;
    }

    [Serializable]
    internal sealed class ElementDto
    {
        public string id;
        public string piece;
        public string articulation;
        public string displayName;
        public bool isOptional;
    }

    [Serializable]
    internal sealed class MappingDto
    {
        public string id;
        public string elementId;
        public string kind;
        public int channel = int.MinValue;
        public int data1 = int.MinValue;
        public int minimumValue = int.MinValue;
        public int maximumValue = int.MinValue;
        public int priority;
        public string source;
        public bool enabled;
        public string notes;
        public string verificationState;
    }

    [Serializable]
    internal sealed class ReviewIssueDto
    {
        public string elementId;
        public string kind;
        public string description;
        public bool blocksCompletion;
    }
}
