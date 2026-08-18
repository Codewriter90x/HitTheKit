using HitTheKit.Core;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class CoreIntegrationTests
    {
        [Test]
        public void Default_settings_convert_milliseconds_to_core_seconds()
        {
            TimingWindows windows = new TimingWindowSettings().ToCore();

            Assert.That(windows.PerfectSeconds, Is.EqualTo(0.040).Within(0.000001));
            Assert.That(windows.GoodSeconds, Is.EqualTo(0.090).Within(0.000001));
            Assert.That(windows.HitSeconds, Is.EqualTo(0.150).Within(0.000001));
        }

        [Test]
        public void Unity_test_assembly_uses_core_matcher_for_exact_hit()
        {
            var windows = new TimingWindows(0.040, 0.090, 0.150);
            var matcher = new HitMatcher(windows);
            var note = new ChartNote(1.0, DrumPad.Snare);
            var hit = new DrumHit(DrumPad.Snare, 1.0);

            bool matched = matcher.TryMatch(new[] { note }, hit, out HitResult result);

            Assert.That(matched, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Grade, Is.EqualTo(HitGrade.Perfect));
        }
    }
}
