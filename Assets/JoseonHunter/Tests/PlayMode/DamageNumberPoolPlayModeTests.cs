using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Presentation.Combat;
using JoseonHunter.Runtime.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class DamageNumberPoolPlayModeTests
    {
        [UnityTest]
        public IEnumerator PoolIsBoundedAndResetAfterPresentersReturn()
        {
            var root = new GameObject("DamageNumberPoolTests");
            var pool = root.AddComponent<DamageNumberPool>();
            var registry = new CombatTargetRegistry();
            var target = new TestTarget(1);
            registry.Register(target);
            var service = new CombatDamageService(registry);
            pool.Bind(service);

            for (var index = 0; index < 120; index++)
            {
                var request = WeaponDamageRequest.Create(index + 1, WeaponId.HwandoFlyingBlade, target, 1, index == 0, new Float2(index, 0f), ContactPhase.Direct, index);
                service.TryApply(request, out _);
            }

            yield return new WaitForSeconds(1f);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.TotalInstances, Is.LessThanOrEqualTo(DamageNumberPool.MaximumCount));
            foreach (var presenter in root.GetComponentsInChildren<DamageNumberPresenter>(true))
            {
                Assert.That(presenter.DisplayText, Is.Empty);
                Assert.That(presenter.IsCritical, Is.False);
                Assert.That(presenter.transform.localScale, Is.EqualTo(Vector3.one));
            }

            Object.Destroy(root);
        }

        private sealed class TestTarget : ICombatTarget
        {
            public TestTarget(int runtimeId) { RuntimeId = runtimeId; }
            public int RuntimeId { get; }
            public bool IsAlive => true;
            public int Health => 1000;
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition => new Float2(0f, 0f);
            public PixelHitMask HurtMask => null;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Identity;
            public void ApplyResolvedDamage(int damage) { }
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
