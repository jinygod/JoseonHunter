using System;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;

namespace JoseonHunter.Presentation.Combat
{
    /// <summary>Presentation-owned bridge from the first playable combat event source to pooled damage numbers.</summary>
    public sealed class FirstPlayableDamageNumberBootstrap : MonoBehaviour
    {
        private FirstPlayableController controller;
        private DamageNumberPool pool;
        private CombatFeedbackDirector feedbackDirector;
        private CombatDamageService boundService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindObjectOfType<FirstPlayableDamageNumberBootstrap>() != null) return;
            var bootstrap = new GameObject("First Playable Damage Number Bootstrap");
            DontDestroyOnLoad(bootstrap);
            bootstrap.AddComponent<FirstPlayableDamageNumberBootstrap>();
        }

        private void Update()
        {
            if (controller == null) controller = FindObjectOfType<FirstPlayableController>();
            var service = controller == null ? null : controller.CombatDamageService;
            if (ReferenceEquals(service, boundService)) return;

            if (pool == null)
            {
                var poolObject = new GameObject("Damage Number Pool");
                poolObject.transform.SetParent(transform, false);
                pool = poolObject.AddComponent<DamageNumberPool>();
                feedbackDirector = poolObject.AddComponent<CombatFeedbackDirector>();
            }

            pool.Unbind();
            feedbackDirector.Unbind();
            boundService = service;
            if (boundService == null) return;

            pool.SetBossTargetPredicate(controller.IsBossCombatTarget);
            pool.Bind(boundService);
            feedbackDirector.SetBossTargetPredicate(controller.IsBossCombatTarget);
            feedbackDirector.SetTargetAlivePredicate(controller.IsCombatTargetAlive);
            feedbackDirector.Bind(boundService);
        }

        private void OnDestroy()
        {
            if (pool != null) pool.Unbind();
            if (feedbackDirector != null) feedbackDirector.Unbind();
            boundService = null;
        }
    }
}
