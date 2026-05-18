namespace CardTower.RuntimeEffects
{
    public sealed class AttackSpeedEffect : RuntimeEffect
    {
        readonly float _multiplier;

        public AttackSpeedEffect(string sourceId, float durationSeconds, float multiplier)
            : base(sourceId, durationSeconds)
        {
            _multiplier = multiplier;
        }

        public override void CollectTowerModifiers(ref TowerStatModifiers modifiers)
        {
            modifiers.AttackSpeedMultiplier *= _multiplier;
        }
    }
}
