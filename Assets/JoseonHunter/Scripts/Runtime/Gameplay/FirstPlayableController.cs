using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Content;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using JoseonHunter.Runtime.Meta;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class FirstPlayableController : MonoBehaviour
    {
        private static readonly CombatVisualScaleProfile VisualScale =
            CombatVisualScaleProfile.MobilePortrait;

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
        [SerializeField] private JangseungGeumjulVisualLibrary jangseungGeumjulVisuals;

        private readonly List<EnemyState> enemies = new List<EnemyState>();
        private readonly List<EnemyState> separationEnemies = new List<EnemyState>();
        private readonly List<EnemySeparationAgent> separationAgents = new List<EnemySeparationAgent>();
        private readonly EnemySeparationGrid separationGrid = new EnemySeparationGrid(.84f);
        private readonly List<PickupState> pickups = new List<PickupState>();
        private readonly List<PickupState> pickupPool = new List<PickupState>();
        private readonly List<BossProjectileState> bossProjectiles = new List<BossProjectileState>();
        private readonly List<Vector2> trail = new List<Vector2>();
        private readonly List<string> upgradeOffers = new List<string>();
        private readonly List<UpgradeOffer> upgradeOfferData = new List<UpgradeOffer>();
        private readonly Dictionary<string, int> weaponLevels = new Dictionary<string, int>();
        private readonly Dictionary<string, int> supportLevels = new Dictionary<string, int>();
        private readonly HashSet<string> unlockedUpgradeIds = new HashSet<string>();
        private readonly HashSet<string> acquiredEvolutionIds = new HashSet<string>();
        private readonly HashSet<string> discardedWeaponIds = new HashSet<string>();
        private readonly HashSet<string> seenSpecialEnemyGuides = new HashSet<string>(StringComparer.Ordinal);
        private readonly WeaponEvolutionState evolutionState = new WeaponEvolutionState();
        private readonly WeaponRunAffixState weaponAffixes = new WeaponRunAffixState();
        private readonly WeaponLegacyState weaponLegacyState = new WeaponLegacyState();
        private readonly RunWeaponKillLedger runWeaponKillLedger = new RunWeaponKillLedger();
        private readonly Dictionary<int, EnemyMasteryClass> pendingMasteryDeaths = new Dictionary<int, EnemyMasteryClass>();
        private PendingWeaponChoice pendingWeaponChoice;
        private int affixRollOrdinal;
#if UNITY_INCLUDE_TESTS
        private Func<WeaponId, int, int, int, IAffixRandom> affixRandomFactoryForTests;
        private int? spawnSideForTests;
        private float? spawnTForTests;
        private float? spawnMarginForTests;
        private bool? forceEliteForTests;
#endif
        private readonly PixelHitMask prototypeCombatMask = new PixelHitMask(1, 1, Vector2.zero, 1f, new[] { 1u });
        private readonly Dictionary<Sprite, PixelHitMask> hurtMasksBySprite = new Dictionary<Sprite, PixelHitMask>();

        private Camera gameplayCamera;
        private GameFlowCoordinator flow;
        private Transform flatField;
        private BattlefieldTilePresenter battlefieldPresenter;
        private Transform runtimeObjects;
        private GameObject player;
        private SpriteRenderer playerRenderer;
        private CombatantVisualRig playerVisualRig;
        private Transform playerHealthFill;
        private GeumjulTrailPresenter geumjulPresenter;
        private BossTelegraphPresenter bossTelegraphPresenter;
        private CombatTargetRegistry combatTargets;
        private CombatDamageService combatDamageService;
        private WeaponRuntimeController weaponRuntime;
        private readonly WeaponPixelMaskCatalog weaponMasks = new WeaponPixelMaskCatalog();
        private readonly List<WeaponId> registeredWeaponIds = new List<WeaponId>();
        private EnemySpriteRoster enemySpriteRoster;
        private WaveSpawnDirector waveSpawnDirector;
        private Texture2D solidTexture;
        private Sprite solidSprite;
        private Material pickupTrailMaterial;
        private SpriteRenderer experienceAbsorbFlash;
        private float experienceAbsorbFlashTimer;
        private Vector2 touchStart;
        private Vector2 movement;
        private Vector3 cameraFollowVelocity;
        private float elapsed;
        private float playerHealth;
        private float playerMaxHealth;
        private float moveSpeed;
        private float pickupRadius;
        private float runDamageMultiplier = 1f;
        private float runExperienceMultiplier = 1f;
        private float runIncomingDamageMultiplier = 1f;
        private float geumjulDamage;
        private float contactInvulnerability;
        private float spawnTimer;
        private float chestSpawnTimer;
        private float trailTimer;
        private float sealCooldown;
        private float magnetMessageTimer;
        private float upgradeQueueGraceRemaining;
        private int experience;
        private int experienceToNext;
        private int level;
        private int coins;
        private int kills;
        private int nextCombatTargetRuntimeId;
        private int pendingUpgradeCount;
        private bool bossSpawned;
        private bool bossAlive;
        private bool magnetSweepActive;
        private bool upgradeOpen;
        private bool awaitingUpgradePresentationClose;
        private bool runEnded;
        private bool victory;
        private bool runAbandoned;
        private bool settlementPrepared;
        private bool settlementSucceeded;
        private bool settlementFailed;
        private int accountExperienceEarned;
        private int accountLevelBefore = 1;
        private int accountLevelAfter = 1;
        private bool returningToLobby;
        private RunSettlement pendingSettlement;
        private StagePacingTimeline stageTimeline;
        private int processedStageMilestones;
        private bool finalBossWarning;
        private string waveAnnouncement = string.Empty;
        private float waveAnnouncementTimer;
        private int waveAnnouncementIntensity;
        private int normalRoleAnnouncementMask;
#if UNITY_INCLUDE_TESTS
        public string LastSpecialEnemyGuideForTests { get; private set; } = string.Empty;
        public int SpecialEnemyGuideCountForTests { get; private set; }
        private bool suppressAutomaticSpawningForTests;
#endif

        private const float PrototypeDurationSeconds = StagePacingTimeline.CanonicalDurationSeconds;
        private const int RunSpawnSeed = 0x4A4F5345;
        private const string JangseungGeumjulResourcesPath = "Presentation/JangseungGeumjulVisualLibrary";
        private const string BattlefieldPresentationResourcesPath = "Presentation/BattlefieldPresentationLibrary";
        private const float StartingPickupRadius = .58f;
        private const float QueuedUpgradeGraceSeconds = 1f;

        /// <summary>Read-only combat event source for presentation components.</summary>
        public CombatDamageService CombatDamageService => combatDamageService;
        public GameFlowCoordinator Flow => flow;
        public WeaponRuntimeController WeaponRuntime => weaponRuntime;
        public IReadOnlyList<WeaponId> RegisteredWeaponIds => registeredWeaponIds;
        public FirstPlayableUiState UiState => BuildUiState();
        public bool IsUpgradeOpen => upgradeOpen;
        public IReadOnlyCollection<string> AcquiredEvolutionIds => acquiredEvolutionIds;
        public event Action<UpgradeChoiceState> UpgradeOpened;
        public event Action<WeaponReplacementState> WeaponReplacementOpened;
        public event Action<WeaponLegacyChoiceState> WeaponLegacyOpened;
        public event Action<ProgressionRewardEvent> UpgradeChosen;
        public event Action RunReset;

#if UNITY_INCLUDE_TESTS
        public IReadOnlyList<UpgradeOffer> CurrentOffers => upgradeOfferData;
        public JangseungGeumjulVisualLibrary ResolvedJangseungGeumjulVisualLibraryForTests => ResolveJangseungGeumjulVisualLibrary();
        public GeumjulTrailPresenter GeumjulPresenterForTests => geumjulPresenter;
        public int AppliedUpgradeCount { get; private set; }
        public int WeaponRebuildCountForTests { get; private set; }
        public int MidBossSpawnCountForTests { get; private set; }
        public int FinalBossSpawnCountForTests { get; private set; }
        public int PackSpawnCountForTests { get; private set; }
        public int PendingUpgradeCountForTests => pendingUpgradeCount;
        public float LastSpawnScaleForTests { get; private set; }
        public bool BossTelegraphVisibleForTests => bossTelegraphPresenter != null && bossTelegraphPresenter.IsVisible;
        public int ActiveExperiencePickupCountForTests => pickups.Count(pickup => pickup.Kind == PickupKind.Experience);
        public int TotalExperiencePickupValueForTests => pickups.Where(pickup => pickup.Kind == PickupKind.Experience).Sum(pickup => pickup.Value);
        public void SpawnExperiencePickupForTests(Vector2 position, int value) => SpawnPickup(position, PickupKind.Experience, value);
        public IReadOnlyDictionary<WeaponId, int> RunMasterySnapshotForTests => runWeaponKillLedger.Snapshot();
        public float StartingMaximumHealthForTests => playerMaxHealth;
        public float StartingDamageMultiplierForTests => runDamageMultiplier;
        public float StartingMoveSpeedForTests => moveSpeed;
        public float StartingPickupRadiusForTests => pickupRadius;
        public float StartingIncomingDamageMultiplierForTests => runIncomingDamageMultiplier;
        public bool RunEndedForTests => runEnded;
        public bool VictoryForTests => victory;
        public bool SettlementSucceededForTests => settlementSucceeded;
        public void AdvanceStageForTests(float previousElapsed, float currentElapsed)
        {
            elapsed = Mathf.Clamp(currentElapsed, 0f, PrototypeDurationSeconds);
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
            if (!upgradeOpen && !flow.TryTransition(GameFlowState.LevelUpSelection)) return;
            upgradeOpen = true;
            pendingWeaponChoice = null;
            upgradeOfferData.Clear();
            upgradeOffers.Clear();
            if (offers != null) upgradeOfferData.AddRange(offers);
            var choices = new List<UpgradeChoiceView>(upgradeOfferData.Count);
            foreach (var offer in upgradeOfferData)
            {
                upgradeOffers.Add(FormatUpgradeOffer(offer));
                choices.Add(BuildUpgradeChoiceView(offer));
            }
            UpgradeOpened?.Invoke(new UpgradeChoiceState(level, choices));
        }
        /// <summary>Publishes forced offers atomically so tests exercise the same controller and visible-card identities.</summary>
        public void OpenUpgradeOffersForTests(params UpgradeOffer[] offers)
        {
            if (!flow.TryTransition(GameFlowState.LevelUpSelection)) return;
            upgradeOpen = true;
            pendingWeaponChoice = null;
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
        public void EndRunForTests(bool didWin) => EndRun(didWin);
        public void AwardRunProgressForTests(WeaponId weaponId, int mastery, int earnedCoins)
        {
            if (mastery < 0 || earnedCoins < 0) throw new ArgumentOutOfRangeException();
            for (var index = 0; index < mastery; index++)
            {
                var targetId = 1000000 + index;
                runWeaponKillLedger.RecordHit(targetId, weaponId);
                runWeaponKillLedger.ConfirmDeath(targetId, EnemyMasteryClass.Normal);
            }
            coins += earnedCoins;
            kills += mastery;
        }
        public void SetWeaponLevelForTests(WeaponId weaponId, int weaponLevel)
        {
            weaponLevels[weaponId.Value] = weaponLevel;
            RebuildWeaponExecutorsForLevel();
        }
        public int WeaponLevelForTests(WeaponId weaponId) => weaponLevels[weaponId.Value];
        public bool HasWeaponForTests(WeaponId weaponId) => weaponLevels.ContainsKey(weaponId.Value);
        public bool IsWeaponDiscardedForTests(WeaponId weaponId) => discardedWeaponIds.Contains(weaponId.Value);
        public WeaponLegacySnapshot LegacySnapshotForTests(WeaponId weaponId) =>
            weaponLegacyState.SnapshotFor(weaponId,
                weaponLevels.TryGetValue(weaponId.Value, out var weaponLevel) ? weaponLevel : 0);
        public bool ChooseWeaponLegacyForTests(WeaponId weaponId, WeaponLegacyPathId pathId)
        {
            weaponLegacyState.Remove(weaponId);
            if (!weaponLegacyState.TryChoose(weaponId, pathId)) return false;
            RebuildWeaponExecutorsForLevel();
            return true;
        }
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
        public ICombatTarget SpawnSpecialEnemyForTests(string contentId, Vector2 position)
        {
            SpawnEnemy(false, 0, contentId);
            var state = enemies[enemies.Count - 1];
            state.Object.transform.position = position;
            state.Facing = player == null ? Vector2.left : ((Vector2)player.transform.position - position).normalized;
            return state.CombatTarget;
        }
        public int LivingSpecialEnemyCountForTests => enemies.Count(enemy => enemy.Object != null &&
            !enemy.IsTreasure && !enemy.IsBoss && !enemy.IsMidBoss && enemy.ArchetypeProfile != null &&
            enemy.ArchetypeProfile.IsSpecial);
        public int LivingNormalOnlyEnemyCountForTests => enemies.Count(enemy => enemy.Object != null &&
            !enemy.IsTreasure && !enemy.IsBoss && !enemy.IsMidBoss && (enemy.ArchetypeProfile == null ||
            !enemy.ArchetypeProfile.IsSpecial));
        public Sprite EnemySpriteForTests(ICombatTarget target) => FindEnemyState(target)?.Renderer?.sprite;
        public int ShieldChargesForTests(ICombatTarget target) => FindEnemyState(target)?.ShieldCharges ?? 0;
        public float ShieldBarFillForTests(ICombatTarget target)
        {
            var fill = FindEnemyState(target)?.ShieldFill;
            return fill == null ? 0f : fill.localScale.x / 2f;
        }
        public bool HasShieldBarForTests(ICombatTarget target)
        {
            var fill = FindEnemyState(target)?.ShieldFill;
            return fill != null && fill.gameObject.activeInHierarchy;
        }
        public Vector2 LastSpawnPositionForTests { get; private set; }
        public Vector2 LastSpawnRootPositionForTests { get; private set; }
        public Bounds LastSpawnRendererBoundsForTests { get; private set; }
        public void SpawnEnemyAtCurrentViewportForTests() => SpawnEnemy(false);
        public void ConfigureViewportSpawnForTests(int side, float t, float margin, bool forceElite)
        {
            spawnSideForTests = side;
            spawnTForTests = t;
            spawnMarginForTests = margin;
            forceEliteForTests = forceElite;
        }
        public void ClearViewportSpawnForTests()
        {
            spawnSideForTests = null;
            spawnTForTests = null;
            spawnMarginForTests = null;
            forceEliteForTests = null;
        }
        public void SpawnEnemyForViewportClearanceTests(bool isBoss, int midBossTier) => SpawnEnemy(isBoss, midBossTier);
        public void SpawnEnemyForSeparationTests(Vector2 position)
        {
            SpawnEnemy(false);
            enemies[enemies.Count - 1].Object.transform.position = position;
        }
        public void SpawnTreasureForSeparationTests(Vector2 position)
        {
            SpawnTreasureChest();
            enemies[enemies.Count - 1].Object.transform.position = position;
        }
        public void ConfigureSeparationLoadScenarioForTests()
        {
            spawnTimer = float.PositiveInfinity;
            chestSpawnTimer = float.PositiveInfinity;
            suppressAutomaticSpawningForTests = true;
        }
        public bool TickGameplayIfRunningForTests(float delta) => TickGameplayIfRunning(delta);
        public void UpdateEnemiesForTests(float delta) => UpdateEnemies(delta);
        public void SpawnBurstForTests(int count) => SpawnBurst(count);
        public void ConfigureFinalSurgePacingForTests() => elapsed = stageTimeline.ToRunSeconds(720f);
        public float ElapsedForTests => elapsed;
        public void RestoreElapsedForTests(float value) => elapsed = value;
        public void SetElapsedForTests(float value)
        {
            elapsed = Mathf.Clamp(value, 0f, PrototypeDurationSeconds);
            spawnTimer = 0f;
            waveSpawnDirector?.Reset();
            normalRoleAnnouncementMask = 0;
            PackSpawnCountForTests = 0;
        }
        public void TickSpawningForTests(float delta) => UpdateSpawning(delta);
        public int EnemyCountForTests => enemies.Count;
        public void SpawnEnemyForLifecycleTests() => SpawnEnemy(false);
        public void DestroyLastEnemyForLifecycleTests()
        {
            if (enemies.Count != 0) ApplyEnemyDamage(enemies[enemies.Count - 1], float.MaxValue);
        }
        public void SetContactInvulnerabilityForTests(float seconds) => contactInvulnerability = Mathf.Max(0f, seconds);
        public float ContactInvulnerabilityForTests => contactInvulnerability;
        public int LastSeparationAgentCountForTests { get; private set; }
        private readonly List<Vector2> livingEnemyPositionsForTests = new List<Vector2>();
        private readonly List<string> livingNormalEnemyIdsForTests = new List<string>();
        private readonly List<string> livingSpecialEnemyIdsForTests = new List<string>();
        public IReadOnlyList<string> LivingNormalEnemyIdsForTests
        {
            get
            {
                livingNormalEnemyIdsForTests.Clear();
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy.Object == null || enemy.IsTreasure || enemy.IsBoss || enemy.IsMidBoss ||
                        (enemy.ArchetypeProfile != null && enemy.ArchetypeProfile.IsSpecial)) continue;
                    livingNormalEnemyIdsForTests.Add(enemy.ContentId);
                }
                return livingNormalEnemyIdsForTests;
            }
        }
        public IReadOnlyList<string> LivingSpecialEnemyIdsForTests
        {
            get
            {
                livingSpecialEnemyIdsForTests.Clear();
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy.Object == null || enemy.IsTreasure || enemy.IsBoss || enemy.IsMidBoss ||
                        enemy.ArchetypeProfile == null || !enemy.ArchetypeProfile.IsSpecial) continue;
                    livingSpecialEnemyIdsForTests.Add(enemy.ContentId);
                }
                return livingSpecialEnemyIdsForTests;
            }
        }
        public IReadOnlyList<Vector2> LivingEnemyPositionsForTests
        {
            get
            {
                livingEnemyPositionsForTests.Clear();
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy.Object != null && !enemy.IsTreasure)
                        livingEnemyPositionsForTests.Add(enemy.Object.transform.position);
                }
                return livingEnemyPositionsForTests;
            }
        }
        public float AverageLivingEnemyDistanceToPlayerForTests
        {
            get
            {
                var count = 0;
                var total = 0f;
                var playerPosition = (Vector2)player.transform.position;
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy.Object == null || enemy.IsTreasure) continue;
                    total += Vector2.Distance(enemy.Object.transform.position, playerPosition);
                    count++;
                }
                return count == 0 ? 0f : total / count;
            }
        }
