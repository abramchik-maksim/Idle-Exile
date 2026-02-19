using System.Collections.Generic;
using Game.Domain.Items;

namespace Game.Application.Ports
{
    public interface IConfigProvider
    {
        ItemDefinition GetItemDefinition(string id);
        IReadOnlyList<ItemDefinition> GetAllItems();
        float GetEnemyHealthBase(int waveIndex);
        float GetEnemyDamageBase(int waveIndex);
    }
}
