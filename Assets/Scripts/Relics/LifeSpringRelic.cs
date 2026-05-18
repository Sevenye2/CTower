using CardTower.RuntimeEffects;

namespace CardTower.Relics
{
    public sealed class LifeSpringRelic : RelicBase
    {
        public override RelicRuntimeConfig Config => new RelicRuntimeConfig
        {
            Id = "life_spring",
            DisplayName = "生命之泉",
            Description = "每10秒恢复塔10%最大生命值。",
            IconPath = "Art/Relics/LifeSpring",
            Rarity = "Rare",
            Price = 100
        };

        public override void CreateRuntimeEffects(RuntimeEffectContext context)
        {
            RuntimeEffectManager.Instance.Effects.Add(new LifeSpringEffect(), context);
        }
    }
}
