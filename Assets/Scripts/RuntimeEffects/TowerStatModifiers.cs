namespace CardTower.RuntimeEffects
{
    public struct TowerStatModifiers
    {
        public float DamageMultiplier;
        public float AttackSpeedMultiplier;
        public float AttackRangeMultiplier;
        public float GoldMultiplier;

        public static TowerStatModifiers Identity => new TowerStatModifiers
        {
            DamageMultiplier = 1f,
            AttackSpeedMultiplier = 1f,
            AttackRangeMultiplier = 1f,
            GoldMultiplier = 1f
        };
    }
}
