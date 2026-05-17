using CardTower.RuntimeEffects;

namespace CardTower.Cards
{
    public sealed class DoubleDamageCard : CardBase
    {
        public override CardConfig Config => new CardConfig
        {
            Id = "double_damage",
            DisplayName = "力量爆发",
            Description = "5秒内攻击力翻倍。",
            ManaCost = 1,
            Price = 8
        };

        public override void Play(RuntimeEffectContext context)
        {
            var effect = new DamageMultiplierEffect("card_double_damage", 5f, 2f);
            RuntimeEffectManager.Instance.Effects.Add(effect, context);
        }
    }
}