#endif

        private EnemyState FindEnemyState(ICombatTarget target) => target == null
            ? null
            : enemies.Find(enemy => ReferenceEquals(enemy.CombatTarget, target));

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

        private sealed class PendingWeaponChoice
        {
            public PendingWeaponChoice(UpgradeOffer offer)
            {
                Offer = offer;
                ResolvedLevel = offer.NextLevel;
            }

            public UpgradeOffer Offer { get; }
            public string DiscardedWeaponId { get; set; }
            public int ResolvedLevel { get; set; }
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
            public Transform ShieldFill;
            public int ShieldCharges;
            public bool GuardHitPending;
            public bool GuardBrokePending;
            public float NextContactTime;
            public bool IsBoss;
            public bool IsElite;
            public bool IsMidBoss;
            public int MidBossTier;
            public bool IsTreasure;
            public int ExperienceValue = 1;
            public string ContentId;
            public EnemyArchetypeProfile ArchetypeProfile;
            public SpecialEnemyMotionState SpecialMotion;
            public Vector2 Facing = Vector2.left;
            public float AuraRemaining;
            public bool WasKnockedBack;
            public IReadOnlyList<Sprite> SpecialFrames;
            public float SpecialAnimationTime;
            public ICombatTarget CombatTarget;
            public BossCombatRole BossRole;
            public BossAttackController BossAttack;
            public BossAttackSnapshot BossAttackSnapshot;
            public Vector2 ChargeStart;
            public bool BossAttackHitApplied;
            public float ContactRadius;
            private readonly Dictionary<int, float> frostSlowSources = new Dictionary<int, float>();
            private readonly Dictionary<int, float> freezeSources = new Dictionary<int, float>();
            private readonly Dictionary<int, float> jangseungWardSources = new Dictionary<int, float>();
            private readonly Dictionary<int, float> jangseungContactProtectionSources = new Dictionary<int, float>();
            private readonly List<int> statusSourceScratch = new List<int>();
            private float slowDecayRemaining;
            private float slowDecayStartMultiplier = 1f;
            private float staggerRemaining;

            public void ApplyFrostSlow(int sourceId, float strength)
            {
                var resolved = Mathf.Clamp01(strength);
                if (IsBoss || IsMidBoss) resolved = 1f - (1f - resolved) * .35f;
                frostSlowSources[sourceId] = resolved;
                slowDecayRemaining = 0f;
            }

            public void RemoveFrostSlow(int sourceId, float decaySeconds)
            {
                var previousMultiplier = SlowMultiplier();
                if (!frostSlowSources.Remove(sourceId) || frostSlowSources.Count != 0) return;
                slowDecayStartMultiplier = previousMultiplier;
                slowDecayRemaining = Mathf.Max(0f, decaySeconds);
            }

            public void ApplyFreeze(int sourceId, float durationSeconds)
            {
                var resolved = IsBoss || IsMidBoss ? Mathf.Min(.25f, durationSeconds) : durationSeconds;
                freezeSources[sourceId] = Mathf.Max(
                    freezeSources.TryGetValue(sourceId, out var remaining) ? remaining : 0f,
                    Mathf.Max(0f, resolved));
            }

            public void ApplyJangseungWard(int sourceId, float strength) => jangseungWardSources[sourceId] = Mathf.Clamp01(strength);

            public void RemoveJangseungWard(int sourceId) => jangseungWardSources.Remove(sourceId);

            public void ApplyJangseungContactProtection(int sourceId, float reduction) =>
                jangseungContactProtectionSources[sourceId] = Mathf.Clamp01(reduction);

            public void RemoveJangseungContactProtection(int sourceId) =>
                jangseungContactProtectionSources.Remove(sourceId);

            public float ContactDamageMultiplier
            {
                get
                {
                    var multiplier = 1f;
                    foreach (var source in jangseungContactProtectionSources)
                        multiplier = Mathf.Min(multiplier, 1f - source.Value);
                    return multiplier;
                }
            }

            public void ApplyStagger(float durationSeconds)
            {
                if (!float.IsNaN(durationSeconds) && !float.IsInfinity(durationSeconds) && durationSeconds > 0f)
                    staggerRemaining = Mathf.Max(staggerRemaining, durationSeconds);
            }

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
                staggerRemaining = Mathf.Max(0f, staggerRemaining - delta);
                AuraRemaining = Mathf.Max(0f, AuraRemaining - delta);
            }

            public float MovementMultiplier
            {
                get
                {
                    if (freezeSources.Count > 0 || staggerRemaining > 0f) return 0f;
                    if (frostSlowSources.Count > 0) return SlowMultiplier();
                    if (jangseungWardSources.Count > 0) return WardMultiplier();
                    return slowDecayRemaining <= 0f ? 1f : Mathf.Lerp(1f, slowDecayStartMultiplier, slowDecayRemaining / 0.35f);
                }
            }

            public bool IsControlled => freezeSources.Count > 0 || staggerRemaining > 0f;
            public float AuraMultiplier => AuraRemaining > 0f ? 1.2f : 1f;

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

        private sealed class PrototypeCombatTarget : ICombatTarget, IFrostStatusTarget, IJangseungWardStatusTarget,
            IJangseungContactDamageTarget,
            IControlStatusTarget, IConfirmedDamageResistanceTarget
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
            public void ApplyResolvedDamage(int damage) => owner.ApplyEnemyDamage(state, damage, true);
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
                state.WasKnockedBack = true;
            }
            public void ApplyFrostSlow(int sourceId, float strength) => state.ApplyFrostSlow(sourceId, strength);
            public void RemoveFrostSlow(int sourceId, float decaySeconds) => state.RemoveFrostSlow(sourceId, decaySeconds);
            public void ApplyFreeze(int sourceId, float durationSeconds) => state.ApplyFreeze(sourceId, durationSeconds);
            public void ApplyJangseungWard(int sourceId, float strength) => state.ApplyJangseungWard(sourceId, strength);
            public void RemoveJangseungWard(int sourceId) => state.RemoveJangseungWard(sourceId);
            public void ApplyJangseungContactProtection(int sourceId, float reduction) =>
                state.ApplyJangseungContactProtection(sourceId, reduction);
            public void RemoveJangseungContactProtection(int sourceId) =>
                state.RemoveJangseungContactProtection(sourceId);
            public void ApplyStagger(float durationSeconds) => state.ApplyStagger(durationSeconds);
            public float IncomingDamageMultiplier(Float2 attackOrigin, WeaponHitTrait traits)
            {
                if (state.ArchetypeProfile == null || state.Object == null) return 1f;
                var position = (Vector2)state.Object.transform.position;
                var toOrigin = new Vector2(attackOrigin.X - position.x, attackOrigin.Y - position.y);
                return state.ArchetypeProfile.Archetype == EnemyArchetype.ShieldDokkaebi
                    ? ShieldDokkaebiGuard.IncomingDamageMultiplier(state.ShieldCharges, state.Facing,
                        toOrigin, traits)
                    : 1f;
            }

            public void ConfirmIncomingHit(Float2 attackOrigin, WeaponHitTrait traits)
            {
                if (state.ArchetypeProfile == null || state.Object == null ||
                    state.ArchetypeProfile.Archetype != EnemyArchetype.ShieldDokkaebi) return;
                var position = (Vector2)state.Object.transform.position;
                var toOrigin = new Vector2(attackOrigin.X - position.x, attackOrigin.Y - position.y);
                var result = ShieldDokkaebiGuard.ConfirmHit(state.ShieldCharges, state.Facing, toOrigin, traits);
                if (!result.Blocked) return;
                state.ShieldCharges = result.RemainingCharges;
                state.GuardHitPending = true;
                state.GuardBrokePending = result.Broke;
                UpdateBarFill(state.ShieldFill,
                    state.ShieldCharges / (float)ShieldDokkaebiGuard.MaximumCharges, 2f, .10f);
                if (!result.Broke || state.ShieldFill == null) return;
                UnityEngine.Object.Destroy(state.ShieldFill.parent.gameObject);
                state.ShieldFill = null;
            }
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
            public bool Attracting;
            public float AttractionAge;
            public TrailRenderer Trail;
            public float BaseScale;
        }

        private sealed class BossProjectileState
        {
            public GameObject Object;
            public Vector2 Velocity;
            public float Remaining;
        }

        private void Awake()
        {
            GameplayReadySignal.Reset();
            flow = GetComponent<GameFlowCoordinator>() ?? gameObject.AddComponent<GameFlowCoordinator>();
            Application.targetFrameRate = 60;
            SetupCamera();
            CreateSharedSprite();
            CreateField();
            ResetRun();
            GameplayReadySignal.MarkReady();
        }

        private void OnDestroy()
        {
            geumjulPresenter?.Clear();
            bossTelegraphPresenter?.Dispose();
            bossTelegraphPresenter = null;
            if (combatDamageService != null) combatDamageService.DamageConfirmed -= OnCombatDamageConfirmed;
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

            if (pickupTrailMaterial != null)
            {
                Destroy(pickupTrailMaterial);
            }
        }

        private void Update()
        {
            using (FirstPlayableProfilerMarkers.RunUpdate.Auto())
            {
                if (runEnded)
                {
                    if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    {
                        ReturnToLobby();
                    }

                    return;
                }

                TickGameplayIfRunning(Time.deltaTime);
            }
        }

        private bool TickGameplayIfRunning(float delta)
        {
            if (runEnded || flow == null || !flow.IsGameplayRunning) return false;
            TickGameplay(delta);
            return true;
        }

        private void TickGameplay(float delta)
        {
            if (upgradeOpen)
            {
                return;
            }

            if (upgradeQueueGraceRemaining > 0f)
            {
                upgradeQueueGraceRemaining = Mathf.Max(0f,
                    upgradeQueueGraceRemaining - Mathf.Max(0f, delta));
                if (upgradeQueueGraceRemaining <= 0f && OpenNextPendingUpgrade())
                    return;
            }

            var previousElapsed = elapsed;
            elapsed = Mathf.Min(PrototypeDurationSeconds, elapsed + delta);
            contactInvulnerability = Mathf.Max(0f, contactInvulnerability - delta);
            sealCooldown = Mathf.Max(0f, sealCooldown - delta);
            magnetMessageTimer = Mathf.Max(0f, magnetMessageTimer - delta);
            waveAnnouncementTimer = Mathf.Max(0f, waveAnnouncementTimer - delta);
            ProcessStageMilestones(previousElapsed, elapsed);

            ReadMovement();
            UpdatePlayer(delta);
            using (FirstPlayableProfilerMarkers.Spawn.Auto())
            {
                UpdateSpawning(delta);
                UpdateTreasureSpawning(delta);
            }
            UpdateEnemies(delta);
            UpdateBossProjectiles(delta);
            using (FirstPlayableProfilerMarkers.Weapon.Auto()) UpdateAttack(delta);
            UpdateExperienceAbsorbFlash(delta);
            using (FirstPlayableProfilerMarkers.Pickup.Auto()) UpdatePickups(delta);
            UpdateGeumjul(delta);
            UpdateField();

        }

        private void LateUpdate()
        {
            if (flow != null && flow.IsGameplayRunning && gameplayCamera != null && player != null)
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
            var spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                pickupTrailMaterial = new Material(spriteShader)
                {
                    name = "ExperiencePickupTrailMaterial"
                };
            }
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
            battlefieldPresenter = flatField.gameObject.AddComponent<BattlefieldTilePresenter>();
            var presentation = Resources.Load<BattlefieldPresentationLibrary>(
                BattlefieldPresentationResourcesPath);
            if (presentation != null && presentation.GroundTile != null)
            {
                battlefieldPresenter.BuildInfinite(
                    presentation.ChunkPrefab,
                    presentation.GroundTile,
                    presentation.AlternateGroundTile,
                    presentation.Decorations,
                    solidSprite,
                    0x4A4F5345);
            }
            else
            {
                battlefieldPresenter.Build(
                    battlefieldTilePrimary,
                    battlefieldTileAlternate,
                    battlefieldDecals,
                    solidSprite);
            }
        }

        private void ResetRun()
        {
            flow?.ResetToPlaying();
            var visualLibrary = ResolveJangseungGeumjulVisualLibrary();
            geumjulPresenter?.Clear();
            bossTelegraphPresenter?.Dispose();
            bossTelegraphPresenter = null;
            if (combatDamageService != null) combatDamageService.DamageConfirmed -= OnCombatDamageConfirmed;
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
            pickupPool.Clear();
            bossProjectiles.Clear();
            trail.Clear();
            upgradeOffers.Clear();
            upgradeOfferData.Clear();
            pendingWeaponChoice = null;
            weaponLevels.Clear();
            var metaSession = MetaGameSession.Current;
            var patrolLoadout = metaSession != null ? metaSession.ActiveLoadout : null;
            var startingWeapon = patrolLoadout != null
                ? patrolLoadout.StartingWeapon
                : WeaponId.HwandoFlyingBlade;
            weaponLevels.Add(startingWeapon.Value, 1);
            supportLevels.Clear();
            unlockedUpgradeIds.Clear();
            acquiredEvolutionIds.Clear();
            discardedWeaponIds.Clear();
            seenSpecialEnemyGuides.Clear();
            weaponAffixes.Clear();
            weaponLegacyState.Clear();
            if (patrolLoadout != null)
                foreach (var style in patrolLoadout.Styles)
                    weaponLegacyState.EquipForRun(style.Key, style.Value);
            runWeaponKillLedger.Reset();
            pendingMasteryDeaths.Clear();
            affixRollOrdinal = 0;
            evolutionState.Clear();
            foreach (var evolution in WeaponEvolutionCatalog.All) unlockedUpgradeIds.Add(evolution.Id);
            combatTargets = new CombatTargetRegistry();
            combatDamageService = new CombatDamageService(combatTargets);
            combatDamageService.DamageConfirmed += OnCombatDamageConfirmed;
            weaponRuntime = new WeaponRuntimeController(combatTargets, combatDamageService, prototypeCombatMask);
            weaponRuntime.SetTargetVisibilityResolver(IsInsideGameplayViewport);
            weaponRuntime.SetSpriteResolver(ResolveWeaponSprite);
            weaponRuntime.SetPresentationSpriteResolver(ResolveWeaponPresentationSprite);
            weaponRuntime.SetMaskResolver(ResolveWeaponMask);
            weaponRuntime.SetJangseungGeumjulVisualLibrary(visualLibrary);
            elapsed = 0f;
            stageTimeline = StagePacingTimeline.ForDuration(PrototypeDurationSeconds);
            enemySpriteRoster = new EnemySpriteRoster(enemySprite, enemySpriteAlt, enemySprites,
                CombatChoiceVisualCatalog.LoadDefault());
            waveSpawnDirector = new WaveSpawnDirector(RunSpawnSeed);
            processedStageMilestones = 0;
            finalBossWarning = false;
            waveAnnouncement = string.Empty;
            waveAnnouncementTimer = 0f;
            waveAnnouncementIntensity = 0;
            normalRoleAnnouncementMask = 0;
            var training = metaSession != null ? new CommonTrainingProgression(metaSession.Data) : null;
            var vitalityMultiplier = training?.Multiplier(CommonTrainingId.Vitality) ?? 1f;
            runDamageMultiplier = training?.Multiplier(CommonTrainingId.Power) ?? 1f;
            var movementMultiplier = training?.Multiplier(CommonTrainingId.Footwork) ?? 1f;
            runExperienceMultiplier = training?.Multiplier(CommonTrainingId.Learning) ?? 1f;
            var resonanceMultiplier = training?.Multiplier(CommonTrainingId.Resonance) ?? 1f;
            runIncomingDamageMultiplier = training?.DamageTakenMultiplier() ?? 1f;
            playerMaxHealth = 100f * vitalityMultiplier;
            playerHealth = playerMaxHealth;
            moveSpeed = 2.4f * movementMultiplier;
            pickupRadius = StartingPickupRadius * resonanceMultiplier;
            geumjulDamage = 38f * runDamageMultiplier;
            spawnTimer = 0.2f;
            chestSpawnTimer = 18f;
            trailTimer = 0f;
            sealCooldown = 0f;
            magnetMessageTimer = 0f;
            upgradeQueueGraceRemaining = 0f;
            experience = 0;
            level = 1;
            experienceToNext = ExperienceCurve.GetThresholdForNextLevel(level);
            registeredWeaponIds.Clear();
            weaponMasks.Load(weaponCatalog);
            RegisterCatalogWeapons();
            coins = 0;
            kills = 0;
            nextCombatTargetRuntimeId = 1;
            pendingUpgradeCount = 0;
            bossSpawned = false;
            bossAlive = false;
            magnetSweepActive = false;
            upgradeOpen = false;
            awaitingUpgradePresentationClose = false;
            runEnded = false;
            victory = false;
            runAbandoned = false;
            settlementPrepared = false;
            settlementSucceeded = false;
            settlementFailed = false;
            accountExperienceEarned = 0;
            accountLevelBefore = 1;
            accountLevelAfter = 1;
            returningToLobby = false;

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
            CreateExperienceAbsorbFlash();

            geumjulPresenter = new GameObject("Geumjul Presentation")
                .AddComponent<GeumjulTrailPresenter>();
            geumjulPresenter.transform.SetParent(runtimeObjects, false);
            geumjulPresenter.Configure(visualLibrary, runtimeObjects, 4);
            bossTelegraphPresenter = new BossTelegraphPresenter(runtimeObjects);

            gameplayCamera.transform.position = new Vector3(0f, 0f, -10f);
            cameraFollowVelocity = Vector3.zero;
#if UNITY_INCLUDE_TESTS
            AppliedUpgradeCount = 0;
            MidBossSpawnCountForTests = 0;
            FinalBossSpawnCountForTests = 0;
            PackSpawnCountForTests = 0;
            LastSpecialEnemyGuideForTests = string.Empty;
            SpecialEnemyGuideCountForTests = 0;
            suppressAutomaticSpawningForTests = false;
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

            UpdateBarFill(playerHealthFill, playerHealth / playerMaxHealth, 2f, .14f);
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
                Time.deltaTime);
        }

        private bool IsInsideGameplayViewport(Float2 position)
        {
            if (gameplayCamera == null) return true;
            var viewport = gameplayCamera.WorldToViewportPoint(new Vector3(position.X, position.Y, 0f));
            return viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f &&
                   viewport.y >= 0f && viewport.y <= 1f;
        }

        private void UpdateField()
        {
            if (battlefieldPresenter != null && player != null)
                battlefieldPresenter.Track(player.transform.position);
        }

        private void UpdateSpawning(float delta)
        {
#if UNITY_INCLUDE_TESTS
            if (suppressAutomaticSpawningForTests) return;
#endif
            var phase = RunClock.PhaseAt(elapsed);
            var wave = WaveSchedule.For(phase);
            if (phase == RunPhase.BossWarning || phase == RunPhase.Boss || phase == RunPhase.Expired)
                return;

            ShowNormalRoleAnnouncements();

            var activeEnemyCount = ActiveCombatEnemyCount();
            var availableSlots = Mathf.Max(0, wave.ActiveCap - activeEnemyCount);
            if (waveSpawnDirector.TryCreateIntroduction(elapsed, availableSlots, out var introduction))
            {
                for (var index = 0; index < introduction.SpawnCount && activeEnemyCount < wave.ActiveCap; index++)
                {
                    SpawnEnemy(false, 0, introduction.ContentId);
                    activeEnemyCount++;
                }
            }

            availableSlots = Mathf.Max(0, wave.ActiveCap - activeEnemyCount);
            if (waveSpawnDirector.TryCreatePack(elapsed, availableSlots, out var pack))
            {
                activeEnemyCount += SpawnPack(pack, wave.ActiveCap, activeEnemyCount);
            }

            var pacing = stageTimeline.Sample(elapsed);
            if (activeEnemyCount >= wave.ActiveCap)
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
                 index < batchSize && activeEnemyCount < wave.ActiveCap;
                 index++)
            {
                CountLivingEnemyKinds(out var livingNormal, out var livingSpecial);
                var contentId = waveSpawnDirector.TrySelectSpecial(elapsed, livingNormal, livingSpecial, out var specialId)
                    ? specialId
                    : waveSpawnDirector.SelectNormal(elapsed);
                SpawnEnemy(false, 0, contentId);
                activeEnemyCount++;
            }
        }

        private void ShowNormalRoleAnnouncements()
        {
            if (elapsed >= 120f && (normalRoleAnnouncementMask & 1) == 0)
            {
                normalRoleAnnouncementMask |= 1;
                ShowWaveAnnouncement("원한 처녀귀신 출현 · 매우 빠르지만 약합니다", 1, 2.2f);
            }

            if (elapsed >= 300f && (normalRoleAnnouncementMask & 2) == 0)
            {
                normalRoleAnnouncementMask |= 2;
                ShowWaveAnnouncement("도깨비 출현 · 느리지만 매우 단단합니다", 1, 2.2f);
            }
        }

        private void CountLivingEnemyKinds(out int normal, out int special)
        {
            normal = 0; special = 0;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy.Object == null || enemy.IsTreasure || enemy.IsBoss || enemy.IsMidBoss) continue;
                if (enemy.ArchetypeProfile != null && enemy.ArchetypeProfile.IsSpecial) special++;
                else normal++;
            }
        }

        private int ActiveCombatEnemyCount()
        {
            var count = 0;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy.Object != null && !enemy.IsTreasure) count++;
            }

            return count;
        }

        private int SpawnPack(in EnemyPackPlan pack, int activeCap, int activeEnemyCount)
        {
            var spawned = 0;
            var count = Mathf.Min(pack.Count, Mathf.Max(0, activeCap - activeEnemyCount));
            for (var index = 0; index < count; index++)
            {
                var t = Mathf.Lerp(.08f, .92f, (index + .5f) / count);
                SpawnEnemy(false, 0, pack.ContentId, pack.Side, t);
                spawned++;
            }

#if UNITY_INCLUDE_TESTS
            PackSpawnCountForTests += spawned;
#endif
            return spawned;
        }

        private Rect CurrentVisibleBounds()
        {
            if (gameplayCamera == null)
                return VisualScale.SpawnBounds(
                    player != null ? (Vector2)player.transform.position : Vector2.zero,
                    9f / 16f);

            var bottomLeft = gameplayCamera.ViewportToWorldPoint(Vector3.zero);
            var topRight = gameplayCamera.ViewportToWorldPoint(Vector3.one);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        private void SpawnEnemy(
            bool isBoss,
            int midBossTier = 0,
            string normalContentId = null,
            int? authoredSide = null,
            float? authoredT = null)
        {
            var isMidBoss = !isBoss && midBossTier > 0;
            var side = authoredSide ?? UnityEngine.Random.Range(0, 4);
            var t = authoredT ?? UnityEngine.Random.value;
            var margin = UnityEngine.Random.Range(VisualScale.SpawnMarginMinimum, VisualScale.SpawnMarginMaximum);
#if UNITY_INCLUDE_TESTS
            side = spawnSideForTests ?? side;
            t = spawnTForTests ?? t;
            margin = spawnMarginForTests ?? margin;
#endif
            var spawnBounds = CurrentVisibleBounds();
            var position = ViewportSpawnGeometry.PointOnExpandedPerimeter(spawnBounds, side, t, margin);
#if UNITY_INCLUDE_TESTS
            LastSpawnPositionForTests = position;
#endif

            var pacing = stageTimeline.Sample(elapsed);
            var isElite = !isBoss && !isMidBoss && elapsed >= 60f &&
                          UnityEngine.Random.value < pacing.EliteChance;
#if UNITY_INCLUDE_TESTS
            if (!isBoss && !isMidBoss && forceEliteForTests.HasValue)
            {
                isElite = forceEliteForTests.Value;
            }
#endif
            var rank = isBoss
                ? EnemyRankProfile.Boss
                : (isElite || isMidBoss ? EnemyRankProfile.Elite : EnemyRankProfile.Normal);
            var resolvedContentId = isBoss
                ? "fallen_general"
                : isMidBoss ? "dokkaebi_captain" :
                string.IsNullOrEmpty(normalContentId) ?
                    waveSpawnDirector.SelectNormal(elapsed) : normalContentId;
            var archetypeProfile = EnemyArchetypeProfile.ForContentId(resolvedContentId);
            var chosenSprite = isBoss
                ? bossSprite
                : (isMidBoss
                    ? (midBossTier >= 2 && bossSprite != null ? bossSprite :
                        eliteSprite != null ? eliteSprite : ChooseNormalEnemySprite())
                    : isElite && eliteSprite != null
                    ? eliteSprite
                    : enemySpriteRoster.Resolve(resolvedContentId));
            var specialFrames = enemySpriteRoster.Frames(resolvedContentId);
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
            var bossRole = isBoss
                ? BossCombatRole.FinalBoss
                : midBossTier >= 2
                    ? BossCombatRole.SecondMidBoss
                    : BossCombatRole.FirstMidBoss;
            var baseHealth = isBoss ? 6000f :
                isMidBoss ? (midBossTier >= 2 ? 1400f : 450f) :
                EnemyHealthCurve.BaseHealthAt(elapsed);
            var health = isBoss || isMidBoss ? baseHealth : baseHealth * rank.HealthMultiplier * archetypeProfile.HealthMultiplier;
            var displayScale = isBoss || isMidBoss
                ? VisualScale.NormalEnemyScale * BossScaleProfile.MultiplierFor(bossRole)
                : VisualScale.ScaleFor(rank);
            enemyObject.transform.localScale = Vector3.one * (displayScale * archetypeProfile.DisplayScaleMultiplier);
#if UNITY_INCLUDE_TESTS
            LastSpawnScaleForTests = displayScale * archetypeProfile.DisplayScaleMultiplier;
#endif
#if UNITY_INCLUDE_TESTS
            var finalRendererBounds = MoveRendererOutsideViewport(enemyObject.transform, renderer, spawnBounds, side);
            LastSpawnRootPositionForTests = enemyObject.transform.position;
            LastSpawnRendererBoundsForTests = finalRendererBounds;
#else
            MoveRendererOutsideViewport(enemyObject.transform, renderer, spawnBounds, side);
#endif
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
                Speed = (isBoss ? .98f :
                    isMidBoss ? (midBossTier >= 2 ? 1.08f : 1f) :
                    Mathf.Lerp(0.775f, 1.325f, elapsed / PrototypeDurationSeconds) * rank.SpeedMultiplier) * archetypeProfile.SpeedMultiplier,
                ContactDamage = (isBoss ? 24f :
                    isMidBoss ? (midBossTier >= 2 ? 20f : 16f) :
                    10f * rank.ContactDamageMultiplier) * archetypeProfile.ContactMultiplier,
                IsBoss = rank.IsBoss,
                IsElite = rank.IsElite,
                IsMidBoss = isMidBoss,
                MidBossTier = midBossTier,
                BossRole = bossRole,
                BossAttack = isBoss || isMidBoss ? new BossAttackController(bossRole, 1.2f) : null,
                ContactRadius = isBoss || isMidBoss
                    ? BossScaleProfile.ContactRadius(VisualScale.NormalContactRadius, bossRole)
                    : VisualScale.ContactRadiusFor(rank),
                ExperienceValue = isMidBoss ? (midBossTier >= 2 ? 20 : 12) : rank.ExperienceValue,
                ContentId = resolvedContentId,
                ArchetypeProfile = archetypeProfile,
                SpecialFrames = specialFrames,
                Facing = player == null ? Vector2.left : ((Vector2)player.transform.position - position).normalized
            };
            if (rank.IsElite || isMidBoss)
            {
                state.HealthFill = CreateHealthBar(enemyObject.transform);
                state.HealthFill.parent.localPosition = new Vector3(0f, isMidBoss ? -1.2f : -0.78f, 0f);
                state.HealthFill.parent.localScale = Vector3.one * (isMidBoss ? .82f : .52f);
            }
            if (archetypeProfile.Archetype == EnemyArchetype.ShieldDokkaebi)
            {
                state.ShieldCharges = ShieldDokkaebiGuard.MaximumCharges;
                if (state.HealthFill == null) state.HealthFill = CreateHealthBar(enemyObject.transform);
                state.ShieldFill = CreateShieldBar(enemyObject.transform);
            }
            state.CombatTarget = new PrototypeCombatTarget(this, state, nextCombatTargetRuntimeId++);
            combatTargets.Register(state.CombatTarget);
            enemies.Add(state);
            ShowSpecialEnemyGuide(archetypeProfile);
            if (isBoss || isMidBoss) bossAlive = true;
        }

        private void ShowSpecialEnemyGuide(EnemyArchetypeProfile profile)
        {
            if (profile == null || !profile.IsSpecial || !seenSpecialEnemyGuides.Add(profile.ContentId)) return;
            var guide = profile.Archetype switch
            {
                EnemyArchetype.ShieldDokkaebi => "방패 도깨비 · 정면을 여러 번 치거나 뒤를 노리세요",
                EnemyArchetype.SpiritShaman => "원혼 무당 · 주변 적 강화, 먼저 처치",
                EnemyArchetype.ChargingHornGhost => "돌진 쇠뿔귀 · 붉은 예고선에서 이탈",
                EnemyArchetype.SplittingRat => "분열 쥐 · 처치 시 둘로 분열, 범위 공격 권장",
                _ => string.Empty
            };
            if (guide.Length == 0) return;
            ShowWaveAnnouncement(guide, 2, 2.2f);
#if UNITY_INCLUDE_TESTS
            LastSpecialEnemyGuideForTests = guide;
            SpecialEnemyGuideCountForTests++;
#endif
        }

        private static Bounds MoveRendererOutsideViewport(
            Transform enemyRoot,
            SpriteRenderer renderer,
            Rect viewportBounds,
            int side)
        {
            const float ClearanceEpsilon = .001f;
            var bounds = renderer.bounds;
            var distance = side switch
            {
                0 => Mathf.Max(0f, viewportBounds.yMax - bounds.min.y + ClearanceEpsilon),
                1 => Mathf.Max(0f, viewportBounds.xMax - bounds.min.x + ClearanceEpsilon),
                2 => Mathf.Max(0f, bounds.max.y - viewportBounds.yMin + ClearanceEpsilon),
                3 => Mathf.Max(0f, bounds.max.x - viewportBounds.xMin + ClearanceEpsilon),
                _ => 0f
            };
            if (distance <= 0f) return bounds;

            var direction = side switch
            {
                0 => Vector3.up,
                1 => Vector3.right,
                2 => Vector3.down,
                3 => Vector3.left,
                _ => Vector3.zero
            };
            enemyRoot.position += direction * distance;
            return renderer.bounds;
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
            chestObject.transform.localScale = Vector3.one * 0.52f;
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
            ClearNormalEnemiesForFinalBoss();
            SpawnEnemy(true);
#if UNITY_INCLUDE_TESTS
            FinalBossSpawnCountForTests++;
#endif
        }

        private void ClearNormalEnemiesForFinalBoss()
        {
            for (var index = enemies.Count - 1; index >= 0; index--)
            {
                var enemy = enemies[index];
                if (enemy.Object == null || enemy.IsBoss || enemy.IsMidBoss || enemy.IsTreasure) continue;
                combatTargets.Unregister(enemy.CombatTarget);
                Destroy(enemy.Object);
                enemies.RemoveAt(index);
            }
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
            using (FirstPlayableProfilerMarkers.Spawn.Auto())
            {
                var phase = RunClock.PhaseAt(elapsed);
                var activeCap = WaveSchedule.For(phase).ActiveCap;
                var activeCount = ActiveCombatEnemyCount();
                for (var index = 0; index < count && activeCount < activeCap; index++)
                {
                    SpawnEnemy(false, 0, waveSpawnDirector.SelectNormal(elapsed));
                    activeCount++;
                }
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
            var featuredBoss = FeaturedBossEnemy();
            for (var index = enemies.Count - 1; index >= 0; index--)
            {
                var enemy = enemies[index];
                if (enemy.Object == null)
                {
                    combatTargets.Unregister(enemy.CombatTarget);
                    enemies.RemoveAt(index);
                    continue;
                }
            }

            using (FirstPlayableProfilerMarkers.EnemyMove.Auto())
            {
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (!enemy.IsTreasure) enemy.TickStatuses(delta);
                }
            }

            using (FirstPlayableProfilerMarkers.EnemyGrid.Auto())
            {
                separationEnemies.Clear();
                separationAgents.Clear();
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy.IsTreasure) continue;

                    separationEnemies.Add(enemy);
                    separationAgents.Add(new EnemySeparationAgent(
                        enemy.CombatTarget.RuntimeId,
                        enemy.Object.transform.position,
                        enemy.ContactRadius));
                }

                separationGrid.Rebuild(separationAgents);
            }
