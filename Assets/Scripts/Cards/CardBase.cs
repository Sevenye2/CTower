using Unity.Entities;

namespace CardTower.Cards
{
    public abstract class CardBase
    {
        public abstract CardConfig Config { get; }

        public abstract void Play(EntityManager em, Entity towerEntity);
    }

    public sealed class CardConfig
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int ManaCost;
        public int Price;
        public bool RequiresTargeting;
    }
}
