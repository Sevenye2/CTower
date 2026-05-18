namespace CardTower.RuntimeEffects
{
    public sealed class GoldHarvestEffect : RuntimeEffect
    {
        const int GoldPerRound = 100;

        public GoldHarvestEffect()
            : base("gold_harvest")
        {
        }

        public override void OnBattleEnd(RuntimeEffectContext context)
        {
            SaveDataManager.instance.AddGold(GoldPerRound);
        }
    }
}
