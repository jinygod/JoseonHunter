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
        public IEnumerator NormalDamageNumberIsSmallAndAnchoredJustAboveTheContact()
        {
            var root = new GameObject("Damage Number Readability");
            var presenter = root.AddComponent<DamageNumberPresenter>();
            var display = new DamageNumberDisplay(27, new Float2(2f, 3f), false,
                WeaponId.HwandoFlyingBlade, 1, false);

            presenter.Play(display, false, Color.white, _ => { });
            yield return null;

            Assert.That(presenter.DisplayFontSize, Is.InRange(2f, 4f));
            Assert.That(presenter.DisplayFontName, Is.EqualTo("BlackAndWhitePicture-Dynamic SDF"));
            Assert.That(presenter.transform.position.y, Is.InRange(3.15f, 3.7f));
            Assert.That(presenter.transform.localScale.x, Is.LessThanOrEqualTo(1f));
            Object.Destroy(root);
        }

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
