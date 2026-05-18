namespace CardTower.RuntimeEffects
{
    public sealed class GoldMultiplierEffect : RuntimeEffect
    {
        readonly float _multiplier;

        public GoldMultiplierEffect(string sourceId, float durationSeconds, float multiplier)
            : base(sourceId, durationSeconds)
        {
            _multiplier = multiplier;
        }

        public override void CollectTowerModifiers(ref TowerStatModifiers modifiers)
        {
            modifiers.GoldMultiplier *= _multiplier;
        }
    }
}
