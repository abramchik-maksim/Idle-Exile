using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Domain.Characters;
using UnityEngine;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class GameInitializer : IAsyncStartable, IDisposable
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

        public async UniTask StartAsync(System.Threading.CancellationToken cancellation)
        {
            Debug.Log("[GameInitializer] Starting game...");

            Progress = _progressRepo.Load();
            Hero = new HeroState(Progress.HeroId ?? "default_hero");
            Inventory = _inventoryRepo.Load();

            _inventoryRepo.Save(Inventory);

            Debug.Log($"[GameInitializer] Hero '{Hero.Id}' ready. Wave: {Progress.CurrentWave}. Inventory: {Inventory.Items.Count}/{Inventory.Capacity}");

            await UniTask.CompletedTask;
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
