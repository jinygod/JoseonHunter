using System;
using System.Reflection;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Presentation.Combat;
using JoseonHunter.Runtime.Combat;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class CombatFeedbackDirectorPlayModeTests
    {
        private static readonly MethodInfo PreCull = typeof(CombatFeedbackDirector).GetMethod("OnCameraPreCull", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo PostRender = typeof(CombatFeedbackDirector).GetMethod("OnCameraPostRender", BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void Render_scoped_impulse_restores_each_camera_owner_baseline()
        {
            var cameraObject = new GameObject("Feedback Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            var root = new GameObject("Feedback Director");
            var director = root.AddComponent<CombatFeedbackDirector>();
            var baseline = new Vector3(2f, 3f, -10f);

            try
            {
                TriggerCriticalImpulse(director);
                camera.transform.position = baseline;
                Invoke(PreCull, director, camera);
                Invoke(PostRender, director, camera);
                Assert.That(camera.transform.position, Is.EqualTo(baseline));

                var movedByAnotherOwner = new Vector3(-4f, 7f, -10f);
                camera.transform.position = movedByAnotherOwner;
                Invoke(PreCull, director, camera);
                Invoke(PostRender, director, camera);
                Assert.That(camera.transform.position, Is.EqualTo(movedByAnotherOwner));
            }
            finally
            {
                Time.timeScale = 1f;
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void Disable_and_destroy_restore_a_render_scoped_camera_baseline()
        {
            var cameraObject = new GameObject("Feedback Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            var baseline = new Vector3(1f, -2f, -10f);
            var root = new GameObject("Feedback Director");
            var director = root.AddComponent<CombatFeedbackDirector>();

            try
            {
                TriggerCriticalImpulse(director);
                camera.transform.position = baseline;
                Invoke(PreCull, director, camera);
                director.enabled = false;
                Assert.That(camera.transform.position, Is.EqualTo(baseline));

                director.enabled = true;
                TriggerCriticalImpulse(director);
                camera.transform.position = baseline;
                Invoke(PreCull, director, camera);
                UnityEngine.Object.DestroyImmediate(root);
                Assert.That(camera.transform.position, Is.EqualTo(baseline));
                root = null;
            }
            finally
            {
                Time.timeScale = 1f;
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void Fatal_boss_event_uses_event_time_classification_after_target_death()
        {
            var root = new GameObject("Feedback Director");
            var director = root.AddComponent<CombatFeedbackDirector>();
            var registry = new CombatTargetRegistry();
            var target = new KillableTarget(1, isBoss: true);
            registry.Register(target);
            var service = new CombatDamageService(registry);
            director.SetTargetAlivePredicate(id => id == target.RuntimeId && target.IsAlive);
            director.Bind(service);

            try
            {
                Assert.That(service.TryApply(WeaponDamageRequest.Create(1, WeaponId.HwandoFlyingBlade, target, 10, false,
                    new Float2(0f, 0f), ContactPhase.Direct, 1), out var confirmed), Is.True);
                Assert.That(confirmed.IsBossTarget, Is.True);
                Assert.That(target.IsAlive, Is.False);
                var flash = root.transform.Find("Contact Flash");
                Assert.That(flash, Is.Not.Null);
                Assert.That(flash.localScale.x, Is.EqualTo(.56f).Within(.001f));
            }
            finally
            {
                Time.timeScale = 1f;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void TriggerCriticalImpulse(CombatFeedbackDirector director)
        {
            var registry = new CombatTargetRegistry();
            var target = new KillableTarget(1);
            registry.Register(target);
            var service = new CombatDamageService(registry);
            director.SetTargetAlivePredicate(id => id == target.RuntimeId && target.IsAlive);
            director.Bind(service);
            var request = WeaponDamageRequest.Create(1, WeaponId.HwandoFlyingBlade, target, 10, true,
                new Float2(0f, 0f), ContactPhase.Direct, 1);
            Assert.That(service.TryApply(request, out _), Is.True);
        }

        private static void Invoke(MethodInfo method, CombatFeedbackDirector director, Camera camera)
        {
            Assert.That(method, Is.Not.Null);
            method.Invoke(director, new object[] { camera });
        }

        private sealed class KillableTarget : ICombatTarget
        {
            private int health = 10;
            private readonly bool isBoss;
            public KillableTarget(int runtimeId, bool isBoss = false) { RuntimeId = runtimeId; this.isBoss = isBoss; }
            public int RuntimeId { get; }
            public bool IsAlive => health > 0;
            public int Health => health;
            public bool IsBoss => isBoss;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition => new Float2(0f, 0f);
            public PixelHitMask HurtMask => null;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Identity;
            public void ApplyResolvedDamage(int damage) { health = Math.Max(0, health - damage); }
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
