using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Combat
{
    public sealed class AttackInstance
    {
        private readonly Dictionary<(int targetId, ContactPhase phase), float> hits = new Dictionary<(int targetId, ContactPhase phase), float>();
        private readonly Dictionary<(int targetId, ContactPhase phase), float> timedHits = new Dictionary<(int targetId, ContactPhase phase), float>();

        public AttackInstance(int instanceId, RepeatHitPolicy repeatHitPolicy, float repeatInterval)
        {
            if (repeatInterval < 0f) throw new ArgumentOutOfRangeException(nameof(repeatInterval));
            InstanceId = instanceId;
            RepeatHitPolicy = repeatHitPolicy;
            RepeatInterval = repeatInterval;
        }

        public int InstanceId { get; }
        public RepeatHitPolicy RepeatHitPolicy { get; }
        public float RepeatInterval { get; }

        public bool TryRecordHit(int targetId, ContactPhase phase, float time)
        {
            var key = (targetId, phase);
            switch (RepeatHitPolicy)
            {
                case RepeatHitPolicy.OncePerInstance:
                    if (ContainsTarget(targetId)) return false;
                    hits.Add(key, time);
                    return true;
                case RepeatHitPolicy.OncePerPhase:
                    if (hits.ContainsKey(key)) return false;
                    hits.Add(key, time);
                    return true;
                case RepeatHitPolicy.TimedTicks:
                case RepeatHitPolicy.BoundaryReentry:
                    if (timedHits.TryGetValue(key, out var lastHit) && time - lastHit < RepeatInterval) return false;
                    timedHits[key] = time;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Reset()
        {
            hits.Clear();
            timedHits.Clear();
        }

        private bool ContainsTarget(int targetId)
        {
            foreach (var hit in hits)
            {
                if (hit.Key.targetId == targetId) return true;
            }

            return false;
        }
    }
}
