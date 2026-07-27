using System;
using System.Collections.Generic;

namespace JoseonHunter.Runtime.Combat
{
    public sealed class CombatTargetRegistry
    {
        private readonly Dictionary<int, ICombatTarget> targets = new Dictionary<int, ICombatTarget>();

        public bool Register(ICombatTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (targets.TryGetValue(target.RuntimeId, out var registered)) return ReferenceEquals(registered, target);
            targets.Add(target.RuntimeId, target);
            return true;
        }

        public bool Unregister(ICombatTarget target)
        {
            if (target == null) return false;
            return targets.TryGetValue(target.RuntimeId, out var registered) && ReferenceEquals(registered, target) && targets.Remove(target.RuntimeId);
        }

        public bool Contains(ICombatTarget target) => target != null && targets.TryGetValue(target.RuntimeId, out var registered) && ReferenceEquals(registered, target);
        public bool TryGet(int runtimeId, out ICombatTarget target) => targets.TryGetValue(runtimeId, out target);

        /// <summary>Copies a stable view for one simulation tick without forcing per-frame allocations.</summary>
        public void CopyTo(List<ICombatTarget> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            destination.AddRange(targets.Values);
        }
    }
}
