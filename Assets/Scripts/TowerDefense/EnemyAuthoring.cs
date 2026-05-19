using Unity.Entities;
using UnityEngine;

namespace CardTower.TowerDefense
{
    [DisallowMultipleComponent]
    public class EnemyAuthoring : MonoBehaviour
    {
        [Min(1f)] public float MaxHealth = 40f;
        [Min(0.1f)] public float MoveSpeed = 5f;
        [Min(0.1f)] public float AttackRange = 2.5f;
        [Min(0f)] public float DamagePerHit = 6f;
        [Min(0.05f)] public float HitIntervalSeconds = 1f;

        [Header("Ranged")]
        public bool IsRanged;
        [Min(0.1f)] public float BulletDuration = 0.5f;
        [Min(0f)] public float BulletMaxHeight = 4f;
    }

    public class EnemyAuthoringBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntityType { Value = EntityKind.Enemy });
            AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new Health
            {
                Max = authoring.MaxHealth,
                Current = authoring.MaxHealth
            });
            AddComponent(entity, new EnemyAttackConfig
            {
                AttackRange = authoring.AttackRange,
                DamagePerHit = authoring.DamagePerHit,
                HitIntervalSeconds = authoring.HitIntervalSeconds,
                IsRanged = authoring.IsRanged,
                BulletDuration = authoring.BulletDuration,
                BulletMaxHeight = authoring.BulletMaxHeight
            });
            AddComponent(entity, new EnemyAttackState { NextHitTime = 0f });
            AddComponent(entity, new CurrentTarget { Value = Entity.Null, Distance = float.MaxValue });
            AddComponent(entity, EntityModifiers.Identity);
            AddBuffer<BuffInstance>(entity);
        }
    }
}
