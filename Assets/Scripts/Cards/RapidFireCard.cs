using CardTower.RuntimeEffects;

namespace CardTower.Cards
{
    public sealed class RapidFireCard : CardBase
    {
        public override CardConfig Config => new CardConfig
        {
            Id = "rapid_fire",
            DisplayName = "急速射击",
            Description = "15秒内塔攻速+25%。",
            ManaCost = 2,
            Price = 10
        };

        public override void Play(RuntimeEffectContext context)
        {
            var effect = new AttackSpeedEffect("card_rapid_fire", 15f, 1.25f);
            RuntimeEffectManager.Instance.Effects.Add(effect, context);
        }
    }
}
