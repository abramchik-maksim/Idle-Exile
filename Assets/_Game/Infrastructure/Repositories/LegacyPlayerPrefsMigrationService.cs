using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.SaveSystem;
using UnityEngine;

namespace Game.Infrastructure.Repositories
{
    public sealed class LegacyPlayerPrefsMigrationService
    {
        private const string LegacyProgressKey = "player_progress";
        private const string LegacyInventoryKey = "player_inventory";
        private const string LegacyTreeTalentsKey = "tree_talents_state";

        private readonly ISaveSlotManager _slotManager;
        private readonly IPlayerProgressRepository _fileProgressRepository;
        private readonly IInventoryRepository _fileInventoryRepository;
        private readonly ITreeTalentsRepository _fileTreeRepository;
        private readonly IConfigProvider _configProvider;
        private readonly IItemAffixModifierResolver _affixResolver;

        public LegacyPlayerPrefsMigrationService(
            ISaveSlotManager slotManager,
            IPlayerProgressRepository fileProgressRepository,
            IInventoryRepository fileInventoryRepository,
            ITreeTalentsRepository fileTreeRepository,
            IConfigProvider configProvider,
            IItemAffixModifierResolver affixResolver)
        {
            _slotManager = slotManager;
            _fileProgressRepository = fileProgressRepository;
            _fileInventoryRepository = fileInventoryRepository;
            _fileTreeRepository = fileTreeRepository;
            _configProvider = configProvider;
            _affixResolver = affixResolver;
        }

        public void TryMigrate()
        {
            if (System.IO.Directory.Exists(FileSavePaths.SaveRoot))
                return;

            bool hasLegacy =
                PlayerPrefs.HasKey(LegacyProgressKey) ||
                PlayerPrefs.HasKey(LegacyInventoryKey) ||
                PlayerPrefs.HasKey(LegacyTreeTalentsKey);

            if (!hasLegacy)
                return;

            var legacyProgressRepo = new PlayerPrefsProgressRepository();
            var legacyInventoryRepo = new PlayerPrefsInventoryRepository(_configProvider, _affixResolver);
            var legacyTreeRepo = new PlayerPrefsTreeTalentsRepository();

            _slotManager.SetActiveSlot(0);
            var progress = legacyProgressRepo.Load();
            var inventory = legacyInventoryRepo.Load();
            var tree = legacyTreeRepo.Load();

            _fileProgressRepository.Save(progress);
            _fileInventoryRepository.Save(inventory);
            _fileTreeRepository.Save(tree);

            _slotManager.UpdateMetadata(new SaveSlotMetadata(
                0,
                false,
                progress.HeroId ?? "default_hero",
                HeroItemClass.Warrior,
                tree != null && tree.Level > 0 ? tree.Level : 1,
                progress.CurrentTier,
                progress.CurrentMap,
                System.DateTime.UtcNow.Ticks));

            PlayerPrefs.DeleteKey(LegacyProgressKey);
            PlayerPrefs.DeleteKey(LegacyInventoryKey);
            PlayerPrefs.DeleteKey(LegacyTreeTalentsKey);
            PlayerPrefs.Save();
        }
    }
}
