using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using VContainer;
using Unity.Entities;
using Unity.Mathematics;
using Game.Application.Ports;
using Game.Application.Skills;
using Game.Domain.Combat;
using Game.Domain.Combat.Progression;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Skills;
using Game.Domain.DTOs.Stats;
using Game.Domain.Items;
using Game.Domain.Skills;
using Game.Domain.Skills.Crafting;
using Game.Domain.Stats;
using Game.Presentation.Combat.Arena;
using Game.Presentation.Combat.Components;
using Game.Presentation.Combat.Rendering;

namespace Game.Presentation.Combat
{
    public sealed class CombatBridge : MonoBehaviour, IHeroHealthProvider
    {
        private IGameStateProvider _gameState;
        private WaveSpawner _waveSpawner;
        private DamageEventProcessor _damageProcessor;
        private UtilitySkillRunner _utilityRunner;
        private CombatVisualManager _visualManager;
        private ICharacterConfigProvider _characterConfig;
        private GameSessionContext _session;

        private ISubscriber<HeroStatsChangedDTO> _heroStatsChangedSub;
        private ISubscriber<SkillEquippedDTO> _skillEquippedSub;
        private ISubscriber<SkillUnequippedDTO> _skillUnequippedSub;
        private ISubscriber<SkillsChangedDTO> _skillsChangedSub;
        private ISubscriber<ItemEquippedDTO> _itemEquippedSub;
        private ISubscriber<ItemUnequippedDTO> _itemUnequippedSub;
        private ISubscriber<SkillAffixAddedDTO> _affixAddedSub;
        private ISubscriber<SkillAffixRemovedDTO> _affixRemovedSub;

        private EntityManager _entityManager;
        private Entity _heroEntity;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<Entity> _bakedWallEntities = new();

        [Tooltip("Optional fallback: instantiate this prefab under CombatBridge when no location root is registered. " +
                 "When LocationController loads the same arena from Addressables, leave this false and it will RegisterLocationArenaRoot instead (avoids duplicate hierarchy and double scale).")]
        [SerializeField] private GameObject _arenaPrefab;

        [SerializeField]
        private bool _spawnArenaPrefabFromBridge;

        [Tooltip("After Instantiate, adjust arena root localScale so world scale matches the prefab root design scale, divided by this transform's lossyScale (fixes 0.4×0.4 when parent is also 0.4).")]
        [SerializeField]
        private bool _compensateArenaRootScaleForParentLossyScale = true;

        private GameObject _arenaRuntimeInstance;
        private GameObject _externalArenaRoot;

        public bool IsReady { get; private set; }

        [Inject]
        public void Construct(
            IGameStateProvider gameState,
            WaveSpawner waveSpawner,
            DamageEventProcessor damageProcessor,
            UtilitySkillRunner utilityRunner,
            CombatVisualManager visualManager,
            ICharacterConfigProvider characterConfig,
            GameSessionContext session,
            ISubscriber<HeroStatsChangedDTO> heroStatsChangedSub,
            ISubscriber<SkillEquippedDTO> skillEquippedSub,
            ISubscriber<SkillUnequippedDTO> skillUnequippedSub,
            ISubscriber<SkillsChangedDTO> skillsChangedSub,
            ISubscriber<ItemEquippedDTO> itemEquippedSub,
            ISubscriber<ItemUnequippedDTO> itemUnequippedSub,
            ISubscriber<SkillAffixAddedDTO> affixAddedSub,
            ISubscriber<SkillAffixRemovedDTO> affixRemovedSub)
        {
            _gameState = gameState;
            _waveSpawner = waveSpawner;
            _damageProcessor = damageProcessor;
            _utilityRunner = utilityRunner;
            _visualManager = visualManager;
            _characterConfig = characterConfig;
            _session = session;
            _heroStatsChangedSub = heroStatsChangedSub;
            _skillEquippedSub = skillEquippedSub;
            _skillUnequippedSub = skillUnequippedSub;
            _skillsChangedSub = skillsChangedSub;
            _itemEquippedSub = itemEquippedSub;
            _itemUnequippedSub = itemUnequippedSub;
            _affixAddedSub = affixAddedSub;
            _affixRemovedSub = affixRemovedSub;

            _subscriptions.Add(_heroStatsChangedSub.Subscribe(OnHeroStatsChanged));
            _subscriptions.Add(_skillEquippedSub.Subscribe(_ => { RefreshAttackState(); ReinitializeUtilityRunner(); }));
            _subscriptions.Add(_skillUnequippedSub.Subscribe(_ => { RefreshAttackState(); ReinitializeUtilityRunner(); }));
            _subscriptions.Add(_skillsChangedSub.Subscribe(_ => { RefreshAttackState(); ReinitializeUtilityRunner(); }));
            _subscriptions.Add(_itemEquippedSub.Subscribe(_ => RefreshAttackState()));
            _subscriptions.Add(_itemUnequippedSub.Subscribe(_ => RefreshAttackState()));
            _subscriptions.Add(_affixAddedSub.Subscribe(_ => RefreshSkillAffixData()));
            _subscriptions.Add(_affixRemovedSub.Subscribe(_ => RefreshSkillAffixData()));
        }

