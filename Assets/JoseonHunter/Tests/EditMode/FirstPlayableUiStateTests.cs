using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class FirstPlayableUiStateTests
    {
        [Test]
        public void Upgrade_choice_state_copies_source_items()
        {
            var source = new[]
            {
                new UpgradeChoiceView("gakgung_shot", UpgradeKind.Weapon, 1, "신규 무기", "각궁", "직선 관통 공격", "신규", null),
                new UpgradeChoiceView("boots", UpgradeKind.Support, 2, "능력 강화", "경쾌한 버선", "이동 속도 증가", "+12%", null)
            };

            var state = new UpgradeChoiceState(3, source);
            source[0] = default;

            Assert.That(state.Level, Is.EqualTo(3));
            Assert.That(state.Choices[0].Id, Is.EqualTo("gakgung_shot"));
        }
    }
}
