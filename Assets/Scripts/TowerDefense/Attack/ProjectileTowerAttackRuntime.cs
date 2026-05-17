using Unity.Entities;

namespace CardTower.TowerDefense
{
    public sealed class ProjectileTowerAttackRuntime : TowerAttackBase
    {
        public override TowerAttackConfig Config => new TowerAttackConfig
        {
            Id = "projectile",
            DisplayName = "子弹",
            Description = "基础远程攻击方式，周期性对范围内最近敌人造成伤害。",
            MaxLevel = 10,
            BaseAttackRange = 22f,
            AttackRangePerLevel = 1.5f,
            BaseDamagePerShot = 12f,
            DamagePerLevel = 3f,
            BaseShotsPerSecond = 2f,
            ShotsPerSecondPerLevel = 0.15f,
            Price = 10
        };

        public override void Apply(EntityCommandBuffer ecb, EntityManager entityManager, Entity entity, int level)
        {
            ecb.AddComponent(entity, CreateProjectileAttack(level));
        }

        ProjectileTowerAttack CreateProjectileAttack(int level)
        {
            level = UnityEngine.Mathf.Max(1, level);
            var config = Config;
            return new ProjectileTowerAttack
            {
                AttackRange = config.BaseAttackRange + (level - 1) * config.AttackRangePerLevel,
                DamagePerShot = config.BaseDamagePerShot + (level - 1) * config.DamagePerLevel,
                ShotsPerSecond = config.BaseShotsPerSecond + (level - 1) * config.ShotsPerSecondPerLevel,
                NextShotTime = 0f
            };
        }
    }
}
