using System.Linq;
using JoseonHunter.Editor.Scenes;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PortraitStateValidationCapturePolicyTests
    {
        [Test]
        public void Release_capture_contract_has_every_portrait_resolution_and_state_name()
        {
            Assert.That(PortraitStateValidationCapture.Resolutions.Select(value => new[] { value.x, value.y }).ToArray(), Is.EqualTo(new[]
            {
                new[] { 720, 1280 }, new[] { 1080, 1920 }, new[] { 1080, 2340 }, new[] { 1170, 2532 }, new[] { 1440, 3200 }
            }));
            Assert.That(PortraitStateValidationCapture.CaptureNames, Is.EqualTo(new[]
            {
                "01-gameplay.png", "02-level-up.png", "03-appraisal.png", "04-pause.png",
                "05-resumed-combat.png"
            }));
        }

        [Test]
        public void Capture_policy_waits_one_editor_update_after_a_state_transition()
        {
            Assert.That(PortraitStateValidationCapturePolicy.ShouldCaptureThisTick(1), Is.False);
            Assert.That(PortraitStateValidationCapturePolicy.ShouldCaptureThisTick(0), Is.True);
            Assert.That(PortraitStateValidationCapturePolicy.CanResumeInCurrentProcess(4815, 4815), Is.True);
            Assert.That(PortraitStateValidationCapturePolicy.CanResumeInCurrentProcess(4815, 9261), Is.False);
        }

        [Test]
        public void Every_capture_phase_keeps_its_own_output_name()
        {
            var names = PortraitStateValidationCapture.CaptureNames;
            var mapped = Enumerable.Range(0, names.Count)
                .Select(index => PortraitStateValidationCapturePolicy.CaptureNameForPhase(names, index));
            Assert.That(mapped, Is.EqualTo(names));
        }
    }
}
