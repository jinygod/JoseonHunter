using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class SupportUpgradeIconCatalog
    {
        private const string ResourceRoot = "UI/SupportIcons/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(
            StringComparer.Ordinal);

        public static Sprite Resolve(string id)
        {
            if (!IsKnownSupport(id)) return null;
            if (Cache.TryGetValue(id, out var sprite)) return sprite;

            sprite = Resources.Load<Sprite>(ResourceRoot + id);
            if (sprite != null) Cache[id] = sprite;
            return sprite;
        }

        private static bool IsKnownSupport(string id) => id == "talisman" || id == "boots" ||
            id == "warding_bell";
    }
}
