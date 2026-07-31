using System.Collections;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class FirstPlayableLoadPlayModeTests
    {
        private const int WarmupFrameCount = 30;
        private const int SampleFrameCount = 120;
        private static readonly long[] FrameDurationsNanoseconds = new long[SampleFrameCount];
        private static readonly string[] MarkerNames =
        {
            FirstPlayableProfilerMarkers.RunUpdateName,
            FirstPlayableProfilerMarkers.EnemyGridName,
            FirstPlayableProfilerMarkers.EnemyMoveName,
            FirstPlayableProfilerMarkers.SpawnName,
            FirstPlayableProfilerMarkers.WeaponName,
            FirstPlayableProfilerMarkers.PickupName,
            FirstPlayableProfilerMarkers.UiHudName,
            FirstPlayableProfilerMarkers.UiModalName
        };

        [UnityTest]
        public IEnumerator SeparationKeepsThirtyAgentsDistinctAndChasing()
        {
            yield return VerifyCrowd(30);
        }

        [UnityTest]
        public IEnumerator SeparationKeepsFiftyAgentsDistinctAndChasing()
        {
            yield return VerifyCrowd(50);
        }

        [UnityTest]
        public IEnumerator SeparationKeepsOneHundredAgentsDistinctAndChasing()
        {
            yield return VerifyCrowd(100);
        }

        [UnityTest]
        public IEnumerator LoadMeasurementRecordsProfilerAvailabilityAndThirtyWarmFrames()
        {
            yield return CaptureLoadEvidence(30);
        }

        [UnityTest]
        public IEnumerator LoadMeasurementRecordsProfilerAvailabilityAtFiftyEnemies()
        {
            yield return CaptureLoadEvidence(50);
        }

        [UnityTest]
        public IEnumerator LoadMeasurementRecordsProfilerAvailabilityAtOneHundredEnemies()
        {
            yield return CaptureLoadEvidence(100);
        }

        [UnityTest]
        public IEnumerator FullGameplayTickRunsOnlyWhileFlowIsPlaying()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            var randomState = Random.state;
            try
            {
                controller.ConfigureSeparationLoadScenarioForTests();
                controller.SpawnEnemyForSeparationTests(new Vector2(10f, 0f));
                controller.SetContactInvulnerabilityForTests(.5f);
                var beforePosition = controller.LivingEnemyPositionsForTests[0];
                var beforeInvulnerability = controller.ContactInvulnerabilityForTests;

                Assert.That(controller.Flow.TryTransition(GameFlowState.Paused), Is.True);
                Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.False);
                Assert.That(controller.LivingEnemyPositionsForTests[0], Is.EqualTo(beforePosition));
                Assert.That(controller.ContactInvulnerabilityForTests, Is.EqualTo(beforeInvulnerability));

                Assert.That(controller.Flow.TryTransition(GameFlowState.Playing), Is.True);
                Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);
                Assert.That(controller.LastSeparationAgentCountForTests, Is.EqualTo(1));
                Assert.That(controller.ContactInvulnerabilityForTests, Is.LessThan(beforeInvulnerability));
            }
            finally
            {
                Random.state = randomState;
                controller.Flow.ResetToPlaying();
                Time.timeScale = 1f;
            }
        }

        private static IEnumerator VerifyCrowd(int count)
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            var randomState = Random.state;
            try
            {
                controller.ConfigureSeparationLoadScenarioForTests();
                for (var index = 0; index < count; index++) controller.SpawnEnemyForSeparationTests(new Vector2(10f, 0f));
                controller.SpawnTreasureForSeparationTests(new Vector2(10f, 0f));
                var before = controller.AverageLivingEnemyDistanceToPlayerForTests;
                for (var tick = 0; tick < 80; tick++)
                    Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);
                var positions = controller.LivingEnemyPositionsForTests;

                Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
                Assert.That(controller.RunEndedForTests, Is.False);
                Assert.That(controller.LastSeparationAgentCountForTests, Is.EqualTo(count));
                Assert.That(controller.AverageLivingEnemyDistanceToPlayerForTests, Is.LessThan(before));
                Assert.That(positions.Count, Is.EqualTo(count));
                for (var first = 0; first < positions.Count; first++)
                for (var second = first + 1; second < positions.Count; second++)
                    Assert.That((positions[first] - positions[second]).sqrMagnitude, Is.GreaterThan(0f));
            }
            finally
            {
                Random.state = randomState;
                controller.Flow.ResetToPlaying();
                Time.timeScale = 1f;
            }
        }

        private static IEnumerator CaptureLoadEvidence(int count)
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            var randomState = Random.state;
            var originalTimeScale = Time.timeScale;
            var frameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            var markerRecorders = new ProfilerRecorder[MarkerNames.Length];
            for (var markerIndex = 0; markerIndex < markerRecorders.Length; markerIndex++)
                markerRecorders[markerIndex] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, MarkerNames[markerIndex], 1);
            try
            {
                controller.ConfigureSeparationLoadScenarioForTests();
                for (var index = 0; index < count; index++) controller.SpawnEnemyForSeparationTests(new Vector2(10f, 0f));

                for (var frame = 0; frame < WarmupFrameCount; frame++) yield return null;

                var maximumGcBytes = 0L;
                for (var frame = 0; frame < SampleFrameCount; frame++)
                {
                    yield return null;
                    FrameDurationsNanoseconds[frame] = frameTimeRecorder.LastValue;
                    maximumGcBytes = System.Math.Max(maximumGcBytes, gcRecorder.LastValue);
                }

                System.Array.Sort(FrameDurationsNanoseconds);
                var positions = controller.LivingEnemyPositionsForTests;
                var minimumSpacing = float.PositiveInfinity;
                for (var first = 0; first < positions.Count; first++)
                for (var second = first + 1; second < positions.Count; second++)
                    minimumSpacing = Mathf.Min(minimumSpacing, Vector2.Distance(positions[first], positions[second]));

                var movementAllocationBefore = System.GC.GetAllocatedBytesForCurrentThread();
                for (var tick = 0; tick < SampleFrameCount; tick++) controller.UpdateEnemiesForTests(.05f);
                var movementAllocationBytes = System.GC.GetAllocatedBytesForCurrentThread() - movementAllocationBefore;

                var medianMilliseconds = FrameDurationsNanoseconds[SampleFrameCount / 2] / 1000000d;
                var p95Milliseconds = FrameDurationsNanoseconds[(SampleFrameCount * 95 + 99) / 100 - 1] / 1000000d;
                var markerAvailability = string.Empty;
                for (var markerIndex = 0; markerIndex < markerRecorders.Length; markerIndex++)
                {
                    if (markerIndex != 0) markerAvailability += ",";
                    markerAvailability += $"{MarkerNames[markerIndex]}:valid={markerRecorders[markerIndex].Valid}:samples={markerRecorders[markerIndex].Count}:lastNs={markerRecorders[markerIndex].LastValue}";
                }
                TestContext.WriteLine($"LOAD AFTER count={count}; warmupFrames={WarmupFrameCount}; sampleFrames={SampleFrameCount}; active={positions.Count}; medianMs={medianMilliseconds:F3}; p95Ms={p95Milliseconds:F3}; maxGcBytes={maximumGcBytes}; minSpacing={minimumSpacing:F4}; movementTickAllocatedBytes={movementAllocationBytes}; mainThreadValid={frameTimeRecorder.Valid}; markers=[{markerAvailability}]");

                Assert.That(frameTimeRecorder.Valid, Is.True, "The Main Thread recorder must be available in this test environment.");
                for (var markerIndex = 0; markerIndex < markerRecorders.Length; markerIndex++)
                    Assert.That(markerRecorders[markerIndex].Valid, Is.True, $"The '{MarkerNames[markerIndex]}' marker must be available.");
                Assert.That(movementAllocationBytes, Is.EqualTo(0L), "Enemy movement must not allocate managed memory after warmup.");
                Assert.That(p95Milliseconds, Is.LessThanOrEqualTo(33.34d), "The headless p95 frame time must stay within two 60 Hz frame budgets.");
            }
            finally
            {
                for (var markerIndex = 0; markerIndex < markerRecorders.Length; markerIndex++) markerRecorders[markerIndex].Dispose();
                gcRecorder.Dispose();
                frameTimeRecorder.Dispose();
                Random.state = randomState;
                controller.Flow.ResetToPlaying();
                Time.timeScale = originalTimeScale;
            }
        }
    }
}