        private void Update()
        {
            TryInitialize();
            if (!IsReady) return;
            TickUtilitySkills(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!IsReady) return;
            _damageProcessor.ProcessFrame();
        }

        private void TryInitialize()
        {
            if (IsReady) return;
            if (_gameState?.Hero == null) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _entityManager = world.EntityManager;

            SpawnHeroEntity();
            SpawnArenaWalls();

            _waveSpawner.Initialize(_entityManager, _visualManager, 1);
            _damageProcessor.Initialize(world.GetExistingSystemManaged<Systems.DamageEventBufferSystem>());

            _utilityRunner.OnBuffsChanged += ApplyBuffBonuses;
            _utilityRunner.OnCloneRequested += HandleCloneRequested;
            _utilityRunner.Initialize(_gameState.Loadout);

            IsReady = true;
            RefreshAttackState();

            Debug.Log("[CombatBridge] ECS bridge initialized.");
        }

        private static readonly float2 HeroStartPosition = new(0f, -1.7f);

        private void SpawnHeroEntity()
        {
            var hero = _gameState.Hero;
            _heroEntity = _entityManager.CreateEntity(
                typeof(HeroTag),
                typeof(Position2D),
                typeof(CombatStats),
                typeof(AttackCooldown),
                typeof(ActorId),
                typeof(Targetable),
                typeof(StatusEffects),
                typeof(AilmentState),
                typeof(HeroAttackRange),
                typeof(HeroSkillAffixData),
                typeof(VisualId),
                typeof(ProjectileVisualId)
            );
            _entityManager.AddBuffer<BleedStack>(_heroEntity);

            float attackSpeed = hero.Stats.GetFinal(StatType.AttackSpeed);
            float cooldown = attackSpeed > 0 ? 1f / attackSpeed : 1f;

            _entityManager.SetComponentData(_heroEntity, new Position2D { Value = HeroStartPosition });
            _entityManager.SetComponentData(_heroEntity, BuildCombatStats(hero, attackSpeed));
            _entityManager.SetComponentData(_heroEntity, new AttackCooldown { Cooldown = cooldown, Timer = cooldown });
            _entityManager.SetComponentData(_heroEntity, new ActorId { Value = 0 });
            _entityManager.SetComponentData(_heroEntity, new Targetable { AggroWeight = 10f });

            var charDef = _characterConfig.GetByClass(_session.SelectedClass);
            int heroVisualId = charDef.VisualId;
            _entityManager.SetComponentData(_heroEntity, new VisualId { Value = heroVisualId });
            _entityManager.SetComponentData(_heroEntity, new ProjectileVisualId { Value = charDef.ProjectileVisualId });
            _visualManager.OnEntitySpawned(0, heroVisualId);

            UpdateHeroAttackRange();
            RefreshSkillAffixData();

            Debug.Log($"[CombatBridge] Hero entity created. Damage: {hero.Stats.GetFinal(StatType.PhysicalDamage)}, AS: {attackSpeed}");
        }

