using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Presentation.UI.Services
{
    public sealed class AddressableIconProvider : IIconProvider
    {
        private readonly Dictionary<string, Sprite> _cache = new();

        public async UniTask<Sprite> LoadIconAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            if (_cache.TryGetValue(address, out var cached))
                return cached;

            try
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(address);
                var sprite = await handle.Task;
                _cache[address] = sprite;
                return sprite;
            }
            catch
            {
                Debug.LogWarning($"[AddressableIconProvider] Failed to load icon: {address}");
                return null;
            }
        }

        public void ReleaseIcon(string address)
        {
            if (string.IsNullOrEmpty(address) || !_cache.TryGetValue(address, out var sprite))
                return;

            _cache.Remove(address);
            Addressables.Release(sprite);
        }
    }
}
