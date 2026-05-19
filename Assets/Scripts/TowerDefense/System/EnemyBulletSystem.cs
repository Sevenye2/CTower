using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAttackSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct EnemyBulletSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var healthLookup = SystemAPI.GetComponentLookup<Health>(false);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new EnemyBulletJob
            {
                Time = time,
                HealthLookup = healthLookup,
                ECB = ecb.AsParallelWriter()
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    [WithAll(typeof(EntityType), typeof(EnemyBullet))]
    partial struct EnemyBulletJob : IJobEntity
    {
        [ReadOnly] public float Time;
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<Health> HealthLookup;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int sortKey, ref LocalTransform lt,
                     in EnemyBullet bullet, Entity entity)
        {
            var progress = math.saturate((Time - bullet.StartTime) / math.max(0.001f, bullet.Duration));

            if (progress >= 1f)
            {
                if (bullet.Target != Entity.Null &&
                    HealthLookup.TryGetComponent(bullet.Target, out var hp) &&
                    hp.Current > 0f)
                {
                    var dmg = bullet.Damage;
                    if (hp.Shield > 0f)
                    {
                        if (hp.Shield >= dmg)
                        {
                            hp.Shield -= dmg;
                            ECB.SetComponent(sortKey, bullet.Target, hp);
                            ECB.DestroyEntity(sortKey, entity);
                            return;
                        }
                        dmg -= hp.Shield;
                        hp.Shield = 0f;
                    }
                    hp.Current -= dmg;
                    ECB.SetComponent(sortKey, bullet.Target, hp);
                }
                ECB.DestroyEntity(sortKey, entity);
                return;
            }

            var horiz = math.lerp(bullet.StartPos, bullet.TargetPos, progress);
            var height = bullet.MaxHeight * (1f - math.pow(progress * 2f - 1f, 2f));
            lt.Position = new float3(horiz.x,
                math.max(bullet.StartPos.y, bullet.TargetPos.y) + height, horiz.z);
        }
    }
}
