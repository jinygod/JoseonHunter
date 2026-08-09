using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public enum GameplayPickupVisualKind
    {
        Experience,
        Yeopjeon,
        Magnet
    }

    /// <summary>Builds or binds the runtime-owned visual shells used by gameplay.</summary>
    public sealed class GameplayVisualFactory
    {
        private readonly GameplayVisualPrefabLibrary library;
        private readonly CombatMotionLibrary motionLibrary;
        private readonly Sprite solidSprite;
        private readonly Action<string, string> warnOnce;

        public GameplayVisualFactory(
            GameplayVisualPrefabLibrary library,
            CombatMotionLibrary motionLibrary,
            Sprite solidSprite,
            Action<string, string> warnOnce)
        {
            this.library = library;
            this.motionLibrary = motionLibrary;
            this.solidSprite = solidSprite;
            this.warnOnce = warnOnce;
        }

        public GameObject BindAuthoredCombatant(
            GameObject root,
            string objectName,
            Sprite sprite,
            int sortingOrder,
            MotionWeight weight,
            float phaseOffset,
            out CombatantVisualRig visualRig,
            CombatantVisualRole role)
        {
            var view = root == null ? null : root.GetComponent<CombatantVisualView>();
            if (view == null || !view.HasRequiredBindings(role))
                throw new ArgumentException("Authored combatant visual is missing required bindings.", nameof(root));

            root.name = objectName;
            visualRig = CombatantVisualRig.Bind(
                root,
                view,
                sprite,
                sortingOrder,
                motionLibrary == null ? null : motionLibrary.Find(sprite),
                weight,
                phaseOffset,
                role);
            return root;
        }

        public GameObject CreateCombatant(
            string objectName,
            Sprite sprite,
            Vector2 position,
            int sortingOrder,
            Transform parent,
            MotionWeight weight,
            float phaseOffset,
            out CombatantVisualRig visualRig,
            CombatantVisualRole role)
        {
            var prefab = role == CombatantVisualRole.Player
                ? library?.PlayerVisual
                : library?.EnemyVisual;
            var prefabView = prefab == null ? null : prefab.GetComponent<CombatantVisualView>();
            if (prefabView != null && prefabView.HasRequiredBindings(role))
            {
                var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
                instance.name = objectName;
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.identity;
                visualRig = CombatantVisualRig.Bind(
                    instance,
                    instance.GetComponent<CombatantVisualView>(),
                    sprite,
                    sortingOrder,
                    motionLibrary == null ? null : motionLibrary.Find(sprite),
                    weight,
                    phaseOffset,
                    role);
                return instance;
            }

            Warn(
                role == CombatantVisualRole.Player ? "combatant:player" : "combatant:enemy",
                prefab == null
                    ? $"Gameplay visual prefab is missing for combatant role '{role}'. Using the legacy visual fallback."
                    : $"Gameplay visual prefab '{prefab.name}' has invalid CombatantVisualView bindings for role '{role}'. Using the legacy visual fallback.");

            var result = new GameObject(objectName);
            result.transform.SetParent(parent, false);
            result.transform.position = position;
            visualRig = CombatantVisualRig.Create(
                result,
                sprite,
                sortingOrder,
                motionLibrary == null ? null : motionLibrary.Find(sprite),
                weight,
                phaseOffset,
                role);
            return result;
        }

        public GameObject CreatePickup(
            GameplayPickupVisualKind kind,
            string objectName,
            Sprite sprite,
            Vector2 position,
            Transform parent,
            out PickupVisualView pickupView)
        {
            pickupView = null;
            var prefab = kind == GameplayPickupVisualKind.Experience
                ? library?.ExperiencePickup
                : kind == GameplayPickupVisualKind.Yeopjeon
                    ? library?.YeopjeonPickup
                    : library?.MagnetPickup;
            var prefabView = prefab == null ? null : prefab.GetComponent<PickupVisualView>();
            var valid = prefabView != null && prefabView.HasRequiredBindings &&
                        (kind != GameplayPickupVisualKind.Experience || prefabView.TrailRenderer != null);
            if (valid)
            {
                var result = UnityEngine.Object.Instantiate(prefab, parent, false);
                result.name = objectName;
                result.transform.position = position;
                result.transform.rotation = Quaternion.identity;
                pickupView = result.GetComponent<PickupVisualView>();
                pickupView.VisualRenderer.sprite = sprite;
                pickupView.VisualRenderer.sortingOrder = 6;
                return result;
            }

            Warn(
                $"pickup:{kind}",
                prefab == null
                    ? $"Gameplay visual prefab is missing for pickup '{kind}'. Using the legacy visual fallback."
                    : $"Gameplay visual prefab '{prefab.name}' has invalid PickupVisualView bindings. Using the legacy visual fallback.");
            return CreateSpriteObject(objectName, sprite, position, 6, parent);
        }

        public Transform CreateHealthBar(
            Transform owner,
            Vector3 fallbackLocalPosition,
            float fallbackLocalScale,
            bool overrideAuthoredAnchor = false)
        {
            var combatantView = owner == null ? null : owner.GetComponent<CombatantVisualView>();
            var anchor = combatantView != null && combatantView.HealthBarAnchor != null
                ? combatantView.HealthBarAnchor
                : owner;
            if (anchor != owner && overrideAuthoredAnchor)
            {
                anchor.localPosition = fallbackLocalPosition;
                anchor.localScale = Vector3.one * fallbackLocalScale;
            }

            var authoredBar = FindValidDirectBar(anchor);
            if (authoredBar != null)
            {
                authoredBar.Prepare(solidSprite);
                authoredBar.SetNormalizedValue(1f);
                return authoredBar.Fill;
            }

            return CreateWorldBar(
                "Health Bar",
                library?.WorldHealthBar,
                anchor,
                anchor == owner ? fallbackLocalPosition : Vector3.zero,
                anchor == owner ? fallbackLocalScale : 1f,
                anchor != owner,
                new Vector3(2.2f, .24f, 1f),
                new Vector3(2f, .14f, 1f),
                new Color(.16f, .12f, .12f, .92f),
                new Color(.24f, .86f, .34f));
        }

        public Transform CreateShieldBar(
            Transform owner,
            Vector3 fallbackLocalPosition,
            float fallbackLocalScale)
        {
            var combatantView = owner == null ? null : owner.GetComponent<CombatantVisualView>();
            var anchor = combatantView != null && combatantView.ShieldBarAnchor != null
                ? combatantView.ShieldBarAnchor
                : owner;
            return CreateWorldBar(
                "Shield Guard Bar",
                library?.WorldShieldBar,
                anchor,
                anchor == owner ? fallbackLocalPosition : Vector3.zero,
                anchor == owner ? fallbackLocalScale : 1f,
                anchor != owner,
                new Vector3(2.2f, .20f, 1f),
                new Vector3(2f, .10f, 1f),
                new Color(.12f, .09f, .06f, .94f),
                new Color(.72f, .45f, .14f, 1f));
        }

        public static void UpdateBarFill(Transform fill, float normalizedValue, float width, float height)
        {
            if (fill == null) return;

            var authoredView = fill.GetComponentInParent<WorldBarView>();
            if (authoredView != null)
            {
                authoredView.SetNormalizedValue(normalizedValue);
                return;
            }

            var ratio = Mathf.Clamp01(normalizedValue);
            fill.localScale = new Vector3(width * ratio, height, 1f);
            fill.localPosition = new Vector3(-width * .5f + width * ratio * .5f, 0f, -.01f);
        }

        private Transform CreateWorldBar(
            string runtimeName,
            GameObject prefab,
            Transform parent,
            Vector3 localPosition,
            float localScale,
            bool preservePrefabRootTransform,
            Vector3 fallbackBackgroundScale,
            Vector3 fallbackFillScale,
            Color fallbackBackgroundColor,
            Color fallbackFillColor)
        {
            var prefabView = prefab == null ? null : prefab.GetComponent<WorldBarView>();
            if (prefabView != null && prefabView.HasRequiredBindings)
            {
                var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
                instance.name = runtimeName;
                if (!preservePrefabRootTransform)
                {
                    instance.transform.localPosition = localPosition;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one * localScale;
                }
                var view = instance.GetComponent<WorldBarView>();
                view.Prepare(solidSprite);
                view.SetNormalizedValue(1f);
                return view.Fill;
            }

            Warn(
                $"bar:{runtimeName}",
                prefab == null
                    ? $"Gameplay visual prefab is missing for '{runtimeName}'. Using the legacy visual fallback."
                    : $"Gameplay visual prefab '{prefab.name}' has invalid WorldBarView bindings. Using the legacy visual fallback.");

            var root = new GameObject(runtimeName).transform;
            root.SetParent(parent, false);
            root.localPosition = localPosition;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * localScale;

            var background = new GameObject("Background");
            background.transform.SetParent(root, false);
            background.transform.localScale = fallbackBackgroundScale;
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = solidSprite;
            backgroundRenderer.color = fallbackBackgroundColor;
            backgroundRenderer.sortingOrder = 20;

            var fill = new GameObject("Fill").transform;
            fill.SetParent(root, false);
            fill.localScale = fallbackFillScale;
            var fillRenderer = fill.gameObject.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = solidSprite;
            fillRenderer.color = fallbackFillColor;
            fillRenderer.sortingOrder = 21;
            return fill;
        }

        private static WorldBarView FindValidDirectBar(Transform anchor)
        {
            if (anchor == null) return null;
            var bars = anchor.GetComponentsInChildren<WorldBarView>(true);
            for (var index = 0; index < bars.Length; index++)
                if (bars[index].transform.parent == anchor && bars[index].HasRequiredBindings)
                    return bars[index];
            return null;
        }

        private static GameObject CreateSpriteObject(
            string objectName,
            Sprite sprite,
            Vector2 position,
            int sortingOrder,
            Transform parent)
        {
            var result = new GameObject(objectName);
            result.transform.SetParent(parent, false);
            result.transform.position = position;
            var renderer = result.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return result;
        }

        private void Warn(string key, string message) => warnOnce?.Invoke(key, message);
    }
}
