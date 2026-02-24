using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Idle Exile/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabaseSO : ScriptableObject
    {
        public List<ItemDefinitionSO> items = new();
    }
}
