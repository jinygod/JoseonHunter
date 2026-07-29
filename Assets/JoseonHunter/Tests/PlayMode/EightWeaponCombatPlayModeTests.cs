using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Presentation.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class EightWeaponCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameplayStartsWithHwandoAndCanAcquireAnOfferedWeapon()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.RegisteredWeaponIds.Single().Value, Is.EqualTo("hwando_flying_blade"));
            Assert.That(controller.WeaponRuntime, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<DamageNumberPool>(), Is.Not.Null);

            var openUpgrade = typeof(FirstPlayableController).GetMethod("OpenUpgrade", BindingFlags.Instance | BindingFlags.NonPublic);
            var chooseUpgrade = typeof(FirstPlayableController).GetMethod("ChooseUpgrade", BindingFlags.Instance | BindingFlags.NonPublic);
            var offerField = typeof(FirstPlayableController).GetField("upgradeOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            openUpgrade.Invoke(controller, null);
            var labels = (List<string>)offerField.GetValue(controller);
            var newWeaponIndex = labels.FindIndex(label => label.StartsWith("[신규]"));

            Assert.That(newWeaponIndex, Is.GreaterThanOrEqualTo(0));
            chooseUpgrade.Invoke(controller, new object[] { newWeaponIndex });

            Assert.That(controller.RegisteredWeaponIds.Distinct().Count(), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator LinearProjectileAdvancesThroughPresentationFrames()
        {
            var root = new GameObject("Animated Projectile Root");
            var texture = new Texture2D(2, 1);
            texture.SetPixels(new[] { Color.white, Color.black });
            texture.Apply();
            var first = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * .5f, 1f);
            var second = Sprite.Create(texture, new Rect(1f, 0f, 1f, 1f), Vector2.one * .5f, 1f);
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), mask);
            var executor = new LinearProjectileExecutor(runtime);
            Sprite Resolve(WeaponId _, int partIndex) => partIndex == 5 ? first : second;
            var context = new WeaponExecutionContext(default, root.transform, first, null, Resolve, null, 0, 1);
            var spec = new LinearProjectileSpec(
                new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f),
                WeaponId.GakgungShot,
                default,
                new Float2(1f, 0f),
                1f,
                1f,
                1,
                1,
                "Animated Arrow",
                visualPartStart: 5,
                visualFrameCount: 2,
                visualFrameSeconds: .01f);

            executor.Launch(context, spec);
            Assert.That(root.GetComponentInChildren<SpriteRenderer>().sprite, Is.SameAs(first));

            executor.Tick(.011f, context);
            yield return null;

            Assert.That(root.GetComponentInChildren<SpriteRenderer>().sprite, Is.SameAs(second));
            executor.Dispose();
            runtime.Dispose();
            Object.Destroy(root);
            Object.Destroy(first);
            Object.Destroy(second);
            Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator StatefulWeaponsRenderCanonicalFramesAtTheirLogicalStages()
        {
            var root = new GameObject("Stateful Weapon Presentation Root");
            var texture = new Texture2D(WeaponVisualPartIndex.ThunderCrash.RequiredCount, 1);
            var frames = new Sprite[WeaponVisualPartIndex.ThunderCrash.RequiredCount];
            for (var index = 0; index < frames.Length; index++)
                frames[index] = Sprite.Create(texture, new Rect(index, 0f, 1f, 1f), Vector2.one * .5f, 1f);
            Sprite Resolve(WeaponId _, int partIndex) => frames[partIndex];
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), mask);
            registry.Register(new TestTarget(1, new Float2(1f, 0f), mask));
            var thunder = new ThunderBombExecutor(runtime, 10f, 10f, 3f, 1f, .2f, 1f, 3);
            var context = new WeaponExecutionContext(default, root.transform, frames[0], null, Resolve, null, 4, 1);

            thunder.Tick(.5f, context);
            yield return null;

            var bomb = root.transform.Find("Thunder Crash Bomb");
            var shadow = root.transform.Find("Bomb Shadow");
            Assert.That(bomb, Is.Not.Null);
            Assert.That(bomb.GetComponent<SpriteRenderer>().sprite, Is.SameAs(frames[3]));
            Assert.That(bomb.position.y, Is.GreaterThan(shadow.position.y));
            Assert.That(shadow.gameObject.activeSelf, Is.True);

            thunder.Dispose();
            runtime.Dispose();
            Object.Destroy(root);
            foreach (var frame in frames) Object.Destroy(frame);
            Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator JangseungRiseAndBoundaryPresentationUsesOnlyCanonicalFrames()
        {
            var root = new GameObject("Jangseung presentation root");
            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * .5f, 1f);
            var requestedParts = new List<int>();
            Sprite Resolve(WeaponId _, int partIndex)
            {
                requestedParts.Add(partIndex);
                return sprite;
            }
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            var damageEvents = new List<ConfirmedDamageEvent>();
            damage.DamageConfirmed += damageEvents.Add;
            var ward = new JangseungWardExecutor(runtime, 10f, 10f, 1f, 3, 1, 0f, 3);
            var context = new WeaponExecutionContext(default, root.transform, sprite, null, Resolve, null, 0, 1);

            ward.Tick(.04f, context);
            ward.Tick(.10f, context);
            yield return null;

            Assert.That(ward.FirstWardVisualRiseForTests, Is.InRange(0f, 1f));
            Assert.That(requestedParts, Has.Some.InRange(
                WeaponVisualPartIndex.Jangseung.Windup,
                WeaponVisualPartIndex.Jangseung.Windup + WeaponVisualPartIndex.Jangseung.WindupFrameCount - 1));
            Assert.That(requestedParts, Has.Some.InRange(
                WeaponVisualPartIndex.Jangseung.Field,
                WeaponVisualPartIndex.Jangseung.Field + WeaponVisualPartIndex.Jangseung.FieldFrameCount - 1));
            Assert.That(damageEvents, Is.Empty);

            ward.Dispose();
            runtime.Dispose();
            Object.Destroy(root);
            Object.Destroy(sprite);
            Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator FanGustMarksAndLightningUseCanonicalFramesWithoutExtraContacts()
        {
            var root = new GameObject("Fan presentation root");
            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * .5f, 1f);
            var requestedParts = new List<int>();
            Sprite Resolve(WeaponId _, int partIndex)
            {
                requestedParts.Add(partIndex);
                return sprite;
            }
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, mask);
            registry.Register(new TestTarget(1, new Float2(.5f, 0f), mask));
            var fan = new WindThunderFanExecutor(runtime, 10f, 10f, 2f, 1f, 1, 1);
            var events = new List<ConfirmedDamageEvent>();
            damage.DamageConfirmed += events.Add;
            var context = new WeaponExecutionContext(default, root.transform, sprite, null, Resolve, null, 0, 1);

            fan.Tick(.01f, context);
            fan.Tick(.12f, context);
            fan.Tick(.01f, context);
            yield return null;

            Assert.That(events.Count(value => value.Phase == ContactPhase.Wind), Is.EqualTo(1));
            Assert.That(events.Count(value => value.Phase == ContactPhase.Lightning), Is.EqualTo(1));
            Assert.That(requestedParts, Has.Some.InRange(
                WeaponVisualPartIndex.WindThunderFan.Projectile,
                WeaponVisualPartIndex.WindThunderFan.Projectile + WeaponVisualPartIndex.WindThunderFan.ProjectileFrameCount - 1));
            Assert.That(requestedParts, Has.Some.InRange(
                WeaponVisualPartIndex.WindThunderFan.Field,
                WeaponVisualPartIndex.WindThunderFan.Field + WeaponVisualPartIndex.WindThunderFan.FieldFrameCount - 1));
            Assert.That(requestedParts, Has.Some.InRange(
                WeaponVisualPartIndex.WindThunderFan.Impact,
                WeaponVisualPartIndex.WindThunderFan.Impact + WeaponVisualPartIndex.WindThunderFan.ImpactFrameCount - 1));

            fan.Dispose();
            runtime.Dispose();
            Object.Destroy(root);
            Object.Destroy(sprite);
            Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator EvolvedFrostStoredShatterRendersAtAcceptedTargetsCurrentPosition()
        {
            var root = new GameObject("Stored Frost Shatter Root");
            var texture = new Texture2D(WeaponVisualPartIndex.FrostFlask.RequiredCount, 1);
            var frames = new Sprite[WeaponVisualPartIndex.FrostFlask.RequiredCount];
            for (var index = 0; index < frames.Length; index++)
                frames[index] = Sprite.Create(texture, new Rect(index, 0f, 1f, 1f), Vector2.one * .5f, 1f);
            Sprite Resolve(WeaponId _, int partIndex) => frames[partIndex];
            var mask = PixelHitMask.FromRows("1");
            var registry = new CombatTargetRegistry();
            var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), mask);
            var target = new TestTarget(1, new Float2(.4f, 0f), mask);
            registry.Register(target);
            var frost = new FrostFlaskExecutor(runtime, 10f, 10f, 2f, .1f, 1f, 1f, 1, 5, evolved: true);
            var context = new WeaponExecutionContext(default, root.transform, frames[0], null, Resolve, null, 4, 1);

            frost.Tick(.1f, context);
            frost.Tick(.75f, context);
            target.Position = new Float2(1.2f, .3f);
            frost.Tick(.25f, context);
            yield return null;

            var shatter = root.GetComponentsInChildren<SpriteRenderer>()
                .Single(renderer => renderer.gameObject.name == "Weapon Transient Visual" &&
                                    renderer.sprite == frames[WeaponVisualPartIndex.FrostFlask.RequiredCount - 1]);
            Assert.That(shatter.transform.position.x, Is.EqualTo(1.2f).Within(.001f));
            Assert.That(shatter.transform.position.y, Is.EqualTo(.3f).Within(.001f));

            frost.Dispose();
            runtime.Dispose();
            Object.Destroy(root);
            foreach (var frame in frames) Object.Destroy(frame);
            Object.Destroy(texture);
        }
    }
}
