namespace CardTower.RuntimeEffects
{
    public sealed class DamageMultiplierEffect : RuntimeEffect
    {
        readonly float _multiplier;

        public DamageMultiplierEffect(string sourceId, float durationSeconds, float multiplier)
            : base(sourceId, durationSeconds)
        {
            _multiplier = multiplier;
        }

        public override void CollectTowerModifiers(ref TowerStatModifiers modifiers)
        {
            modifiers.DamageMultiplier *= _multiplier;
        }
    }
}
