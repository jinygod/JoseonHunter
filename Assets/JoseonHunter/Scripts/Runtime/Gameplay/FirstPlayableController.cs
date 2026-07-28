using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class FirstPlayableController : MonoBehaviour
    {
        [Header("Static sprite assets")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private Sprite enemySpriteAlt;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite experienceSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite treasureChestSprite;
        [SerializeField] private WeaponCatalogAsset weaponCatalog;

        private readonly List<EnemyState> enemies = new List<EnemyState>();
        private readonly List<PickupState> pickups = new List<PickupState>();
        private readonly List<Vector2> trail = new List<Vector2>();
        private readonly List<string> upgradeOffers = new List<string>();
        private readonly List<UpgradeOffer> upgradeOfferData = new List<UpgradeOffer>();
        private readonly Dictionary<string, int> weaponLevels = new Dictionary<string, int>();
        private readonly Dictionary<string, int> supportLevels = new Dictionary<string, int>();
        private readonly HashSet<string> unlockedUpgradeIds = new HashSet<string>();
        private readonly HashSet<string> acquiredEvolutionIds = new HashSet<string>();
        private readonly PixelHitMask prototypeCombatMask = new PixelHitMask(1, 1, Vector2.zero, 1f, new[] { 1u });
        private readonly Dictionary<Sprite, PixelHitMask> hurtMasksBySprite = new Dictionary<Sprite, PixelHitMask>();

        private Camera gameplayCamera;
        private Transform flatField;
        private Transform runtimeObjects;
        private GameObject player;
        private SpriteRenderer playerRenderer;
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

        private const float TestDuration = 60f;
        private const float BossWarningTime = 45f;
        private const float BossSpawnTime = 50f;

        /// <summary>Read-only combat event source for presentation components.</summary>
        public CombatDamageService CombatDamageService => combatDamageService;
        public WeaponRuntimeController WeaponRuntime => weaponRuntime;
        public IReadOnlyList<WeaponId> RegisteredWeaponIds => registeredWeaponIds;
        public FirstPlayableUiState UiState => BuildUiState();
        public bool IsUpgradeOpen => upgradeOpen;
        public event Action<UpgradeChoiceState> UpgradeOpened;
        public event Action<ProgressionRewardEvent> UpgradeChosen;
        public event Action RunReset;

#if UNITY_INCLUDE_TESTS
        public IReadOnlyList<UpgradeOffer> CurrentOffers => upgradeOfferData;
        public int AppliedUpgradeCount { get; private set; }
        public void OpenUpgradeForTests() => OpenUpgrade();
        public void AddExperienceForTests(int amount) => AddExperience(amount);
        public void ResetRunForTests() => ResetRun();
#endif

        public bool IsCombatTargetAlive(int runtimeId) =>
            combatTargets != null && combatTargets.TryGet(runtimeId, out var target) && target.IsAlive;

        public bool IsBossCombatTarget(int runtimeId)
        {
            var enemy = enemies.Find(candidate => candidate.CombatTarget != null && candidate.CombatTarget.RuntimeId == runtimeId);
            return enemy != null && enemy.IsBoss;
        }

        private sealed class EnemyState
        {
            public GameObject Object;
            public SpriteRenderer Renderer;
            public float Health;
            public float MaximumHealth;
            public float Speed;
            public float ContactDamage;
            public float NextContactTime;
            public bool IsBoss;
            public bool IsTreasure;
            public ICombatTarget CombatTarget;
            private readonly Dictionary<int, float> frostSlowSources = new Dictionary<int, float>();
            private readonly Dictionary<int, float> freezeSources = new Dictionary<int, float>();
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
                    return slowDecayRemaining <= 0f ? 1f : Mathf.Lerp(1f, slowDecayStartMultiplier, slowDecayRemaining / 0.35f);
                }
            }

            private float SlowMultiplier()
            {
                var multiplier = 1f;
                foreach (var source in frostSlowSources) multiplier = Mathf.Min(multiplier, source.Value);
                return multiplier;
            }
        }

        private sealed class PrototypeCombatTarget : ICombatTarget, IFrostStatusTarget
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
            public bool IsBoss => state.IsBoss;
            public bool IsElite => false;
            public float ThreatScore => state.IsBoss ? 100f : 0f;
            public Float2 WorldPosition
            {
                get
                {
                    var position = state.Object == null ? Vector2.zero : (Vector2)state.Object.transform.position;
                    return new Float2(position.x, position.y);
                }
            }
            public PixelHitMask HurtMask => owner.MaskFor(state.Renderer);
            public PixelMaskTransform HurtMaskTransform => owner.TransformFor(state.Renderer, WorldPosition);
            public void ApplyResolvedDamage(int damage) => owner.ApplyEnemyDamage(state, damage);
            public void ApplyKnockback(Float2 direction, float force) { }
            public void ApplyFrostSlow(int sourceId, float strength) => state.ApplyFrostSlow(sourceId, strength);
            public void RemoveFrostSlow(int sourceId, float decaySeconds) => state.RemoveFrostSlow(sourceId, decaySeconds);
            public void ApplyFreeze(int sourceId, float durationSeconds) => state.ApplyFreeze(sourceId, durationSeconds);
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
            elapsed = Mathf.Min(TestDuration, elapsed + delta);
            contactInvulnerability = Mathf.Max(0f, contactInvulnerability - delta);
            sealCooldown = Mathf.Max(0f, sealCooldown - delta);
            magnetMessageTimer = Mathf.Max(0f, magnetMessageTimer - delta);

            ReadMovement();
            UpdatePlayer(delta);
            UpdateSpawning(delta);
            UpdateTreasureSpawning(delta);
            UpdateEnemies(delta);
            UpdateAttack(delta);
            UpdatePickups(delta);
            UpdateGeumjul(delta);
            UpdateCamera();
            UpdateField();

            if (!bossSpawned && elapsed >= BossSpawnTime)
            {
                SpawnBoss();
            }

            if (elapsed >= TestDuration && bossAlive)
            {
                EndRun(false);
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
            gameplayCamera.orthographicSize = 7.2f;
            gameplayCamera.transform.position = new Vector3(0f, 0f, -10f);
            gameplayCamera.backgroundColor = new Color(0.78f, 0.88f, 0.72f);
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

            var ground = CreateSpriteObject("Soft Grass", solidSprite, new Vector2(0f, 0f), -20, flatField);
            ground.transform.localScale = new Vector3(22f, 32f, 1f);
            ground.GetComponent<SpriteRenderer>().color = new Color(0.80f, 0.89f, 0.73f);

            for (var x = -10; x <= 10; x += 2)
            {
                var line = CreateSpriteObject("Grass Grid V", solidSprite, new Vector2(x, 0f), -19, flatField);
                line.transform.localScale = new Vector3(0.025f, 32f, 1f);
                line.GetComponent<SpriteRenderer>().color = new Color(0.63f, 0.78f, 0.57f, 0.35f);
            }

            for (var y = -15; y <= 15; y += 2)
            {
                var line = CreateSpriteObject("Grass Grid H", solidSprite, new Vector2(0f, y), -19, flatField);
                line.transform.localScale = new Vector3(22f, 0.025f, 1f);
                line.GetComponent<SpriteRenderer>().color = new Color(0.63f, 0.78f, 0.57f, 0.35f);
            }
        }

        private void ResetRun()
        {
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
            combatTargets = new CombatTargetRegistry();
            combatDamageService = new CombatDamageService(combatTargets);
            weaponRuntime = new WeaponRuntimeController(combatTargets, combatDamageService, prototypeCombatMask);
            weaponRuntime.SetSpriteResolver(ResolveWeaponSprite);
            weaponRuntime.SetMaskResolver(ResolveWeaponMask);
            elapsed = 0f;
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

            player = CreateSpriteObject(
                "Rookie Constable",
                playerSprite != null ? playerSprite : solidSprite,
                Vector2.zero,
                10,
                runtimeObjects);
            player.transform.localScale = Vector3.one * 0.3125f;
            playerRenderer = player.GetComponent<SpriteRenderer>();
            playerHealthFill = CreateHealthBar(player.transform);
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
            geumjulRenderer.startWidth = 0.09f;
            geumjulRenderer.endWidth = 0.04f;
            geumjulRenderer.sortingOrder = 4;
            geumjulRenderer.positionCount = 0;

            gameplayCamera.transform.position = new Vector3(0f, 0f, -10f);
#if UNITY_INCLUDE_TESTS
            AppliedUpgradeCount = 0;
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
            var position = (Vector2)player.transform.position + movement * (moveSpeed * delta);
            player.transform.position = position;

            if (movement.x > 0.01f)
            {
                playerRenderer.flipX = false;
            }
            else if (movement.x < -0.01f)
            {
                playerRenderer.flipX = true;
            }

            UpdateHealthBar(playerHealthFill, playerHealth / playerMaxHealth);
        }

        private void UpdateCamera()
        {
            var target = player.transform.position;
            var current = gameplayCamera.transform.position;
            var next = Vector3.Lerp(current, new Vector3(target.x, target.y, -10f), 7f * Time.deltaTime);
            gameplayCamera.transform.position = next;
        }

        private void UpdateField()
        {
            if (flatField == null)
            {
                return;
            }

            var cameraPosition = gameplayCamera.transform.position;
            flatField.position = new Vector3(
                Mathf.Round(cameraPosition.x / 2f) * 2f,
                Mathf.Round(cameraPosition.y / 2f) * 2f,
                0f);
        }

        private void UpdateSpawning(float delta)
        {
            if (enemies.Count >= 48)
            {
                return;
            }

            spawnTimer -= delta;
            if (spawnTimer > 0f)
            {
                return;
            }

            var normalized = elapsed / TestDuration;
            spawnTimer = Mathf.Lerp(0.72f, 0.28f, normalized);
            SpawnEnemy(false);
        }

        private void SpawnEnemy(bool isBoss)
        {
            var angle = UnityEngine.Random.value * Mathf.PI * 2f;
            var radius = isBoss ? 7.5f : UnityEngine.Random.Range(7.5f, 9.5f);
            var position = (Vector2)player.transform.position +
                           new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var chosenSprite = isBoss
                ? bossSprite
                : (UnityEngine.Random.value < 0.35f ? enemySpriteAlt : enemySprite);
            var enemyObject = CreateSpriteObject(
                isBoss ? "Fallen General" : "Pursuing Enemy",
                chosenSprite != null ? chosenSprite : solidSprite,
                position,
                isBoss ? 9 : 8,
                runtimeObjects);

            var renderer = enemyObject.GetComponent<SpriteRenderer>();
            var health = isBoss ? 220f : Mathf.Lerp(18f, 42f, elapsed / TestDuration);
            enemyObject.transform.localScale = Vector3.one *
                                               (isBoss ? 0.525f : UnityEngine.Random.Range(0.23f, 0.29f));
            if (chosenSprite == null)
            {
                renderer.color = isBoss ? new Color(0.55f, 0.12f, 0.16f) : new Color(0.45f, 0.20f, 0.18f);
            }

            var state = new EnemyState
            {
                Object = enemyObject,
                Renderer = renderer,
                Health = health,
                MaximumHealth = health,
                Speed = isBoss ? 1.125f : Mathf.Lerp(0.775f, 1.325f, elapsed / TestDuration),
                ContactDamage = isBoss ? 24f : 10f,
                IsBoss = isBoss
            };
            state.CombatTarget = new PrototypeCombatTarget(this, state, nextCombatTargetRuntimeId++);
            combatTargets.Register(state.CombatTarget);
            enemies.Add(state);
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
            SpawnEnemy(true);
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
                enemy.Object.transform.position = enemyPosition + direction * (enemy.Speed * enemy.MovementMultiplier * delta);
                enemy.Renderer.flipX = direction.x < 0f;

                var hitDistance = enemy.IsBoss ? 0.85f : 0.55f;
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
                if (id.Equals(WeaponId.HwandoFlyingBlade)) executor = new FlyingBladeExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ProjectileCount);
                else if (id.Equals(WeaponId.GakgungShot)) executor = new GakgungExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.Level);
                else if (id.Equals(WeaponId.TalismanThrow)) executor = new TalismanExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ChainCount, data.Level);
                else if (id.Equals(WeaponId.ThunderCrashBomb)) executor = new ThunderBombExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.DurationSeconds, 0.15f, data.Range * 0.45f, data.Level);
                else if (id.Equals(WeaponId.JangseungWard)) executor = new JangseungWardExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.ProjectileCount, data.Pierce, 0.2f, data.Level);
                else if (id.Equals(WeaponId.SingijeonVolley)) executor = new SingijeonExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ProjectileCount, data.Level);
                else if (id.Equals(WeaponId.FrostFlask)) executor = new FrostFlaskExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.DurationSeconds, data.DurationSeconds, data.Range * 0.35f, data.Pierce, data.Level);
                else if (id.Equals(WeaponId.WindThunderFan)) executor = new WindThunderFanExecutor(weaponRuntime, data.BaseDamage, data.CooldownSeconds, data.Range, data.Knockback, data.ChainCount, data.Level);
                else throw new InvalidOperationException($"No executor is available for '{id}'.");
                weaponRuntime.Register(executor);
                registeredWeaponIds.Add(id);
            }
        }

        private Sprite ResolveWeaponSprite(WeaponId id)
        {
            return weaponCatalog != null && weaponCatalog.TryGet(id, out var definition) && definition.PresentationSprites.Count > 0
                ? definition.PresentationSprites[0]
                : solidSprite;
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
            if (enemy.Health > 0f)
            {
                return;
            }

            var wasBoss = enemy.IsBoss;
            var wasTreasure = enemy.IsTreasure;
            var deathPosition = enemy.Object.transform.position;
            combatTargets.Unregister(enemy.CombatTarget);
            enemies.Remove(enemy);
            Destroy(enemy.Object);
            if (wasTreasure)
            {
                ScatterTreasure(deathPosition);
                return;
            }

            kills++;
            if (wasBoss)
            {
                bossAlive = false;
                EndRun(true);
                return;
            }

            SpawnPickup(deathPosition, PickupKind.Experience, 1);
            if (UnityEngine.Random.value < 0.01f)
            {
                SpawnPickup(
                    deathPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f),
                    PickupKind.Magnet,
                    0);
            }
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
            weaponRuntime.Dispose();
            weaponRuntime = new WeaponRuntimeController(combatTargets, combatDamageService, prototypeCombatMask);
            weaponRuntime.SetSpriteResolver(ResolveWeaponSprite);
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
                RebuildWeaponExecutorsForLevel();
                return new ProgressionRewardEvent(offer.Id, offer.Id, offer.NextLevel,
                    offer.NextLevel == 1 ? ProgressionRewardKind.NewWeapon : ProgressionRewardKind.WeaponLevel,
                    WeaponDisplayName(offer.Id), offer.NextLevel == 1 ? "새 무기 획득" : $"레벨 {offer.NextLevel} 효과 적용", ResolveWeaponSprite(new WeaponId(offer.Id)));
            }
            if (offer.Kind == UpgradeKind.Support)
            {
                supportLevels[offer.Id] = offer.NextLevel;
                ApplySupportUpgrade(offer.Id);
                return new ProgressionRewardEvent(offer.Id, null, offer.NextLevel, ProgressionRewardKind.Support,
                    SupportDisplayName(offer.Id), SupportDelta(offer.Id), null);
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

        private FirstPlayableUiState BuildUiState()
        {
            var weapons = new List<WeaponSlotView>(weaponLevels.Count);
            foreach (var weapon in weaponLevels)
            {
                weapons.Add(new WeaponSlotView(
                    weapon.Key,
                    WeaponDisplayName(weapon.Key),
                    weapon.Value,
                    ResolveWeaponSprite(new WeaponId(weapon.Key))));
            }

            var boss = enemies.Find(candidate => candidate.IsBoss && candidate.Object != null);
            return new FirstPlayableUiState(
                level, experience, experienceToNext, coins, kills, elapsed, TestDuration,
                playerHealth, playerMaxHealth, !bossSpawned && elapsed >= BossWarningTime, bossAlive,
                boss != null ? boss.Health : 0f, boss != null ? boss.MaximumHealth : 0f, weapons);
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

#if false
        private void LegacyOnGUI()
        {
            var scale = Mathf.Min(Screen.width / 1080f, Screen.height / 1920f);
            var offsetX = (Screen.width - 1080f * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, 0f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            var box = new GUIStyle(GUI.skin.box)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.12f, 0.12f, 0.10f) }
            };
            var centered = new GUIStyle(box) { alignment = TextAnchor.MiddleCenter };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 30, wordWrap = true };

            GUI.Box(new Rect(35f, 35f, 560f, 64f),
                $"레벨 {level}    경험 {experience}/{experienceToNext}    엽전 {coins}", box);
            GUI.Box(new Rect(805f, 35f, 240f, 84f),
                $"{Mathf.CeilToInt(Mathf.Max(0f, TestDuration - elapsed)):00}초", centered);
            GUI.Box(new Rect(35f, 1730f, 430f, 72f), $"처치 {kills}", box);
            GUI.Box(new Rect(480f, 1730f, 565f, 72f), "이동하며 금줄을 닫아 봉인!", centered);

            if (!bossSpawned && elapsed >= BossWarningTime)
            {
                GUI.Box(new Rect(165f, 250f, 750f, 100f), "⚠ 타락한 장수가 다가옵니다!", centered);
            }

            if (bossAlive)
            {
                var boss = enemies.Find(value => value.IsBoss && value.Object != null);
                if (boss != null)
                {
                    GUI.Box(new Rect(155f, 135f, 770f, 112f), string.Empty, centered);
                    GUI.Label(new Rect(180f, 145f, 720f, 42f), "타락한 장수", centered);

                    var previousColor = GUI.color;
                    GUI.color = new Color(0.12f, 0.08f, 0.08f, 0.95f);
                    GUI.DrawTexture(new Rect(195f, 194f, 690f, 24f), solidTexture);
                    GUI.color = new Color(0.82f, 0.16f, 0.18f, 1f);
                    GUI.DrawTexture(
                        new Rect(
                            198f,
                            197f,
                            684f * Mathf.Clamp01(boss.Health / boss.MaximumHealth),
                            18f),
                        solidTexture);
                    GUI.color = previousColor;
                    GUI.Label(
                        new Rect(195f, 187f, 690f, 38f),
                        $"{Mathf.CeilToInt(boss.Health)} / {Mathf.CeilToInt(boss.MaximumHealth)}",
                        centered);
                }
            }

            if (magnetMessageTimer > 0f)
            {
                GUI.Box(
                    new Rect(240f, 380f, 600f, 100f),
                    "\uD63C\uB839 \uB300\uD68C\uC218!",
                    centered);
            }

            if (upgradeOpen)
            {
                GUI.Box(new Rect(90f, 480f, 900f, 690f), $"레벨 {level} 달성!\n강화를 선택하세요", centered);
                for (var index = 0; index < upgradeOffers.Count; index++)
                {
                    if (GUI.Button(new Rect(160f, 650f + index * 155f, 760f, 120f),
                            upgradeOffers[index].Replace("|", "\n"), button))
                    {
                        TryChooseUpgrade(index);
                    }
                }
            }

            if (runEnded)
            {
                GUI.Box(new Rect(100f, 470f, 880f, 700f),
                    victory
                        ? $"봉인 성공!\n\n생존 {elapsed:0.0}초\n처치 {kills}  ·  레벨 {level}\n획득 엽전 {coins}"
                        : $"수렵 실패\n\n생존 {elapsed:0.0}초\n처치 {kills}  ·  레벨 {level}\n다시 도전해 보세요",
                    centered);
                if (GUI.Button(new Rect(260f, 1000f, 560f, 120f), "다시 시작  (R)", button))
                {
                    ResetRun();
                }
            }
        }
#endif
    }
}
