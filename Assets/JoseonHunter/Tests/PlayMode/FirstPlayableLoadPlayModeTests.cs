using System.Collections;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI;
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
        private static readonly double[] LifecycleMilliseconds = new double[24];
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
        public IEnumerator LoadMeasurementRecordsProfilerAvailabilityAtMobileCapOfOneHundredFortyEnemies()
        {
            yield return CaptureLoadEvidence(140);
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

        [UnityTest]
        public IEnumerator ProfilerMarkersRecordBurstHudAndModalLifecyclePaths()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);

            var randomState = Random.state;
            var originalTimeScale = Time.timeScale;
            var spawnRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, FirstPlayableProfilerMarkers.SpawnName, 8);
            var hudRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, FirstPlayableProfilerMarkers.UiHudName, 8);
            var modalRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, FirstPlayableProfilerMarkers.UiModalName, 16);
            try
            {
                controller.AdvanceStageForTests(119f, 121f);
                yield return new WaitForSeconds(.12f);
                controller.OpenUpgradeForTests();
                yield return null;
                modalRecorder.Dispose();
                modalRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, FirstPlayableProfilerMarkers.UiModalName, 16);
                controller.ResetRunForTests();
                yield return null;

                Assert.That(HasNonZeroSample(modalRecorder), Is.True, "RunReset close leaves must record Modal after the open sample is cleared.");
                modalRecorder.Dispose();
                controller.OpenUpgradeForTests();
                yield return null;
                modalRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, FirstPlayableProfilerMarkers.UiModalName, 16);
                Object.Destroy(bootstrap.gameObject);
                yield return null;

                Assert.That(spawnRecorder.Valid, Is.True);
                Assert.That(hudRecorder.Valid, Is.True);
                Assert.That(modalRecorder.Valid, Is.True);
                TestContext.WriteLine(DescribeSamples("Spawn", spawnRecorder));
                TestContext.WriteLine(DescribeSamples("HUD", hudRecorder));
                TestContext.WriteLine(DescribeSamples("Modal", modalRecorder));
                Assert.That(HasNonZeroSample(spawnRecorder), Is.True);
                Assert.That(hudRecorder.ToArray(), Is.Not.Empty,
                    "HUD marker must record a scope even when the headless profiler clock rounds short samples to zero.");
                Assert.That(HasNonZeroSample(modalRecorder), Is.True, "Destroy cleanup leaves must record Modal after the open sample is cleared.");
            }
            finally
            {
                modalRecorder.Dispose();
                hudRecorder.Dispose();
                spawnRecorder.Dispose();
                Random.state = randomState;
                controller.Flow.ResetToPlaying();
                Time.timeScale = originalTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator LifecycleEvidenceMeasuresExistingSpawnCleanupAndBurstAtOneHundredEnemyTier()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            var randomState = Random.state;
            var originalTimeScale = Time.timeScale;
            var originalElapsed = controller.ElapsedForTests;
            var burstRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, FirstPlayableProfilerMarkers.SpawnName, 8);
            try
            {
                controller.ConfigureSeparationLoadScenarioForTests();
                controller.ConfigureFinalSurgePacingForTests();
                for (var index = 0; index < 100; index++) controller.SpawnEnemyForSeparationTests(new Vector2(10f, 0f));
                Assert.That(controller.EnemyCountForTests, Is.EqualTo(100));
                for (var index = 0; index < LifecycleMilliseconds.Length; index++)
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    controller.SpawnEnemyForLifecycleTests();
                    timer.Stop();
                    LifecycleMilliseconds[index] = timer.Elapsed.TotalMilliseconds;
                }
                var spawnP95 = Percentile95(LifecycleMilliseconds);

                for (var index = 0; index < LifecycleMilliseconds.Length; index++)
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    controller.DestroyLastEnemyForLifecycleTests();
                    timer.Stop();
                    LifecycleMilliseconds[index] = timer.Elapsed.TotalMilliseconds;
                }
                var cleanupP95 = Percentile95(LifecycleMilliseconds);
                yield return null;

                var steadyGcBefore = System.GC.GetAllocatedBytesForCurrentThread();
                for (var tick = 0; tick < SampleFrameCount; tick++) controller.UpdateEnemiesForTests(.05f);
                var steadyGcPerFrame = (System.GC.GetAllocatedBytesForCurrentThread() - steadyGcBefore) / SampleFrameCount;
                var burstTimer = System.Diagnostics.Stopwatch.StartNew();
                controller.SpawnBurstForTests(34);
                burstTimer.Stop();
                Assert.That(controller.EnemyCountForTests, Is.EqualTo(134));
                yield return null;
                Assert.That(HasNonZeroSample(burstRecorder), Is.True);
                Assert.That(burstTimer.Elapsed.TotalMilliseconds, Is.LessThan(16.67d),
                    $"Editor/headless SpawnBurst(34) lifecycle gate missed 16.67 ms: {burstTimer.Elapsed.TotalMilliseconds:F4} ms at 100→134 active enemies.");
                TestContext.WriteLine($"LIFECYCLE 100 spawnP95Ms={spawnP95:F4}; cleanupEntryP95Ms={cleanupP95:F4}; steadyLifecycleGcBytesPerFrame={steadyGcPerFrame}; burst34Ms={burstTimer.Elapsed.TotalMilliseconds:F4}; burstActiveBefore=100; burstActiveAfter=134");
                Assert.That(spawnP95, Is.GreaterThanOrEqualTo(0d));
                Assert.That(cleanupP95, Is.GreaterThanOrEqualTo(0d));
            }
            finally
            {
                burstRecorder.Dispose();
                Random.state = randomState;
                controller.RestoreElapsedForTests(originalElapsed);
                controller.Flow.ResetToPlaying();
                Time.timeScale = originalTimeScale;
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

        private static bool HasNonZeroSample(ProfilerRecorder recorder)
        {
            var samples = recorder.ToArray();
            for (var index = 0; index < samples.Length; index++)
                if (samples[index].Value > 0) return true;
            return false;
        }

        private static string DescribeSamples(string label, ProfilerRecorder recorder)
        {
            var samples = recorder.ToArray();
            long total = 0;
            for (var index = 0; index < samples.Length; index++) total += samples[index].Value;
            return $"{label} marker samples={samples.Length}; totalValue={total}; capacity={recorder.Capacity}";
        }

        private static double Percentile95(double[] values)
        {
            System.Array.Sort(values);
            return values[(values.Length * 95 + 99) / 100 - 1];
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
                Assert.That(gcRecorder.Valid, Is.True, "The GC Allocated In Frame recorder must be available in this test environment.");
                Assert.That(positions.Count, Is.EqualTo(count), "The requested active count must survive the measured window.");
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
