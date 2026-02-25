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
    }
}