        private CombatStats BuildCombatStats(Domain.Characters.HeroState hero, float attackSpeed)
        {
            GetProjectileProcPercents(hero, _gameState.Loadout?.MainSkill, out float fork, out float pierce, out float chain);

            return new CombatStats
            {
                MaxHealth = hero.Stats.GetFinal(StatType.MaxHealth),
                CurrentHealth = hero.Stats.GetFinal(StatType.CurrentHealth),
                PhysicalDamage = hero.Stats.GetFinal(StatType.PhysicalDamage),
                FireDamage = hero.Stats.GetFinal(StatType.FireDamage),
                ColdDamage = hero.Stats.GetFinal(StatType.ColdDamage),
                LightningDamage = hero.Stats.GetFinal(StatType.LightningDamage),
                CorrosionDamage = hero.Stats.GetFinal(StatType.CorrosionDamage),
                CriticalChance = hero.Stats.GetFinal(StatType.CriticalChance),
                CriticalMultiplier = hero.Stats.GetFinal(StatType.CriticalMultiplier),
                AttackSpeed = attackSpeed,
                Armor = hero.Stats.GetFinal(StatType.Armor),
                MoveSpeed = hero.Stats.GetFinal(StatType.MovementSpeed),
                Evasion = hero.Stats.GetFinal(StatType.Evasion),
                BlockChance = hero.Stats.GetFinal(StatType.BlockChance),
                LifeLeech = hero.Stats.GetFinal(StatType.LifeLeech),
                FireResistance = hero.Stats.GetFinal(StatType.FireResistance),
                ColdResistance = hero.Stats.GetFinal(StatType.ColdResistance),
                LightningResistance = hero.Stats.GetFinal(StatType.LightningResistance),
                CorrosionResistance = hero.Stats.GetFinal(StatType.CorrosionResistance),
                DoubleHitChance = hero.Stats.GetFinal(StatType.DoubleHitChance),
                IgnoreArmorChance = hero.Stats.GetFinal(StatType.IgnoreArmorChance),
                ProjectileForkPercent = fork,
                ProjectilePiercePercent = pierce,
                ProjectileChainPercent = chain,
            };
        }

        private static bool IsMeleeWeapon(WeaponType wt) =>
            wt == WeaponType.Sword || wt == WeaponType.Axe || wt == WeaponType.Dagger;

        private static void GetProjectileProcPercents(
            Domain.Characters.HeroState hero,
            SkillInstance mainSkill,
            out float fork,
            out float pierce,
            out float chain)
        {
            fork = 0f;
            pierce = 0f;
            chain = 0f;

            if (mainSkill == null) return;

            var wt = mainSkill.Definition.RequiredWeapon;
            if (IsMeleeWeapon(wt)) return;

            if (wt == WeaponType.Bow)
            {
                fork = hero.Stats.GetFinal(StatType.RangedForkChance);
                pierce = hero.Stats.GetFinal(StatType.RangedPierceChance);
                chain = hero.Stats.GetFinal(StatType.RangedChainChance);
                return;
            }

            fork = hero.Stats.GetFinal(StatType.SpellForkChance);
            pierce = hero.Stats.GetFinal(StatType.SpellPierceChance);
            chain = hero.Stats.GetFinal(StatType.SpellChainChance);
        }

