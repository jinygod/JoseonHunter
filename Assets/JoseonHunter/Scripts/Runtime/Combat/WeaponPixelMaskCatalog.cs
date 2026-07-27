using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JoseonHunter.Content.Weapons;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat
{
    /// <summary>Loads validated content once and owns the runtime-only immutable mask data.</summary>
    public sealed class WeaponPixelMaskCatalog
    {
        private readonly Dictionary<Texture2D, PixelHitMask> masksByTexture = new Dictionary<Texture2D, PixelHitMask>();
        private readonly Dictionary<WeaponDefinitionAsset, IReadOnlyList<PixelHitMask>> masksByDefinition = new Dictionary<WeaponDefinitionAsset, IReadOnlyList<PixelHitMask>>();

        public void Load(WeaponCatalogAsset catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var errors = catalog.ValidateLaunchRoster();
            if (errors.Count != 0) throw new InvalidOperationException("Cannot load an invalid weapon catalog: " + string.Join("; ", errors));

            masksByTexture.Clear();
            masksByDefinition.Clear();
            foreach (var definition in catalog.Definitions) LoadDefinition(definition);
        }

        public IReadOnlyList<PixelHitMask> GetMasks(WeaponDefinitionAsset definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!masksByDefinition.TryGetValue(definition, out var masks)) throw new InvalidOperationException("Weapon masks have not been loaded for this definition.");
            return masks;
        }

        private void LoadDefinition(WeaponDefinitionAsset definition)
        {
            var result = new List<PixelHitMask>();
            var sources = definition.BinaryMaskSourceReferences;
            for (var index = 0; index < sources.Count; index++)
            {
                var texture = sources[index];
                if (texture == null) throw new InvalidOperationException("Weapon mask source cannot be null.");
                if (!masksByTexture.TryGetValue(texture, out var mask))
                {
                    var sprite = index < definition.PresentationSprites.Count ? definition.PresentationSprites[index] : null;
                    var pivot = sprite != null ? sprite.pivot : new Vector2(texture.width * 0.5f, texture.height * 0.5f);
                    var ppu = sprite != null ? sprite.pixelsPerUnit : 32f;
                    mask = PixelHitMask.FromTexture(texture, pivot, ppu);
                    masksByTexture.Add(texture, mask);
                }
                result.Add(mask);
            }
            masksByDefinition.Add(definition, new ReadOnlyCollection<PixelHitMask>(result));
        }
    }
}
