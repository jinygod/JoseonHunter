using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyProjectilePool : MonoBehaviour
    {
        private sealed class Slot
        {
            public GameObject Object;
            public Vector2 Velocity;
            public float Remaining;
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
            if (slots.Length != 0) throw new InvalidOperationException("Projectile pool is already configured.");
            slots = new Slot[capacity];
            for (var index = 0; index < slots.Length; index++)
            {
                var projectile = new GameObject($"Stage Projectile {index + 1}");
                projectile.transform.SetParent(transform, false);
                var renderer = projectile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 7;
                projectile.SetActive(false);
                slots[index] = new Slot { Object = projectile };
            }
        }

        public void Launch(
            Vector2 position,
            Vector2 velocity,
            float lifetimeSeconds,
            float damage,
            Color color)
        {
            if (lifetimeSeconds <= 0f || float.IsNaN(lifetimeSeconds) || float.IsInfinity(lifetimeSeconds))
                throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds));
            var slot = Acquire();
            slot.Object.transform.position = position;
            slot.Object.transform.localScale = Vector3.one * .34f;
            slot.Object.GetComponent<SpriteRenderer>().color = color;
            slot.Velocity = velocity;
            slot.Remaining = lifetimeSeconds;
            slot.Damage = Mathf.Max(0f, damage);
            slot.Ordinal = ++nextOrdinal;
            slot.Object.SetActive(true);
        }

        public void Tick(float deltaSeconds, Vector2 playerPosition, float hitRadius, Action<float> onHit)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            var hitRadiusSquared = Mathf.Max(0f, hitRadius) * Mathf.Max(0f, hitRadius);
            for (var index = 0; index < slots.Length; index++)
            {
                var slot = slots[index];
                if (!slot.Object.activeSelf) continue;
                slot.Remaining -= deltaSeconds;
                slot.Object.transform.position += (Vector3)(slot.Velocity * deltaSeconds);
                if (((Vector2)slot.Object.transform.position - playerPosition).sqrMagnitude <= hitRadiusSquared)
                {
                    onHit?.Invoke(slot.Damage);
                    slot.Object.SetActive(false);
                }
                else if (slot.Remaining <= 0f)
                {
                    slot.Object.SetActive(false);
                }
            }
        }

        public void Clear()
        {
            for (var index = 0; index < slots.Length; index++)
                slots[index].Object.SetActive(false);
        }

        private Slot Acquire()
        {
            if (slots.Length == 0) throw new InvalidOperationException("Projectile pool is not configured.");
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
