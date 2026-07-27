using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;
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

        private readonly List<EnemyState> enemies = new List<EnemyState>();
        private readonly List<PickupState> pickups = new List<PickupState>();
        private readonly List<Vector2> trail = new List<Vector2>();
        private readonly List<string> upgradeOffers = new List<string>();
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
        private FlyingBladeExecutor flyingBlade;
        private Texture2D solidTexture;
        private Sprite solidSprite;
        private Vector2 touchStart;
        private Vector2 movement;
        private float elapsed;
        private float playerHealth;
        private float playerMaxHealth;
        private float moveSpeed;
        private float attackDamage;
        private float attackCooldownMultiplier;
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
        private bool bossSpawned;
        private bool bossAlive;
        private bool upgradeOpen;
        private bool runEnded;
        private bool victory;

        private const float TestDuration = 60f;
        private const float BossWarningTime = 45f;
        private const float BossSpawnTime = 50f;

        /// <summary>Read-only combat event source for presentation components.</summary>
        public CombatDamageService CombatDamageService => combatDamageService;

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
        }

        private sealed class PrototypeCombatTarget : ICombatTarget
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
            public void ApplyResolvedDamage(int damage) => owner.DamageEnemy(state, damage);
            public void ApplyKnockback(Float2 direction, float force) { }
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
            weaponRuntime?.Reset();
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
            weaponRuntime?.Reset();
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
            combatTargets = new CombatTargetRegistry();
            combatDamageService = new CombatDamageService(combatTargets);
            weaponRuntime = new WeaponRuntimeController(combatTargets, combatDamageService, prototypeCombatMask);
            flyingBlade = new FlyingBladeExecutor(weaponRuntime, 12f, 0.42f, 4.5f, 10f, 1);
            weaponRuntime.Register(flyingBlade);

            elapsed = 0f;
            playerMaxHealth = 100f;
            playerHealth = playerMaxHealth;
            moveSpeed = 2.4f;
            attackDamage = 12f;
            attackCooldownMultiplier = 1f;
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
            coins = 0;
            kills = 0;
            bossSpawned = false;
            bossAlive = false;
            upgradeOpen = false;
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
            state.CombatTarget = new PrototypeCombatTarget(this, state, enemyObject.GetInstanceID());
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
            state.CombatTarget = new PrototypeCombatTarget(this, state, chestObject.GetInstanceID());
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

                var enemyPosition = (Vector2)enemy.Object.transform.position;
                var direction = (playerPosition - enemyPosition).normalized;
                enemy.Object.transform.position = enemyPosition + direction * (enemy.Speed * delta);
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

            // Prototype bridge: production content binding can replace these values with WeaponLevelData.
            var weaponLevel = Mathf.Clamp(level, 1, 5);
            flyingBlade.Reconfigure(
                attackDamage * (1f + (weaponLevel - 1) * 0.12f),
                Mathf.Max(0.16f, (0.42f - (weaponLevel - 1) * 0.035f) * attackCooldownMultiplier),
                4.5f + (weaponLevel - 1) * 0.4f,
                10f + (weaponLevel - 1) * 0.8f,
                weaponLevel >= 5 ? 3 : 1);
            weaponRuntime.Tick(delta, player.transform.position, runtimeObjects, solidSprite, 15);
        }

        private void DamageEnemy(EnemyState enemy, float damage)
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
            if (experience < experienceToNext)
            {
                return;
            }

            experience -= experienceToNext;
            level++;
            experienceToNext = 7 + level * 4;
            OpenUpgrade();
        }

        private void OpenUpgrade()
        {
            upgradeOpen = true;
            upgradeOffers.Clear();
            var available = new List<string>
            {
                "환도 단련|공격력 +25%",
                "쾌속 발도|공격 간격 -15%",
                "경공술|이동속도 +12%",
                "호신 부적|최대 체력 +20",
                "혼불 자석|습득 범위 +0.7",
                "금줄 강화|봉인 피해 +35%"
            };

            while (upgradeOffers.Count < 3)
            {
                var pick = available[UnityEngine.Random.Range(0, available.Count)];
                if (!upgradeOffers.Contains(pick))
                {
                    upgradeOffers.Add(pick);
                }
            }
        }

        private void ChooseUpgrade(int index)
        {
            if (!upgradeOpen || index < 0 || index >= upgradeOffers.Count)
            {
                return;
            }

            var choice = upgradeOffers[index];
            if (choice.StartsWith("환도", StringComparison.Ordinal)) attackDamage *= 1.25f;
            else if (choice.StartsWith("쾌속", StringComparison.Ordinal)) attackCooldownMultiplier = Mathf.Max(0.38f, attackCooldownMultiplier * 0.85f);
            else if (choice.StartsWith("경공", StringComparison.Ordinal)) moveSpeed *= 1.12f;
            else if (choice.StartsWith("호신", StringComparison.Ordinal))
            {
                playerMaxHealth += 20f;
                playerHealth = Mathf.Min(playerMaxHealth, playerHealth + 20f);
            }
            else if (choice.StartsWith("혼불", StringComparison.Ordinal)) pickupRadius += 0.7f;
            else if (choice.StartsWith("금줄", StringComparison.Ordinal)) geumjulDamage *= 1.35f;

            upgradeOpen = false;
            upgradeOffers.Clear();
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
                    DamageEnemy(enemy, enemy.IsBoss ? geumjulDamage * 0.35f : geumjulDamage);
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
                        ChooseUpgrade(index);
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
    }
}
