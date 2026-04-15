using Game.Domain.Combat.Progression;

namespace Game.Application.Ports
{
    public interface ICombatConfigProvider
    {
        TierDefinition GetTier(int tierIndex);
        MapDefinition GetMap(int tierIndex, int mapIndex);
        BattleDefinition GetBattle(int tierIndex, int mapIndex, int battleIndex);
        EnemyDefinition GetEnemy(string enemyId);
        float GetTierScaling(int tierIndex);
        int GetTierCount();
        int GetMapCount(int tierIndex);
        int GetBattleCount(int tierIndex, int mapIndex);

        float GetDropChance(int battleIndex, int tierIndex);
        float GetBonusDropChance(int tierIndex);

        /// <summary>
        /// Returns the two map options for a given choice point within a tier.
        /// choiceIndex: 0-based index of the choice (e.g. 0 = first choice after forced/previous map).
        /// </summary>
        (MapDefinition Option1, MapDefinition Option2) GetMapChoice(int tierIndex, int choiceIndex);
    }
}
