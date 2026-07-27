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
            }

            pool.Unbind();
            boundService = service;
            if (boundService == null) return;

            pool.SetBossTargetPredicate(controller.IsBossCombatTarget);
            pool.Bind(boundService);
        }

        private void OnDestroy()
        {
            if (pool != null) pool.Unbind();
            boundService = null;
        }
    }
}
