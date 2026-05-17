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
    }

    public class EnemyAuthoringBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemyTag());
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
                HitIntervalSeconds = authoring.HitIntervalSeconds
            });
            AddComponent(entity, new EnemyAttackState { NextHitTime = 0f });
        }
    }
 
}
  