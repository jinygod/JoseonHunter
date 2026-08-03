using System.IO;
using System.Linq;
using JoseonHunter.Content;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CombatChoicePixelAssetContractTests
    {
        [Test]
        public void ApprovedPixelLabAssetSetHasExactCountsAndImporterContract()
        {
            var branches = Files("Branches");
            var reactions = Files("Reactions");
            var enemies = Files("SpecialEnemies", SearchOption.AllDirectories);
            Assert.That(branches.Length, Is.EqualTo(16));
            Assert.That(reactions.Length, Is.EqualTo(4));
            Assert.That(enemies.Length, Is.EqualTo(12));
            foreach (var enemyId in CombatChoicePixelAssetContract.EnemyIds)
                Assert.That(enemies.Count(path => path.Replace('\\', '/').Contains($"/{enemyId}/")), Is.EqualTo(3));

            foreach (var path in branches.Concat(reactions).Concat(enemies))
            {
                var assetPath = path.Replace('\\', '/');
                Assert.That(CombatChoicePixelAssetContract.Validate(assetPath), Is.Empty, assetPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                Assert.That(texture.width, Is.EqualTo(48), assetPath);
                Assert.That(texture.height, Is.EqualTo(48), assetPath);
            }
        }

        [Test]
        public void CatalogResolvesEveryLegacyReactionAndSpecialEnemyWithoutFallback()
        {
            var catalog = CombatChoiceVisualCatalog.LoadDefault();
            Assert.That(catalog, Is.Not.Null);
            foreach (var id in CombatChoicePixelAssetContract.LegacyIds)
                Assert.That(catalog.LegacyIcon(new WeaponLegacyPathId(id)), Is.Not.Null, id);
            foreach (var kind in new[] { StatusReactionKind.IceShatter, StatusReactionKind.FireWind,
                         StatusReactionKind.FormationBreak, StatusReactionKind.Overload })
                Assert.That(catalog.ReactionIcon(kind), Is.Not.Null, kind.ToString());
            foreach (var id in CombatChoicePixelAssetContract.EnemyIds)
                Assert.That(catalog.EnemyFrames(id).Count, Is.EqualTo(3), id);
        }

        private static string[] Files(string folder, SearchOption option = SearchOption.TopDirectoryOnly)
        {
            var path = Path.Combine(CombatChoicePixelAssetContract.Root, folder);
            return Directory.Exists(path) ? Directory.GetFiles(path, "*.png", option) : new string[0];
        }
    }
}
