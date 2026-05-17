using CardTower.RuntimeEffects;

namespace CardTower.Relics
{
    public sealed class UnstableEngineRelic : RelicBase
    {
        public const string RelicId = "unstable_engine";

        public override RelicRuntimeConfig Config => new RelicRuntimeConfig
        {
            Id = RelicId,
            DisplayName = "不稳定引擎",
            Description = "每过2秒，攻击速度+2%，攻击力-1%。",
            IconPath = "Art/Relics/UnstableEngine",
            Rarity = "Rare",
            Price = 120
        };

        public override void CreateRuntimeEffects(RuntimeEffectContext context)
        {
            RuntimeEffectManager.Instance.Effects.Add(new UnstableEngineBattleEffect(), context);
        }
    }
}
