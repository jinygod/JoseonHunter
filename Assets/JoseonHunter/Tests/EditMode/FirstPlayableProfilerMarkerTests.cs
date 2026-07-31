using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class FirstPlayableProfilerMarkerTests
    {
        [Test]
        public void MarkerNamesMatchThePortraitCombatContract()
        {
            Assert.That(FirstPlayableProfilerMarkers.RunUpdateName, Is.EqualTo("JoseonHunter.Run.Update"));
            Assert.That(FirstPlayableProfilerMarkers.EnemyGridName, Is.EqualTo("JoseonHunter.Enemy.Grid"));
            Assert.That(FirstPlayableProfilerMarkers.EnemyMoveName, Is.EqualTo("JoseonHunter.Enemy.Move"));
            Assert.That(FirstPlayableProfilerMarkers.SpawnName, Is.EqualTo("JoseonHunter.Spawn"));
            Assert.That(FirstPlayableProfilerMarkers.WeaponName, Is.EqualTo("JoseonHunter.Weapon"));
            Assert.That(FirstPlayableProfilerMarkers.PickupName, Is.EqualTo("JoseonHunter.Pickup"));
            Assert.That(FirstPlayableProfilerMarkers.UiHudName, Is.EqualTo("JoseonHunter.UI.Hud"));
            Assert.That(FirstPlayableProfilerMarkers.UiModalName, Is.EqualTo("JoseonHunter.UI.Modal"));
        }
    }
}
