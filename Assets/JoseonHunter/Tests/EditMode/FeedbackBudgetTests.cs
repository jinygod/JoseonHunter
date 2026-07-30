using JoseonHunter.Presentation.Combat;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class FeedbackBudgetTests
    {
        [Test]
        public void Normal_contact_never_requests_camera_impulse()
        {
            var profile = CombatFeedbackBudget.Resolve(new FeedbackRequest(
                critical: false, killed: false, boss: false, reducedEffects: false));

            Assert.That(profile.Intensity, Is.EqualTo(70));
            Assert.That(profile.HitStopSeconds, Is.EqualTo(0f));
            Assert.That(profile.CameraImpulse, Is.EqualTo(0f));
        }

        [Test]
        public void Reduced_effects_preserves_contact_flash_but_removes_camera_impulse()
        {
            var profile = CombatFeedbackBudget.Resolve(new FeedbackRequest(
                critical: true, killed: true, boss: false, reducedEffects: true));

            Assert.That(profile.ShowContactFlash, Is.True);
            Assert.That(profile.CameraImpulse, Is.EqualTo(0f));
            Assert.That(profile.HitStopSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void Fatal_boss_contact_uses_maximum_feedback_intensity()
        {
            var profile = CombatFeedbackBudget.Resolve(new FeedbackRequest(
                critical: false, killed: true, boss: true, reducedEffects: false));

            Assert.That(profile.Intensity, Is.EqualTo(100));
        }

        [Test]
        public void CriticalFeedbackIsShortAndDoesNotReachBossKillIntensity()
        {
            var profile = CombatFeedbackBudget.Resolve(new FeedbackRequest(
                critical: true, killed: false, boss: false, reducedEffects: false));

            Assert.That(profile.Intensity, Is.EqualTo(80));
            Assert.That(profile.HitStopSeconds, Is.InRange(.02f, .035f));
            Assert.That(profile.CameraImpulse, Is.InRange(.05f, .08f));
        }
    }
}
