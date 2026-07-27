using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GeumjulRuleTests
    {
        [Test]
        public void AddReplacesExpiredEndpointAtTheFourSecondCutoff()
        {
            var trail = new GeumjulTrail();
            trail.Add(Point(0f, 0f, 0f));
            trail.Add(Point(1f, 0f, 4f));
            trail.Add(Point(2f, 0f, 4.01f));

            Assert.That(trail.Points, Has.Count.EqualTo(3));
            Assert.That(trail.Points[0].Position.X, Is.EqualTo(0.0025f).Within(0.00001f));
            Assert.That(trail.Points[0].Time, Is.EqualTo(0.01f).Within(0.00001f));
        }

        [Test]
        public void AddInterpolatesAtTheFourSecondTimeCutoff()
        {
            var trail = new GeumjulTrail();
            trail.Add(Point(0f, 0f, 0f));
            trail.Add(Point(5f, 0f, 5f));

            Assert.That(trail.Points, Has.Count.EqualTo(2));
            Assert.That(trail.Points[0], Is.EqualTo(Point(1f, 0f, 1f)));
            Assert.That(trail.Length, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void AddAppliesTimeInterpolationBeforeLengthTrimming()
        {
            var trail = new GeumjulTrail();
            trail.Add(Point(0f, 0f, 0f));
            trail.Add(Point(5f, 0f, 5f));
            trail.Add(Point(10f, 0f, 5f));

            Assert.That(trail.Points[0], Is.EqualTo(Point(3f, 0f, 3f)));
            Assert.That(trail.Length, Is.EqualTo(7f).Within(0.0001f));
        }

        [Test]
        public void AddTrimsTrailToMostRecentSevenMetres()
        {
            var trail = new GeumjulTrail();
            trail.Add(Point(0f, 0f, 0f));
            trail.Add(Point(5f, 0f, 1f));
            trail.Add(Point(10f, 0f, 2f));

            Assert.That(trail.Length, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(trail.Points[0].Position, Is.EqualTo(new Float2(3f, 0f)));
        }

        [Test]
        public void EightClosureMasteryExtendsTrailLimitToEightPointFiveMetres()
        {
            var trail = new GeumjulTrail(GeumjulMastery.ForClosures(8));
            trail.Add(Point(0f, 0f, 0f));
            trail.Add(Point(10f, 0f, 1f));

            Assert.That(trail.Length, Is.EqualTo(8.5f).Within(0.0001f));
        }

        [Test]
        public void PerimeterBelowTwoPointFiveMetresIsInvalid()
        {
            var loop = new LoopDetector().TryClose(Points((0, 0), (0.6f, 0), (0.6f, 0.6f), (0, 0.6f), (0, 0)));

            Assert.That(loop.IsValid, Is.False);
        }

        [Test]
        public void SelfIntersectionClosesPolygon()
        {
            var loop = new LoopDetector().TryClose(Points((0, 0), (2, 0), (2, 2), (0, 2), (0, -1)));

            Assert.That(loop.IsValid, Is.True);
            Assert.That(loop.Polygon, Is.EqualTo(new[] { new Float2(0f, 0f), new Float2(2f, 0f), new Float2(2f, 2f), new Float2(0f, 2f) }));
            Assert.That(loop.Area, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void NearFirstSegmentClosureUsesMasteryTolerance()
        {
            var points = Points((0, 0), (2, 0), (2, 2), (0, 2), (0.2f, 0.1f));

            Assert.That(new LoopDetector(GeumjulMastery.ForClosures(0)).TryClose(points).IsValid, Is.False);
            Assert.That(new LoopDetector(GeumjulMastery.ForClosures(3)).TryClose(points).IsValid, Is.True);
        }

        [Test]
        public void NearClosureIncludesActualLastPointAndItsNarrowWedge()
        {
            var loop = new LoopDetector(GeumjulMastery.ForClosures(3)).TryClose(
                Points((0, 0), (2, 0), (2, 2), (0, 2), (-0.2f, 0.1f)));
            var hits = new SealResolver().Resolve(loop, new[] { new TargetPoint(7, new Float2(-0.1f, 0.2f), false) });

            Assert.That(loop.Polygon, Is.EqualTo(new[] { new Float2(0f, 0f), new Float2(2f, 0f), new Float2(2f, 2f), new Float2(0f, 2f), new Float2(-0.2f, 0.1f) }));
            Assert.That(loop.Area, Is.EqualTo(4.2f).Within(0.0001f));
            Assert.That(hits, Has.Count.EqualTo(1));
        }

        [Test]
        public void AreaAboveThreeMetreRadiusLimitIsInvalid()
        {
            var loop = new LoopDetector().TryClose(Points((0, 0), (6, 0), (6, 6), (0, 6), (0, 0)));

            Assert.That(loop.IsValid, Is.False);
        }

        [Test]
        public void EightClosureMasteryIncreasesMaximumAreaByFifteenPercent()
        {
            var points = Points((0, 0), (5.5f, 0), (5.5f, 5.5f), (0, 5.5f), (0, 0));

            Assert.That(new LoopDetector().TryClose(points).IsValid, Is.False);
            Assert.That(new LoopDetector(GeumjulMastery.ForClosures(8)).TryClose(points).IsValid, Is.True);
        }

        [Test]
        public void MapBoundaryIsNeverPartOfAClosingSegment()
        {
            var loop = new LoopDetector().TryClose(Points((0, 0), (2, 0), (2, 2), (0, 2)));

            Assert.That(loop.IsValid, Is.False);
        }

        [Test]
        public void DegenerateRepeatedPointsAndTooFewUniqueVerticesAreInvalid()
        {
            var detector = new LoopDetector();

            Assert.That(detector.TryClose(Points((0, 0), (1, 0), (1, 0), (0, 0))).IsValid, Is.False);
            Assert.That(detector.TryClose(Points((0, 0), (1, 0), (0, 0))).IsValid, Is.False);
        }

        [Test]
        public void ResolveSelectsOnlyContainedTargetsInTargetIdOrder()
        {
            var loop = SquareLoop();
            var hits = new SealResolver().Resolve(loop, new[]
            {
                new TargetPoint(8, new Float2(1f, 1f), false),
                new TargetPoint(2, new Float2(3f, 1f), false),
                new TargetPoint(3, new Float2(0f, 1f), false),
                new TargetPoint(1, new Float2(1f, 1f), false)
            });

            Assert.That(hits, Has.Count.EqualTo(2));
            Assert.That(hits[0].TargetId, Is.EqualTo(1));
            Assert.That(hits[1].TargetId, Is.EqualTo(8));
        }

        [Test]
        public void NormalTargetReceivesTwentyDamageAndOnePointTwoSecondBind()
        {
            var hit = new SealResolver().Resolve(SquareLoop(), new[] { new TargetPoint(1, new Float2(1f, 1f), false) })[0];

            Assert.That(hit.Damage, Is.EqualTo(20));
            Assert.That(hit.BindSeconds, Is.EqualTo(1.2f));
            Assert.That(hit.Branch, Is.EqualTo(SealBranch.None));
        }

        [Test]
        public void BossReceivesFloorOfThirtyFivePercentDamageAndNoBind()
        {
            var hit = new SealResolver().Resolve(SquareLoop(), new[] { new TargetPoint(1, new Float2(1f, 1f), true) })[0];

            Assert.That(hit.Damage, Is.EqualTo(7));
            Assert.That(hit.BindSeconds, Is.Zero);
        }

        [TestCase(2, 0.15f, SealBranch.None)]
        [TestCase(3, 0.25f, SealBranch.None)]
        [TestCase(8, 0.25f, SealBranch.None)]
        [TestCase(14, 0.25f, SealBranch.None)]
        [TestCase(20, 0.25f, SealBranch.FiveColorBarrier)]
        public void MasteryChangesAtApprovedClosureThresholds(int closures, float tolerance, SealBranch expectedBranch)
        {
            var mastery = GeumjulMastery.ForClosures(closures);

            Assert.That(mastery.ClosureTolerance, Is.EqualTo(tolerance));
            Assert.That(mastery.ActiveBranch, Is.EqualTo(expectedBranch));
        }

        [Test]
        public void FourteenClosureMasteryAllowsExactlyOneSelectedFireOrIceBranch()
        {
            var mastery = GeumjulMastery.ForClosures(14);

            Assert.That(mastery.AvailableBranches, Is.EqualTo(new[] { SealBranch.FireMark, SealBranch.IceBind }));
            Assert.That(mastery.RequiresBranchChoice, Is.True);
            Assert.That(mastery.WithSelectedBranch(SealBranch.IceBind).ActiveBranch, Is.EqualTo(SealBranch.IceBind));
            Assert.That(mastery.WithSelectedBranch(SealBranch.IceBind).RequiresBranchChoice, Is.False);
        }

        [Test]
        public void MasteryRejectsPrematureOrInvalidBranchSelection()
        {
            Assert.That(() => GeumjulMastery.ForClosures(13, SealBranch.FireMark), Throws.ArgumentException);
            Assert.That(() => GeumjulMastery.ForClosures(14, SealBranch.FiveColorBarrier), Throws.ArgumentException);
        }

        [Test]
        public void MasteryRejectsReselectingAnAlreadyChosenBranchOrBarrierBranch()
        {
            var selected = GeumjulMastery.ForClosures(14).WithSelectedBranch(SealBranch.FireMark);

            Assert.That(() => selected.WithSelectedBranch(SealBranch.IceBind), Throws.InvalidOperationException);
            Assert.That(() => GeumjulMastery.ForClosures(20).WithSelectedBranch(SealBranch.FireMark), Throws.InvalidOperationException);
        }

        [Test]
        public void ResolverRejectsAnUnresolvedBranchChoice()
        {
            var resolver = new SealResolver(GeumjulMastery.ForClosures(14));

            Assert.That(
                () => resolver.Resolve(SquareLoop(), new[] { new TargetPoint(1, new Float2(1f, 1f), false) }),
                Throws.InvalidOperationException.With.Message.EqualTo("A Fire Mark or Ice Bind branch must be selected before resolving seals."));
        }

        [Test]
        public void TwentyClosureMasteryUsesFortyBaseDamageForBarrier()
        {
            var hit = new SealResolver(GeumjulMastery.ForClosures(20)).Resolve(
                SquareLoop(), new[] { new TargetPoint(1, new Float2(1f, 1f), false) })[0];

            Assert.That(hit.Damage, Is.EqualTo(40));
            Assert.That(hit.Branch, Is.EqualTo(SealBranch.FiveColorBarrier));
        }

        [Test]
        public void InteriorCrossingBuildsPolygonAtTheActualIntersection()
        {
            var loop = new LoopDetector().TryClose(Points((0, 0), (4, 0), (4, 4), (0, 4), (0, 3), (5, 3)));

            Assert.That(loop.Polygon, Is.EqualTo(new[] { new Float2(4f, 3f), new Float2(4f, 4f), new Float2(0f, 4f), new Float2(0f, 3f) }));
            Assert.That(loop.Area, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void MultipleCrossingsUseEarliestPointOnFinalSegment()
        {
            var loop = new LoopDetector().TryClose(Points((1, 0), (1, 4), (3, 4), (3, 0), (0, 2), (5, 2)));

            Assert.That(loop.Polygon, Is.EqualTo(new[] { new Float2(1f, 2f), new Float2(1f, 4f), new Float2(3f, 4f), new Float2(3f, 0f), new Float2(0f, 2f) }));
            Assert.That(loop.Area, Is.EqualTo(7f).Within(0.0001f));
        }

        [Test]
        public void EarlierFinalSegmentCrossingWinsEvenWhenItsHistoricalSegmentIsLater()
        {
            var loop = new LoopDetector().TryClose(Points((3, 0), (3, 4), (1, 4), (1, 0), (0, 2), (5, 2)));

            Assert.That(loop.Polygon, Is.EqualTo(new[] { new Float2(1f, 2f), new Float2(1f, 0f), new Float2(0f, 2f) }));
            Assert.That(loop.Area, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void SimultaneousCrossingsUseMostRecentHistoricalSegment()
        {
            var loop = new LoopDetector().TryClose(Points((0, 0), (2, 2), (0, 2), (2, 0), (0, 1), (2, 1)));

            Assert.That(loop.Polygon, Is.EqualTo(new[] { new Float2(1f, 1f), new Float2(2f, 0f), new Float2(0f, 1f) }));
            Assert.That(loop.Area, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void NonFiniteTrailValuesAreRejectedAtTheDomainBoundary()
        {
            var trail = new GeumjulTrail();

            Assert.That(() => trail.Add(Point(float.NaN, 0f, 0f)), Throws.ArgumentException);
            Assert.That(() => trail.Add(Point(0f, 0f, float.PositiveInfinity)), Throws.ArgumentException);
            Assert.That(new LoopDetector().TryClose(new[]
            {
                Point(0f, 0f, 0f), Point(2f, 0f, 1f), Point(float.PositiveInfinity, 2f, 2f), Point(0f, 2f, 3f), Point(0f, 0f, 4f)
            }).IsValid, Is.False);
        }

        private static LoopResult SquareLoop() => new LoopDetector().TryClose(Points((0, 0), (2, 0), (2, 2), (0, 2), (0, 0)));

        private static List<TrailPoint> Points(params (float x, float y)[] values)
        {
            var result = new List<TrailPoint>();
            for (var index = 0; index < values.Length; index++) result.Add(Point(values[index].x, values[index].y, index));
            return result;
        }

        private static TrailPoint Point(float x, float y, float time) => new TrailPoint(new Float2(x, y), time);
    }
}
