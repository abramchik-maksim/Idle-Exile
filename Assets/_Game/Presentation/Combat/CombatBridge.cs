using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using VContainer;
using Unity.Entities;
using Unity.Mathematics;
using Game.Application.Ports;
using Game.Application.Skills;
using Game.Domain.Combat.Progression;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Skills;
using Game.Domain.DTOs.Stats;
using Game.Domain.Items;
using Game.Domain.Skills;
using Game.Domain.Stats;
using Game.Presentation.Combat.Components;

namespace Game.Presentation.Combat
{
    public sealed class CombatBridge : MonoBehaviour, IHeroHealthProvider
    {
        private IGameStateProvider _gameState;
        private WaveSpawner _waveSpawner;
        private DamageEventProcessor _damageProcessor;
        private UtilitySkillRunner _utilityRunner;

        private ISubscriber<HeroStatsChangedDTO> _heroStatsChangedSub;
        private ISubscriber<SkillEquippedDTO> _skillEquippedSub;
        private ISubscriber<SkillUnequippedDTO> _skillUnequippedSub;
        private ISubscriber<SkillsChangedDTO> _skillsChangedSub;
        private ISubscriber<ItemEquippedDTO> _itemEquippedSub;
        private ISubscriber<ItemUnequippedDTO> _itemUnequippedSub;

        private EntityManager _entityManager;
        private Entity _heroEntity;
        private readonly List<IDisposable> _subscriptions = new();

        public bool IsReady { get; private set; }

        [Inject]
        public void Construct(
            IGameStateProvider gameState,
            WaveSpawner waveSpawner,
            DamageEventProcessor damageProcessor,
            UtilitySkillRunner utilityRunner,
            ISubscriber<HeroStatsChangedDTO> heroStatsChangedSub,
            ISubscriber<SkillEquippedDTO> skillEquippedSub,
            ISubscriber<SkillUnequippedDTO> skillUnequippedSub,
            ISubscriber<SkillsChangedDTO> skillsChangedSub,
            ISubscriber<ItemEquippedDTO> itemEquippedSub,
            ISubscriber<ItemUnequippedDTO> itemUnequippedSub)
        {
            _gameState = gameState;
            _waveSpawner = waveSpawner;
            _damageProcessor = damageProcessor;
            _utilityRunner = utilityRunner;
            _heroStatsChangedSub = heroStatsChangedSub;
            _skillEquippedSub = skillEquippedSub;
            _skillUnequippedSub = skillUnequippedSub;
            _skillsChangedSub = skillsChangedSub;
            _itemEquippedSub = itemEquippedSub;
            _itemUnequippedSub = itemUnequippedSub;

            _subscriptions.Add(_heroStatsChangedSub.Subscribe(OnHeroStatsChanged));
            _subscriptions.Add(_skillEquippedSub.Subscribe(_ => { RefreshAttackState(); ReinitializeUtilityRunner(); }));
            _subscriptions.Add(_skillUnequippedSub.Subscribe(_ => { RefreshAttackState(); ReinitializeUtilityRunner(); }));
            _subscriptions.Add(_skillsChangedSub.Subscribe(_ => { RefreshAttackState(); ReinitializeUtilityRunner(); }));
            _subscriptions.Add(_itemEquippedSub.Subscribe(_ => RefreshAttackState()));
            _subscriptions.Add(_itemUnequippedSub.Subscribe(_ => RefreshAttackState()));
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

            _waveSpawner.Initialize(_entityManager, 1);
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
                typeof(HeroAttackRange)
            );
            _entityManager.AddBuffer<BleedStack>(_heroEntity);

            float attackSpeed = hero.Stats.GetFinal(StatType.AttackSpeed);
            float cooldown = attackSpeed > 0 ? 1f / attackSpeed : 1f;

