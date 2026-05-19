using Unity.Entities;

namespace CardTower.Relics
{
    public sealed class GoldHarvestRelic : RelicBase
    {
        const int GoldPerRound = 100;

        public override RelicRuntimeConfig Config => new RelicRuntimeConfig
        {
            Id = "gold_harvest",
            DisplayName = "满载而归",
            Description = "每回合结束时获得100金币。",
            IconPath = "Art/Relics/GoldHarvest",
            Rarity = "Common",
            Price = 60
        };

        public override void Activate(EntityManager em, Entity towerEntity)
        {
        }

        public override void Deactivate()
        {
            SaveDataManager.instance.AddGold(GoldPerRound);
        }
    }
}
