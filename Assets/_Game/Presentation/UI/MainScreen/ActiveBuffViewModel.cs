using Game.Domain.Skills;

namespace Game.Presentation.UI.MainScreen
{
    public readonly struct ActiveBuffViewModel
    {
        public string SkillName { get; }
        public SkillEffectType EffectType { get; }
        public float EffectValue { get; }
        public float RemainingTime { get; }
        public float TotalDuration { get; }

        public ActiveBuffViewModel(
            string skillName,
            SkillEffectType effectType,
            float effectValue,
            float remainingTime,
            float totalDuration)
        {
            SkillName = skillName;
            EffectType = effectType;
            EffectValue = effectValue;
            RemainingTime = remainingTime;
            TotalDuration = totalDuration;
        }
    }
}
