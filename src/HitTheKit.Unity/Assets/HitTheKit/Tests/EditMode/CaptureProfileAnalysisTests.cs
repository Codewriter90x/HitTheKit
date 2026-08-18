using System;
using System.Linq;
using HitTheKit.Unity.Devices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class CaptureProfileAnalysisTests
    {
        private const string SyntheticFixturePath = "Assets/HitTheKit/Tests/EditMode/Fixtures/hampback-candidate-001-synthetic.json";
        private const string CandidateProfilePath = "Assets/HitTheKit/Fixtures/DeviceProfiles/Candidates/hampback-exploratory-001.json";

        [Test]
        public void Schema_v1_loader_loads_explicitly_synthetic_fixture()
        {
            GuidedCaptureAnalysisInput input = LoadSynthetic();

            Assert.That(input.CaptureId, Is.EqualTo("hampback-candidate-001-synthetic"));
            Assert.That(input.CaptureSha256, Is.EqualTo(new string('a', 64)));
            Assert.That(input.Events, Has.Count.EqualTo(34));
            Assert.That(input.Events.Select(value => value.Sequence), Is.EqualTo(Enumerable.Range(1, 34).Select(value => (long)value)));
            Assert.That(input.Events.Zip(input.Events.Skip(1), (left, right) => right.ElapsedSeconds >= left.ElapsedSeconds).All(value => value), Is.True);
        }

        [Test]
        public void Schema_loader_rejects_non_synthetic_fixture_and_truncated_jsonl()
        {
            string fixture = FixtureText(SyntheticFixturePath);
            Assert.Throws<GuidedCaptureSchemaLoadException>(() => new GuidedCaptureSchemaV1Loader().LoadSyntheticFixture(fixture.Replace("\"isSynthetic\": true", "\"isSynthetic\": false")));

            const string session = "{\"schemaVersion\":1,\"deviceDisplayName\":\"Synthetic\",\"captureMode\":\"guided-capture\",\"stepDefinitions\":[],\"steps\":[],\"eventCount\":0}";
            Assert.Throws<GuidedCaptureSchemaLoadException>(() => new GuidedCaptureSchemaV1Loader().Load(session, "{}", "capture-id", new string('b', 64)));
        }

        [Test]
        public void Schema_loader_reads_session_and_jsonl_through_separate_adapter()
        {
            const string session = "{\"schemaVersion\":1,\"deviceDisplayName\":\"Synthetic port\",\"captureMode\":\"guided-capture\",\"stepDefinitions\":[{\"id\":\"kick\",\"displayName\":\"Kick\",\"optional\":false,\"targetSamples\":1}],\"steps\":[{\"id\":\"kick\",\"completed\":true,\"skipped\":false,\"eventCount\":1}],\"eventCount\":1}";
            const string jsonl = "{\"schemaVersion\":1,\"sequence\":1,\"elapsedSeconds\":0.5,\"stepId\":\"kick\",\"rawKind\":\"noteOn\",\"channel\":9,\"data1\":36,\"data2\":100,\"stepAttempt\":1}\n";

            GuidedCaptureAnalysisInput input = new GuidedCaptureSchemaV1Loader().Load(session, jsonl, "capture-v1", new string('d', 64), "Observed only");

            Assert.That(input.Events, Has.Count.EqualTo(1));
            Assert.That(input.Events[0].Data1, Is.EqualTo(36));
            Assert.That(input.ObservedManufacturer, Is.EqualTo("Observed only"));
        }

        [Test]
        public void Clean_attempt_can_support_high_confidence_without_discarding_contamination()
        {
            CaptureAnalysisResult result = Analyze();
            CaptureStepObservation kick = Observation(result, "kick");
            CandidateMappingEvidence candidate = Mapping(result, "kick", "strike", 36);

            Assert.That(kick.Confidence, Is.EqualTo(EvidenceConfidence.High));
            Assert.That(kick.PossibleContamination, Is.True);
            Assert.That(kick.Notes.Select(value => value.Note), Is.EquivalentTo(new[] { 36, 40 }));
            Assert.That(candidate.Confidence, Is.EqualTo(EvidenceConfidence.High));
            Assert.That(candidate.RequiresRecapture, Is.True);
        }

        [Test]
        public void Conflicted_ride_retains_both_note_candidates()
        {
            CaptureAnalysisResult result = Analyze();
            CandidateMappingEvidence[] ride = result.CandidateMappings.Where(value => value.StepId == "ride-bow").ToArray();

            Assert.That(ride, Has.Length.EqualTo(2));
            Assert.That(ride.Select(value => value.Trigger.Data1), Is.EquivalentTo(new int?[] { 51, 59 }));
            Assert.That(ride.All(value => value.Confidence == EvidenceConfidence.Conflicted && value.RequiresRecapture), Is.True);
        }

        [Test]
        public void Missing_continuous_controller_is_insufficient_and_does_not_infer_cc4()
        {
            CaptureAnalysisResult result = Analyze();
            CaptureStepObservation observation = Observation(result, "hihat-continuous");
            CandidateMappingEvidence mapping = result.CandidateMappings.Single(value => value.StepId == "hihat-continuous");

            Assert.That(observation.ControlChanges, Is.Empty);
            Assert.That(observation.Confidence, Is.EqualTo(EvidenceConfidence.Insufficient));
            Assert.That(mapping.Trigger, Is.Null);
            Assert.That(string.Join(" ", mapping.Evidence), Does.Contain("do not infer CC4").IgnoreCase);
        }

        [Test]
        public void Strike_and_poly_aftertouch_evidence_remain_separate()
        {
            CaptureAnalysisResult result = Analyze();
            CaptureStepObservation crash = Observation(result, "crash-choke");
            CandidateMappingEvidence choke = Mapping(result, "crash-choke", "choke", 26);

            Assert.That(crash.Notes.Select(value => value.Note), Is.EquivalentTo(new[] { 26, 46 }));
            Assert.That(crash.Aftertouch.Single().Kind, Is.EqualTo("polyAftertouch"));
            Assert.That(choke.Trigger.Kind, Is.EqualTo("polyAftertouch"));
            Assert.That(choke.Confidence, Is.EqualTo(EvidenceConfidence.Medium));
        }

        [Test]
        public void Skipped_step_is_insufficient_and_has_no_trigger()
        {
            CaptureAnalysisResult result = Analyze();
            CaptureStepObservation observation = Observation(result, "crash-2-optional");
            CandidateMappingEvidence mapping = result.CandidateMappings.Single(value => value.StepId == "crash-2-optional");

            Assert.That(observation.Skipped, Is.True);
            Assert.That(observation.Confidence, Is.EqualTo(EvidenceConfidence.Insufficient));
            Assert.That(mapping.Trigger, Is.Null);
        }

        [Test]
        public void Candidate_profile_is_separate_non_production_schema_and_preserves_capture_hash()
        {
            string json = FixtureText(CandidateProfilePath);
            DeviceProfileCandidate candidate = new DeviceProfileCandidateLoader().Load(json);

            Assert.That(candidate.Status, Is.EqualTo(DeviceProfileLifecycleStatus.Candidate));
            Assert.That(candidate.CaptureSha256, Is.EqualTo("0b0cc8d4f007177ab5d9ae865558a92b3027380ac0bed4eab1acf316b4197162"));
            Assert.That(candidate.ProductionReady, Is.False);
            Assert.That(candidate.AutoSelectable, Is.False);
            Assert.That(candidate.RequiresConfirmation, Is.True);
            Assert.That(candidate.CanEnterBuiltInLibrary, Is.False);
            Assert.Throws<ElectronicDrumProfileLoadException>(() => new ElectronicDrumProfileLoader().Load(json));
        }

        [Test]
        public void Candidate_loader_rejects_candidate_marked_auto_selectable()
        {
            string json = FixtureText(CandidateProfilePath).Replace("\"autoSelectable\": false", "\"autoSelectable\": true");
            Assert.Throws<DeviceProfileCandidateLoadException>(() => new DeviceProfileCandidateLoader().Load(json));
        }

        [Test]
        public void Analyzer_output_is_deterministic_for_same_input()
        {
            GuidedCaptureAnalysisInput input = LoadSynthetic();
            var analyzer = new GuidedCaptureProfileAnalyzer();
            CaptureAnalysisResult first = analyzer.Analyze(input);
            CaptureAnalysisResult second = analyzer.Analyze(input);

            Assert.That(Signature(second), Is.EqualTo(Signature(first)));
            Assert.That(second.RecommendedRecaptureSteps, Is.EqualTo(first.RecommendedRecaptureSteps));
        }

        [Test]
        public void Analysis_input_defensively_copies_collections()
        {
            var definitions = LoadSynthetic().StepDefinitions.ToArray();
            var states = LoadSynthetic().Steps.ToArray();
            var events = LoadSynthetic().Events.ToArray();
            var input = new GuidedCaptureAnalysisInput("copy-test", new string('c', 64), "device", null, definitions, states, events);

            definitions[0] = null;
            states[0] = null;
            events[0] = null;

            Assert.That(input.StepDefinitions[0], Is.Not.Null);
            Assert.That(input.Steps[0], Is.Not.Null);
            Assert.That(input.Events[0], Is.Not.Null);
        }

        [Test]
        public void Tracked_candidate_artifacts_do_not_contain_local_paths_or_claim_verification()
        {
            string candidate = FixtureText(CandidateProfilePath);
            string synthetic = FixtureText(SyntheticFixturePath);

            Assert.That(candidate + synthetic, Does.Not.Contain(string.Concat("/", "Users", "/")));
            Assert.That(candidate, Does.Contain("\"status\": \"candidate\""));
            Assert.That(candidate, Does.Not.Contain("\"status\": \"verified\""));
        }

        private static GuidedCaptureAnalysisInput LoadSynthetic() =>
            new GuidedCaptureSchemaV1Loader().LoadSyntheticFixture(FixtureText(SyntheticFixturePath));

        private static CaptureAnalysisResult Analyze() => new GuidedCaptureProfileAnalyzer().Analyze(LoadSynthetic());

        private static CaptureStepObservation Observation(CaptureAnalysisResult result, string stepId) =>
            result.Observations.Single(value => value.StepId == stepId);

        private static CandidateMappingEvidence Mapping(CaptureAnalysisResult result, string stepId, string role, int data1) =>
            result.CandidateMappings.Single(value => value.StepId == stepId && value.Role == role && value.Trigger?.Data1 == data1);

        private static string Signature(CaptureAnalysisResult result) => string.Join("|", result.CandidateMappings.Select(value =>
            $"{value.StepId}:{value.Role}:{value.Trigger?.Kind}:{value.Trigger?.Data1}:{value.Confidence}:{value.RequiresRecapture}"));

        private static string FixtureText(string path)
        {
            TextAsset fixture = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(fixture, Is.Not.Null, $"Missing fixture: {path}");
            return fixture.text;
        }
    }
}
