using Unity.Entities;

namespace CardTower.Relics
{
    public abstract class RelicBase
    {
        public abstract RelicRuntimeConfig Config { get; }

        public virtual void OnOwned(EntityManager em, Entity towerEntity)
        {
        }

        public virtual void Activate(EntityManager em, Entity towerEntity)
        {
        }

        public virtual void Deactivate()
        {
        }
    }

    public sealed class RelicRuntimeConfig
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public string Rarity;
        public int Price;
    }
}
