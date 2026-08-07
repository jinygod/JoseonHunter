using JoseonHunter.Runtime.Audio;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameMusicPolicyTests
    {
        [TestCase(-1f, CombatMusicPhase.Early)]
        [TestCase(0f, CombatMusicPhase.Early)]
        [TestCase(299.99f, CombatMusicPhase.Early)]
        [TestCase(300f, CombatMusicPhase.Mid)]
        [TestCase(599.99f, CombatMusicPhase.Mid)]
        [TestCase(600f, CombatMusicPhase.Late)]
        [TestCase(900f, CombatMusicPhase.Late)]
        public void PhaseAtUsesFiveMinuteBoundaries(float elapsed, CombatMusicPhase expected)
        {
            Assert.That(GameMusicPolicy.PhaseAt(elapsed), Is.EqualTo(expected));
        }

        [TestCase(CombatMusicPhase.Early, GameMusicRole.CombatEarly)]
        [TestCase(CombatMusicPhase.Mid, GameMusicRole.CombatMid)]
        [TestCase(CombatMusicPhase.Late, GameMusicRole.CombatLate)]
        public void CombatPhaseMapsToItsMusicRole(CombatMusicPhase phase, GameMusicRole expected)
        {
            Assert.That(GameMusicPolicy.RoleFor(phase), Is.EqualTo(expected));
        }
    }
}
