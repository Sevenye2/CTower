using Unity.Entities;

namespace CardTower.RuntimeEffects
{
    public sealed class RuntimeEffectManager
    {
        static RuntimeEffectManager _instance;

        public static RuntimeEffectManager Instance => _instance ??= new RuntimeEffectManager();

        public RuntimeEffectContainer Effects { get; } = new RuntimeEffectContainer();

        public RuntimeEffectContext CreateContext(EntityManager entityManager, Entity towerEntity = default)
        {
            return new RuntimeEffectContext(entityManager, towerEntity);
        }
    }
}
