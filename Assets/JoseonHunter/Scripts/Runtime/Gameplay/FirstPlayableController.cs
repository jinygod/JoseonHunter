using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class FirstPlayableController : MonoBehaviour
    {
        private static readonly CombatVisualScaleProfile VisualScale =
            CombatVisualScaleProfile.MobileLandscape;

        [Header("Static sprite assets")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private Sprite enemySpriteAlt;
        [SerializeField] private Sprite[] enemySprites;
        [SerializeField] private Sprite eliteSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite experienceSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite treasureChestSprite;
        [SerializeField] private Sprite battlefieldTilePrimary;
        [SerializeField] private Sprite battlefieldTileAlternate;
        [SerializeField] private Sprite[] battlefieldDecals;
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        [SerializeField] private CombatMotionLibrary motionLibrary;

        private readonly List<EnemyState> enemies = new List<EnemyState>();
        private readonly List<PickupState> pickups = new List<PickupState>();
        private readonly List<Vector2> trail = new List<Vector2>();
        private readonly List<string> upgradeOffers = new List<string>();
        private readonly List<UpgradeOffer> upgradeOfferData = new List<UpgradeOffer>();
        private readonly Dictionary<string, int> weaponLevels = new Dictionary<string, int>();
        private readonly Dictionary<string, int> supportLevels = new Dictionary<string, int>();
        private readonly HashSet<string> unlockedUpgradeIds = new HashSet<string>();
        private readonly HashSet<string> acquiredEvolutionIds = new HashSet<string>();
        private readonly WeaponEvolutionState evolutionState = new WeaponEvolutionState();
        private readonly WeaponRunAffixState weaponAffixes = new WeaponRunAffixState();
        private int affixRollOrdinal;
#if UNITY_INCLUDE_TESTS
        private Func<WeaponId, int, int, int, IAffixRandom> affixRandomFactoryForTests;
#endif
        private readonly PixelHitMask prototypeCombatMask = new PixelHitMask(1, 1, Vector2.zero, 1f, new[] { 1u });
        private readonly Dictionary<Sprite, PixelHitMask> hurtMasksBySprite = new Dictionary<Sprite, PixelHitMask>();

        private Camera gameplayCamera;
        private Transform flatField;
        private Transform runtimeObjects;
        private GameObject player;
        private SpriteRenderer playerRenderer;
        private CombatantVisualRig playerVisualRig;
        private Transform playerHealthFill;
        private LineRenderer geumjulRenderer;
        private CombatTargetRegistry combatTargets;
        private CombatDamageService combatDamageService;
        private WeaponRuntimeController weaponRuntime;
        private readonly WeaponPixelMaskCatalog weaponMasks = new WeaponPixelMaskCatalog();
        private readonly List<WeaponId> registeredWeaponIds = new List<WeaponId>();
        private Texture2D solidTexture;
        private Sprite solidSprite;
        private Vector2 touchStart;
        private Vector2 movement;
        private Vector3 cameraFollowVelocity;
        private float elapsed;
        private float playerHealth;
        private float playerMaxHealth;
        private float moveSpeed;
        private float pickupRadius;
        private float geumjulDamage;
        private float contactInvulnerability;
        private float spawnTimer;
        private float chestSpawnTimer;
        private float trailTimer;
        private float sealCooldown;
        private float magnetMessageTimer;
        private int experience;
        private int experienceToNext;
        private int level;
        private int coins;
        private int kills;
        private int nextCombatTargetRuntimeId;
        private int pendingUpgradeCount;
        private bool bossSpawned;
        private bool bossAlive;
        private bool upgradeOpen;
        private bool awaitingUpgradePresentationClose;
        private bool runEnded;
        private bool victory;
        private StagePacingTimeline stageTimeline;
        private int processedStageMilestones;
        private bool finalBossWarning;
        private string waveAnnouncement = string.Empty;
        private float waveAnnouncementTimer;
        private int waveAnnouncementIntensity;

        private const float TestDuration = 60f;

        /// <summary>Read-only combat event source for presentation components.</summary>
        public CombatDamageService CombatDamageService => combatDamageService;
        public WeaponRuntimeController WeaponRuntime => weaponRuntime;
        public IReadOnlyList<WeaponId> RegisteredWeaponIds => registeredWeaponIds;
        public FirstPlayableUiState UiState => BuildUiState();
        public bool IsUpgradeOpen => upgradeOpen;
        public IReadOnlyCollection<string> AcquiredEvolutionIds => acquiredEvolutionIds;
        public event Action<UpgradeChoiceState> UpgradeOpened;
        public event Action<ProgressionRewardEvent> UpgradeChosen;
        public event Action RunReset;

#if UNITY_INCLUDE_TESTS
        public IReadOnlyList<UpgradeOffer> CurrentOffers => upgradeOfferData;
        public int AppliedUpgradeCount { get; private set; }
        public int WeaponRebuildCountForTests { get; private set; }
        public int MidBossSpawnCountForTests { get; private set; }
        public int FinalBossSpawnCountForTests { get; private set; }
        public bool RunEndedForTests => runEnded;
        public bool VictoryForTests => victory;
        public void AdvanceStageForTests(float previousElapsed, float currentElapsed)
        {
            elapsed = Mathf.Clamp(currentElapsed, 0f, TestDuration);
            ProcessStageMilestones(previousElapsed, elapsed);
        }
        public void DefeatMidBossesForTests()
        {
            var targets = enemies.FindAll(candidate => candidate.IsMidBoss);
            foreach (var target in targets) ApplyEnemyDamage(target, target.MaximumHealth + 1f);
        }
        public void DefeatFinalBossForTests()
        {
            var target = enemies.Find(candidate => candidate.IsBoss);
            if (target != null) ApplyEnemyDamage(target, target.MaximumHealth + 1f);
        }
        public void OpenUpgradeForTests() => OpenUpgrade();
        public void SetUpgradeOffersForTests(params UpgradeOffer[] offers)
        {
            upgradeOpen = true;
            upgradeOfferData.Clear();
            upgradeOfferData.AddRange(offers);
        }
        /// <summary>Publishes forced offers atomically so tests exercise the same controller and visible-card identities.</summary>
        public void OpenUpgradeOffersForTests(params UpgradeOffer[] offers)
        {
            upgradeOpen = true;
            upgradeOffers.Clear();
            upgradeOfferData.Clear();
            if (offers != null) upgradeOfferData.AddRange(offers);
            var choices = new List<UpgradeChoiceView>(upgradeOfferData.Count);
            foreach (var offer in upgradeOfferData)
            {
                upgradeOffers.Add(FormatUpgradeOffer(offer));
                choices.Add(BuildUpgradeChoiceView(offer));
            }
            UpgradeOpened?.Invoke(new UpgradeChoiceState(level, choices));
        }
        public bool RegisterCombatTargetForTests(ICombatTarget target) => target != null && combatTargets != null && combatTargets.Register(target);
        public void AddExperienceForTests(int amount) => AddExperience(amount);
        public void ResetRunForTests() => ResetRun();
        public void SetWeaponLevelForTests(WeaponId weaponId, int weaponLevel)
        {
            weaponLevels[weaponId.Value] = weaponLevel;
            RebuildWeaponExecutorsForLevel();
        }
        public int WeaponLevelForTests(WeaponId weaponId) => weaponLevels[weaponId.Value];
        public void SetAffixRandomFactoryForTests(Func<WeaponId, int, int, int, IAffixRandom> factory) => affixRandomFactoryForTests = factory;
        public WeaponRunAffixProfile AffixProfileForTests(WeaponId weaponId) => weaponAffixes.TryProfileFor(weaponId, out var profile) ? new WeaponRunAffixProfile(profile.GeneralRolls, profile.PotentialIds) : null;
        public WeaponAffixRollResult RollWeaponAffixForTests(WeaponId weaponId) => RollWeaponAffix(weaponId);
        public void AcquireEvolutionForTests(string evolutionId)
        {
            if (!WeaponEvolutionCatalog.TryGet(evolutionId, out var evolution)) return;
            acquiredEvolutionIds.Add(evolutionId);
            evolutionState.SetEvolved(evolution.RequiredWeaponId);
            RebuildWeaponExecutorsForLevel();
        }
        public void UnlockEvolutionForTests(string evolutionId) => unlockedUpgradeIds.Add(evolutionId);
        public ICombatTarget SpawnEnemyForTests(Vector2 position)
        {
            SpawnEnemy(false);
            var target = enemies[enemies.Count - 1].CombatTarget;
            enemies[enemies.Count - 1].Object.transform.position = position;
            return target;
        }
#endif

        public bool IsCombatTargetAlive(int runtimeId) =>
            combatTargets != null && combatTargets.TryGet(runtimeId, out var target) && target.IsAlive;

        public bool IsBossCombatTarget(int runtimeId)
        {
            var enemy = enemies.Find(candidate => candidate.CombatTarget != null && candidate.CombatTarget.RuntimeId == runtimeId);
            return enemy != null && (enemy.IsBoss || enemy.IsMidBoss);
        }

        public bool HasJangseungWardMark(int runtimeId)
        {
            var enemy = enemies.Find(candidate => candidate.CombatTarget != null && candidate.CombatTarget.RuntimeId == runtimeId);
            return enemy != null && enemy.HasJangseungWard;
        }

        private sealed class EnemyState
        {
            public GameObject Object;
            public SpriteRenderer Renderer;
            public CombatantVisualRig VisualRig;
            public MotionWeight MotionWeight;
            public float Health;
            public float MaximumHealth;
            public float Speed;
            public float ContactDamage;
            public Transform HealthFill;
            public float NextContactTime;
            public bool IsBoss;
            public bool IsElite;
            public bool IsMidBoss;
            public int MidBossTier;
            public bool IsTreasure;
            public int ExperienceValue = 1;
            public ICombatTarget CombatTarget;
            private readonly Dictionary<int, float> frostSlowSources = new Dictionary<int, float>();
            private readonly Dictionary<int, float> freezeSources = new Dictionary<int, float>();
            private readonly Dictionary<int, float> jangseungWardSources = new Dictionary<int, float>();
            private readonly List<int> statusSourceScratch = new List<int>();
            private float slowDecayRemaining;
            private float slowDecayStartMultiplier = 1f;

            public void ApplyFrostSlow(int sourceId, float strength)
            {
                frostSlowSources[sourceId] = Mathf.Clamp01(strength);
                slowDecayRemaining = 0f;
            }

            public void RemoveFrostSlow(int sourceId, float decaySeconds)
            {
                var previousMultiplier = SlowMultiplier();
                if (!frostSlowSources.Remove(sourceId) || frostSlowSources.Count != 0) return;
                slowDecayStartMultiplier = previousMultiplier;
                slowDecayRemaining = Mathf.Max(0f, decaySeconds);
            }

            public void ApplyFreeze(int sourceId, float durationSeconds) => freezeSources[sourceId] = Mathf.Max(freezeSources.TryGetValue(sourceId, out var remaining) ? remaining : 0f, Mathf.Max(0f, durationSeconds));

            public void ApplyJangseungWard(int sourceId, float strength) => jangseungWardSources[sourceId] = Mathf.Clamp01(strength);

            public void RemoveJangseungWard(int sourceId) => jangseungWardSources.Remove(sourceId);

            public bool HasJangseungWard => jangseungWardSources.Count > 0;

            public void TickStatuses(float delta)
            {
                statusSourceScratch.Clear();
                foreach (var source in freezeSources) statusSourceScratch.Add(source.Key);
                for (var index = statusSourceScratch.Count - 1; index >= 0; index--)
                {
                    var sourceId = statusSourceScratch[index];
                    var remaining = freezeSources[sourceId] - delta;
                    if (remaining <= 0f) freezeSources.Remove(sourceId);
                    else freezeSources[sourceId] = remaining;
                }
                slowDecayRemaining = Mathf.Max(0f, slowDecayRemaining - delta);
            }

            public float MovementMultiplier
            {
                get
                {
                    if (freezeSources.Count > 0) return 0f;
                    if (frostSlowSources.Count > 0) return SlowMultiplier();
                    if (jangseungWardSources.Count > 0) return WardMultiplier();
                    return slowDecayRemaining <= 0f ? 1f : Mathf.Lerp(1f, slowDecayStartMultiplier, slowDecayRemaining / 0.35f);
                }
            }

            private float SlowMultiplier()
            {
                var multiplier = 1f;
                foreach (var source in frostSlowSources) multiplier = Mathf.Min(multiplier, source.Value);
                return multiplier;
            }

            private float WardMultiplier()
            {
                var multiplier = 1f;
                foreach (var source in jangseungWardSources) multiplier = Mathf.Min(multiplier, 1f - source.Value);
                return multiplier;
            }
        }

        private sealed class PrototypeCombatTarget : ICombatTarget, IFrostStatusTarget, IJangseungWardStatusTarget
        {
            private readonly FirstPlayableController owner;
            private readonly EnemyState state;
            private readonly int runtimeId;

            public PrototypeCombatTarget(FirstPlayableController owner, EnemyState state, int runtimeId)
            {
                this.owner = owner;
                this.state = state;
                this.runtimeId = runtimeId;
            }

            public int RuntimeId => runtimeId;
            public bool IsAlive => state.Object != null && state.Health > 0f;
            public int Health => Mathf.CeilToInt(Mathf.Max(0f, state.Health));
            public bool IsBoss => state.IsBoss || state.IsMidBoss;
            public bool IsElite => state.IsElite;
            public float ThreatScore => state.IsBoss ? 100f : state.IsMidBoss ? 70f : (state.IsElite ? 25f : 0f);
            public Float2 WorldPosition
            {
                get
                {
                    var position = state.Object == null ? Vector2.zero : (Vector2)state.Object.transform.position;
                    return new Float2(position.x, position.y);
                }
            }
            public PixelHitMask HurtMask => owner.MaskFor(state.Renderer);
            public PixelMaskTransform HurtMaskTransform => state.VisualRig != null
                ? state.VisualRig.CollisionTransform(WorldPosition)
                : owner.TransformFor(state.Renderer, WorldPosition);
            public void ApplyResolvedDamage(int damage) => owner.ApplyEnemyDamage(state, damage);
            public void ApplyKnockback(Float2 direction, float force)
            {
                if (state.Object == null || force <= 0f || float.IsNaN(force) || float.IsInfinity(force) ||
                    float.IsNaN(direction.X) || float.IsInfinity(direction.X) || float.IsNaN(direction.Y) || float.IsInfinity(direction.Y)) return;
                var magnitudeSquared = direction.X * direction.X + direction.Y * direction.Y;
                if (magnitudeSquared <= 0.000001f || float.IsNaN(magnitudeSquared) || float.IsInfinity(magnitudeSquared)) return;
                var inverseMagnitude = 1f / Mathf.Sqrt(magnitudeSquared);
                var displacement = Mathf.Min(force, 4f);
                var position = state.Object.transform.position;
                state.Object.transform.position = new Vector3(
                    position.x + direction.X * inverseMagnitude * displacement,
                    position.y + direction.Y * inverseMagnitude * displacement,
                    position.z);
            }
            public void ApplyFrostSlow(int sourceId, float strength) => state.ApplyFrostSlow(sourceId, strength);
            public void RemoveFrostSlow(int sourceId, float decaySeconds) => state.RemoveFrostSlow(sourceId, decaySeconds);
            public void ApplyFreeze(int sourceId, float durationSeconds) => state.ApplyFreeze(sourceId, durationSeconds);
            public void ApplyJangseungWard(int sourceId, float strength) => state.ApplyJangseungWard(sourceId, strength);
            public void RemoveJangseungWard(int sourceId) => state.RemoveJangseungWard(sourceId);
        }

        private enum PickupKind
        {
            Experience,
            Yeopjeon,
            Magnet
        }

        private sealed class PickupState
        {
            public GameObject Object;
            public PickupKind Kind;
            public int Value;
            public bool ForceCollect;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            SetupCamera();
            CreateSharedSprite();
            CreateField();
            ResetRun();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            weaponRuntime?.Dispose();
            weaponRuntime = null;
            if (solidSprite != null)
            {
                Destroy(solidSprite);
            }

            if (solidTexture != null)
            {
                Destroy(solidTexture);
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (runEnded)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    ResetRun();
                }

                return;
            }

            if (upgradeOpen)
            {
                return;
            }

            var delta = Time.deltaTime;
            var previousElapsed = elapsed;
            elapsed = Mathf.Min(TestDuration, elapsed + delta);
            contactInvulnerability = Mathf.Max(0f, contactInvulnerability - delta);
            sealCooldown = Mathf.Max(0f, sealCooldown - delta);
            magnetMessageTimer = Mathf.Max(0f, magnetMessageTimer - delta);
            waveAnnouncementTimer = Mathf.Max(0f, waveAnnouncementTimer - delta);
            ProcessStageMilestones(previousElapsed, elapsed);

            ReadMovement();
            UpdatePlayer(delta);
            UpdateSpawning(delta);
            UpdateTreasureSpawning(delta);
            UpdateEnemies(delta);
            UpdateAttack(delta);
            UpdatePickups(delta);
            UpdateGeumjul(delta);
            UpdateField();

        }

        private void LateUpdate()
        {
            if (gameplayCamera != null && player != null)
            {
                UpdateCamera();
            }
        }

        private void SetupCamera()
        {
            gameplayCamera = Camera.main;
            if (gameplayCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                gameplayCamera = cameraObject.AddComponent<Camera>();
            }

            gameplayCamera.orthographic = true;
            gameplayCamera.orthographicSize = VisualScale.CameraOrthographicSize;
            gameplayCamera.transform.position = new Vector3(0f, 0f, -10f);
            gameplayCamera.backgroundColor = new Color(0.075f, 0.07f, 0.08f);
            gameplayCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void CreateSharedSprite()
        {
            solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            solidTexture.name = "FirstPlayableSolidTexture";
            solidTexture.SetPixel(0, 0, Color.white);
            solidTexture.Apply();
            solidSprite = Sprite.Create(
                solidTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            solidSprite.name = "FirstPlayableSolidSprite";
        }

        private void CreateField()
        {
            var oldField = transform.Find("FlatField");
            if (oldField != null)
            {
                Destroy(oldField.gameObject);
            }

            flatField = new GameObject("FlatField").transform;
            flatField.SetParent(transform, false);
            flatField.gameObject.AddComponent<BattlefieldTilePresenter>().Build(
                battlefieldTilePrimary,
                battlefieldTileAlternate,
                battlefieldDecals,
                solidSprite);
        }

        private void ResetRun()
        {
            Time.timeScale = 1f;
            weaponRuntime?.Dispose();
            weaponRuntime = null;
            if (runtimeObjects != null)
            {
                Destroy(runtimeObjects.gameObject);
            }

            runtimeObjects = new GameObject("RuntimeObjects").transform;
            runtimeObjects.SetParent(transform, false);
            enemies.Clear();
            pickups.Clear();
            trail.Clear();
            upgradeOffers.Clear();
            upgradeOfferData.Clear();
            weaponLevels.Clear();
            weaponLevels.Add(WeaponId.HwandoFlyingBlade.Value, 1);
            supportLevels.Clear();
            unlockedUpgradeIds.Clear();
            acquiredEvolutionIds.Clear();
            weaponAffixes.Clear();
            affixRollOrdinal = 0;
            evolutionState.Clear();
            foreach (var evolution in WeaponEvolutionCatalog.All) unlockedUpgradeIds.Add(evolution.Id);
            combatTargets = new CombatTargetRegistry();
            combatDamageService = new CombatDamageService(combatTargets);
            weaponRuntime = new WeaponRuntimeController(combatTargets, combatDamageService, prototypeCombatMask);
            weaponRuntime.SetSpriteResolver(ResolveWeaponSprite);
            weaponRuntime.SetPresentationSpriteResolver(ResolveWeaponPresentationSprite);
            weaponRuntime.SetMaskResolver(ResolveWeaponMask);
            elapsed = 0f;
            stageTimeline = StagePacingTimeline.ForDuration(TestDuration);
            processedStageMilestones = 0;
            finalBossWarning = false;
            waveAnnouncement = string.Empty;
            waveAnnouncementTimer = 0f;
            waveAnnouncementIntensity = 0;
            playerMaxHealth = 100f;
            playerHealth = playerMaxHealth;
            moveSpeed = 2.4f;
            pickupRadius = 2.2f;
            geumjulDamage = 38f;
            spawnTimer = 0.2f;
            chestSpawnTimer = 18f;
            trailTimer = 0f;
            sealCooldown = 0f;
            magnetMessageTimer = 0f;
            experience = 0;
            experienceToNext = 8;
            level = 1;
            registeredWeaponIds.Clear();
            weaponMasks.Load(weaponCatalog);
            RegisterCatalogWeapons();
            coins = 0;
            kills = 0;
            nextCombatTargetRuntimeId = 1;
            pendingUpgradeCount = 0;
            bossSpawned = false;
            bossAlive = false;
            upgradeOpen = false;
            awaitingUpgradePresentationClose = false;
            runEnded = false;
            victory = false;

            player = CreateCombatantObject(
                "Han Yeonhwa",
                playerSprite != null ? playerSprite : solidSprite,
                Vector2.zero,
                10,
                runtimeObjects,
                MotionWeight.Light,
                0f,
                out playerVisualRig,
                CombatantVisualRole.Player);
            player.transform.localScale = Vector3.one * VisualScale.PlayerScale;
            playerRenderer = playerVisualRig.Renderer;
            playerHealthFill = CreateHealthBar(player.transform);
            playerHealthFill.parent.localPosition = new Vector3(0f, -0.30f, 0f);
            playerHealthFill.parent.localScale = Vector3.one * 0.58f;
            if (playerSprite == null)
            {
                playerRenderer.color = new Color(0.18f, 0.38f, 0.72f);
            }

            geumjulRenderer = new GameObject("Geumjul Trail").AddComponent<LineRenderer>();
            geumjulRenderer.transform.SetParent(runtimeObjects, false);
            geumjulRenderer.useWorldSpace = true;
            geumjulRenderer.material = new Material(Shader.Find("Sprites/Default"));
            geumjulRenderer.startColor = new Color(1f, 0.78f, 0.18f, 0.9f);
            geumjulRenderer.endColor = new Color(1f, 0.95f, 0.45f, 0.45f);
            geumjulRenderer.startWidth = 0.045f;
            geumjulRenderer.endWidth = 0.022f;
            geumjulRenderer.sortingOrder = 4;
            geumjulRenderer.positionCount = 0;

            gameplayCamera.transform.position = new Vector3(0f, 0f, -10f);
            cameraFollowVelocity = Vector3.zero;
#if UNITY_INCLUDE_TESTS
            AppliedUpgradeCount = 0;
            MidBossSpawnCountForTests = 0;
            FinalBossSpawnCountForTests = 0;
#endif
            RunReset?.Invoke();
        }

        private void ReadMovement()
        {
            movement = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y += 1f;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    touchStart = touch.position.ReadValue();
                }

                if (touch.press.isPressed)
                {
                    var delta = touch.position.ReadValue() - touchStart;
                    movement = Vector2.ClampMagnitude(delta / 90f, 1f);
                }
            }

            movement = Vector2.ClampMagnitude(movement, 1f);
        }

        private void UpdatePlayer(float delta)
        {
            var velocity = movement * moveSpeed;
            var position = (Vector2)player.transform.position + velocity * delta;
            player.transform.position = position;
            playerVisualRig?.Tick(velocity, delta, MotionWeight.Light);

            UpdateHealthBar(playerHealthFill, playerHealth / playerMaxHealth);
        }

        private void UpdateCamera()
        {
            var target = player.transform.position;
            var lookAhead = Vector2.ClampMagnitude(movement, 1f) * 0.35f;
            var desired = new Vector3(target.x + lookAhead.x, target.y + lookAhead.y, -10f);
            gameplayCamera.transform.position = Vector3.SmoothDamp(
                gameplayCamera.transform.position,
                desired,
                ref cameraFollowVelocity,
                0.12f,
                100f,
                Time.unscaledDeltaTime);
        }

        private void UpdateField()
        {
            // The battlefield is deliberately world-anchored. Moving it in camera-sized steps made
            // its high-contrast landmarks jump underneath the player.
        }

        private void UpdateSpawning(float delta)
        {
            var pacing = stageTimeline.Sample(elapsed);
            if (enemies.Count >= pacing.ActiveCap)
            {
                return;
            }

            spawnTimer -= delta;
            if (spawnTimer > 0f)
            {
                return;
            }

            spawnTimer = EnemyDensityProfile.SpawnInterval(pacing);
            var batchSize = EnemyDensityProfile.BatchSize(pacing);
            for (var index = 0;
                 index < batchSize && enemies.Count < pacing.ActiveCap;
                 index++)
            {
                SpawnEnemy(false);
            }
        }

        private void SpawnEnemy(bool isBoss, int midBossTier = 0)
        {
            var isMidBoss = !isBoss && midBossTier > 0;
            var angle = UnityEngine.Random.value * Mathf.PI * 2f;
            var radius = isBoss || isMidBoss
                ? VisualScale.SpawnRadiusMinimum
                : UnityEngine.Random.Range(VisualScale.SpawnRadiusMinimum, VisualScale.SpawnRadiusMaximum);
            var position = (Vector2)player.transform.position +
                           new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var pacing = stageTimeline.Sample(elapsed);
            var isElite = !isBoss && !isMidBoss && elapsed >= 3f &&
                          UnityEngine.Random.value < pacing.EliteChance;
            var rank = isBoss
                ? EnemyRankProfile.Boss
                : (isElite || isMidBoss ? EnemyRankProfile.Elite : EnemyRankProfile.Normal);
            var chosenSprite = isBoss
                ? bossSprite
                : (isMidBoss
                    ? (midBossTier >= 2 && bossSprite != null ? bossSprite :
                        eliteSprite != null ? eliteSprite : ChooseNormalEnemySprite())
                    : isElite && eliteSprite != null
                    ? eliteSprite
                    : ChooseNormalEnemySprite());
            var enemyObject = CreateCombatantObject(
                isBoss ? "Fallen General" :
                isMidBoss ? (midBossTier >= 2 ? "Vengeful Field Commander" : "Dokkaebi Captain") :
                isElite ? "Elite Pursuer" : "Pursuing Enemy",
                chosenSprite != null ? chosenSprite : solidSprite,
                position,
                isBoss || isMidBoss ? 9 : 8,
                runtimeObjects,
                isBoss || isElite || isMidBoss ? MotionWeight.Heavy : MotionWeight.Medium,
                nextCombatTargetRuntimeId * 0.173f,
                out var visualRig,
                isBoss ? CombatantVisualRole.Boss :
                isMidBoss ? CombatantVisualRole.Boss :
                isElite ? CombatantVisualRole.Elite : CombatantVisualRole.Enemy);

            var renderer = visualRig.Renderer;
            var baseHealth = isBoss ? 680f :
                isMidBoss ? (midBossTier >= 2 ? 320f : 180f) :
                Mathf.Lerp(18f, 42f, elapsed / TestDuration);
            var health = isBoss || isMidBoss ? baseHealth : baseHealth * rank.HealthMultiplier;
            var displayScale = isBoss
                ? VisualScale.BossEnemyScale
                : isMidBoss
                    ? Mathf.Lerp(VisualScale.EliteEnemyScale, VisualScale.BossEnemyScale,
                        midBossTier >= 2 ? .68f : .42f)
                    : VisualScale.ScaleFor(rank);
            enemyObject.transform.localScale = Vector3.one * displayScale;
            if (chosenSprite == null)
            {
                renderer.color = isBoss ? new Color(0.55f, 0.12f, 0.16f) : new Color(0.45f, 0.20f, 0.18f);
            }

            var state = new EnemyState
            {
                Object = enemyObject,
                Renderer = renderer,
                VisualRig = visualRig,
                MotionWeight = isBoss || isElite ? MotionWeight.Heavy : MotionWeight.Medium,
                Health = health,
                MaximumHealth = health,
                Speed = isBoss ? .98f :
                    isMidBoss ? (midBossTier >= 2 ? 1.08f : 1f) :
                    Mathf.Lerp(0.775f, 1.325f, elapsed / TestDuration) * rank.SpeedMultiplier,
                ContactDamage = isBoss ? 24f :
                    isMidBoss ? (midBossTier >= 2 ? 20f : 16f) :
                    10f * rank.ContactDamageMultiplier,
                IsBoss = rank.IsBoss,
                IsElite = rank.IsElite,
                IsMidBoss = isMidBoss,
                MidBossTier = midBossTier,
                ExperienceValue = isMidBoss ? (midBossTier >= 2 ? 20 : 12) : rank.ExperienceValue
            };
            if (rank.IsElite || isMidBoss)
            {
                state.HealthFill = CreateHealthBar(enemyObject.transform);
                state.HealthFill.parent.localPosition = new Vector3(0f, isMidBoss ? -1.02f : -0.78f, 0f);
                state.HealthFill.parent.localScale = Vector3.one * (isMidBoss ? .66f : .52f);
            }
            state.CombatTarget = new PrototypeCombatTarget(this, state, nextCombatTargetRuntimeId++);
            combatTargets.Register(state.CombatTarget);
            enemies.Add(state);
            if (isBoss || isMidBoss) bossAlive = true;
        }

        private Sprite ChooseNormalEnemySprite()
        {
            if (enemySprites != null && enemySprites.Length > 0)
            {
                var start = UnityEngine.Random.Range(0, enemySprites.Length);
                for (var offset = 0; offset < enemySprites.Length; offset++)
                {
                    var candidate = enemySprites[(start + offset) % enemySprites.Length];
                    if (candidate != null) return candidate;
                }
            }

            return UnityEngine.Random.value < 0.35f ? enemySpriteAlt : enemySprite;
        }

        private void UpdateTreasureSpawning(float delta)
        {
            chestSpawnTimer -= delta;
            if (chestSpawnTimer > 0f)
            {
                return;
            }

            var activeChests = enemies.FindAll(value => value.IsTreasure).Count;
            if (activeChests >= 2)
            {
                chestSpawnTimer = 3f;
                return;
            }

            SpawnTreasureChest();
            chestSpawnTimer = UnityEngine.Random.Range(40f, 60f);
        }

        private void SpawnTreasureChest()
        {
            var angle = UnityEngine.Random.value * Mathf.PI * 2f;
            var radius = UnityEngine.Random.Range(7f, 10f);
            var position = (Vector2)player.transform.position +
                           new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            var chestObject = CreateSpriteObject(
                "Treasure Chest",
                treasureChestSprite != null ? treasureChestSprite : solidSprite,
                position,
                8,
                runtimeObjects);
            chestObject.transform.localScale = Vector3.one * 0.32f;
            var renderer = chestObject.GetComponent<SpriteRenderer>();
            if (treasureChestSprite == null)
            {
                renderer.color = new Color(0.72f, 0.40f, 0.12f);
            }

            var state = new EnemyState
            {
                Object = chestObject,
                Renderer = renderer,
                Health = 75f,
                MaximumHealth = 75f,
                Speed = 0f,
                ContactDamage = 0f,
                IsTreasure = true
            };
            state.CombatTarget = new PrototypeCombatTarget(this, state, nextCombatTargetRuntimeId++);
            combatTargets.Register(state.CombatTarget);
            enemies.Add(state);
        }

        private void SpawnBoss()
        {
            bossSpawned = true;
            bossAlive = true;
            finalBossWarning = false;
            SpawnEnemy(true);
#if UNITY_INCLUDE_TESTS
            FinalBossSpawnCountForTests++;
#endif
        }

        private void SpawnMidBoss(int tier)
        {
            SpawnEnemy(false, Mathf.Clamp(tier, 1, 2));
#if UNITY_INCLUDE_TESTS
            MidBossSpawnCountForTests++;
#endif
        }

        private void ProcessStageMilestones(float previousElapsed, float currentElapsed)
        {
            ProcessMilestone(previousElapsed, currentElapsed, StageMilestone.FirstSurge, () =>
            {
                SpawnBurst(18);
                ShowWaveAnnouncement("요기 떼가 몰려옵니다!", 1, .9f);
            });
            ProcessMilestone(previousElapsed, currentElapsed, StageMilestone.FirstMidBoss, () =>
            {
                SpawnMidBoss(1);
                ShowWaveAnnouncement("중간보스 · 도깨비 대장", 2, 1.4f);
            });
            ProcessMilestone(previousElapsed, currentElapsed, StageMilestone.SecondSurge, () =>
            {
                SpawnBurst(26);
                ShowWaveAnnouncement("사방에서 포위해 옵니다!", 2, 1.05f);
            });
            ProcessMilestone(previousElapsed, currentElapsed, StageMilestone.SecondMidBoss, () =>
            {
                SpawnMidBoss(2);
                ShowWaveAnnouncement("중간보스 · 원혼 장수", 2, 1.4f);
            });
            ProcessMilestone(previousElapsed, currentElapsed, StageMilestone.FinalSurge, () =>
            {
                SpawnBurst(34);
                ShowWaveAnnouncement("마지막 대공세!", 3, 1.2f);
            });
            ProcessMilestone(previousElapsed, currentElapsed, StageMilestone.FinalBossWarning, () =>
            {
                finalBossWarning = true;
                ShowWaveAnnouncement("강대한 요기가 다가옵니다", 3, 1.8f);
            });
            ProcessMilestone(previousElapsed, currentElapsed, StageMilestone.FinalBoss, () =>
            {
                SpawnBoss();
                ShowWaveAnnouncement("최종보스 · 타락한 장군", 3, 1.8f);
            });
        }

        private void ProcessMilestone(
            float previousElapsed,
            float currentElapsed,
            StageMilestone milestone,
            Action action)
        {
            var bit = 1 << (int)milestone;
            if ((processedStageMilestones & bit) != 0 ||
                !stageTimeline.Crossed(previousElapsed, currentElapsed, milestone))
            {
                return;
            }

            processedStageMilestones |= bit;
            action();
        }

        private void SpawnBurst(int count)
        {
            var activeCap = stageTimeline.Sample(elapsed).ActiveCap;
            for (var index = 0; index < count && enemies.Count < activeCap; index++)
            {
                SpawnEnemy(false);
            }
        }

        private void ShowWaveAnnouncement(string message, int intensity, float duration)
        {
            waveAnnouncement = message ?? string.Empty;
            waveAnnouncementIntensity = Mathf.Clamp(intensity, 1, 3);
            waveAnnouncementTimer = Mathf.Max(0f, duration);
        }

        private void UpdateEnemies(float delta)
        {
            var playerPosition = (Vector2)player.transform.position;
            for (var index = enemies.Count - 1; index >= 0; index--)
            {
                var enemy = enemies[index];
                if (enemy.Object == null)
                {
                    combatTargets.Unregister(enemy.CombatTarget);
                    enemies.RemoveAt(index);
                    continue;
                }

                if (enemy.IsTreasure)
                {
                    continue;
                }

                enemy.TickStatuses(delta);

                var enemyPosition = (Vector2)enemy.Object.transform.position;
                var direction = (playerPosition - enemyPosition).normalized;
                var velocity = direction * (enemy.Speed * enemy.MovementMultiplier);
                enemy.Object.transform.position = enemyPosition + velocity * delta;
                enemy.VisualRig?.Tick(velocity, delta, enemy.MotionWeight);

                var rank = enemy.IsBoss
                    ? EnemyRankProfile.Boss
                    : enemy.IsElite ? EnemyRankProfile.Elite : EnemyRankProfile.Normal;
                var hitDistance = VisualScale.ContactRadiusFor(rank);
                if (Vector2.Distance(enemy.Object.transform.position, playerPosition) <= hitDistance &&
                    contactInvulnerability <= 0f)
                {
                    playerHealth = Mathf.Max(0f, playerHealth - enemy.ContactDamage);
                    UpdateHealthBar(playerHealthFill, playerHealth / playerMaxHealth);
                    contactInvulnerability = 0.55f;
                    StartCoroutine(FlashPlayer());
                    if (playerHealth <= 0f)
                    {
                        EndRun(false);
                        return;
                    }
                }
            }
        }

        private System.Collections.IEnumerator FlashPlayer()
        {
            playerRenderer.color = new Color(1f, 0.45f, 0.45f);
            yield return new WaitForSeconds(0.1f);
            if (playerRenderer != null)
            {
                playerRenderer.color = Color.white;
            }
        }

        private void UpdateAttack(float delta)
        {
            if (weaponRuntime == null || player == null)
            {
                return;
            }

            weaponRuntime.Tick(delta, player.transform.position, runtimeObjects, solidSprite, 15);
        }

        private void RegisterCatalogWeapons()
        {
            if (weaponCatalog == null) throw new InvalidOperationException("Gameplay requires the eight-weapon catalog.");
            var errors = weaponCatalog.ValidateLaunchRoster();
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("; ", errors));
            foreach (var id in WeaponRoster.All)
            {
                if (!weaponLevels.TryGetValue(id.Value, out var ownedLevel)) continue;
                if (!weaponCatalog.TryGet(id, out var definition) || definition.Levels.Count != 5)
                    throw new InvalidOperationException($"Gameplay catalog is missing '{id}'.");
                var data = definition.Levels[Mathf.Clamp(ownedLevel - 1, 0, 4)];
                IWeaponExecutor executor;
                var evolved = evolutionState.IsEvolved(id);
                var modifiers = WeaponRuntimeModifiers.From(weaponAffixes.TryProfileFor(id, out var profile) ? profile : null);
                if (id.Equals(WeaponId.HwandoFlyingBlade)) executor = new FlyingBladeExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ProjectileCount, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.GakgungShot)) executor = new GakgungExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.TalismanThrow)) executor = new TalismanExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ChainCount, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.ThunderCrashBomb)) executor = new ThunderBombExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.DurationSeconds, 0.15f, data.Range * 0.45f, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.JangseungWard)) executor = new JangseungWardExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.ProjectileCount, data.Pierce, 0.2f, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.SingijeonVolley)) executor = new SingijeonExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ProjectileCount, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.FrostFlask)) executor = new FrostFlaskExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.DurationSeconds, data.DurationSeconds, data.Range * 0.35f, data.Pierce, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.WindThunderFan)) executor = new WindThunderFanExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Knockback, data.ChainCount, data.Level, evolved, modifiers);
                else throw new InvalidOperationException($"No executor is available for '{id}'.");
                weaponRuntime.Register(id, executor);
                registeredWeaponIds.Add(id);
            }
        }

        private Sprite ResolveWeaponSprite(WeaponId id)
        {
            return weaponCatalog != null && weaponCatalog.TryGet(id, out var definition) && definition.PresentationSprites.Count > 0
                ? definition.PresentationSprites[0]
                : solidSprite;
        }

        private Sprite ResolveWeaponPresentationSprite(WeaponId id, int partIndex)
        {
            if (weaponCatalog == null || !weaponCatalog.TryGet(id, out var definition) ||
                definition.PresentationSprites.Count == 0)
            {
                return solidSprite;
            }

            if (partIndex < 0 || partIndex >= definition.PresentationSprites.Count)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning(
                    $"Weapon presentation sprite request is out of range for '{id}': requested part index {partIndex}.");
#endif
                return definition.PresentationSprites[0];
            }

            return definition.PresentationSprites[partIndex];
        }

        private PixelHitMask ResolveWeaponMask(WeaponId id)
        {
            if (weaponCatalog == null || !weaponCatalog.TryGet(id, out var definition)) return null;
            var masks = weaponMasks.GetMasks(definition);
            return masks.Count == 0 ? null : masks[0];
        }

        private void ApplyEnemyDamage(EnemyState enemy, float damage)
        {
            if (enemy == null || enemy.Object == null)
            {
                return;
            }

            enemy.Health -= damage;
            enemy.Renderer.color = Color.white;
            var enemyPosition = (Vector2)enemy.Object.transform.position;
            var incomingDirection = enemyPosition - (Vector2)player.transform.position;
            enemy.VisualRig?.ShowHit(incomingDirection, enemy.IsBoss ? 0.05f : enemy.IsElite ? 0.075f : 0.095f);
            UpdateHealthBar(enemy.HealthFill, enemy.Health / enemy.MaximumHealth);
            if (enemy.Health > 0f)
            {
                return;
            }

            var wasBoss = enemy.IsBoss;
            var wasMidBoss = enemy.IsMidBoss;
            var wasTreasure = enemy.IsTreasure;
            var deathPosition = enemy.Object.transform.position;
            combatTargets.Unregister(enemy.CombatTarget);
            enemies.Remove(enemy);
            if (wasBoss || wasMidBoss)
            {
                bossAlive = enemies.Exists(candidate =>
                    candidate.Object != null && (candidate.IsBoss || candidate.IsMidBoss));
            }
            if (enemy.VisualRig != null && !enemy.IsTreasure)
            {
                enemy.VisualRig.PlayDeath();
                StartCoroutine(AnimateDeathAndDestroy(enemy));
            }
            else
            {
                Destroy(enemy.Object);
            }
            if (wasTreasure)
            {
                ScatterTreasure(deathPosition);
                return;
            }

            kills++;
            if (wasBoss)
            {
                EndRun(true);
                return;
            }

            SpawnPickup(deathPosition, PickupKind.Experience, enemy.ExperienceValue);
            if (wasMidBoss)
            {
                ScatterTreasure(deathPosition);
            }
            if (UnityEngine.Random.value < 0.01f)
            {
                SpawnPickup(
                    deathPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f),
                    PickupKind.Magnet,
                    0);
            }
        }

        private System.Collections.IEnumerator AnimateDeathAndDestroy(EnemyState enemy)
        {
            var elapsedDeath = 0f;
            const float duration = 0.30f;
            while (enemy.Object != null && elapsedDeath < duration)
            {
                var delta = Time.unscaledDeltaTime;
                elapsedDeath += delta;
                enemy.VisualRig.Tick(Vector2.zero, delta, enemy.MotionWeight);
                yield return null;
            }

            if (enemy.Object != null) Destroy(enemy.Object);
        }

        private void ScatterTreasure(Vector2 position)
        {
            var count = UnityEngine.Random.Range(6, 11);
            for (var index = 0; index < count; index++)
            {
                var angle = Mathf.PI * 2f * index / count + UnityEngine.Random.Range(-0.18f, 0.18f);
                var radius = UnityEngine.Random.Range(0.45f, 1.15f);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                SpawnPickup(position + offset, PickupKind.Yeopjeon, UnityEngine.Random.Range(1, 4));
            }
        }

        private void SpawnPickup(Vector2 position, PickupKind kind, int value)
        {
            var sprite = kind == PickupKind.Experience
                ? experienceSprite
                : kind == PickupKind.Yeopjeon
                    ? coinSprite
                    : treasureChestSprite;
            var objectName = kind == PickupKind.Experience
                ? "Experience Flame"
                : kind == PickupKind.Yeopjeon
                    ? "Yeopjeon"
                    : "Spirit Magnet";
            var pickupObject = CreateSpriteObject(
                objectName,
                sprite != null ? sprite : solidSprite,
                position,
                6,
                runtimeObjects);
            pickupObject.transform.localScale = Vector3.one *
                                                (kind == PickupKind.Yeopjeon ? 0.18f : 0.14f);
            var renderer = pickupObject.GetComponent<SpriteRenderer>();
            if (kind == PickupKind.Magnet)
            {
                renderer.color = new Color(0.25f, 0.92f, 1f);
            }
            if (sprite == null)
            {
                renderer.color = kind == PickupKind.Yeopjeon
                    ? new Color(0.95f, 0.68f, 0.12f)
                    : new Color(0.35f, 0.85f, 1f);
            }

            pickups.Add(new PickupState { Object = pickupObject, Kind = kind, Value = value });
        }

        private void UpdatePickups(float delta)
        {
            var playerPosition = (Vector2)player.transform.position;
            for (var index = pickups.Count - 1; index >= 0; index--)
            {
                var pickup = pickups[index];
                if (pickup.Object == null)
                {
                    pickups.RemoveAt(index);
                    continue;
                }

                var distance = Vector2.Distance(pickup.Object.transform.position, playerPosition);
                if (pickup.ForceCollect || distance <= pickupRadius)
                {
                    pickup.Object.transform.position = Vector2.MoveTowards(
                        pickup.Object.transform.position,
                        playerPosition,
                        pickup.ForceCollect
                            ? 24f * delta
                            : Mathf.Lerp(4f, 12f, 1f - distance / pickupRadius) * delta);
                }

                if (distance > 0.42f)
                {
                    continue;
                }

                if (pickup.Kind == PickupKind.Yeopjeon)
                {
                    coins += pickup.Value;
                }
                else if (pickup.Kind == PickupKind.Experience)
                {
                    AddExperience(pickup.Value);
                }
                else
                {
                    CollectMagnet();
                }

                Destroy(pickup.Object);
                pickups.RemoveAt(index);
            }
        }

        private void CollectMagnet()
        {
            foreach (var pickup in pickups)
            {
                if (pickup.Kind == PickupKind.Experience)
                {
                    pickup.ForceCollect = true;
                }
            }

            magnetMessageTimer = 1.2f;
        }

        private void AddExperience(int amount)
        {
            experience += amount;
            while (experience >= experienceToNext)
            {
                experience -= experienceToNext;
                level++;
                experienceToNext = 7 + level * 4;
                pendingUpgradeCount++;
            }

            if (!upgradeOpen && !awaitingUpgradePresentationClose && pendingUpgradeCount > 0)
            {
                pendingUpgradeCount--;
                OpenUpgrade();
            }
        }

        private void RebuildWeaponExecutorsForLevel()
        {
            if (weaponRuntime == null) return;
#if UNITY_INCLUDE_TESTS
            WeaponRebuildCountForTests++;
#endif
            weaponRuntime.Dispose();
            weaponRuntime = new WeaponRuntimeController(combatTargets, combatDamageService, prototypeCombatMask);
            weaponRuntime.SetSpriteResolver(ResolveWeaponSprite);
            weaponRuntime.SetPresentationSpriteResolver(ResolveWeaponPresentationSprite);
            weaponRuntime.SetMaskResolver(ResolveWeaponMask);
            registeredWeaponIds.Clear();
            weaponMasks.Load(weaponCatalog);
            RegisterCatalogWeapons();
        }

        private void OpenUpgrade()
        {
            upgradeOpen = true;
            upgradeOffers.Clear();
            upgradeOfferData.Clear();
            var state = new UpgradeState(weaponLevels, supportLevels, unlockedUpgradeIds, acquiredEvolutionIds);
            var selected = UpgradeSelector.Select(state, level * 397 ^ kills);
            foreach (var offer in selected)
            {
                upgradeOfferData.Add(offer);
                upgradeOffers.Add(FormatUpgradeOffer(offer));
            }
            var choices = new List<UpgradeChoiceView>(upgradeOfferData.Count);
            foreach (var offer in upgradeOfferData) choices.Add(BuildUpgradeChoiceView(offer));
            UpgradeOpened?.Invoke(new UpgradeChoiceState(level, choices));
        }

        public bool TryChooseUpgrade(int index)
        {
            if (!upgradeOpen || index < 0 || index >= upgradeOfferData.Count)
            {
                return false;
            }

            var reward = ApplyUpgrade(upgradeOfferData[index]);
            upgradeOpen = false;
            upgradeOffers.Clear();
            upgradeOfferData.Clear();
#if UNITY_INCLUDE_TESTS
            AppliedUpgradeCount++;
#endif
            awaitingUpgradePresentationClose = true;
            UpgradeChosen?.Invoke(reward);
            return true;
        }

        /// <summary>Signals that presentation has finished showing the selected reward.</summary>
        public bool NotifyUpgradePresentationClosed()
        {
            if (!awaitingUpgradePresentationClose)
            {
                return false;
            }

            awaitingUpgradePresentationClose = false;
            if (pendingUpgradeCount > 0)
            {
                pendingUpgradeCount--;
                OpenUpgrade();
            }

            return true;
        }

        // Retained for the existing gameplay smoke test while presentation migrates to TryChooseUpgrade.
        private void ChooseUpgrade(int index) => TryChooseUpgrade(index);

        private ProgressionRewardEvent ApplyUpgrade(UpgradeOffer offer)
        {
            if (offer.Kind == UpgradeKind.Weapon)
            {
                weaponLevels[offer.Id] = offer.NextLevel;
                var affixResult = RollWeaponAffix(new WeaponId(offer.Id));
                RebuildWeaponExecutorsForLevel();
#if false
                return new ProgressionRewardEvent(offer.Id, offer.Id, offer.NextLevel,
                    offer.NextLevel == 1 ? ProgressionRewardKind.NewWeapon : ProgressionRewardKind.WeaponLevel,
                    WeaponDisplayName(offer.Id), offer.NextLevel == 1 ? "새 무기 획득" : $"레벨 {offer.NextLevel} 효과 적용", ResolveWeaponSprite(new WeaponId(offer.Id)));
            }
 #endif
            if (offer.Kind == UpgradeKind.Weapon)
            {
                return new ProgressionRewardEvent(offer.Id, offer.Id, offer.NextLevel,
                    offer.NextLevel == 1 ? ProgressionRewardKind.NewWeapon : ProgressionRewardKind.WeaponLevel,
                    WeaponDisplayName(offer.Id), FormatUpgradeOffer(offer), ResolveWeaponSprite(new WeaponId(offer.Id)),
                    affixResult);
            }
            }
            if (offer.Kind == UpgradeKind.Support)
            {
                supportLevels[offer.Id] = offer.NextLevel;
                ApplySupportUpgrade(offer.Id);
                return new ProgressionRewardEvent(offer.Id, null, offer.NextLevel, ProgressionRewardKind.Support,
                    SupportDisplayName(offer.Id), SupportDelta(offer.Id), null);
            }
            if (offer.Kind == UpgradeKind.Evolution && WeaponEvolutionCatalog.TryGet(offer.Id, out var evolution))
            {
                acquiredEvolutionIds.Add(offer.Id);
                evolutionState.SetEvolved(evolution.RequiredWeaponId);
                RebuildWeaponExecutorsForLevel();
                return new ProgressionRewardEvent(offer.Id, evolution.RequiredWeaponId.Value, 5,
                    ProgressionRewardKind.Evolution, evolution.DisplayName, evolution.Summary,
                    ResolveWeaponSprite(evolution.RequiredWeaponId));
            }

            acquiredEvolutionIds.Add(offer.Id);
            return new ProgressionRewardEvent(offer.Id, WeaponId.HwandoFlyingBlade.Value, 1,
                ProgressionRewardKind.Evolution, "환도 비검 진화", "진화 완료", ResolveWeaponSprite(WeaponId.HwandoFlyingBlade));
        }

        private static string FormatUpgradeOffer(UpgradeOffer offer)
        {
            if (offer.Kind == UpgradeKind.Weapon)
            {
                var prefix = offer.NextLevel == 1 ? "[신규]" : "[강화]";
                var detail = offer.NextLevel == 1 ? "새 무기 획득" : $"레벨 {offer.NextLevel} 효과 적용";
                return $"{prefix} {WeaponDisplayName(offer.Id)}|{detail}";
            }

            switch (offer.Id)
            {
                case "talisman": return "[지원] 호신부적|최대 체력 +20";
                case "boots": return "[지원] 경쾌한 버선|이동속도 +12%";
                case "warding_bell": return "[지원] 수호 방울|획득 범위 +0.7";
                default: return $"[지원] {offer.Id}|레벨 {offer.NextLevel}";
            }
        }

        private static string WeaponDisplayName(string id)
        {
            if (id == WeaponId.HwandoFlyingBlade.Value) return "환도 비검";
            if (id == WeaponId.GakgungShot.Value) return "각궁";
            if (id == WeaponId.TalismanThrow.Value) return "주술 부적";
            if (id == WeaponId.ThunderCrashBomb.Value) return "벽력탄";
            if (id == WeaponId.JangseungWard.Value) return "장승진";
            if (id == WeaponId.SingijeonVolley.Value) return "신기전";
            if (id == WeaponId.FrostFlask.Value) return "서리병";
            if (id == WeaponId.WindThunderFan.Value) return "풍뢰선";
            return id;
        }

        private WeaponAffixRollResult RollWeaponAffix(WeaponId weaponId)
        {
            var ordinal = affixRollOrdinal++;
#if UNITY_INCLUDE_TESTS
            var testRandom = affixRandomFactoryForTests?.Invoke(weaponId, level, kills, ordinal);
            if (testRandom != null) return WeaponAffixRoller.RollAndApply(weaponAffixes, weaponId, testRandom);
#endif
            return WeaponAffixRoller.RollAndApply(weaponAffixes, weaponId,
                new SeededAffixRandom(WeaponAffixRoller.StableSeed(weaponId, level, kills, ordinal)));
        }

        private string GeneralAffixSummary(WeaponId weaponId)
        {
            if (!weaponAffixes.TryProfileFor(weaponId, out var profile) || profile.GeneralRolls.Count == 0) return string.Empty;
            var modifiers = WeaponRuntimeModifiers.From(profile);
            var values = new List<string>();
            if (modifiers.DamageBonus != 0f) values.Add($"Damage +{Mathf.RoundToInt(modifiers.DamageBonus * 100f)}%");
            if (modifiers.CooldownReduction != 0f) values.Add($"Cooldown -{Mathf.RoundToInt(modifiers.CooldownReduction * 100f)}%");
            if (modifiers.AreaBonus != 0f) values.Add($"Area +{Mathf.RoundToInt(modifiers.AreaBonus * 100f)}%");
            if (modifiers.SpeedBonus != 0f) values.Add($"Speed +{Mathf.RoundToInt(modifiers.SpeedBonus * 100f)}%");
            if (modifiers.DurationBonus != 0f) values.Add($"Duration +{Mathf.RoundToInt(modifiers.DurationBonus * 100f)}%");
            return string.Join(" · ", values);
        }

        private FirstPlayableUiState BuildUiState()
        {
            var weapons = new List<WeaponSlotView>(weaponLevels.Count);
            foreach (var weapon in weaponLevels)
            {
                weapons.Add(new WeaponSlotView(
                    weapon.Key,
                    WeaponDisplayName(weapon.Key),
                    weapon.Value,
                    ResolveWeaponSprite(new WeaponId(weapon.Key)),
                    GeneralAffixSummary(new WeaponId(weapon.Key)),
                    weaponAffixes.TryProfileFor(new WeaponId(weapon.Key), out var profile) ? profile.PotentialIds : null,
                    profile == null ? null : profile.GeneralRolls.Select(roll => roll.Tier),
                    WeaponBehavior(weapon.Key)));
            }

            var boss = enemies.Find(candidate =>
                (candidate.IsBoss || candidate.IsMidBoss) && candidate.Object != null);
            return new FirstPlayableUiState(
                level, experience, experienceToNext, coins, kills, elapsed, TestDuration,
                playerHealth, playerMaxHealth, finalBossWarning && !bossSpawned, bossAlive,
                boss != null ? boss.Health : 0f, boss != null ? boss.MaximumHealth : 0f, weapons,
                waveAnnouncement, waveAnnouncementTimer, waveAnnouncementIntensity);
        }

        private UpgradeChoiceView BuildUpgradeChoiceView(UpgradeOffer offer)
        {
            if (offer.Kind == UpgradeKind.Weapon)
            {
                return new UpgradeChoiceView(
                    offer.Id, offer.Kind, offer.NextLevel,
                    offer.NextLevel == 1 ? "신규 무기" : "무기 강화",
                    WeaponDisplayName(offer.Id),
                    WeaponBehavior(offer.Id),
                    offer.NextLevel == 1 ? "신규" : $"레벨 {offer.NextLevel}",
                    ResolveWeaponSprite(new WeaponId(offer.Id)));
            }

            if (offer.Kind == UpgradeKind.Support)
            {
                return new UpgradeChoiceView(
                    offer.Id, offer.Kind, offer.NextLevel, "능력 강화", SupportDisplayName(offer.Id),
                    SupportBehavior(offer.Id), SupportDelta(offer.Id), null);
            }

            return new UpgradeChoiceView(
                offer.Id, offer.Kind, offer.NextLevel, "진화", "환도 비검 진화",
                "환도의 힘을 해방", "진화", ResolveWeaponSprite(WeaponId.HwandoFlyingBlade));
        }

        private static string WeaponBehavior(string id)
        {
            if (id == WeaponId.HwandoFlyingBlade.Value) return "주변 적을 베는 비검";
            if (id == WeaponId.GakgungShot.Value) return "직선 관통 공격";
            if (id == WeaponId.TalismanThrow.Value) return "적 사이를 잇는 부적";
            if (id == WeaponId.ThunderCrashBomb.Value) return "범위 폭발 공격";
            if (id == WeaponId.JangseungWard.Value) return "주변을 지키는 장승";
            if (id == WeaponId.SingijeonVolley.Value) return "다발 화살 일제사격";
            if (id == WeaponId.FrostFlask.Value) return "빙결 지대 생성";
            if (id == WeaponId.WindThunderFan.Value) return "부채 바람으로 밀쳐냄";
            return "무기 효과 강화";
        }

        private static string SupportDisplayName(string id)
        {
            if (id == "talisman") return "호신부적";
            if (id == "boots") return "경쾌한 버선";
            if (id == "warding_bell") return "수호 방울";
            return id;
        }

        private static string SupportBehavior(string id)
        {
            if (id == "talisman") return "최대 체력 증가";
            if (id == "boots") return "이동 속도 증가";
            if (id == "warding_bell") return "획득 범위 증가";
            return "지원 능력 강화";
        }

        private static string SupportDelta(string id)
        {
            if (id == "talisman") return "+20";
            if (id == "boots") return "+12%";
            if (id == "warding_bell") return "+0.7";
            return "강화";
        }

        private void ApplySupportUpgrade(string id)
        {
            if (id == "talisman")
            {
                playerMaxHealth += 20f;
                playerHealth = Mathf.Min(playerMaxHealth, playerHealth + 20f);
            }
            else if (id == "boots") moveSpeed *= 1.12f;
            else if (id == "warding_bell") pickupRadius += 0.7f;
        }

        private void UpdateGeumjul(float delta)
        {
            trailTimer -= delta;
            if (trailTimer > 0f || movement.sqrMagnitude < 0.01f)
            {
                return;
            }

            trailTimer = 0.12f;
            var current = (Vector2)player.transform.position;
            if (trail.Count > 0 && Vector2.Distance(trail[trail.Count - 1], current) < 0.14f)
            {
                return;
            }

            trail.Add(current);
            if (trail.Count > 90)
            {
                trail.RemoveAt(0);
            }

            geumjulRenderer.positionCount = trail.Count;
            for (var index = 0; index < trail.Count; index++)
            {
                geumjulRenderer.SetPosition(index, trail[index]);
            }

            TryCloseSeal(current);
        }

        private void TryCloseSeal(Vector2 current)
        {
            if (sealCooldown > 0f || trail.Count < 16)
            {
                return;
            }

            var closureIndex = -1;
            for (var index = 0; index <= trail.Count - 14; index++)
            {
                if (Vector2.Distance(trail[index], current) <= 0.48f)
                {
                    closureIndex = index;
                    break;
                }
            }

            if (closureIndex < 0)
            {
                return;
            }

            var polygon = trail.GetRange(closureIndex, trail.Count - closureIndex);
            if (Mathf.Abs(SignedArea(polygon)) < 2.2f)
            {
                return;
            }

            var targets = new List<EnemyState>(enemies);
            foreach (var enemy in targets)
            {
                if (enemy.Object != null && PointInPolygon(enemy.Object.transform.position, polygon))
                {
                    ApplyEnemyDamage(enemy, enemy.IsBoss ? geumjulDamage * 0.35f : geumjulDamage);
                }
            }

            sealCooldown = 1.5f;
            trail.Clear();
            geumjulRenderer.positionCount = 0;
        }

        private static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            var area = 0f;
            for (var index = 0; index < polygon.Count; index++)
            {
                var next = (index + 1) % polygon.Count;
                area += polygon[index].x * polygon[next].y - polygon[next].x * polygon[index].y;
            }

            return area * 0.5f;
        }

        private static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            var inside = false;
            for (int current = 0, previous = polygon.Count - 1; current < polygon.Count; previous = current++)
            {
                var a = polygon[current];
                var b = polygon[previous];
                if ((a.y > point.y) == (b.y > point.y))
                {
                    continue;
                }

                var crossing = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < crossing)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private void EndRun(bool didWin)
        {
            if (runEnded)
            {
                return;
            }

            runEnded = true;
            victory = didWin;
            movement = Vector2.zero;
        }

        private PixelHitMask MaskFor(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return prototypeCombatMask;
            if (hurtMasksBySprite.TryGetValue(renderer.sprite, out var mask)) return mask;
            try
            {
                mask = PixelHitMask.FromSprite(renderer.sprite);
            }
            catch (ArgumentException)
            {
                mask = PixelHitMask.OpaqueSpriteRect(renderer.sprite);
            }
            catch (UnityException)
            {
                mask = PixelHitMask.OpaqueSpriteRect(renderer.sprite);
            }
            hurtMasksBySprite.Add(renderer.sprite, mask);
            return mask;
        }

        private PixelMaskTransform TransformFor(SpriteRenderer renderer, Float2 position)
        {
            if (renderer == null) return PixelMaskTransform.Translation(position.X, position.Y);
            var scale = renderer.transform.lossyScale;
            return new PixelMaskTransform(
                position,
                Mathf.RoundToInt(renderer.transform.eulerAngles.z),
                renderer.flipX,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        }

        private GameObject CreateSpriteObject(
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

        private GameObject CreateCombatantObject(
            string objectName,
            Sprite sprite,
            Vector2 position,
            int sortingOrder,
            Transform parent,
            MotionWeight weight,
            float phaseOffset,
            out CombatantVisualRig visualRig,
            CombatantVisualRole role = CombatantVisualRole.Enemy)
        {
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

        private Transform CreateHealthBar(Transform owner)
        {
            var root = new GameObject("Health Bar").transform;
            root.SetParent(owner, false);
            root.localPosition = new Vector3(0f, -1.25f, 0f);
            root.localRotation = Quaternion.identity;

            var background = new GameObject("Background");
            background.transform.SetParent(root, false);
            background.transform.localScale = new Vector3(2.2f, 0.24f, 1f);
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = solidSprite;
            backgroundRenderer.color = new Color(0.16f, 0.12f, 0.12f, 0.92f);
            backgroundRenderer.sortingOrder = 20;

            var fill = new GameObject("Fill").transform;
            fill.SetParent(root, false);
            fill.localScale = new Vector3(2f, 0.14f, 1f);
            var fillRenderer = fill.gameObject.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = solidSprite;
            fillRenderer.color = new Color(0.24f, 0.86f, 0.34f);
            fillRenderer.sortingOrder = 21;
            return fill;
        }

        private static void UpdateHealthBar(Transform fill, float normalizedHealth)
        {
            if (fill == null)
            {
                return;
            }

            var ratio = Mathf.Clamp01(normalizedHealth);
            fill.localScale = new Vector3(2f * ratio, 0.14f, 1f);
            fill.localPosition = new Vector3(-1f + ratio, 0f, -0.01f);
        }

        private void OnGUI()
        {
            if (!runEnded)
            {
                return;
            }

            var scale = Mathf.Min(Screen.width / 1080f, Screen.height / 1920f);
            var offsetX = (Screen.width - 1080f * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, 0f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            var centered = new GUIStyle(GUI.skin.box)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.12f, 0.12f, 0.10f) }
            };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 30, wordWrap = true };
            GUI.Box(new Rect(100f, 470f, 880f, 700f),
                victory
                    ? $"Run complete!\\n\\nSurvived {elapsed:0.0}s  Kills {kills}  Level {level}\\nCoins {coins}"
                    : $"Run failed!\\n\\nSurvived {elapsed:0.0}s  Kills {kills}  Level {level}\\nTry again.",
                centered);
            if (GUI.Button(new Rect(260f, 1000f, 560f, 120f), "Restart (R)", button))
            {
                ResetRun();
            }
        }

    }
}
