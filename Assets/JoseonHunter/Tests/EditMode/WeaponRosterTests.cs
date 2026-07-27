using System.Linq;
using JoseonHunter.Domain.Combat;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponRosterTests
    {
        [Test]
        public void LaunchRosterContainsExactlyEightDistinctWeapons()
        {
            Assert.That(WeaponRoster.All.Select(id => id.Value).Distinct().Count(), Is.EqualTo(8));
            Assert.That(WeaponRoster.All, Is.EquivalentTo(new[]
            {
                WeaponId.HwandoFlyingBlade, WeaponId.GakgungShot,
                WeaponId.TalismanThrow, WeaponId.ThunderCrashBomb,
                WeaponId.JangseungWard, WeaponId.SingijeonVolley,
                WeaponId.FrostFlask, WeaponId.WindThunderFan
            }));
        }
    }
}
