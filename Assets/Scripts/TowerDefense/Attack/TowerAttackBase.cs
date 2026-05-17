using Unity.Entities;

namespace CardTower.TowerDefense
{
    public abstract class TowerAttackBase
    {
        public abstract TowerAttackConfig Config { get; }

        public abstract void Apply(EntityCommandBuffer ecb, EntityManager em, Entity entity, int level);
    }

    public sealed class TowerAttackConfig
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int MaxLevel;
        public float BaseAttackRange;
        public float AttackRangePerLevel;
        public float BaseDamagePerShot;
        public float DamagePerLevel;
        public float BaseShotsPerSecond;
        public float ShotsPerSecondPerLevel;
        public int Price;
    }
}