        private void UpdateHeroAttackRange()
        {
            if (!_entityManager.Exists(_heroEntity)) return;

            var mainSkill = _gameState.Loadout?.MainSkill;
            bool isMelee = false;
            float range = 50f;

            if (mainSkill != null)
            {
                var wt = mainSkill.Definition.RequiredWeapon;
                isMelee = IsMeleeWeapon(wt);
                range = isMelee ? 1.5f : 50f;
            }

            _entityManager.SetComponentData(_heroEntity, new HeroAttackRange
            {
                Value = range,
                IsMelee = (byte)(isMelee ? 1 : 0)
            });
        }

        public void ResetHeroPosition()
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;
            _entityManager.SetComponentData(_heroEntity, new Position2D { Value = HeroStartPosition });
        }

        public void SpawnWave(WaveDefinition wave, float tierScaling) =>
            _waveSpawner.SpawnWave(wave, tierScaling);

        public int GetAliveEnemyCount() => _waveSpawner.GetAliveEnemyCount();

        public void DespawnAllEnemies() => _waveSpawner.DespawnAllEnemies();

        private void HandleCloneRequested(float damagePercent, float duration) =>
            _waveSpawner.SpawnClone(_heroEntity, damagePercent, duration);

        public bool IsHeroDead()
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return false;
            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            return stats.CurrentHealth <= 0f;
        }

        public void RestoreHeroHealth()
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;
            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            stats.CurrentHealth = stats.MaxHealth;
            _entityManager.SetComponentData(_heroEntity, stats);
        }

        public float GetHeroHealthPercent()
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return 1f;
            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            return stats.MaxHealth > 0f
                ? math.clamp(stats.CurrentHealth / stats.MaxHealth, 0f, 1f)
                : 1f;
        }

        private void ReinitializeUtilityRunner()
        {
            if (_utilityRunner == null) return;
            _utilityRunner.Initialize(_gameState.Loadout);
            ApplyBuffBonuses();
        }

        private void TickUtilitySkills(float dt)
        {
            if (_utilityRunner == null) return;
            _utilityRunner.Tick(dt);

            float healPerSec = _utilityRunner.GetHealPerSecond();
            if (healPerSec > 0f && _entityManager.Exists(_heroEntity))
            {
                var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
                stats.CurrentHealth = math.min(stats.CurrentHealth + healPerSec * dt, stats.MaxHealth);
                _entityManager.SetComponentData(_heroEntity, stats);
            }
        }

        private void ApplyBuffBonuses()
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;

            var hero = _gameState.Hero;
            var mainSkill = _gameState.Loadout?.MainSkill;

            float baseAtkSpd = hero.Stats.GetFinal(StatType.AttackSpeed);
            float baseArmor = hero.Stats.GetFinal(StatType.Armor);

            float dmgMult = mainSkill?.Definition.DamageMultiplierPercent / 100f ?? 1f;
            float asMult = mainSkill?.Definition.AttackSpeedMultiplierPercent / 100f ?? 1f;

            var bonuses = _utilityRunner.GetBuffBonuses();

            float finalAs = baseAtkSpd * asMult;
            float finalArmor = baseArmor;

            if (bonuses.TryGetValue(StatType.AttackSpeed, out float asBonus))
                finalAs += baseAtkSpd * asBonus;
            if (bonuses.TryGetValue(StatType.Armor, out float armorBonus))
                finalArmor += armorBonus;

            var affixData = _entityManager.GetComponentData<HeroSkillAffixData>(_heroEntity);
            var attacker = new StatCollection();
            attacker.SetBase(StatType.PhysicalDamage, hero.Stats.GetFinal(StatType.PhysicalDamage));
            attacker.SetBase(StatType.FireDamage, hero.Stats.GetFinal(StatType.FireDamage));
            attacker.SetBase(StatType.ColdDamage, hero.Stats.GetFinal(StatType.ColdDamage));
            attacker.SetBase(StatType.LightningDamage, hero.Stats.GetFinal(StatType.LightningDamage));
            attacker.SetBase(StatType.CorrosionDamage, hero.Stats.GetFinal(StatType.CorrosionDamage));
            attacker.SetBase(StatType.CriticalChance, hero.Stats.GetFinal(StatType.CriticalChance));
            attacker.SetBase(StatType.CriticalMultiplier, hero.Stats.GetFinal(StatType.CriticalMultiplier));
            if (dmgMult != 1f)
                attacker.AddModifier(new Modifier(StatType.PhysicalDamage, ModifierType.More, dmgMult - 1f, "main_skill_damage_mult"));

            foreach (var mod in hero.Stats.Modifiers)
            {
                if (mod.Stat == StatType.GlobalDamage)
                    attacker.AddModifier(mod);
            }

            var defender = new StatCollection();
            defender.SetBase(StatType.Armor, 0f);
            var breakdown = DamageCalculator.CalculateMultiType(
                attacker,
                defender,
                new GainAsElementData(
                    affixData.GainAsFirePercent,
                    affixData.GainAsColdPercent,
                    affixData.GainAsLightningPercent,
                    affixData.GainAsPhysicalPercent,
                    affixData.GainAsCorrosionPercent),
                () => 1d);

            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            stats.PhysicalDamage = breakdown.PhysicalDamage;
            stats.FireDamage = breakdown.FireDamage;
            stats.ColdDamage = breakdown.ColdDamage;
            stats.LightningDamage = breakdown.LightningDamage;
            stats.CorrosionDamage = breakdown.CorrosionDamage;
            stats.CriticalChance = hero.Stats.GetFinal(StatType.CriticalChance);
            stats.CriticalMultiplier = hero.Stats.GetFinal(StatType.CriticalMultiplier);
            stats.AttackSpeed = finalAs;
            stats.Armor = finalArmor;
            GetProjectileProcPercents(hero, mainSkill, out float fork, out float pierce, out float chain);
            stats.ProjectileForkPercent = fork;
            stats.ProjectilePiercePercent = pierce;
            stats.ProjectileChainPercent = chain;
            _entityManager.SetComponentData(_heroEntity, stats);

            float cooldown = finalAs > 0 ? 1f / finalAs : 1f;
            var cd = _entityManager.GetComponentData<AttackCooldown>(_heroEntity);
            cd.Cooldown = cooldown;
            _entityManager.SetComponentData(_heroEntity, cd);
        }

        private const string SkillAffixSourcePrefix = "skill_affix_";

        private void RefreshSkillAffixData()
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;

            _gameState.Hero.Stats.RemoveModifiersBySourcePrefix(SkillAffixSourcePrefix);

            var mainSkill = _gameState.Loadout?.MainSkill;
            var data = new HeroSkillAffixData();

            if (mainSkill != null)
            {
                foreach (var affix in mainSkill.Affixes.GetAll())
                {
                    switch (affix.Definition.Type)
                    {
                        case SkillAffixType.AddFlatElementalDamage:
                            ApplyFlatDamageAffix(affix);
                            break;

                        case SkillAffixType.ChanceToAilmentOnHit:
                            ApplyAilmentChance(ref data, affix);
                            break;

                        case SkillAffixType.GainDamageAsElement:
                            ApplyGainAsElement(ref data, affix);
                            break;

                        case SkillAffixType.ChanceToAoEAilmentOnKill:
                            data.AoEAilmentChance = affix.Value1 / 100f;
                            data.AoEAilmentRadius = affix.Value2;
                            data.AoEAilmentType = affix.Definition.AilmentType;
                            break;
                    }
                }
            }

            MergeGearAilmentChances(ref data);
            MergeGearGainAs(ref data);

            _entityManager.SetComponentData(_heroEntity, data);
            ApplyBuffBonuses();
        }

        private void MergeGearAilmentChances(ref HeroSkillAffixData data)
        {
            var hero = _gameState.Hero;
            data.IgniteChance += hero.Stats.GetFinal(StatType.IgniteChance)
                                 + hero.Stats.GetFinal(StatType.AilmentChanceAll);
            data.ChillChance  += hero.Stats.GetFinal(StatType.ChillChance)
                                 + hero.Stats.GetFinal(StatType.AilmentChanceAll);
            data.ShockChance  += hero.Stats.GetFinal(StatType.ShockChance)
                                 + hero.Stats.GetFinal(StatType.AilmentChanceAll);
            data.BleedChance  += hero.Stats.GetFinal(StatType.BleedChance)
                                 + hero.Stats.GetFinal(StatType.AilmentChanceAll);
        }

        private void MergeGearGainAs(ref HeroSkillAffixData data)
        {
            var hero = _gameState.Hero;
            data.GainAsFirePercent      += hero.Stats.GetFinal(StatType.GainAsFirePercent);
            data.GainAsColdPercent      += hero.Stats.GetFinal(StatType.GainAsColdPercent);
            data.GainAsLightningPercent += hero.Stats.GetFinal(StatType.GainAsLightningPercent);
            data.GainAsPhysicalPercent  += hero.Stats.GetFinal(StatType.GainAsPhysicalPercent);
            data.GainAsCorrosionPercent += hero.Stats.GetFinal(StatType.GainAsCorrosionPercent);
        }

        private void ApplyFlatDamageAffix(SkillAffix affix)
        {
            var hero = _gameState.Hero;
            float avgDamage = (affix.Value1 + affix.Value2) * 0.5f;
            var statType = affix.Definition.DamageType switch
            {
                DamageType.Fire => StatType.FireDamage,
                DamageType.Cold => StatType.ColdDamage,
                DamageType.Lightning => StatType.LightningDamage,
                _ => StatType.PhysicalDamage
            };
            hero.Stats.RemoveModifiersBySource("skill_affix_" + affix.Definition.Id);
            hero.Stats.AddModifier(new Modifier(statType, ModifierType.Flat, avgDamage, "skill_affix_" + affix.Definition.Id));
        }

        private static void ApplyAilmentChance(ref HeroSkillAffixData data, SkillAffix affix)
        {
            float chance = affix.Value1 / 100f;
            switch (affix.Definition.AilmentType)
            {
                case AilmentType.Ignite: data.IgniteChance += chance; break;
                case AilmentType.Chill: data.ChillChance += chance; break;
                case AilmentType.Shock: data.ShockChance += chance; break;
                case AilmentType.Bleed: data.BleedChance += chance; break;
            }
        }

        private static void ApplyGainAsElement(ref HeroSkillAffixData data, SkillAffix affix)
        {
            float fraction = affix.Value1 / 100f;
            switch (affix.Definition.DamageType)
            {
                case DamageType.Fire: data.GainAsFirePercent += fraction; break;
                case DamageType.Cold: data.GainAsColdPercent += fraction; break;
                case DamageType.Lightning: data.GainAsLightningPercent += fraction; break;
            }
        }

        private void RefreshAttackState()
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;

            UpdateHeroAttackRange();

            bool canAttack = CanHeroAttack();
            bool hasComponent = _entityManager.HasComponent<AttackEnabled>(_heroEntity);

            if (canAttack && !hasComponent)
            {
                _entityManager.AddComponent<AttackEnabled>(_heroEntity);
                ApplyBuffBonuses();
                Debug.Log("[CombatBridge] Attack ENABLED.");
            }
            else if (!canAttack && hasComponent)
            {
                _entityManager.RemoveComponent<AttackEnabled>(_heroEntity);
                Debug.Log("[CombatBridge] Attack DISABLED — no valid main skill or weapon.");
            }
            else if (canAttack)
            {
                ApplyBuffBonuses();
            }
        }

        private bool CanHeroAttack()
        {
            var mainSkill = _gameState.Loadout?.MainSkill;
            if (mainSkill == null) return false;

            var requiredWeapon = mainSkill.Definition.RequiredWeapon;
            if (requiredWeapon == WeaponType.None) return true;

            if (!_gameState.Inventory.Equipped.TryGetValue(EquipmentSlotType.MainHand, out var weapon))
                return false;

            return weapon.Definition.WeaponType == requiredWeapon;
        }

        private static float Stat(IReadOnlyDictionary<StatType, float> s, StatType t, float fallback = 0f) =>
            s.TryGetValue(t, out var v) ? v : fallback;

        private void OnHeroStatsChanged(HeroStatsChangedDTO dto)
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;

            var f = dto.FinalStats;
            float atkSpd = Stat(f, StatType.AttackSpeed, 1f);

            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            stats.MaxHealth          = Stat(f, StatType.MaxHealth, 100f);
            stats.PhysicalDamage     = Stat(f, StatType.PhysicalDamage, 10f);
            stats.FireDamage         = Stat(f, StatType.FireDamage);
            stats.ColdDamage         = Stat(f, StatType.ColdDamage);
            stats.LightningDamage    = Stat(f, StatType.LightningDamage);
            stats.CorrosionDamage    = Stat(f, StatType.CorrosionDamage);
            stats.CriticalChance     = Stat(f, StatType.CriticalChance, 0.05f);
            stats.CriticalMultiplier = Stat(f, StatType.CriticalMultiplier, 1.5f);
            stats.Armor              = Stat(f, StatType.Armor, 5f);
            stats.AttackSpeed        = atkSpd;
            stats.MoveSpeed          = Stat(f, StatType.MovementSpeed, 3f);
            stats.Evasion            = Stat(f, StatType.Evasion);
            stats.BlockChance        = Stat(f, StatType.BlockChance);
            stats.LifeLeech          = Stat(f, StatType.LifeLeech);
            stats.FireResistance     = Stat(f, StatType.FireResistance);
            stats.ColdResistance     = Stat(f, StatType.ColdResistance);
            stats.LightningResistance = Stat(f, StatType.LightningResistance);
            stats.CorrosionResistance = Stat(f, StatType.CorrosionResistance);
            stats.DoubleHitChance    = Stat(f, StatType.DoubleHitChance);
            stats.IgnoreArmorChance  = Stat(f, StatType.IgnoreArmorChance);
            GetProjectileProcPercents(_gameState.Hero, _gameState.Loadout?.MainSkill, out float fork, out float pierce, out float chain);
            stats.ProjectileForkPercent = fork;
            stats.ProjectilePiercePercent = pierce;
            stats.ProjectileChainPercent = chain;
            _entityManager.SetComponentData(_heroEntity, stats);

            float cooldown = atkSpd > 0 ? 1f / atkSpd : 1f;
            var cd = _entityManager.GetComponentData<AttackCooldown>(_heroEntity);
            cd.Cooldown = cooldown;
            _entityManager.SetComponentData(_heroEntity, cd);

            RefreshSkillAffixData();
            RefreshAttackState();

            Debug.Log($"[CombatBridge] Hero stats updated. Damage: {stats.PhysicalDamage}, AS: {atkSpd}");
        }

        private void OnDestroy()
        {
            if (_utilityRunner != null)
            {
                _utilityRunner.OnBuffsChanged -= ApplyBuffBonuses;
                _utilityRunner.OnCloneRequested -= HandleCloneRequested;
            }

            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            ClearBakedWallEntities();
            if (_arenaRuntimeInstance != null)
            {
                Destroy(_arenaRuntimeInstance);
                _arenaRuntimeInstance = null;
            }

            _externalArenaRoot = null;

            _waveSpawner?.Dispose();
        }

        /// <summary>
        /// Call from <c>LocationController</c> after it instantiates the location prefab.
        /// Re-bakes ECS walls from that hierarchy and destroys any arena instance previously spawned by the bridge (removes duplicate + wrong scale).
        /// </summary>
        public void RegisterLocationArenaRoot(GameObject locationRoot)
        {
            if (locationRoot == null)
                return;

            _externalArenaRoot = locationRoot;

            if (_arenaRuntimeInstance != null)
            {
                Destroy(_arenaRuntimeInstance);
                _arenaRuntimeInstance = null;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || _entityManager == default)
                return;

            RebakeArenaWallsFromCurrentSource();
        }

        /// <summary>Call before destroying the location instance so ECS walls are cleared.</summary>
        public void UnregisterLocationArenaRoot(GameObject locationRoot)
        {
            if (locationRoot == null || _externalArenaRoot != locationRoot)
                return;

            _externalArenaRoot = null;
            ClearBakedWallEntities();
        }

        private void RebakeArenaWallsFromCurrentSource()
        {
            ClearBakedWallEntities();

            var baker = FindArenaBaker();
            if (baker == null)
            {
                Debug.LogWarning("[CombatBridge] RebakeArenaWallsFromCurrentSource: no ArenaColliderBaker found.");
                return;
            }

            baker.BakeIntoEntities(_entityManager, _bakedWallEntities);
        }

        private ArenaColliderBaker FindArenaBaker()
        {
            if (_externalArenaRoot != null)
                return _externalArenaRoot.GetComponentInChildren<ArenaColliderBaker>(true);
            if (_arenaRuntimeInstance != null)
                return _arenaRuntimeInstance.GetComponentInChildren<ArenaColliderBaker>(true);
            return null;
        }

        private static void ApplyArenaRootScaleForParent(Transform instanceRoot, Transform prefabRootTemplate,
            Transform parent)
        {
            Vector3 p = parent.lossyScale;
            Vector3 design = prefabRootTemplate.localScale;
            float cx = Mathf.Abs(p.x) > 1e-6f ? design.x / p.x : design.x;
            float cy = Mathf.Abs(p.y) > 1e-6f ? design.y / p.y : design.y;
            float cz = Mathf.Abs(p.z) > 1e-6f ? design.z / p.z : design.z;
            instanceRoot.localScale = new Vector3(cx, cy, cz);
        }

        private void SpawnArenaWalls()
        {
            ClearBakedWallEntities();

            ArenaColliderBaker baker = null;

            if (_externalArenaRoot != null)
            {
                baker = _externalArenaRoot.GetComponentInChildren<ArenaColliderBaker>(true);
            }
            else if (_spawnArenaPrefabFromBridge && _arenaPrefab != null)
            {
                _arenaRuntimeInstance = Instantiate(_arenaPrefab, transform);
                _arenaRuntimeInstance.name = "ArenaRuntime";

                if (_compensateArenaRootScaleForParentLossyScale)
                    ApplyArenaRootScaleForParent(_arenaRuntimeInstance.transform, _arenaPrefab.transform, transform);

                baker = _arenaRuntimeInstance.GetComponentInChildren<ArenaColliderBaker>(true);
            }

            if (baker == null)
            {
                if (!_spawnArenaPrefabFromBridge && _externalArenaRoot == null)
                    Debug.Log(
                        "[CombatBridge] Arena wall bake deferred: _spawnArenaPrefabFromBridge is off; " +
                        "walls will bake when LocationController calls RegisterLocationArenaRoot.");
                else
                    Debug.LogWarning(
                        "[CombatBridge] No arena wall bake source: enable _spawnArenaPrefabFromBridge and assign _arenaPrefab, " +
                        "or ensure the location prefab contains ArenaColliderBaker.");
                return;
            }

            baker.BakeIntoEntities(_entityManager, _bakedWallEntities);
        }

        private void ClearBakedWallEntities()
        {
            if (_bakedWallEntities.Count == 0)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                _bakedWallEntities.Clear();
                return;
            }

            var em = world.EntityManager;
            foreach (var e in _bakedWallEntities)
            {
                if (em.Exists(e))
                    em.DestroyEntity(e);
            }

            _bakedWallEntities.Clear();
        }
    }
}
