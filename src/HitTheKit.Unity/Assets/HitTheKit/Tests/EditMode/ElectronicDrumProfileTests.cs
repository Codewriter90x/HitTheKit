using System;
using HitTheKit.Unity.Devices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class ElectronicDrumProfileTests
    {
        private const string FixturePath = "Assets/HitTheKit/Fixtures/DeviceProfiles/generic-gm-drums-v1.json";

        [Test]
        public void Loads_real_generic_profile_fixture()
        {
            TextAsset fixture = AssetDatabase.LoadAssetAtPath<TextAsset>(FixturePath);
            Assert.That(fixture, Is.Not.Null);
            ElectronicDrumProfile profile = new ElectronicDrumProfileLoader().Load(fixture.text);
            Assert.That(profile.ProfileId, Is.EqualTo("generic-gm-drums-v1"));
            Assert.That(profile.Elements, Has.Count.EqualTo(12));
            Assert.That(profile.DefaultMappings, Has.Count.EqualTo(15));
            Assert.That(profile.Notes, Does.Contain("user confirmation required"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Loader_rejects_null_or_empty_json(string json)
        {
            Assert.Throws<ElectronicDrumProfileLoadException>(() => new ElectronicDrumProfileLoader().Load(json));
        }

        [Test]
        public void Loader_rejects_malformed_json()
        {
            Assert.Throws<ElectronicDrumProfileLoadException>(() => new ElectronicDrumProfileLoader().Load("{"));
        }

        [Test]
        public void Loader_rejects_missing_and_unsupported_schema()
        {
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadJson("\"profileId\":\"test.profile\""));
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadJson("\"schemaVersion\":2"));
        }

        [Test]
        public void Loader_rejects_missing_profile_id()
        {
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadJson("\"schemaVersion\":1"));
        }

        [Test]
        public void Loader_rejects_missing_profile_version_and_required_trigger_numbers()
        {
            string missingProfileVersion = "{\"schemaVersion\":1,\"profileId\":\"synthetic.profile\",\"displayName\":\"Synthetic\",\"elements\":[],\"mappings\":[]}";
            Assert.Throws<ElectronicDrumProfileLoadException>(() => new ElectronicDrumProfileLoader().Load(missingProfileVersion));

            string missingChannel = "[{\"id\":\"map.1\",\"elementId\":\"kick.default\",\"kind\":\"noteOn\",\"data1\":36,\"minimumValue\":1,\"maximumValue\":127,\"source\":\"builtInProfile\",\"enabled\":true}]";
            string missingData1 = "[{\"id\":\"map.1\",\"elementId\":\"kick.default\",\"kind\":\"noteOn\",\"channel\":-1,\"minimumValue\":1,\"maximumValue\":127,\"source\":\"builtInProfile\",\"enabled\":true}]";
            string missingRange = "[{\"id\":\"map.1\",\"elementId\":\"kick.default\",\"kind\":\"noteOn\",\"channel\":-1,\"data1\":36,\"source\":\"builtInProfile\",\"enabled\":true}]";
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(DefaultElements, missingChannel));
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(DefaultElements, missingData1));
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(DefaultElements, missingRange));
        }

        [Test]
        public void Loader_rejects_duplicate_element_ids()
        {
            string elements = "[{\"id\":\"kick.default\",\"piece\":\"kick\",\"articulation\":\"default\",\"displayName\":\"Kick\"},{\"id\":\"kick.default\",\"piece\":\"kick\",\"articulation\":\"default\",\"displayName\":\"Kick 2\"}]";
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(elements, "[]"));
        }

        [Test]
        public void Loader_rejects_mapping_to_missing_element()
        {
            string mapping = "[{\"id\":\"map.1\",\"elementId\":\"snare.head\",\"kind\":\"noteOn\",\"channel\":-1,\"data1\":38,\"minimumValue\":1,\"maximumValue\":127,\"source\":\"builtInProfile\",\"enabled\":true}]";
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(DefaultElements, mapping));
        }

        [Test]
        public void Loader_rejects_duplicate_mapping_triggers()
        {
            string mappings = "[{\"id\":\"map.1\",\"elementId\":\"kick.default\",\"kind\":\"noteOn\",\"channel\":-1,\"data1\":36,\"minimumValue\":1,\"maximumValue\":127,\"source\":\"builtInProfile\",\"enabled\":true},{\"id\":\"map.2\",\"elementId\":\"kick.default\",\"kind\":\"noteOn\",\"channel\":-1,\"data1\":36,\"minimumValue\":1,\"maximumValue\":127,\"source\":\"builtInProfile\",\"enabled\":true}]";
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(DefaultElements, mappings));
        }

        [TestCase("Kick", "default")]
        [TestCase("kick", "Default")]
        public void Loader_uses_explicit_case_sensitive_enum_mapping(string piece, string articulation)
        {
            string elements = $"[{{\"id\":\"kick.default\",\"piece\":\"{piece}\",\"articulation\":\"{articulation}\",\"displayName\":\"Kick\"}}]";
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(elements, "[]"));
        }

        [Test]
        public void Loader_rejects_invalid_trigger()
        {
            string mapping = "[{\"id\":\"map.1\",\"elementId\":\"kick.default\",\"kind\":\"noteOn\",\"channel\":16,\"data1\":36,\"minimumValue\":1,\"maximumValue\":127,\"source\":\"builtInProfile\",\"enabled\":true}]";
            Assert.Throws<ElectronicDrumProfileLoadException>(() => LoadValidShell(DefaultElements, mapping));
        }

        [Test]
        public void Loader_ignores_unknown_properties_by_documented_schema_contract()
        {
            string json = ValidShell(DefaultElements, "[]").Replace("\"profileVersion\":1", "\"profileVersion\":1,\"futureField\":42");
            Assert.That(new ElectronicDrumProfileLoader().Load(json).ProfileId, Is.EqualTo("synthetic.profile"));
        }

        [Test]
        public void Library_rejects_duplicate_profile_ids()
        {
            ElectronicDrumProfile profile = SyntheticProfile("known.profile", "Known", "K1");
            Assert.Throws<ArgumentException>(() => new ElectronicDrumProfileLibrary(new[] { profile, profile }));
        }

        [Test]
        public void Matcher_supports_exact_manufacturer_model_and_vendor_product()
        {
            ElectronicDrumProfile profile = SyntheticProfile("known.profile", "Known", "K1", "V1", "P1");
            var matcher = new ElectronicDrumProfileMatcher();
            Assert.That(matcher.Match(new MidiDeviceIdentity("port", "Known", "K1"), new[] { profile }).Kind,
                Is.EqualTo(ElectronicDrumProfileMatchKind.Exact));
            Assert.That(matcher.Match(new MidiDeviceIdentity("port", vendorId: "V1", productId: "P1"), new[] { profile }).Candidates[0].Confidence,
                Is.EqualTo(110));
        }

        [Test]
        public void Matcher_supports_alias_and_port_pattern_case_insensitively()
        {
            ElectronicDrumProfile profile = SyntheticProfile("known.profile", "Known", "K1", aliases: new[] { "Known Alias" }, patterns: new[] { "kit-port" });
            var matcher = new ElectronicDrumProfileMatcher();
            Assert.That(matcher.Match(new MidiDeviceIdentity("known alias"), new[] { profile }).Kind,
                Is.EqualTo(ElectronicDrumProfileMatchKind.Probable));
            Assert.That(matcher.Match(new MidiDeviceIdentity("USB KIT-PORT 1"), new[] { profile }).Kind,
                Is.EqualTo(ElectronicDrumProfileMatchKind.Probable));
        }

        [Test]
        public void Matcher_does_not_auto_select_ambiguous_candidates()
        {
            var profiles = new[]
            {
                SyntheticProfile("known.a", "A", "A", aliases: new[] { "shared" }),
                SyntheticProfile("known.b", "B", "B", aliases: new[] { "shared" })
            };
            ElectronicDrumProfileMatchResult result = new ElectronicDrumProfileMatcher().Match(new MidiDeviceIdentity("shared"), profiles);
            Assert.That(result.Kind, Is.EqualTo(ElectronicDrumProfileMatchKind.Ambiguous));
            Assert.That(result.SelectedCandidate, Is.Null);
        }

        [Test]
        public void Matcher_returns_generic_fallback_or_no_match()
        {
            ElectronicDrumProfile generic = new ElectronicDrumProfileLoader().Load(AssetDatabase.LoadAssetAtPath<TextAsset>(FixturePath).text);
            var matcher = new ElectronicDrumProfileMatcher();
            Assert.That(matcher.Match(new MidiDeviceIdentity("unknown"), new[] { generic }).Kind,
                Is.EqualTo(ElectronicDrumProfileMatchKind.GenericFallback));
            Assert.That(matcher.Match(new MidiDeviceIdentity("unknown"), Array.Empty<ElectronicDrumProfile>()).Kind,
                Is.EqualTo(ElectronicDrumProfileMatchKind.NoMatch));
        }

        private static ElectronicDrumProfile LoadJson(string fragment)
        {
            return new ElectronicDrumProfileLoader().Load("{" + fragment + "}");
        }

        private static ElectronicDrumProfile LoadValidShell(string elements, string mappings)
        {
            return new ElectronicDrumProfileLoader().Load(ValidShell(elements, mappings));
        }

        private static string ValidShell(string elements, string mappings)
        {
            return $"{{\"schemaVersion\":1,\"profileId\":\"synthetic.profile\",\"profileVersion\":1,\"displayName\":\"Synthetic\",\"elements\":{elements},\"mappings\":{mappings}}}";
        }

        private static ElectronicDrumProfile SyntheticProfile(
            string id,
            string manufacturer,
            string model,
            string vendorId = null,
            string productId = null,
            string[] aliases = null,
            string[] patterns = null)
        {
            return new ElectronicDrumProfile(1, id, 1, manufacturer, model, id,
                aliases ?? Array.Empty<string>(), patterns ?? Array.Empty<string>(),
                Array.Empty<KitElement>(), Array.Empty<MidiMappingEntry>(), ElectronicDrumCapability.None,
                vendorId: vendorId, productId: productId);
        }

        private const string DefaultElements =
            "[{\"id\":\"kick.default\",\"piece\":\"kick\",\"articulation\":\"default\",\"displayName\":\"Kick\"}]";
    }
}
