using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Idle Exile/Locations/Location Database", fileName = "LocationDatabase")]
    public sealed class LocationDatabaseSO : ScriptableObject
    {
        public List<LocationEntry> locations = new();

        public AssetReferenceGameObject GetPrefabReference(string locationId)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                if (locations[i].locationId == locationId)
                    return locations[i].prefab;
            }
            return null;
        }
    }

    [Serializable]
    public struct LocationEntry
    {
        public string locationId;
        public AssetReferenceGameObject prefab;
    }
}
