using CardTower.RuntimeEffects;

namespace CardTower.Relics
{
    public sealed class MeteorMarkRelic : RelicBase
    {
        public override RelicRuntimeConfig Config => new RelicRuntimeConfig
        {
            Id = "meteor_mark",
            DisplayName = "天罚",
            Description = "每击败5个敌人，随机选择一名敌人落下陨石造成范围伤害。",
            IconPath = "Art/Relics/MeteorMark",
            Rarity = "Epic",
            Price = 150
        };

        public override void CreateRuntimeEffects(RuntimeEffectContext context)
        {
            RuntimeEffectManager.Instance.Effects.Add(new MeteorMarkEffect(), context);
        }
    }
}
