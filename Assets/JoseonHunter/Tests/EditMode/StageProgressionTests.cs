using System;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StageProgressionTests
    {
        [Test]
        public void NewAccountUnlocksOnlyStageOneNormal()
        {
            var records = Array.Empty<StageClearRecord>();

            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), records), Is.True);
            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal), records), Is.False);
            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.GwigokField, StageDifficulty.Omen), records), Is.False);
            Assert.That(StageUnlockRules.LockReason(
                new StageSelection(StageId.GwigokField, StageDifficulty.Omen), records),
                Is.EqualTo("이 장 보통 승리 시 해금"));
        }

        [Test]
        public void StageOneNormalVictoryOpensNextNormalAndCurrentOmen()
        {
            var records = new[]
            {
                StageClearRecord.Victory(
                    new StageSelection(StageId.GwigokField, StageDifficulty.Normal),
                    900f,
                    500,
                    35)
            };

            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal), records), Is.True);
            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.GwigokField, StageDifficulty.Omen), records), Is.True);
            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.GwigokField, StageDifficulty.GreatOmen), records), Is.False);
        }

        [Test]
        public void OmenVictoryOpensOnlyTheSameStagesGreatOmen()
        {
            var records = new[]
            {
                StageClearRecord.Victory(
                    new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35),
                StageClearRecord.Victory(
                    new StageSelection(StageId.GwigokField, StageDifficulty.Omen), 900f, 530, 35)
            };

            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.GwigokField, StageDifficulty.GreatOmen), records), Is.True);
            Assert.That(StageUnlockRules.IsUnlocked(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Omen), records), Is.False);
        }

        [Test]
        public void CatalogHasThreeOrderedPlayableStages()
        {
            Assert.That(StageCatalog.All.Count, Is.EqualTo(3));
            Assert.That(StageCatalog.All[0].Id, Is.EqualTo(StageId.GwigokField));
            Assert.That(StageCatalog.All[0].DisplayName, Is.EqualTo("귀곡 들판"));
            Assert.That(StageCatalog.All[0].HasPlayableContent, Is.True);
            Assert.That(StageCatalog.All[1].DisplayName, Is.EqualTo("도깨비 고갯길"));
            Assert.That(StageCatalog.All[1].HasPlayableContent, Is.True);
            Assert.That(StageCatalog.All[2].DisplayName, Is.EqualTo("월식 고분"));
            Assert.That(StageCatalog.All[2].HasPlayableContent, Is.True);
        }

        [TestCase(StageDifficulty.Normal, 1f, 1f, 1f, 1f, 1f, 1f, 0)]
        [TestCase(StageDifficulty.Omen, 1.35f, 1.15f, 1.10f, 1.35f, 1.25f, 1.20f, 1)]
        [TestCase(StageDifficulty.GreatOmen, 1.75f, 1.30f, 1.20f, 1.75f, 1.50f, 1.40f, 2)]
        public void DifficultyProfilesMatchTheApprovedBalance(
            StageDifficulty difficulty,
            float health,
            float damage,
            float density,
            float coins,
            float accountExperience,
            float mastery,
            int bossPressure)
        {
            var profile = StageDifficultyProfile.For(difficulty);

            Assert.That(profile.EnemyHealthMultiplier, Is.EqualTo(health).Within(.001f));
            Assert.That(profile.EnemyDamageMultiplier, Is.EqualTo(damage).Within(.001f));
            Assert.That(profile.WaveDensityMultiplier, Is.EqualTo(density).Within(.001f));
            Assert.That(profile.CoinRewardMultiplier, Is.EqualTo(coins).Within(.001f));
            Assert.That(profile.AccountExperienceMultiplier, Is.EqualTo(accountExperience).Within(.001f));
            Assert.That(profile.MasteryRewardMultiplier, Is.EqualTo(mastery).Within(.001f));
            Assert.That(profile.BossPressureTier, Is.EqualTo(bossPressure));
            Assert.That(profile.ScaleActiveCap(140), Is.EqualTo(140));
        }
    }
}
