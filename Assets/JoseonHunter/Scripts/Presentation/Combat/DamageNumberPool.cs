using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Combat;
using UnityEngine;

namespace JoseonHunter.Presentation.Combat
{
    public sealed class DamageNumberPool : MonoBehaviour
    {
        public const int PrewarmCount = 48;
        public const int MaximumCount = 96;

        private static readonly Color Ivory = new Color(0.94f, 0.91f, 0.80f, 1f);
        private static readonly Color Ember = new Color(0.93f, 0.39f, 0.23f, 1f);
        private static readonly Color Cyan = new Color(0.34f, 0.86f, 0.94f, 1f);
        private static readonly Color Violet = new Color(0.70f, 0.48f, 0.94f, 1f);
        private static readonly Color Gold = new Color(0.90f, 0.70f, 0.28f, 1f);

        private readonly DamageNumberAccumulator accumulator = new DamageNumberAccumulator(0.25f);
        private readonly Stack<DamageNumberPresenter> available = new Stack<DamageNumberPresenter>();
        private readonly HashSet<DamageNumberPresenter> active = new HashSet<DamageNumberPresenter>();
        private CombatDamageService damageService;
        private Func<int, bool> isBossTarget;
        private int totalInstances;

        public int ActiveCount => active.Count;
        public int TotalInstances => totalInstances;

        private void Awake()
        {
            Prewarm();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            var displays = accumulator.FlushReady(Time.time);
            foreach (var display in displays) Show(display);
        }

        public void Bind(CombatDamageService service)
        {
            if (ReferenceEquals(damageService, service)) return;
            Unsubscribe();
            damageService = service ?? throw new ArgumentNullException(nameof(service));
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            damageService = null;
            Clear();
        }

        public void SetBossTargetPredicate(Func<int, bool> predicate)
        {
            isBossTarget = predicate;
        }

        public void Clear()
        {
            accumulator.Clear();
            var activePresenters = new List<DamageNumberPresenter>(active);
            foreach (var presenter in activePresenters) Release(presenter);
        }

        private void Prewarm()
        {
            while (totalInstances < PrewarmCount) available.Push(CreatePresenter());
        }

        private void Subscribe()
        {
            if (isActiveAndEnabled && damageService != null) damageService.DamageConfirmed += OnDamageConfirmed;
        }

        private void Unsubscribe()
        {
            if (damageService != null) damageService.DamageConfirmed -= OnDamageConfirmed;
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent confirmed)
        {
            accumulator.Add(confirmed, Time.time);
        }

        private void Show(DamageNumberDisplay display)
        {
            var presenter = Rent();
            if (presenter == null) return;

            active.Add(presenter);
            presenter.Play(display, isBossTarget != null && isBossTarget(display.TargetRuntimeId), AccentFor(display.WeaponId), Release);
        }

        private DamageNumberPresenter Rent()
        {
            if (available.Count > 0) return available.Pop();
            return totalInstances < MaximumCount ? CreatePresenter() : null;
        }

        private DamageNumberPresenter CreatePresenter()
        {
            var numberObject = new GameObject("Damage Number", typeof(TMPro.TextMeshPro), typeof(DamageNumberPresenter));
            numberObject.transform.SetParent(transform, false);
            numberObject.SetActive(false);
            totalInstances++;
            return numberObject.GetComponent<DamageNumberPresenter>();
        }

        private void Release(DamageNumberPresenter presenter)
        {
            if (presenter == null || !active.Remove(presenter)) return;
            presenter.ResetState();
            presenter.gameObject.SetActive(false);
            available.Push(presenter);
        }

        private static Color AccentFor(WeaponId weaponId)
        {
            if (weaponId.Equals(WeaponId.SingijeonVolley)) return Ember;
            if (weaponId.Equals(WeaponId.ThunderCrashBomb) || weaponId.Equals(WeaponId.WindThunderFan)) return Violet;
            if (weaponId.Equals(WeaponId.FrostFlask)) return Cyan;
            if (weaponId.Equals(WeaponId.TalismanThrow) || weaponId.Equals(WeaponId.JangseungWard)) return Gold;
            return Ivory;
        }
    }
}
