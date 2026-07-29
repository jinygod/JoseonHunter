using JoseonHunter.Domain.Combat;
using JoseonHunter.Editor.Scenes;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class EightWeaponPolishCapturePolicyTests
    {
        [TearDown]
        public void TearDown()
        {
            CaptureSessionState.Clear();
        }

        [Test]
        public void RequiredPredicateTimeoutFailsInsteadOfCapturing()
        {
            Assert.That(
                CapturePhasePolicy.Evaluate(
                    predicateSatisfied: false,
                    elapsedSeconds: 6d,
                    earliestCaptureSeconds: .025d,
                    timeoutSeconds: 6d),
                Is.EqualTo(CapturePhaseAction.Fail));
        }

        [Test]
        public void SatisfiedPredicateAfterEarliestTimeCaptures()
        {
            Assert.That(
                CapturePhasePolicy.Evaluate(
                    predicateSatisfied: true,
                    elapsedSeconds: .04d,
                    earliestCaptureSeconds: .025d,
                    timeoutSeconds: 6d),
                Is.EqualTo(CapturePhaseAction.Capture));
        }

        [TestCase("frost_flask", true, "SpecialEvolved")]
        [TestCase("jangseung_ward", true, "SpecialEvolved")]
        [TestCase("singijeon_volley", true, "SpecialEvolved")]
        [TestCase("gakgung_shot", true, "SunPiercer")]
        [TestCase("gakgung_shot", false, "NearPlayerPresentation")]
        [TestCase("wind_thunder_fan", false, "NearPlayerPresentation")]
        public void WeaponStateSelectsRequiredPredicate(
            string weaponId,
            bool evolved,
            string expected)
        {
            var captureCase = new EightWeaponPolishCapture.CaptureCase(
                new WeaponId(weaponId),
                evolved ? 5 : 3,
                evolved,
                evolved ? "evolved" : "level-3");

            Assert.That(CapturePhasePolicy.PredicateFor(captureCase).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void SessionClearRemovesPendingFlagAndWeaponFilter()
        {
            CaptureSessionState.Begin("frost_flask");
            Assert.That(CaptureSessionState.IsPending, Is.True);
            Assert.That(CaptureSessionState.WeaponFilter, Is.EqualTo("frost_flask"));

            CaptureSessionState.Clear();

            Assert.That(CaptureSessionState.IsPending, Is.False);
            Assert.That(CaptureSessionState.WeaponFilter, Is.Empty);
        }
    }
}
