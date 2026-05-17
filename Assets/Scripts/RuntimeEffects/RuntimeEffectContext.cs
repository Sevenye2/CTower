using Unity.Entities;

namespace CardTower.RuntimeEffects
{
    public sealed class RuntimeEffectContext
    {
        public EntityManager EntityManager { get; }
        public Entity TowerEntity { get; }

        public RuntimeEffectContext(EntityManager entityManager, Entity towerEntity = default)
        {
            EntityManager = entityManager;
            TowerEntity = towerEntity;
        }
    }
}