#if UNITY_INCLUDE_TESTS
            LastSeparationAgentCountForTests = separationAgents.Count;
#endif
            using (FirstPlayableProfilerMarkers.EnemyMove.Auto())
            {
                for (var index = 0; index < separationEnemies.Count; index++)
                {
                    var enemy = separationEnemies[index];

                    var enemyPosition = (Vector2)enemy.Object.transform.position;
                    var chase = (playerPosition - enemyPosition).normalized;
                    var separate = separationGrid.Resolve(index, 8);
                    var direction = Vector2.ClampMagnitude(chase + separate * .72f, 1f);
                    var archetype = enemy.ArchetypeProfile == null ? EnemyArchetype.Normal : enemy.ArchetypeProfile.Archetype;
                    var specialMotion = SpecialEnemyMotion.Tick(archetype, ref enemy.SpecialMotion, delta,
                        chase, enemy.IsControlled, enemy.WasKnockedBack, false, 0, 0);
                    enemy.WasKnockedBack = false;
                    if (specialMotion.AuraPulse) ApplyShamanAura(enemy);
                    UpdateSpecialEnemyFrame(enemy, archetype, specialMotion, delta,
                        Vector2.Distance(enemyPosition, playerPosition));
                    var velocity = archetype == EnemyArchetype.ChargingHornGhost
                        ? specialMotion.Velocity * enemy.MovementMultiplier
                        : direction * (enemy.Speed * enemy.MovementMultiplier * enemy.AuraMultiplier);
                    if (ReferenceEquals(enemy, featuredBoss) && enemy.BossAttack != null)
                        velocity = ResolveBossVelocity(enemy, enemyPosition, playerPosition, chase, delta);
                    if (velocity.sqrMagnitude > .0001f) enemy.Facing = velocity.normalized;
                    enemy.Object.transform.position = enemyPosition + velocity * delta;
                    enemy.VisualRig?.Tick(velocity, delta, enemy.MotionWeight);

                    var hitDistance = enemy.ContactRadius;
                    if (Vector2.Distance(enemy.Object.transform.position, playerPosition) <= hitDistance &&
                        contactInvulnerability <= 0f)
                    {
                        playerHealth = Mathf.Max(0f, playerHealth -
                            enemy.ContactDamage * enemy.ContactDamageMultiplier * enemy.AuraMultiplier * runIncomingDamageMultiplier);
                        UpdateBarFill(playerHealthFill, playerHealth / playerMaxHealth, 2f, .14f);
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

            if (featuredBoss == null) bossTelegraphPresenter?.Hide();
        }

        private EnemyState FeaturedBossEnemy()
        {
            EnemyState midBoss = null;
            for (var index = 0; index < enemies.Count; index++)
            {
                var candidate = enemies[index];
                if (candidate.Object == null) continue;
                if (candidate.IsBoss) return candidate;
                if (midBoss == null && candidate.IsMidBoss) midBoss = candidate;
            }

            return midBoss;
        }

        private Vector2 ResolveBossVelocity(
            EnemyState enemy,
            Vector2 enemyPosition,
            Vector2 playerPosition,
            Vector2 chase,
            float delta)
        {
            var snapshot = enemy.BossAttack.Tick(
                delta,
                new Float2(enemyPosition.x, enemyPosition.y),
                new Float2(playerPosition.x, playerPosition.y),
                enemy.MaximumHealth <= 0f ? 0f : enemy.Health / enemy.MaximumHealth);
            enemy.BossAttackSnapshot = snapshot;
            var lockedTarget = new Vector2(snapshot.LockedTarget.X, snapshot.LockedTarget.Y);
            if (snapshot.Phase == BossAttackPhase.Telegraph)
                bossTelegraphPresenter?.Show(snapshot.Kind, enemyPosition, lockedTarget,
                    enemy.Object.transform.localScale.x, Time.time);
            else
                bossTelegraphPresenter?.Hide();

            if (snapshot.ExecuteStarted)
            {
                enemy.BossAttackHitApplied = false;
                enemy.ChargeStart = enemyPosition;
                if (snapshot.Kind == BossAttackKind.SuppressionSlam)
                {
                    var radius = enemy.IsBoss ? 2.5f : 2.1f;
                    if ((playerPosition - lockedTarget).sqrMagnitude <= radius * radius)
                        ApplyBossDamageToPlayer(enemy.IsBoss ? 18f : 16f);
                    enemy.BossAttackHitApplied = true;
                }
                else if (snapshot.Kind == BossAttackKind.SpiritVolley)
                {
                    SpawnBossVolley(enemyPosition, enemy.Health < enemy.MaximumHealth * .5f ? 10 : 8);
                    enemy.BossAttackHitApplied = true;
                }
            }

            if (snapshot.Phase == BossAttackPhase.Execute && snapshot.Kind == BossAttackKind.BloodCharge)
            {
                var charge = lockedTarget - enemy.ChargeStart;
                var velocity = charge.sqrMagnitude <= .0001f ? Vector2.zero : charge.normalized * 14f;
                if (!enemy.BossAttackHitApplied &&
                    (playerPosition - enemyPosition).sqrMagnitude <=
                    (enemy.ContactRadius + .45f) * (enemy.ContactRadius + .45f))
                {
                    ApplyBossDamageToPlayer(enemy.IsBoss ? 22f : 20f);
                    enemy.BossAttackHitApplied = true;
                }
                return velocity;
            }

            return snapshot.Phase == BossAttackPhase.Chase
                ? chase * (enemy.Speed * enemy.MovementMultiplier * enemy.AuraMultiplier)
                : Vector2.zero;
        }

        private void SpawnBossVolley(Vector2 center, int count)
        {
            for (var index = 0; index < count; index++)
            {
                var projectile = AcquireBossProjectile();
                var angle = Mathf.PI * 2f * (index + .35f) / count;
                projectile.Object.transform.position = center;
                projectile.Object.transform.localScale = Vector3.one * .32f;
                projectile.Object.SetActive(true);
                projectile.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6.2f;
                projectile.Remaining = 4f;
            }
        }

        private BossProjectileState AcquireBossProjectile()
        {
            for (var index = 0; index < bossProjectiles.Count; index++)
                if (bossProjectiles[index].Object != null && !bossProjectiles[index].Object.activeSelf)
                    return bossProjectiles[index];

            var projectileObject = CreateSpriteObject(
                "Fallen General Spirit Projectile",
                solidSprite,
                Vector2.zero,
                7,
                runtimeObjects);
            projectileObject.GetComponent<SpriteRenderer>().color = new Color(.58f, .05f, .16f, .95f);
            projectileObject.SetActive(false);
            var created = new BossProjectileState { Object = projectileObject };
            bossProjectiles.Add(created);
            return created;
        }

        private void UpdateBossProjectiles(float delta)
        {
            if (player == null) return;
            var playerPosition = (Vector2)player.transform.position;
            for (var index = 0; index < bossProjectiles.Count; index++)
            {
                var projectile = bossProjectiles[index];
                if (projectile.Object == null || !projectile.Object.activeSelf) continue;
                projectile.Remaining -= delta;
                projectile.Object.transform.position += (Vector3)(projectile.Velocity * delta);
                if (((Vector2)projectile.Object.transform.position - playerPosition).sqrMagnitude <= .22f * .22f)
                {
                    ApplyBossDamageToPlayer(11f);
                    projectile.Object.SetActive(false);
                }
                else if (projectile.Remaining <= 0f)
                {
                    projectile.Object.SetActive(false);
                }
            }
        }

        private void ApplyBossDamageToPlayer(float amount)
        {
            if (contactInvulnerability > 0f || playerHealth <= 0f) return;
            playerHealth = Mathf.Max(0f, playerHealth - amount * runIncomingDamageMultiplier);
            UpdateBarFill(playerHealthFill, playerHealth / playerMaxHealth, 2f, .14f);
            contactInvulnerability = .35f;
            StartCoroutine(FlashPlayer());
            if (playerHealth <= 0f) EndRun(false);
        }

        private void ApplyShamanAura(EnemyState shaman)
        {
            if (shaman == null || shaman.Object == null) return;
            var center = (Vector2)shaman.Object.transform.position;
            const float radiusSquared = 12.25f;
            for (var index = 0; index < enemies.Count; index++)
            {
                var target = enemies[index];
                if (ReferenceEquals(target, shaman) || target.Object == null || target.IsTreasure) continue;
                if (((Vector2)target.Object.transform.position - center).sqrMagnitude > radiusSquared) continue;
                target.AuraRemaining = Mathf.Max(target.AuraRemaining, .35f);
            }
        }

        private static void UpdateSpecialEnemyFrame(EnemyState enemy, EnemyArchetype archetype,
            SpecialEnemyMotionResult motion, float delta, float playerDistance)
        {
            if (archetype == EnemyArchetype.ShieldDokkaebi) return;
            if (enemy.Renderer == null || enemy.SpecialFrames == null || enemy.SpecialFrames.Count < 3) return;
            enemy.SpecialAnimationTime += Mathf.Max(0f, delta);
            var animate = archetype switch
            {
                EnemyArchetype.SpiritShaman => true,
                EnemyArchetype.ChargingHornGhost => motion.IsTelegraphing,
                EnemyArchetype.SplittingRat => playerDistance <= 4f,
                _ => false
            };
            var frame = animate ? 1 + Mathf.FloorToInt(enemy.SpecialAnimationTime / .14f) % 2 : 0;
            if (enemy.SpecialFrames[frame] != null) enemy.Renderer.sprite = enemy.SpecialFrames[frame];
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
                var baseDamage = Mathf.Max(1, Mathf.RoundToInt(data.BaseDamage * runDamageMultiplier));
                IWeaponExecutor executor;
                var evolved = evolutionState.IsEvolved(id);
                var modifiers = WeaponRuntimeModifiers.From(
                    weaponAffixes.TryProfileFor(id, out var profile) ? profile : null,
                    weaponLegacyState.SnapshotFor(id, ownedLevel));
                if (id.Equals(WeaponId.HwandoFlyingBlade)) executor = new FlyingBladeExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ProjectileCount, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.GakgungShot)) executor = new GakgungExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, data.Speed, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.TalismanThrow)) executor = new TalismanExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ChainCount, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.ThunderCrashBomb)) executor = new ThunderBombExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, data.DurationSeconds, 0.15f, data.Range * 0.45f, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.JangseungWard)) executor = new JangseungWardExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, data.ProjectileCount, data.Pierce, 0.2f, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.SingijeonVolley)) executor = new SingijeonExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, data.Speed, data.ProjectileCount, data.Level, evolved, modifiers);
                else if (id.Equals(WeaponId.FrostFlask)) executor = new FrostFlaskExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, 0.4f, data.DurationSeconds, data.Range * 0.35f, data.Pierce, data.Level, evolved, modifiers, slowFraction: data.SlowFraction);
                else if (id.Equals(WeaponId.WindThunderFan)) executor = new WindThunderFanExecutor(weaponRuntime, baseDamage, data.CooldownSeconds, data.Range, data.Knockback, data.ChainCount, data.Level, evolved, modifiers);
                else throw new InvalidOperationException($"No executor is available for '{id}'.");
                weaponRuntime.Register(id, executor);
                registeredWeaponIds.Add(id);
            }
        }

        private Sprite ResolveWeaponSprite(WeaponId id)
        {
            if (weaponCatalog == null || !weaponCatalog.TryGet(id, out var definition)) return solidSprite;
            if (definition.UiIcon != null) return definition.UiIcon;
            return definition.PresentationSprites.Count > 0 ? definition.PresentationSprites[0] : solidSprite;
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

        private void ApplyEnemyDamage(EnemyState enemy, float damage, bool confirmedWeaponDamage = false)
        {
            if (enemy == null || enemy.Object == null)
            {
                return;
            }

            enemy.Health -= damage;
            var enemyPosition = (Vector2)enemy.Object.transform.position;
            var incomingDirection = enemyPosition - (Vector2)player.transform.position;
            if (enemy.GuardHitPending)
            {
                enemy.GuardHitPending = false;
                enemy.VisualRig?.ShowGuardHit(incomingDirection, enemy.GuardBrokePending);
                enemy.GuardBrokePending = false;
            }
            else
            {
                enemy.VisualRig?.ShowHit(incomingDirection,
                    enemy.IsBoss ? 0.05f : enemy.IsElite ? 0.075f : 0.095f);
            }
            UpdateBarFill(enemy.HealthFill, enemy.Health / enemy.MaximumHealth, 2f, .14f);
            if (enemy.Health > 0f)
            {
                return;
            }

            var wasBoss = enemy.IsBoss;
            var wasMidBoss = enemy.IsMidBoss;
            var wasTreasure = enemy.IsTreasure;
            var targetRuntimeId = enemy.CombatTarget.RuntimeId;
            if (confirmedWeaponDamage && !wasTreasure)
                pendingMasteryDeaths[targetRuntimeId] = MasteryClassFor(enemy);
            else
                runWeaponKillLedger.ForgetTarget(targetRuntimeId);
            var deathPosition = enemy.Object.transform.position;
            var splitResult = default(SpecialEnemyMotionResult);
            if (enemy.ArchetypeProfile != null && enemy.ArchetypeProfile.Archetype == EnemyArchetype.SplittingRat)
            {
                var phase = RunClock.PhaseAt(elapsed);
                var activeCap = WaveSchedule.For(phase).ActiveCap;
                splitResult = SpecialEnemyMotion.Tick(EnemyArchetype.SplittingRat, ref enemy.SpecialMotion, 0f,
                    Vector2.zero, false, false, true, Mathf.Max(0, ActiveCombatEnemyCount() - 1), activeCap);
            }
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

            for (var child = 0; child < splitResult.SplitChildren; child++)
            {
                SpawnEnemy(false, 0, "plague_rat");
                var spawned = enemies[enemies.Count - 1];
                var offset = child == 0 ? Vector2.left * .22f : Vector2.right * .22f;
                spawned.Object.transform.position = (Vector2)deathPosition + offset;
                spawned.Health *= .55f; spawned.MaximumHealth = spawned.Health;
                spawned.Speed *= 1.12f;
            }
            if (splitResult.FallbackBlast && player != null &&
                Vector2.Distance(deathPosition, player.transform.position) <= 1.6f)
            {
                playerHealth = Mathf.Max(0f, playerHealth - 6f);
                UpdateBarFill(playerHealthFill, playerHealth / playerMaxHealth, 2f, .14f);
            }

            kills++;
            if (wasBoss)
            {
                if (!confirmedWeaponDamage) EndRun(true);
                return;
            }

            SpawnPickup(deathPosition, PickupKind.Experience, enemy.ExperienceValue);
            if (wasMidBoss)
            {
                ScatterTreasure(deathPosition);
                SpawnPickup(
                    deathPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f),
                    PickupKind.Magnet,
                    0);
            }
        }

        private void OnCombatDamageConfirmed(ConfirmedDamageEvent confirmed)
        {
            runWeaponKillLedger.RecordHit(confirmed.TargetRuntimeId, confirmed.WeaponId);
            if (!pendingMasteryDeaths.TryGetValue(confirmed.TargetRuntimeId, out var enemyClass)) return;
            pendingMasteryDeaths.Remove(confirmed.TargetRuntimeId);
            runWeaponKillLedger.ConfirmDeath(confirmed.TargetRuntimeId, enemyClass);
            if (enemyClass == EnemyMasteryClass.FinalBoss) EndRun(true);
        }

        private static EnemyMasteryClass MasteryClassFor(EnemyState enemy)
        {
            if (enemy.IsBoss) return EnemyMasteryClass.FinalBoss;
            if (enemy.IsMidBoss) return EnemyMasteryClass.MidBoss;
            if (enemy.IsElite) return EnemyMasteryClass.Elite;
            return enemy.ArchetypeProfile != null && enemy.ArchetypeProfile.IsSpecial
                ? EnemyMasteryClass.Special
                : EnemyMasteryClass.Normal;
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
            if (kind == PickupKind.Experience)
            {
                if (level >= RunLoadoutRules.MaximumPlayerLevel) return;
                var activeExperienceCount = 0;
                PickupState nearest = null;
                var nearestDistance = float.PositiveInfinity;
                for (var index = 0; index < pickups.Count; index++)
                {
                    var candidate = pickups[index];
                    if (candidate.Kind != PickupKind.Experience || candidate.Object == null) continue;
                    activeExperienceCount++;
                    var distance = ((Vector2)candidate.Object.transform.position - position).sqrMagnitude;
                    if (distance >= nearestDistance) continue;
                    nearest = candidate;
                    nearestDistance = distance;
                }

                if (ExperiencePickupBudget.ShouldMerge(activeExperienceCount) && nearest != null)
                {
                    nearest.Value = ExperiencePickupBudget.MergeValue(nearest.Value, value);
                    UpdateExperiencePickupVisual(nearest);
                    return;
                }
            }

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
            PickupState pickup = null;
            for (var index = 0; index < pickupPool.Count; index++)
            {
                var candidate = pickupPool[index];
                if (candidate.Kind == kind && candidate.Object != null && !candidate.Object.activeSelf)
                {
                    pickup = candidate;
                    break;
                }
            }

            if (pickup == null)
            {
                var createdObject = CreateSpriteObject(
                    objectName,
                    sprite != null ? sprite : solidSprite,
                    position,
                    6,
                    runtimeObjects);
                pickup = new PickupState { Object = createdObject, Kind = kind };
                if (kind == PickupKind.Experience)
                {
                    var trailRenderer = createdObject.AddComponent<TrailRenderer>();
                    trailRenderer.time = .14f;
                    trailRenderer.minVertexDistance = .035f;
                    trailRenderer.startWidth = .12f;
                    trailRenderer.endWidth = 0f;
                    trailRenderer.startColor = new Color(.30f, 1f, .92f, .78f);
                    trailRenderer.endColor = new Color(.20f, .86f, 1f, 0f);
                    trailRenderer.sortingOrder = 5;
                    trailRenderer.sharedMaterial = pickupTrailMaterial;
                    trailRenderer.emitting = false;
                    pickup.Trail = trailRenderer;
                }
                pickupPool.Add(pickup);
            }

            var pickupObject = pickup.Object;
            pickupObject.name = objectName;
            pickupObject.transform.position = position;
            pickupObject.transform.rotation = Quaternion.identity;
            pickupObject.SetActive(true);
            pickup.Value = value;
            pickup.ForceCollect = false;
            pickup.Attracting = false;
            pickup.AttractionAge = 0f;
            if (pickup.Trail != null)
            {
                pickup.Trail.Clear();
                pickup.Trail.emitting = false;
            }

            var renderer = pickupObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : solidSprite;
            renderer.color = Color.white;
            pickup.BaseScale = kind == PickupKind.Experience ? .72f : kind == PickupKind.Yeopjeon ? .48f : .50f;
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

            if (kind == PickupKind.Experience) UpdateExperiencePickupVisual(pickup);
            else pickupObject.transform.localScale = Vector3.one * pickup.BaseScale;
            pickups.Add(pickup);
        }

        private static void UpdateExperiencePickupVisual(PickupState pickup)
        {
            var tier = ExperiencePickupBudget.TierFor(pickup.Value);
            pickup.BaseScale = tier == ExperiencePickupTier.Large ? 1.05f :
                tier == ExperiencePickupTier.Medium ? .88f : .72f;
            var renderer = pickup.Object.GetComponent<SpriteRenderer>();
            renderer.color = tier == ExperiencePickupTier.Large
                ? new Color(.92f, .18f, .72f)
                : tier == ExperiencePickupTier.Medium
                    ? new Color(.58f, .34f, .96f)
                    : new Color(.20f, .95f, .90f);
            pickup.Object.transform.localScale = Vector3.one * pickup.BaseScale;
        }

        private void CreateExperienceAbsorbFlash()
        {
            var flashObject = CreateSpriteObject(
                "Experience Absorb Flash",
                experienceSprite != null ? experienceSprite : solidSprite,
                player.transform.position,
                14,
                runtimeObjects);
            experienceAbsorbFlash = flashObject.GetComponent<SpriteRenderer>();
            experienceAbsorbFlash.enabled = false;
            experienceAbsorbFlashTimer = 0f;
        }

        private void UpdateExperienceAbsorbFlash(float delta)
        {
            if (experienceAbsorbFlash == null) return;
            experienceAbsorbFlashTimer = Mathf.Max(0f, experienceAbsorbFlashTimer - Mathf.Max(0f, delta));
            if (experienceAbsorbFlashTimer <= 0f)
            {
                experienceAbsorbFlash.enabled = false;
                return;
            }

            const float duration = .14f;
            var progress = 1f - experienceAbsorbFlashTimer / duration;
            experienceAbsorbFlash.enabled = true;
            experienceAbsorbFlash.transform.position = player.transform.position;
            experienceAbsorbFlash.transform.localScale = Vector3.one * Mathf.Lerp(.18f, .46f, progress);
            experienceAbsorbFlash.color = new Color(.34f, 1f, .94f, 1f - progress);
        }

        private void TriggerExperienceAbsorbFlash()
        {
            experienceAbsorbFlashTimer = .14f;
            UpdateExperienceAbsorbFlash(0f);
        }

        private void BeginExperienceAttraction(PickupState pickup)
        {
            if (pickup.Attracting || pickup.Object == null) return;
            pickup.Attracting = true;
            pickup.AttractionAge = 0f;
            if (pickup.Trail != null)
            {
                pickup.Trail.Clear();
                pickup.Trail.emitting = true;
            }
        }

        private void UpdatePickups(float delta)
        {
            ActivateMagnetBatch();
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
                if (pickup.Kind == PickupKind.Experience)
                {
                    var pulse = pickup.BaseScale + Mathf.Sin(Time.time * 4.5f + index * .73f) * .05f;
                    if (pickup.ForceCollect || distance <= pickupRadius)
                    {
                        BeginExperienceAttraction(pickup);
                        pickup.AttractionAge += Mathf.Max(0f, delta);
                        var direction = playerPosition - (Vector2)pickup.Object.transform.position;
                        pickup.Object.transform.localScale = Vector3.Scale(
                            Vector3.one * pulse,
                            ExperiencePickupMotion.StretchAt(direction, pickup.AttractionAge));
                        if (direction.sqrMagnitude > .0001f)
                        {
                            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                            pickup.Object.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        }
                    }
                    else
                    {
                        pickup.Object.transform.localScale = Vector3.one * pulse;
                    }
                }
                else if (pickup.Kind == PickupKind.Yeopjeon)
                {
                    var pulse = .48f + Mathf.Sin(Time.time * 4.1f + index * .61f) * .035f;
                    pickup.Object.transform.localScale = Vector3.one * pulse;
                }
                if (pickup.ForceCollect || distance <= pickupRadius)
                {
                    pickup.Object.transform.position = pickup.Kind == PickupKind.Experience
                        ? ExperiencePickupMotion.Step(
                            pickup.Object.transform.position,
                            playerPosition,
                            pickup.AttractionAge,
                            delta,
                            pickup.ForceCollect)
                        : Vector2.MoveTowards(
                            pickup.Object.transform.position,
                            playerPosition,
                            Mathf.Lerp(4f, 12f, 1f - distance / pickupRadius) * delta);
                }

                var collectionDistance = pickup.Kind == PickupKind.Experience ? .18f : .42f;
                if (distance > collectionDistance)
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
                    TriggerExperienceAbsorbFlash();
                }
                else
                {
                    CollectMagnet();
                }

                ReleasePickupAt(index);
            }
        }

        private void CollectMagnet()
        {
            magnetSweepActive = true;
            magnetMessageTimer = 1.2f;
        }

        private void ActivateMagnetBatch()
        {
            if (!magnetSweepActive) return;
            const int batchSize = 24;
            var activated = 0;
            var remaining = false;
            for (var index = 0; index < pickups.Count; index++)
            {
                var pickup = pickups[index];
                if (pickup.Kind != PickupKind.Experience || pickup.ForceCollect) continue;
                if (activated < batchSize)
                {
                    pickup.ForceCollect = true;
                    activated++;
                }
                else
                {
                    remaining = true;
                }
            }
            magnetSweepActive = remaining;
        }

        private void ReleasePickupAt(int index)
        {
            var pickup = pickups[index];
            if (pickup.Trail != null)
            {
                pickup.Trail.emitting = false;
                pickup.Trail.Clear();
            }
            pickup.Attracting = false;
            pickup.ForceCollect = false;
            pickup.AttractionAge = 0f;
            pickup.Object.transform.rotation = Quaternion.identity;
            pickup.Object.SetActive(false);
            pickups.RemoveAt(index);
        }

        private void AddExperience(int amount)
        {
            if (level >= RunLoadoutRules.MaximumPlayerLevel) return;
            experience += Mathf.Max(0, Mathf.CeilToInt(amount * runExperienceMultiplier));
            while (level < RunLoadoutRules.MaximumPlayerLevel && experience >= experienceToNext)
            {
                experience -= experienceToNext;
                level++;
                experienceToNext = level < RunLoadoutRules.MaximumPlayerLevel
                    ? ExperienceCurve.GetThresholdForNextLevel(level)
                    : 0;
                pendingUpgradeCount++;
            }

            if (level >= RunLoadoutRules.MaximumPlayerLevel) experience = 0;

            if (!upgradeOpen && !awaitingUpgradePresentationClose && upgradeQueueGraceRemaining <= 0f)
                OpenNextPendingUpgrade();
        }

        private bool OpenNextPendingUpgrade()
        {
            if (pendingUpgradeCount <= 0) return false;
            pendingUpgradeCount--;
            OpenUpgrade();
            if (upgradeOpen) return true;
            pendingUpgradeCount++;
            return false;
        }

        private void RebuildWeaponExecutorsForLevel()
        {
            if (weaponRuntime == null) return;
#if UNITY_INCLUDE_TESTS
            WeaponRebuildCountForTests++;
#endif
            weaponRuntime.Dispose();
            weaponRuntime = new WeaponRuntimeController(combatTargets, combatDamageService, prototypeCombatMask);
            weaponRuntime.SetTargetVisibilityResolver(IsInsideGameplayViewport);
            weaponRuntime.SetSpriteResolver(ResolveWeaponSprite);
            weaponRuntime.SetPresentationSpriteResolver(ResolveWeaponPresentationSprite);
            weaponRuntime.SetMaskResolver(ResolveWeaponMask);
            weaponRuntime.SetJangseungGeumjulVisualLibrary(ResolveJangseungGeumjulVisualLibrary());
            registeredWeaponIds.Clear();
            weaponMasks.Load(weaponCatalog);
            RegisterCatalogWeapons();
        }

        private JangseungGeumjulVisualLibrary ResolveJangseungGeumjulVisualLibrary()
        {
            if (jangseungGeumjulVisuals == null)
                jangseungGeumjulVisuals = Resources.Load<JangseungGeumjulVisualLibrary>(JangseungGeumjulResourcesPath);
            return jangseungGeumjulVisuals;
        }

        private void OpenUpgrade()
        {
            if (!flow.TryTransition(GameFlowState.LevelUpSelection)) return;
            upgradeOpen = true;
            pendingWeaponChoice = null;
            upgradeOffers.Clear();
            upgradeOfferData.Clear();
            var state = new UpgradeState(weaponLevels, supportLevels, unlockedUpgradeIds,
                acquiredEvolutionIds, discardedWeaponIds);
            var selected = UpgradeSelector.Select(state, level * 397 ^ kills, level);
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

            var offer = upgradeOfferData[index];
            if (offer.Kind == UpgradeKind.Weapon && offer.RequiresReplacement)
            {
                if (!flow.TryTransition(GameFlowState.WeaponReplacement)) return false;
                pendingWeaponChoice = new PendingWeaponChoice(offer);
                PublishWeaponReplacement(pendingWeaponChoice);
                return true;
            }

            if (offer.Kind == UpgradeKind.Weapon && NeedsLegacyChoice(offer.Id, offer.NextLevel))
            {
                if (!flow.TryTransition(GameFlowState.WeaponLegacySelection)) return false;
                pendingWeaponChoice = new PendingWeaponChoice(offer);
                PublishWeaponLegacy(pendingWeaponChoice);
                return true;
            }

            return CompleteUpgrade(offer);
        }

        public bool CancelWeaponReplacement()
        {
            if (flow == null || flow.State != GameFlowState.WeaponReplacement || pendingWeaponChoice == null)
                return false;
            if (!flow.TryTransition(GameFlowState.LevelUpSelection)) return false;

            pendingWeaponChoice = null;
            var choices = new List<UpgradeChoiceView>(upgradeOfferData.Count);
            foreach (var offer in upgradeOfferData) choices.Add(BuildUpgradeChoiceView(offer));
            UpgradeOpened?.Invoke(new UpgradeChoiceState(level, choices));
            return true;
        }

        public bool TryChooseWeaponReplacement(string discardedWeaponId)
        {
            if (flow == null || flow.State != GameFlowState.WeaponReplacement || pendingWeaponChoice == null ||
                string.IsNullOrEmpty(discardedWeaponId) ||
                !weaponLevels.TryGetValue(discardedWeaponId, out var discardedLevel) ||
                discardedWeaponId == pendingWeaponChoice.Offer.Id)
            {
                return false;
            }

            var discardedId = new WeaponId(discardedWeaponId);
            var newWeaponId = new WeaponId(pendingWeaponChoice.Offer.Id);
            var replacementLevel = RunLoadoutRules.ReplacementLevel(discardedLevel);
            weaponLevels.Remove(discardedWeaponId);
            weaponLegacyState.Remove(discardedId);
            weaponAffixes.Remove(discardedId);
            evolutionState.Remove(discardedId);
            acquiredEvolutionIds.RemoveWhere(evolutionId =>
                WeaponEvolutionCatalog.TryGet(evolutionId, out var evolution) &&
                evolution.RequiredWeaponId.Equals(discardedId));
            discardedWeaponIds.Add(discardedWeaponId);
            weaponLevels[newWeaponId.Value] = replacementLevel;
            pendingWeaponChoice.DiscardedWeaponId = discardedWeaponId;
            pendingWeaponChoice.ResolvedLevel = replacementLevel;

            if (NeedsLegacyChoice(newWeaponId.Value, replacementLevel))
            {
                if (!flow.TryTransition(GameFlowState.WeaponLegacySelection)) return false;
                PublishWeaponLegacy(pendingWeaponChoice);
                return true;
            }

            return CompletePendingWeapon();
        }

        public bool TryChooseWeaponLegacy(WeaponLegacyPathId pathId)
        {
            if (flow == null || flow.State != GameFlowState.WeaponLegacySelection || pendingWeaponChoice == null)
                return false;

            var weaponId = new WeaponId(pendingWeaponChoice.Offer.Id);
            if (!weaponLegacyState.TryChoose(weaponId, pathId)) return false;
            return CompletePendingWeapon();
        }

        private bool CompletePendingWeapon()
        {
            if (pendingWeaponChoice == null) return false;
            var resolved = new UpgradeOffer(pendingWeaponChoice.Offer.Id, UpgradeKind.Weapon,
                pendingWeaponChoice.ResolvedLevel);
            return CompleteUpgrade(resolved);
        }

        private bool CompleteUpgrade(UpgradeOffer offer)
        {
            if (!flow.TryTransition(GameFlowState.AugmentResult)) return false;

            var reward = ApplyUpgrade(offer);
            upgradeOpen = false;
            upgradeOffers.Clear();
            upgradeOfferData.Clear();
            pendingWeaponChoice = null;
#if UNITY_INCLUDE_TESTS
            AppliedUpgradeCount++;
#endif
            awaitingUpgradePresentationClose = true;
            UpgradeChosen?.Invoke(reward);
            return true;
        }

        private bool NeedsLegacyChoice(string weaponId, int resolvedLevel) =>
            MetaGameSession.Current == null &&
            resolvedLevel == 3 &&
            !weaponLegacyState.SnapshotFor(new WeaponId(weaponId), 3).HasPath;

        private void PublishWeaponReplacement(PendingWeaponChoice pending)
        {
            var choices = new List<WeaponReplacementChoiceView>(weaponLevels.Count);
            foreach (var weapon in weaponLevels)
            {
                var id = new WeaponId(weapon.Key);
                var snapshot = weaponLegacyState.SnapshotFor(id, weapon.Value);
                var legacyName = snapshot.HasPath && WeaponLegacyCatalog.TryGet(snapshot.PathId, out var definition)
                    ? definition.DisplayName
                    : string.Empty;
                choices.Add(new WeaponReplacementChoiceView(weapon.Key, WeaponDisplayName(weapon.Key),
                    weapon.Value, legacyName, ResolveWeaponSprite(id)));
            }

            WeaponReplacementOpened?.Invoke(new WeaponReplacementState(pending.Offer.Id,
                WeaponDisplayName(pending.Offer.Id), choices));
        }

        private void PublishWeaponLegacy(PendingWeaponChoice pending)
        {
            var weaponId = new WeaponId(pending.Offer.Id);
            var icon = ResolveWeaponSprite(weaponId);
            var choices = new List<WeaponLegacyChoiceView>(2);
            foreach (var definition in WeaponLegacyCatalog.PathsFor(weaponId))
            {
                choices.Add(new WeaponLegacyChoiceView(definition.Id, definition.DisplayName,
                    definition.CombatStyle, definition.Benefit, definition.Cost, icon));
            }

            WeaponLegacyOpened?.Invoke(new WeaponLegacyChoiceState(weaponId.Value,
                WeaponDisplayName(weaponId.Value), choices));
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
                if (!flow.TryTransition(GameFlowState.Playing)) return false;
                upgradeQueueGraceRemaining = QueuedUpgradeGraceSeconds;
            }
            else
            {
                upgradeQueueGraceRemaining = 0f;
                flow.TryTransition(GameFlowState.Playing);
            }

            return true;
        }

        /// <summary>Clears a UI-owned modal when its bootstrap is disabled or destroyed.</summary>
        public void CancelUiModalPresentation()
        {
            if (flow == null) return;
            if (flow.State == GameFlowState.LevelUpSelection ||
                flow.State == GameFlowState.WeaponReplacement ||
                flow.State == GameFlowState.WeaponLegacySelection ||
                flow.State == GameFlowState.AugmentResult)
            {
                upgradeOpen = false;
                awaitingUpgradePresentationClose = false;
                upgradeQueueGraceRemaining = 0f;
                pendingWeaponChoice = null;
                upgradeOffers.Clear();
                upgradeOfferData.Clear();
            }

            if (flow.State == GameFlowState.LevelUpSelection ||
                flow.State == GameFlowState.WeaponReplacement ||
                flow.State == GameFlowState.WeaponLegacySelection ||
                flow.State == GameFlowState.AugmentResult ||
                flow.State == GameFlowState.Paused)
                flow.ResetToPlaying();
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
                    SupportDisplayName(offer.Id), SupportRewardSummary(offer.Id), null);
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
            if (modifiers.DamageBonus != 0f) values.Add(WeaponAffixDisplayFormatter.Describe(
                WeaponAffixStat.Damage, Mathf.RoundToInt(modifiers.DamageBonus * 100f)));
            if (modifiers.CooldownReduction != 0f) values.Add(WeaponAffixDisplayFormatter.Describe(
                WeaponAffixStat.Cooldown, -Mathf.RoundToInt(modifiers.CooldownReduction * 100f)));
            if (modifiers.AreaBonus != 0f) values.Add(WeaponAffixDisplayFormatter.Describe(
                WeaponAffixStat.Area, Mathf.RoundToInt(modifiers.AreaBonus * 100f)));
            if (modifiers.SpeedBonus != 0f) values.Add(WeaponAffixDisplayFormatter.Describe(
                WeaponAffixStat.ProjectileSpeed, Mathf.RoundToInt(modifiers.SpeedBonus * 100f)));
            if (modifiers.DurationBonus != 0f) values.Add(WeaponAffixDisplayFormatter.Describe(
                WeaponAffixStat.Duration, Mathf.RoundToInt(modifiers.DurationBonus * 100f)));
            return string.Join(" · ", values);
        }

        private FirstPlayableUiState BuildUiState()
        {
            var weapons = new List<WeaponSlotView>(weaponLevels.Count);
            foreach (var weapon in weaponLevels)
            {
                var weaponId = new WeaponId(weapon.Key);
                var legacy = weaponLegacyState.SnapshotFor(weaponId, weapon.Value);
                var legacyName = legacy.HasPath && WeaponLegacyCatalog.TryGet(legacy.PathId, out var definition)
                    ? definition.DisplayName
                    : "미선택";
                weapons.Add(new WeaponSlotView(
                    weapon.Key,
                    WeaponDisplayName(weapon.Key),
                    weapon.Value,
                    ResolveWeaponSprite(weaponId),
                    GeneralAffixSummary(weaponId),
                    weaponAffixes.TryProfileFor(weaponId, out var profile) ? profile.PotentialIds : null,
                    profile == null ? null : profile.GeneralRolls.Select(roll => roll.Tier),
                    WeaponBehavior(weapon.Key),
                    legacyName,
                    LegacyStageName(legacy.Stage),
                    NextLegacyMilestone(legacy.Stage),
                    profile?.GeneralRolls));
            }

            // The final boss owns the featured health bar even if a midboss survived into
            // the last encounter. Midbosses are the fallback featured target.
            var boss = enemies.Find(candidate => candidate.IsBoss && candidate.Object != null) ??
                       enemies.Find(candidate => candidate.IsMidBoss && candidate.Object != null);
            return new FirstPlayableUiState(
                level, experience, experienceToNext, coins, kills, elapsed, PrototypeDurationSeconds,
                playerHealth, playerMaxHealth, finalBossWarning && !bossSpawned, bossAlive,
                boss != null ? boss.Health : 0f, boss != null ? boss.MaximumHealth : 0f, weapons,
                waveAnnouncement, waveAnnouncementTimer, waveAnnouncementIntensity, runEnded, victory,
                RunMasteryTotal(), settlementFailed, accountExperienceEarned,
                accountLevelBefore, accountLevelAfter);
        }

        private static string LegacyStageName(WeaponLegacyStage stage) => stage switch
        {
            WeaponLegacyStage.Chosen => "성장 방향 선택 완료",
            WeaponLegacyStage.Reinforced => "선택 효과 강화됨",
            WeaponLegacyStage.Completed => "최종 효과 완성",
            _ => "무기 3레벨에서 두 방식 중 하나 선택"
        };

        private static string NextLegacyMilestone(WeaponLegacyStage stage) => stage switch
        {
            WeaponLegacyStage.Chosen => "무기 4레벨 달성 시 효과 강화",
            WeaponLegacyStage.Reinforced => "무기 5레벨 달성 시 최종 효과 완성",
            WeaponLegacyStage.Completed => "최종 효과 적용 중",
            _ => "무기 3레벨에서 두 방식 중 하나 선택"
        };

        public void RestartRun()
        {
            if (!runEnded) return;
            ResetRun();
        }

        public void ReturnToLobby()
        {
            if (!runEnded || returningToLobby) return;
            if (!TrySettleRun(runAbandoned)) return;
            returningToLobby = true;
            StartCoroutine(RouteToLobby());
        }

        public void ConfirmAbandonAndReturn()
        {
            if (runEnded || returningToLobby) return;
            runEnded = true;
            victory = false;
            runAbandoned = true;
            movement = Vector2.zero;
            flow?.TryTransition(GameFlowState.GameOver);
            ReturnToLobby();
        }

        private IEnumerator RouteToLobby()
        {
            var session = MetaGameSession.Current ?? MetaGameSession.EnsureExists();
            yield return session.Router.LoadLobby();
            returningToLobby = false;
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
                    SupportBehavior(offer.Id), SupportDelta(offer.Id), SupportUpgradeIconCatalog.Resolve(offer.Id));
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
            if (id == WeaponId.FrostFlask.Value) return "착지 폭발 후 서리 지대: 지속 피해·둔화·빙결";
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

        private static string SupportRewardSummary(string id)
        {
            if (id == "talisman") return "최대 체력 +20";
            if (id == "boots") return "이동 속도 +12%";
            if (id == "warding_bell") return "경험치 획득 범위 +0.7";
            return "지원 능력 강화";
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

            geumjulPresenter.SetTrail(trail, .48f);

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
            geumjulPresenter.PlayClosure(polygon);
            trail.Clear();
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
            runAbandoned = false;
            movement = Vector2.zero;
            flow?.TryTransition(GameFlowState.GameOver);
            TrySettleRun(false);
        }

        private bool TrySettleRun(bool abandoned)
        {
            if (!settlementPrepared)
            {
                pendingSettlement = new RunSettlement(
                    runWeaponKillLedger.Snapshot(), coins, kills, elapsed, victory, abandoned);
                settlementPrepared = true;
            }
            if (settlementSucceeded) return true;

            var session = MetaGameSession.Current;
            if (session == null)
            {
                settlementSucceeded = true;
                settlementFailed = false;
                return true;
            }

            var before = AccountProgression.StateFor(session.Data.AccountExperience);
            var pendingAccountReward = AccountProgression.RewardFor(pendingSettlement);
            var result = session.CommitRun(pendingSettlement);
            settlementSucceeded = result.Success;
            settlementFailed = !result.Success;
            accountLevelBefore = before.Level;
            if (result.Success)
            {
                accountExperienceEarned = pendingAccountReward;
                accountLevelAfter = AccountProgression.StateFor(session.Data.AccountExperience).Level;
            }
            else
            {
                accountExperienceEarned = 0;
                accountLevelAfter = before.Level;
            }
            return result.Success;
        }

        private int RunMasteryTotal()
        {
            var total = 0;
            foreach (var points in runWeaponKillLedger.Snapshot().Values) total += points;
            return total;
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

        private Transform CreateShieldBar(Transform owner)
        {
            var root = new GameObject("Shield Guard Bar").transform;
            root.SetParent(owner, false);
            root.localPosition = new Vector3(0f, -1.48f, 0f);
            root.localRotation = Quaternion.identity;

            var background = new GameObject("Background");
            background.transform.SetParent(root, false);
            background.transform.localScale = new Vector3(2.2f, .20f, 1f);
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = solidSprite;
            backgroundRenderer.color = new Color(.12f, .09f, .06f, .94f);
            backgroundRenderer.sortingOrder = 20;

            var fill = new GameObject("Fill").transform;
            fill.SetParent(root, false);
            fill.localScale = new Vector3(2f, .10f, 1f);
            var fillRenderer = fill.gameObject.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = solidSprite;
            fillRenderer.color = new Color(.72f, .45f, .14f, 1f);
            fillRenderer.sortingOrder = 21;
            return fill;
        }

        private static void UpdateBarFill(Transform fill, float normalizedValue, float width, float height)
        {
            if (fill == null)
            {
                return;
            }

            var ratio = Mathf.Clamp01(normalizedValue);
            fill.localScale = new Vector3(width * ratio, height, 1f);
            fill.localPosition = new Vector3(-width * .5f + width * ratio * .5f, 0f, -.01f);
        }

    }
}
