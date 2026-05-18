using CardTower.RuntimeEffects;

namespace CardTower.Cards
{
    public sealed class TowerShieldCard : CardBase
    {
        public override CardConfig Config => new CardConfig
        {
            Id = "tower_shield",
            DisplayName = "护盾",
            Description = "为塔生成200护盾，持续15秒。",
            ManaCost = 1,
            Price = 8
        };

        public override void Play(RuntimeEffectContext context)
        {
            var effect = new TowerShieldEffect("card_tower_shield", 15f, 200f);
            RuntimeEffectManager.Instance.Effects.Add(effect, context);
        }
    }
}
