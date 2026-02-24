using System;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Domain.Characters;
using UnityEngine;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class GameInitializer : IInitializable, IDisposable, IGameStateProvider
    {
        private readonly IPlayerProgressRepository _progressRepo;
        private readonly IInventoryRepository _inventoryRepo;

        public HeroState Hero { get; private set; }
        public InventoryModel Inventory { get; private set; }
        public PlayerProgressData Progress { get; private set; }

        public GameInitializer(
            IPlayerProgressRepository progressRepo,
            IInventoryRepository inventoryRepo)
        {
            _progressRepo = progressRepo;
            _inventoryRepo = inventoryRepo;
        }

        public void Initialize()
        {
            Debug.Log("[GameInitializer] Starting game...");

            Progress = _progressRepo.Load();
            Hero = new HeroState(Progress.HeroId ?? "default_hero");
            Inventory = _inventoryRepo.Load();

            _inventoryRepo.Save(Inventory);

            var cam = Camera.main;
            if (cam != null)
                cam.rect = new Rect(0f, 0f, 1f / 3f, 1f);

            Debug.Log($"[GameInitializer] Hero '{Hero.Id}' ready. Wave: {Progress.CurrentWave}. Inventory: {Inventory.Items.Count}/{Inventory.Capacity}");
        }

        public void Dispose()
        {
            if (Progress != null)
                _progressRepo.Save(Progress);
            if (Inventory != null)
                _inventoryRepo.Save(Inventory);

            Debug.Log("[GameInitializer] Progress saved on dispose.");
        }
    }
}
