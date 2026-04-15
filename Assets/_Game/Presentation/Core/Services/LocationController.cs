using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Domain.DTOs.Progression;
using Game.Infrastructure.Configs;

namespace Game.Presentation.Core.Services
{
    public sealed class LocationController : IStartable, IDisposable
    {
        private readonly LocationDatabaseSO _locationDb;
        private readonly ISubscriber<MapChangedDTO> _mapChangedSub;
        private readonly IGameStateProvider _gameState;
        private readonly ICombatConfigProvider _combatConfig;
        private readonly List<IDisposable> _subscriptions = new();

        private AsyncOperationHandle<GameObject> _currentHandle;
        private GameObject _currentInstance;

        public LocationController(
            LocationDatabaseSO locationDb,
            ISubscriber<MapChangedDTO> mapChangedSub,
            IGameStateProvider gameState,
            ICombatConfigProvider combatConfig)
        {
            _locationDb = locationDb;
            _mapChangedSub = mapChangedSub;
            _gameState = gameState;
            _combatConfig = combatConfig;
        }

        public void Start()
        {
            _subscriptions.Add(_mapChangedSub.Subscribe(OnMapChanged));
            LoadInitialLocation().Forget();
        }

        private async UniTaskVoid LoadInitialLocation()
        {
            await UniTask.WaitUntil(() => _gameState.Progress != null);

            var progress = _gameState.Progress;
            var map = _combatConfig.GetMap(progress.CurrentTier, progress.CurrentMap);
            if (map != null && !string.IsNullOrEmpty(map.LocationId))
                await LoadLocation(map.LocationId);
        }

        private void OnMapChanged(MapChangedDTO dto)
        {
            if (!string.IsNullOrEmpty(dto.LocationId))
                LoadLocation(dto.LocationId).Forget();
        }

        private async UniTask LoadLocation(string locationId)
        {
            var prefabRef = _locationDb?.GetPrefabReference(locationId);
            if (prefabRef == null || !prefabRef.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"[LocationController] No valid prefab for locationId '{locationId}'.");
                return;
            }

            if (_currentInstance != null)
            {
                UnityEngine.Object.Destroy(_currentInstance);
                _currentInstance = null;
            }

            if (_currentHandle.IsValid())
            {
                Addressables.Release(_currentHandle);
            }

            _currentHandle = Addressables.LoadAssetAsync<GameObject>(prefabRef);
            await _currentHandle.ToUniTask();
            var prefab = _currentHandle.Result;

            _currentInstance = UnityEngine.Object.Instantiate(prefab);
            Debug.Log($"[LocationController] Loaded location '{locationId}'.");
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            if (_currentInstance != null)
                UnityEngine.Object.Destroy(_currentInstance);

            if (_currentHandle.IsValid())
                Addressables.Release(_currentHandle);
        }
    }
}
