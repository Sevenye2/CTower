using CardTower.RuntimeEffects;

namespace CardTower.Cards
{
    public sealed class GoldRushCard : CardBase
    {
        public override CardConfig Config => new CardConfig
        {
            Id = "gold_rush",
            DisplayName = "淘金热",
            Description = "15秒内金币获取量+50%。",
            ManaCost = 1,
            Price = 8
        };

        public override void Play(RuntimeEffectContext context)
        {
            var effect = new GoldMultiplierEffect("card_gold_rush", 15f, 1.5f);
            RuntimeEffectManager.Instance.Effects.Add(effect, context);
        }
    }
}
