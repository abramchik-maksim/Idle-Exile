using UnityEngine;

namespace Game.Presentation.Combat.Rendering
{
    public interface ICombatVisualProvider
    {
        GameObject GetPrefab(int visualId);
        float GetScale(int visualId);
    }
}
