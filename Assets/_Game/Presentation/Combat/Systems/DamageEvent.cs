namespace Game.Presentation.Combat.Systems
{
    public struct DamageEvent
    {
        public float Amount;
        public float WorldX;
        public float WorldY;
        public bool IsCritical;
        public int TargetActorId;
        public bool IsFromHero;
        public byte DamageCategory; // 0=Physical, 1=Fire/Ignite, 2=Bleed
    }
}
