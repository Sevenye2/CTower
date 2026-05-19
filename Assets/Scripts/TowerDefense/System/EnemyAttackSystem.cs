using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMoveSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct EnemyAttackSystem : ISystem
    {
        Entity _bulletPrefab;
        bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabComponentData>();
        }

        private static void MyTick(float dt)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_initialized)
            {
                _bulletPrefab = SystemAPI.GetSingleton<PrefabComponentData>().EnemyBulletPrefab;
                _initialized = true;
            }

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new EnemyAttackJob
            {
                Time = (float)SystemAPI.Time.ElapsedTime,
                HealthLookup = SystemAPI.GetComponentLookup<Health>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                BulletPrefab = _bulletPrefab,
                ECB = ecb.AsParallelWriter()
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    [WithAll(typeof(EntityType))]
    [WithNone(typeof(Stunned))]
    partial struct EnemyAttackJob : IJobEntity
    {
        [ReadOnly] public float Time;
        [ReadOnly] public ComponentLookup<Health> HealthLookup;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;

        [ReadOnly] public Entity BulletPrefab;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int sortKey, ref LocalTransform lt,
            ref EnemyAttackState atkState, in EnemyAttackConfig atkCfg,
            in EntityModifiers modifiers, in CurrentTarget target,
            in EntityType selfType)
        {
            if (selfType.Value != EntityKind.Enemy)
                return;
            if (target.Value == Entity.Null || target.Distance > atkCfg.AttackRange)
            {
                UpdateScale(ref lt, Time, atkState.LastHitTime);
                return;
            }

            if (Time >= atkState.NextHitTime)
            {
                var hp = HealthLookup[target.Value];
                if (hp.Current > 0f)
                {
                    atkState.NextHitTime = Time + atkCfg.HitIntervalSeconds;
                    atkState.LastHitTime = Time;

                    var damage = atkCfg.DamagePerHit * modifiers.DamageDealt;

                    if (atkCfg.IsRanged && BulletPrefab != Entity.Null)
                    {
                        SpawnBullet(sortKey, lt.Position, target.Value,
                            TransformLookup[target.Value].Position, damage, atkCfg, Time,
                            BulletPrefab, ECB);
                    }
                    else
                    {
                        ApplyMeleeDamage(sortKey, target.Value, damage, hp, ECB);
                    }
                }
            }

            UpdateScale(ref lt, Time, atkState.LastHitTime);
        }

        static void SpawnBullet(int sortKey, float3 enemyPos, Entity targetEntity,
            float3 targetPos, float damage, in EnemyAttackConfig cfg, float time,
            Entity bulletPrefab, EntityCommandBuffer.ParallelWriter ecb)
        {
            var bullet = ecb.Instantiate(sortKey, bulletPrefab);
            ecb.RemoveComponent<Bullet>(sortKey, bullet);
            ecb.SetComponent(sortKey, bullet, new EntityType { Value = EntityKind.EnemyBullet });
            ecb.AddComponent<BattleEntity>(sortKey, bullet);
            ecb.SetComponent(sortKey, bullet, LocalTransform.FromPosition(enemyPos));
            ecb.AddComponent<EnemyBullet>(sortKey, bullet, new EnemyBullet
            {
                StartPos = enemyPos,
                TargetPos = targetPos,
                StartTime = time,
                Duration = cfg.BulletDuration,
                MaxHeight = cfg.BulletMaxHeight,
                Damage = damage,
                Target = targetEntity
            });
        }

        static void ApplyMeleeDamage(int sortKey, Entity targetEntity, float damage,
            Health hp, EntityCommandBuffer.ParallelWriter ecb)
        {
            if (hp.Shield > 0f)
            {
                if (hp.Shield >= damage)
                {
                    hp.Shield -= damage;
                    ecb.SetComponent(sortKey, targetEntity, hp);
                    return;
                }

                damage -= hp.Shield;
                hp.Shield = 0f;
            }

            hp.Current -= damage;
            ecb.SetComponent(sortKey, targetEntity, hp);
        }

        static void UpdateScale(ref LocalTransform lt, float time, float lastHitTime)
        {
            const float duration = 0.3f;
            const float punch = 0.3f;
            var elapsed = time - lastHitTime;
            if (elapsed < duration)
            {
                var half = duration * 0.5f;
                lt.Scale = elapsed < half
                    ? 1f + punch * (elapsed / half)
                    : 1f + punch * (1f - (elapsed - half) / half);
            }
            else
            {
                lt.Scale = 1f;
            }
        }
    }
}