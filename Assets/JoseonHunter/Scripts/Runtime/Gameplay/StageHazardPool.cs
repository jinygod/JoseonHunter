using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StageHazardPool : MonoBehaviour
    {
        private sealed class Slot
        {
            public GameObject Object;
            public float Remaining;
            public float Radius;
            public float Cadence;
            public float NextDamage;
            public float Damage;
            public int Ordinal;
        }

        private Slot[] slots = Array.Empty<Slot>();
        private int nextOrdinal;

        public int Capacity => slots.Length;
        public int ActiveCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < slots.Length; index++)
                    if (slots[index].Object.activeSelf) count++;
                return count;
            }
        }

        public void Configure(int capacity, Sprite sprite)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (slots.Length != 0) throw new InvalidOperationException("Hazard pool is already configured.");
            slots = new Slot[capacity];
            for (var index = 0; index < slots.Length; index++)
            {
                var hazard = new GameObject($"Stage Hazard {index + 1}");
                hazard.transform.SetParent(transform, false);
                var renderer = hazard.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 4;
                hazard.SetActive(false);
                slots[index] = new Slot { Object = hazard };
            }
        }

        public void Activate(
            Vector2 center,
            float radius,
            float lifetimeSeconds,
            float damageCadenceSeconds,
            float damage,
            Color color)
        {
            if (radius <= 0f || lifetimeSeconds <= 0f || damageCadenceSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            var slot = Acquire();
            slot.Object.transform.position = center;
            slot.Object.transform.localScale = Vector3.one * (radius * 2f);
            slot.Object.GetComponent<SpriteRenderer>().color = color;
            slot.Radius = radius;
            slot.Remaining = lifetimeSeconds;
            slot.Cadence = damageCadenceSeconds;
            slot.NextDamage = 0f;
            slot.Damage = Mathf.Max(0f, damage);
            slot.Ordinal = ++nextOrdinal;
            slot.Object.SetActive(true);
        }

        public void Tick(float deltaSeconds, Vector2 playerPosition, Action<float> onDamage)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            for (var index = 0; index < slots.Length; index++)
            {
                var slot = slots[index];
                if (!slot.Object.activeSelf) continue;
                slot.Remaining -= deltaSeconds;
                if (slot.Remaining <= 0f)
                {
                    slot.Object.SetActive(false);
                    continue;
                }
                slot.NextDamage -= deltaSeconds;
                var delta = (Vector2)slot.Object.transform.position - playerPosition;
                if (delta.sqrMagnitude > slot.Radius * slot.Radius || slot.NextDamage > 0f) continue;
                onDamage?.Invoke(slot.Damage);
                slot.NextDamage += slot.Cadence;
            }
        }

        public void Clear()
        {
            for (var index = 0; index < slots.Length; index++)
                slots[index].Object.SetActive(false);
        }

        private Slot Acquire()
        {
            if (slots.Length == 0) throw new InvalidOperationException("Hazard pool is not configured.");
            var oldest = slots[0];
            for (var index = 0; index < slots.Length; index++)
            {
                var candidate = slots[index];
                if (!candidate.Object.activeSelf) return candidate;
                if (candidate.Ordinal < oldest.Ordinal) oldest = candidate;
            }
            return oldest;
        }
    }
}