            _entityManager.SetComponentData(_heroEntity, new Position2D { Value = HeroStartPosition });
            _entityManager.SetComponentData(_heroEntity, new CombatStats
            {
                MaxHealth = hero.Stats.GetFinal(StatType.MaxHealth),
                CurrentHealth = hero.Stats.GetFinal(StatType.CurrentHealth),
                PhysicalDamage = hero.Stats.GetFinal(StatType.PhysicalDamage),
                AttackSpeed = attackSpeed,
                Armor = hero.Stats.GetFinal(StatType.Armor),
                MoveSpeed = hero.Stats.GetFinal(StatType.MovementSpeed)
            });
            _entityManager.SetComponentData(_heroEntity, new AttackCooldown { Cooldown = cooldown, Timer = cooldown });
            _entityManager.SetComponentData(_heroEntity, new ActorId { Value = 0 });
            _entityManager.SetComponentData(_heroEntity, new Targetable { AggroWeight = 10f });

            UpdateHeroAttackRange();

            Debug.Log($"[CombatBridge] Hero entity created. Damage: {hero.Stats.GetFinal(StatType.PhysicalDamage)}, AS: {attackSpeed}");
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
                isMelee = wt == WeaponType.Sword || wt == WeaponType.Axe || wt == WeaponType.Dagger;
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

            float baseDmg = hero.Stats.GetFinal(StatType.PhysicalDamage);
            float baseAtkSpd = hero.Stats.GetFinal(StatType.AttackSpeed);
            float baseArmor = hero.Stats.GetFinal(StatType.Armor);

            float dmgMult = mainSkill?.Definition.DamageMultiplierPercent / 100f ?? 1f;
            float asMult = mainSkill?.Definition.AttackSpeedMultiplierPercent / 100f ?? 1f;

            var bonuses = _utilityRunner.GetBuffBonuses();

            float finalDmg = baseDmg * dmgMult;
            float finalAs = baseAtkSpd * asMult;
            float finalArmor = baseArmor;

            if (bonuses.TryGetValue(StatType.AttackSpeed, out float asBonus))
                finalAs += baseAtkSpd * asBonus;
            if (bonuses.TryGetValue(StatType.Armor, out float armorBonus))
                finalArmor += armorBonus;

            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            stats.PhysicalDamage = finalDmg;
            stats.AttackSpeed = finalAs;
            stats.Armor = finalArmor;
            _entityManager.SetComponentData(_heroEntity, stats);

            float cooldown = finalAs > 0 ? 1f / finalAs : 1f;
            var cd = _entityManager.GetComponentData<AttackCooldown>(_heroEntity);
            cd.Cooldown = cooldown;
            _entityManager.SetComponentData(_heroEntity, cd);
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

        private void OnHeroStatsChanged(HeroStatsChangedDTO dto)
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;

            float physDmg = dto.FinalStats.TryGetValue(StatType.PhysicalDamage, out var d) ? d : 10f;
            float maxHp = dto.FinalStats.TryGetValue(StatType.MaxHealth, out var h) ? h : 100f;
            float armor = dto.FinalStats.TryGetValue(StatType.Armor, out var a) ? a : 5f;
            float atkSpd = dto.FinalStats.TryGetValue(StatType.AttackSpeed, out var s) ? s : 1f;
            float moveSpd = dto.FinalStats.TryGetValue(StatType.MovementSpeed, out var m) ? m : 3f;

            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            stats.MaxHealth = maxHp;
            stats.PhysicalDamage = physDmg;
            stats.Armor = armor;
            stats.AttackSpeed = atkSpd;
            stats.MoveSpeed = moveSpd;
            _entityManager.SetComponentData(_heroEntity, stats);

            float cooldown = atkSpd > 0 ? 1f / atkSpd : 1f;
            var cd = _entityManager.GetComponentData<AttackCooldown>(_heroEntity);
            cd.Cooldown = cooldown;
            _entityManager.SetComponentData(_heroEntity, cd);

            RefreshAttackState();

            Debug.Log($"[CombatBridge] Hero stats updated. Damage: {physDmg}, AS: {atkSpd}");
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

            _waveSpawner?.Dispose();
        }
    }
}
