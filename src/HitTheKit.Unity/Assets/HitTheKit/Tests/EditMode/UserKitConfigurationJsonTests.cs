using System;
using System.Linq;
using System.Text.RegularExpressions;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class UserKitConfigurationJsonTests
    {
        [Test]
        public void Configuration_round_trip_is_deterministic()
        {
            UserKitConfiguration original = Configuration();
            var serializer = new UserKitConfigurationSerializer();
            var loader = new UserKitConfigurationLoader();
            string first = serializer.Serialize(original);
            UserKitConfiguration loaded = loader.Load(first);
            string second = serializer.Serialize(loaded);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(loaded.ConfigurationId, Is.EqualTo("user.my-kit"));
            Assert.That(loaded.MidiDeviceIdentity.PortName, Is.EqualTo("Test Port"));
            Assert.That(loaded.Elements, Has.Count.EqualTo(1));
            Assert.That(loaded.Mappings, Has.Count.EqualTo(1));
        }

        [Test]
        public void Serializer_does_not_mutate_profile_when_configuration_is_derived()
        {
            var profileElements = new[] { new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick") };
            var profileMappings = new[]
            {
                new MidiMappingEntry("profile.kick", new MidiTrigger(RawMidiMessageKind.NoteOn, null, 36, 1, 127), "kick.default")
            };
            var profile = new ElectronicDrumProfile(1, "test.profile", 1, "Test", "Kit", "Test Kit",
                Array.Empty<string>(), Array.Empty<string>(), profileElements, profileMappings, ElectronicDrumCapability.None);
            UserKitConfiguration configuration = UserKitConfiguration.FromProfile(profile, "user.derived", "Derived");

            profileElements[0] = new KitElement("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare");
            profileMappings[0] = new MidiMappingEntry("profile.snare", new MidiTrigger(RawMidiMessageKind.NoteOn, null, 38, 1, 127), "snare.head");

            Assert.That(profile.Elements[0].Id, Is.EqualTo("kick.default"));
            Assert.That(configuration.Elements[0], Is.SameAs(profile.Elements[0]));
            Assert.That(configuration.Mappings[0], Is.SameAs(profile.DefaultMappings[0]));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{")]
        public void Loader_rejects_invalid_json(string json)
        {
            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(json));
        }

        [Test]
        public void Loader_rejects_missing_or_unsupported_version()
        {
            string valid = new UserKitConfigurationSerializer().Serialize(Configuration());
            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(
                valid.Replace("\"schemaVersion\": 2,", "")));
            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(
                valid.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 3")));
            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(
                valid.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 0")));
            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(
                valid.Replace("\"schemaVersion\": 2", "\"schemaVersion\": -1")));
        }

        [Test]
        public void Legacy_schema_one_complete_configuration_migrates_explicitly_to_current_schema()
        {
            var serializer = new UserKitConfigurationSerializer();
            string legacy = ToLegacyV1(serializer.Serialize(Configuration()));

            UserKitConfiguration migrated = new UserKitConfigurationLoader().Load(legacy);
            string current = serializer.Serialize(migrated);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(UserKitConfigurationLoader.SupportedSchemaVersion));
            Assert.That(migrated.IsComplete, Is.True);
            Assert.That(migrated.Mappings.All(mapping =>
                mapping.VerificationState == MidiMappingVerificationState.Confirmed), Is.True);
            Assert.That(migrated.ReviewIssues, Is.Empty);
            Assert.That(current, Does.Contain("\"schemaVersion\": 2"));
            Assert.That(current, Does.Contain("\"verificationState\": \"confirmed\""));
            Assert.That(current, Does.Contain("\"reviewIssues\": []"));
            Assert.That(serializer.Serialize(new UserKitConfigurationLoader().Load(current)), Is.EqualTo(current));
        }

        [Test]
        public void Legacy_draft_and_mixed_version_documents_are_rejected()
        {
            string current = new UserKitConfigurationSerializer().Serialize(Configuration());
            string legacyDraft = ToLegacyV1(current).Replace("\"isComplete\": true", "\"isComplete\": false");
            string mixed = current.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 1");

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(legacyDraft));
            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(mixed));
        }

        [Test]
        public void Channel_aftertouch_round_trip_omits_data1_and_preserves_trigger_shape()
        {
            var element = new KitElement("crash1.choke", KitPiece.Crash1, KitArticulation.Choke, "Crash choke");
            var mapping = new MidiMappingEntry(
                "user.choke",
                new MidiTrigger(RawMidiMessageKind.ChannelAftertouch, 9, null, 20, 127),
                element.Id,
                source: MidiMappingSource.UserOverride);
            var configuration = new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "user.channel-aftertouch", "Channel aftertouch",
                null, null, null, new[] { element }, new[] { mapping }, Array.Empty<string>());

            var serializer = new UserKitConfigurationSerializer();
            string json = serializer.Serialize(configuration);
            UserKitConfiguration loaded = new UserKitConfigurationLoader().Load(json);

            Assert.That(json, Does.Not.Contain("\"data1\""));
            Assert.That(loaded.Mappings[0].Trigger.Kind, Is.EqualTo(RawMidiMessageKind.ChannelAftertouch));
            Assert.That(loaded.Mappings[0].Trigger.Data1, Is.Null);
            Assert.That(serializer.Serialize(loaded), Is.EqualTo(json));
        }

        [Test]
        public void Base_profile_id_and_version_must_be_present_together()
        {
            var element = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            Assert.Throws<ArgumentException>(() => new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "user.invalid", "Invalid",
                "generic-gm-drums-v1", null, null, new[] { element }, Array.Empty<MidiMappingEntry>(), Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "user.invalid", "Invalid",
                null, 1, null, new[] { element }, Array.Empty<MidiMappingEntry>(), Array.Empty<string>()));
        }

        [Test]
        public void Candidate_review_state_round_trips_and_cannot_be_marked_complete()
        {
            UserKitConfiguration draft = CandidateDraft();

            var serializer = new UserKitConfigurationSerializer();
            string json = serializer.Serialize(draft);
            UserKitConfiguration loaded = new UserKitConfigurationLoader().Load(json);
            Assert.That(loaded.Mappings.Single().VerificationState,
                Is.EqualTo(MidiMappingVerificationState.RequiresConfirmation));
            Assert.That(loaded.Mappings.Single().Source, Is.EqualTo(MidiMappingSource.BuiltInProfile));
            Assert.That(loaded.ReviewIssues.Select(issue => issue.Kind),
                Is.EquivalentTo(new[] { KitMappingReviewIssueKind.Conflict, KitMappingReviewIssueKind.Insufficient }));
            Assert.That(serializer.Serialize(loaded), Is.EqualTo(json));
            Assert.Throws<ArgumentException>(() => new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion, "user.invalid-candidate", "Invalid", null, null, null,
                draft.Elements, draft.Mappings, Array.Empty<string>(), null, true, draft.ReviewIssues));
        }

        [Test]
        public void Current_schema_rejects_missing_or_empty_candidate_verification_state()
        {
            string json = new UserKitConfigurationSerializer().Serialize(CandidateDraft());

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(RemoveProperty(json, "verificationState")));
            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(
                    json.Replace("\"verificationState\": \"requiresConfirmation\"", "\"verificationState\": \"\"")));
        }

        [TestCase("Confirmed")]
        [TestCase(" requiresConfirmation ")]
        [TestCase("unknown")]
        public void Current_schema_rejects_noncanonical_verification_state(string state)
        {
            string json = new UserKitConfigurationSerializer().Serialize(CandidateDraft())
                .Replace("\"verificationState\": \"requiresConfirmation\"", $"\"verificationState\": \"{state}\"");

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(json));
        }

        [Test]
        public void Current_schema_rejects_missing_or_null_review_issues()
        {
            string json = new UserKitConfigurationSerializer().Serialize(CandidateDraft());

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(RemoveReviewIssues(json)));
            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(ReplaceReviewIssuesWithNull(json)));
        }

        [Test]
        public void Current_schema_rejects_review_issue_without_required_flag()
        {
            string json = new UserKitConfigurationSerializer().Serialize(CandidateDraft());

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(RemoveProperty(json, "blocksCompletion")));
        }

        [Test]
        public void Current_schema_rejects_unknown_duplicate_or_missing_target_review_issue()
        {
            string json = new UserKitConfigurationSerializer().Serialize(CandidateDraft());
            string unknownKind = json.Replace("\"kind\": \"conflict\"", "\"kind\": \"unknown\"");
            string missingTarget = json.Replace("\"elementId\": \"ride.bow\",\n", string.Empty);
            string duplicate = json
                .Replace("\"elementId\": \"hihat.continuous\"", "\"elementId\": \"ride.bow\"")
                .Replace("\"kind\": \"insufficient\"", "\"kind\": \"conflict\"");

            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(unknownKind));
            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(missingTarget));
            Assert.Throws<UserKitConfigurationLoadException>(() => new UserKitConfigurationLoader().Load(duplicate));
        }

        [TestCase("priority")]
        [TestCase("source")]
        [TestCase("enabled")]
        public void Current_schema_requires_candidate_mapping_fields(string field)
        {
            string json = new UserKitConfigurationSerializer().Serialize(CandidateDraft());

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(RemoveProperty(json, field)));
        }

        [TestCase("\"priority\": 0", "\"priority\": null")]
        [TestCase("\"source\": \"builtInProfile\"", "\"source\": null")]
        [TestCase("\"enabled\": true", "\"enabled\": null")]
        public void Current_schema_rejects_null_candidate_mapping_fields(string current, string replacement)
        {
            string json = new UserKitConfigurationSerializer().Serialize(CandidateDraft())
                .Replace(current, replacement);

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(json));
        }

        [Test]
        public void Complete_configuration_rejects_unconfirmed_mapping_or_blocking_issue_on_load()
        {
            string candidateComplete = new UserKitConfigurationSerializer().Serialize(CandidateDraft())
                .Replace("\"isComplete\": false", "\"isComplete\": true");
            string blockingIssueComplete = new UserKitConfigurationSerializer().Serialize(BlockingIssueDraft())
                .Replace("\"isComplete\": false", "\"isComplete\": true");

            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(candidateComplete));
            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(blockingIssueComplete));
        }

        [Test]
        public void Complete_requires_an_enabled_confirmed_mapping_for_every_required_element()
        {
            var required = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            var optional = new KitElement("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true);
            var optionalMapping = Mapping("optional.snare-rim", optional.Id, 40);
            var disabledRequiredMapping = Mapping("disabled.kick", required.Id, 36, enabled: false);
            var candidateRequiredMapping = Mapping(
                "candidate.kick",
                required.Id,
                36,
                verificationState: MidiMappingVerificationState.RequiresConfirmation);
            var confirmedRequiredMapping = Mapping("confirmed.kick", required.Id, 36);

            Assert.Throws<ArgumentException>(() => ConfigurationWith(
                new[] { required }, Array.Empty<MidiMappingEntry>(), true));
            Assert.Throws<ArgumentException>(() => ConfigurationWith(
                new[] { required, optional }, new[] { optionalMapping }, true));
            Assert.Throws<ArgumentException>(() => ConfigurationWith(
                new[] { required }, new[] { disabledRequiredMapping }, true));
            Assert.Throws<ArgumentException>(() => ConfigurationWith(
                new[] { required }, new[] { candidateRequiredMapping }, true));
            Assert.DoesNotThrow(() => ConfigurationWith(
                new[] { required }, new[] { confirmedRequiredMapping }, true));
            Assert.DoesNotThrow(() => ConfigurationWith(
                new[] { optional }, Array.Empty<MidiMappingEntry>(), true));
            Assert.DoesNotThrow(() => ConfigurationWith(
                new[] { required }, Array.Empty<MidiMappingEntry>(), false));
        }

        [Test]
        public void Loader_rejects_current_and_legacy_complete_documents_missing_required_mappings()
        {
            var required = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            var optional = new KitElement("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true);
            var serializer = new UserKitConfigurationSerializer();
            string draft = serializer.Serialize(ConfigurationWith(
                new[] { required }, Array.Empty<MidiMappingEntry>(), false));
            string currentComplete = draft.Replace("\"isComplete\": false", "\"isComplete\": true");
            string legacyComplete = ToLegacyV1(currentComplete);
            string optionalOnly = serializer.Serialize(ConfigurationWith(
                new[] { required, optional }, new[] { Mapping("optional.snare-rim", optional.Id, 40) }, false))
                .Replace("\"isComplete\": false", "\"isComplete\": true");
            string disabledOnly = serializer.Serialize(ConfigurationWith(
                new[] { required }, new[] { Mapping("disabled.kick", required.Id, 36, enabled: false) }, false))
                .Replace("\"isComplete\": false", "\"isComplete\": true");
            string legacyDisabledOnly = ToLegacyV1(disabledOnly);

            UserKitConfigurationLoadException currentError = Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(currentComplete));
            UserKitConfigurationLoadException legacyError = Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(legacyComplete));
            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(optionalOnly));
            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(disabledOnly));
            Assert.Throws<UserKitConfigurationLoadException>(() =>
                new UserKitConfigurationLoader().Load(legacyDisabledOnly));

            Assert.That(currentError.Message, Does.Contain("required element").And.Contain("kick.default"));
            Assert.That(legacyError.Message, Does.Contain("required element").And.Contain("kick.default"));
        }

        [Test]
        public void Complete_optional_element_may_be_unmapped_and_keep_a_nonblocking_issue()
        {
            var optional = new KitElement("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true);
            var issue = new KitMappingReviewIssue(
                optional.Id,
                KitMappingReviewIssueKind.Insufficient,
                "Optional rim was not captured.",
                false);
            UserKitConfiguration complete = ConfigurationWith(
                new[] { optional },
                Array.Empty<MidiMappingEntry>(),
                true,
                new[] { issue });
            var serializer = new UserKitConfigurationSerializer();
            string json = serializer.Serialize(complete);

            Assert.That(serializer.Serialize(new UserKitConfigurationLoader().Load(json)), Is.EqualTo(json));
            string legacy = ToLegacyV1(json);
            Assert.That(new UserKitConfigurationLoader().Load(legacy).IsComplete, Is.True);
        }

        [Test]
        public void Review_issue_target_must_exist_in_the_configuration_model_and_json()
        {
            var required = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            var validIssue = new KitMappingReviewIssue(
                required.Id,
                KitMappingReviewIssueKind.Conflict,
                "Kick evidence conflicts.",
                true);

            Assert.Throws<ArgumentException>(() => ConfigurationWith(
                new[] { required },
                Array.Empty<MidiMappingEntry>(),
                false,
                new[]
                {
                    new KitMappingReviewIssue(
                        "kick.missing",
                        KitMappingReviewIssueKind.Conflict,
                        "Missing target.",
                        true)
                }));

            string valid = new UserKitConfigurationSerializer().Serialize(ConfigurationWith(
                new[] { required }, Array.Empty<MidiMappingEntry>(), false, new[] { validIssue }));
            foreach (string invalidTarget in new[] { "kick.missing", "Kick.default", " " })
            {
                string invalid = ReplaceLast(valid, "\"elementId\": \"kick.default\"", $"\"elementId\": \"{invalidTarget}\"");
                UserKitConfigurationLoadException error = Assert.Throws<UserKitConfigurationLoadException>(() =>
                    new UserKitConfigurationLoader().Load(invalid));
                Assert.That(error.Message, Does.Contain("Review issue at index 0"));
            }

            Assert.DoesNotThrow(() => new UserKitConfigurationLoader().Load(valid));
        }

        [Test]
        public void Required_elements_cannot_be_disabled_but_optional_elements_can()
        {
            var required = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            var optional = new KitElement("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true);
            MidiMappingEntry requiredMapping = Mapping("confirmed.kick", required.Id, 36);
            MidiMappingEntry optionalMapping = Mapping("confirmed.snare-rim", optional.Id, 40);

            ArgumentException completeError = Assert.Throws<ArgumentException>(() => new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.required-disabled-complete",
                "Required Disabled Complete",
                null,
                null,
                null,
                new[] { required },
                new[] { requiredMapping },
                new[] { required.Id }));
            ArgumentException draftError = Assert.Throws<ArgumentException>(() => new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.required-disabled-draft",
                "Required Disabled Draft",
                null,
                null,
                null,
                new[] { required },
                new[] { requiredMapping },
                new[] { required.Id },
                isComplete: false));

            Assert.That(completeError.Message, Does.Contain("Required").And.Contain(required.Id).And.Contain("Complete"));
            Assert.That(draftError.Message, Does.Contain("Required").And.Contain(required.Id).And.Contain("Draft"));

            var sourceDisabledIds = new[] { optional.Id };
            var optionalConfiguration = new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.optional-disabled",
                "Optional Disabled",
                null,
                null,
                null,
                new[] { optional },
                new[] { optionalMapping },
                sourceDisabledIds);
            sourceDisabledIds[0] = "changed.after.construction";

            Assert.That(optionalConfiguration.DisabledElementIds, Is.EqualTo(new[] { optional.Id }));
            Assert.That(new MidiKitMappingEngine().Map(
                    RawMidiMessage.NoteOn(9, 40, 100),
                    optionalConfiguration).Status,
                Is.EqualTo(MidiKitMappingStatus.Disabled));
        }

        [Test]
        public void Current_and_legacy_json_apply_disabled_element_invariants()
        {
            var required = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            var optional = new KitElement("snare.rim", KitPiece.Snare, KitArticulation.Rim, "Snare rim", true);
            var serializer = new UserKitConfigurationSerializer();
            var loader = new UserKitConfigurationLoader();
            string requiredJson = serializer.Serialize(ConfigurationWith(
                new[] { required },
                new[] { Mapping("confirmed.kick", required.Id, 36) },
                true));
            string requiredDisabled = WithDisabledIds(requiredJson, required.Id);
            string requiredDisabledDraft = requiredDisabled.Replace("\"isComplete\": true", "\"isComplete\": false");

            Assert.Throws<UserKitConfigurationLoadException>(() => loader.Load(requiredDisabled));
            Assert.Throws<UserKitConfigurationLoadException>(() => loader.Load(requiredDisabledDraft));
            Assert.Throws<UserKitConfigurationLoadException>(() => loader.Load(ToLegacyV1(requiredDisabled)));

            var optionalConfiguration = new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.optional-disabled-json",
                "Optional Disabled JSON",
                null,
                null,
                null,
                new[] { optional },
                new[] { Mapping("confirmed.snare-rim", optional.Id, 40) },
                new[] { optional.Id });
            string optionalJson = serializer.Serialize(optionalConfiguration);
            Assert.That(serializer.Serialize(loader.Load(optionalJson)), Is.EqualTo(optionalJson));
            Assert.That(loader.Load(ToLegacyV1(optionalJson)).DisabledElementIds, Is.EqualTo(new[] { optional.Id }));

            foreach (string[] invalidIds in new[]
                     {
                         new[] { "missing.element" },
                         new[] { "Snare.rim" },
                         new[] { " " },
                         new[] { optional.Id, optional.Id }
                     })
            {
                Assert.Throws<UserKitConfigurationLoadException>(() =>
                    loader.Load(WithDisabledIds(serializer.Serialize(ConfigurationWith(
                        new[] { optional }, Array.Empty<MidiMappingEntry>(), true)), invalidIds)));
            }
        }

        private static UserKitConfiguration Configuration()
        {
            var element = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            var mapping = new MidiMappingEntry("user.kick", new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 1, 127),
                element.Id, 10, MidiMappingSource.UserOverride, true, "User choice");
            return new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "user.my-kit", "My Kit", "generic-gm-drums-v1", 1,
                new MidiDeviceIdentity("Test Port", "Synthetic", "Test"),
                new[] { element }, new[] { mapping }, Array.Empty<string>(), "Notes", true);
        }

        private static UserKitConfiguration CandidateDraft()
        {
            var kick = new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick");
            var ride = new KitElement("ride.bow", KitPiece.Ride, KitArticulation.Bow, "Ride bow");
            var hiHat = new KitElement(
                "hihat.continuous",
                KitPiece.HiHat,
                KitArticulation.Pedal,
                "Continuous hi-hat pedal");
            var candidate = new MidiMappingEntry(
                "candidate.kick",
                new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 1, 127),
                kick.Id,
                source: MidiMappingSource.BuiltInProfile,
                verificationState: MidiMappingVerificationState.RequiresConfirmation);
            var issues = new[]
            {
                new KitMappingReviewIssue(
                    ride.Id,
                    KitMappingReviewIssueKind.Conflict,
                    "Ride evidence is conflicted.",
                    true),
                new KitMappingReviewIssue(
                    hiHat.Id,
                    KitMappingReviewIssueKind.Insufficient,
                    "No continuous controller was observed.",
                    true)
            };
            return new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.candidate-draft",
                "Candidate Draft",
                null,
                null,
                null,
                new[] { kick, ride, hiHat },
                new[] { candidate },
                Array.Empty<string>(),
                null,
                false,
                issues);
        }

        private static UserKitConfiguration BlockingIssueDraft()
        {
            UserKitConfiguration confirmed = Configuration();
            var issue = new KitMappingReviewIssue(
                confirmed.Elements[0].Id,
                KitMappingReviewIssueKind.Conflict,
                "Confirmed mapping still has a required conflict.",
                true);
            return new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.blocking-draft",
                "Blocking Draft",
                confirmed.BaseProfileId,
                confirmed.BaseProfileVersion,
                confirmed.MidiDeviceIdentity,
                confirmed.Elements,
                confirmed.Mappings,
                confirmed.DisabledElementIds,
                confirmed.UserNotes,
                false,
                new[] { issue });
        }

        private static UserKitConfiguration ConfigurationWith(
            KitElement[] elements,
            MidiMappingEntry[] mappings,
            bool complete,
            KitMappingReviewIssue[] issues = null)
        {
            return new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                "user.invariant-test",
                "Invariant Test",
                null,
                null,
                null,
                elements,
                mappings,
                Array.Empty<string>(),
                null,
                complete,
                issues ?? Array.Empty<KitMappingReviewIssue>());
        }

        private static MidiMappingEntry Mapping(
            string id,
            string elementId,
            int note,
            bool enabled = true,
            MidiMappingVerificationState verificationState = MidiMappingVerificationState.Confirmed)
        {
            return new MidiMappingEntry(
                id,
                new MidiTrigger(RawMidiMessageKind.NoteOn, 9, note, 1, 127),
                elementId,
                enabled: enabled,
                verificationState: verificationState);
        }

        private static string ToLegacyV1(string current)
        {
            return RemoveReviewIssues(RemoveProperty(current, "verificationState"))
                .Replace("\"schemaVersion\": 2", "\"schemaVersion\": 1");
        }

        private static string RemoveProperty(string json, string property)
        {
            string withTrailingComma = $"^[ \\t]*\"{Regex.Escape(property)}\"[ \\t]*:.*?,\\r?\\n";
            string result = Regex.Replace(
                json,
                withTrailingComma,
                string.Empty,
                RegexOptions.Multiline | RegexOptions.CultureInvariant);
            if (!string.Equals(result, json, StringComparison.Ordinal)) return result;

            string finalProperty = $",\\r?\\n[ \\t]*\"{Regex.Escape(property)}\"[ \\t]*:.*?(?=\\r?\\n[ \\t]*[}}])";
            return Regex.Replace(
                json,
                finalProperty,
                string.Empty,
                RegexOptions.Multiline | RegexOptions.CultureInvariant);
        }

        private static string RemoveReviewIssues(string json)
        {
            return Regex.Replace(
                json,
                ",\\r?\\n[ \\t]*\"reviewIssues\"[ \\t]*:[ \\t]*\\[[\\s\\S]*?\\]\\r?\\n}",
                "\n}",
                RegexOptions.CultureInvariant);
        }

        private static string ReplaceReviewIssuesWithNull(string json)
        {
            return Regex.Replace(
                json,
                "\"reviewIssues\"[ \\t]*:[ \\t]*\\[[\\s\\S]*?\\]\\r?\\n}",
                "\"reviewIssues\": null\n}",
                RegexOptions.CultureInvariant);
        }

        private static string ReplaceLast(string value, string oldValue, string newValue)
        {
            int index = value.LastIndexOf(oldValue, StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), oldValue);
            return value.Substring(0, index) + newValue + value.Substring(index + oldValue.Length);
        }

        private static string WithDisabledIds(string json, params string[] ids)
        {
            string values = string.Join(", ", ids.Select(id => $"\"{id}\""));
            string result = json.Replace(
                "\"disabledElementIds\": []",
                $"\"disabledElementIds\": [{values}]");
            Assert.That(result, Is.Not.EqualTo(json));
            return result;
        }
    }
}
