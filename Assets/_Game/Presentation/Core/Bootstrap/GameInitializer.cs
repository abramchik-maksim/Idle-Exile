using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Stats;
using Game.Domain.Characters;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Progression;
using Game.Domain.Items;
using Game.Domain.Progression.TreeTalents;
using Game.Domain.Skills;
using Game.Infrastructure.Configs;
using UnityEngine;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class GameInitializer : IInitializable, IDisposable, IGameStateProvider
    {
        private readonly IPlayerProgressRepository _progressRepo;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly ITreeTalentsRepository _treeTalentsRepo;
        private readonly CalculateHeroStatsUseCase _calcStats;
        private readonly ISubscriber<InventoryChangedDTO> _inventoryChangedSub;
        private readonly ISubscriber<TreeTalentsChangedDTO> _treeChangedSub;
        private readonly StartingPresetSO _startingPreset;

        private IDisposable _autoSaveSub;
        private IDisposable _treeAutoSaveSub;

        public HeroState Hero { get; private set; }
        public InventoryModel Inventory { get; private set; }
        public PlayerProgressData Progress { get; private set; }
        public SkillCollection Skills { get; private set; }
        public SkillLoadout Loadout { get; private set; }
        public TreeTalentsState TreeTalents { get; private set; }

        public GameInitializer(
            IPlayerProgressRepository progressRepo,
            IInventoryRepository inventoryRepo,
            ITreeTalentsRepository treeTalentsRepo,
            CalculateHeroStatsUseCase calcStats,
            ISubscriber<InventoryChangedDTO> inventoryChangedSub,
            ISubscriber<TreeTalentsChangedDTO> treeChangedSub,
            StartingPresetSO startingPreset)
        {
            _progressRepo = progressRepo;
            _inventoryRepo = inventoryRepo;
            _treeTalentsRepo = treeTalentsRepo;
            _calcStats = calcStats;
            _inventoryChangedSub = inventoryChangedSub;
            _treeChangedSub = treeChangedSub;
            _startingPreset = startingPreset;
        }

        public void Initialize()
        {
            Debug.Log("[GameInitializer] Starting game...");

            Progress = _progressRepo.Load();

            var baseStats = _startingPreset != null && _startingPreset.heroBaseStats.Count > 0
                ? _startingPreset.GetBaseStatsDictionary()
                : null;
            Hero = new HeroState(Progress.HeroId ?? "default_hero", baseStats);
            Inventory = _inventoryRepo.Load();
            Skills = new SkillCollection();
            Loadout = new SkillLoadout();
            TreeTalents = _treeTalentsRepo.Load();

            bool isNewGame = !_progressRepo.HasSave();

            if (_startingPreset != null)
                ApplyStartingPreset(isNewGame);

            if (Inventory.Equipped.Count > 0)
            {
                _calcStats.Execute(Hero, Inventory.Equipped);
                Debug.Log($"[GameInitializer] Restored {Inventory.Equipped.Count} equipped items, stats recalculated.");
            }

            _autoSaveSub = _inventoryChangedSub.Subscribe(_ =>
            {
                _inventoryRepo.Save(Inventory);
                _progressRepo.Save(Progress);
                _treeTalentsRepo.Save(TreeTalents);
            });

            _treeAutoSaveSub = _treeChangedSub.Subscribe(_ => _treeTalentsRepo.Save(TreeTalents));

            Debug.Log($"[GameInitializer] Hero '{Hero.Id}' ready. Tier: {Progress.CurrentTier}, Map: {Progress.CurrentMap}, Battle: {Progress.CurrentBattle}. " +
                      $"Inventory: {Inventory.Items.Count}/{Inventory.Capacity}, Equipped: {Inventory.Equipped.Count}. " +
                      $"Skills: {Skills.Skills.Count}, Loadout main: {(Loadout.MainSkill != null ? Loadout.MainSkill.Definition.Name : "none")}");
        }

        private void ApplyStartingPreset(bool isNewGame)
        {
            if (isNewGame)
            {
                foreach (var entry in _startingPreset.startingItems)
                {
                    if (entry.item == null) continue;
                    var def = entry.item.ToDomain();
                    var instance = new ItemInstance(def, new List<Domain.Stats.Modifier>());
                    Inventory.TryAdd(instance);

                    if (entry.autoEquip)
                        Inventory.TryEquip(instance.Uid, entry.equipSlot, out _, out _, out _);

                    Debug.Log($"[GameInitializer] Preset: added item '{def.Name}', equip={entry.autoEquip}");
                }
            }

            // Skills are always applied from preset (not persisted yet)
            foreach (var entry in _startingPreset.startingSkills)
            {
                if (entry.skill == null) continue;
                var def = entry.skill.ToDomain();
                var instance = new SkillInstance(def);
                Skills.TryAdd(instance);

                if (entry.autoEquip)
                {
                    if (def.Category == SkillCategory.Main)
                        Loadout.TryEquipMain(instance, out _);
                    else
                        Loadout.TryEquipUtility(instance, entry.slotIndex, out _);
                }

                Debug.Log($"[GameInitializer] Preset: added skill '{def.Name}', equip={entry.autoEquip}, slot={entry.slotIndex}");
            }
        }

        public void Dispose()
        {
            _autoSaveSub?.Dispose();
            _treeAutoSaveSub?.Dispose();

            if (Progress != null)
                _progressRepo.Save(Progress);
            if (Inventory != null)
                _inventoryRepo.Save(Inventory);
            if (TreeTalents != null)
                _treeTalentsRepo.Save(TreeTalents);

            Debug.Log("[GameInitializer] Progress saved on dispose.");
        }
    }
}
