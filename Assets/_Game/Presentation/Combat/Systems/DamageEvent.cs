namespace Game.Presentation.Combat.Systems
{
    public struct DamageEvent
    {
        public float Amount;
        public float WorldX;
        public float WorldY;
        public bool IsCritical;
        public int TargetActorId;
    }
}
