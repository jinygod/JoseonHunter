using System.Collections.Generic;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameMusicCatalogTests
    {
        [Test]
        public void DefaultCatalogResolvesSixUniqueMusicRoles()
        {
            var catalog = GameMusicCatalogAsset.LoadDefault();
            Assert.That(catalog, Is.Not.Null);
            var clips = new HashSet<UnityEngine.AudioClip>();
            foreach (var role in new[]
                     {
                         GameMusicRole.Lobby,
                         GameMusicRole.CombatEarly,
                         GameMusicRole.CombatMid,
                         GameMusicRole.CombatLate,
                         GameMusicRole.MidBoss,
                         GameMusicRole.FinalBoss
                     })
            {
                Assert.That(catalog.TryGet(role, out var clip, out var volume), Is.True, role.ToString());
                Assert.That(clip, Is.Not.Null, role.ToString());
                Assert.That(clips.Add(clip), Is.True, role + " reused a clip");
                Assert.That(volume, Is.InRange(.2f, .8f), role.ToString());
            }

            Assert.That(catalog.TryGet(GameMusicRole.None, out _, out _), Is.False);
        }
    }
}
