namespace Game.Application.Ports
{
    public interface ICombatDisplaySettings
    {
        bool ShowHpBars { get; set; }
        bool ShowEffectIndicators { get; set; }
        bool ShowDamageNumbers { get; set; }
    }
}
