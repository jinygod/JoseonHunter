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
    }
}
