using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    struct BulletHitResult
    {
        public Entity BulletEntity;
        public Entity EnemyEntity;
        public float Damage;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMoveSystem))]
    public partial struct BulletCollisionDestroySystem : ISystem
    {
        const float HitRadius = 1.5f;
        const float GroundY = 0f;

        EntityQuery _bulletQuery;
        EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            _bulletQuery = SystemAPI.QueryBuilder()
                .WithAll<BulletTag, LocalTransform, Bullet>()
                .Build();
            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyTag, LocalTransform, Health>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var bulletCount = _bulletQuery.CalculateEntityCount();
            var enemyCount = _enemyQuery.CalculateEntityCount();
            if (bulletCount == 0 || enemyCount == 0)
                return;

            var bulletPositions = _bulletQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var bulletEntities = _bulletQuery.ToEntityArray(Allocator.TempJob);
            var bulletDatas = _bulletQuery.ToComponentDataArray<Bullet>(Allocator.TempJob);
            var enemyPositions = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyHealths = _enemyQuery.ToComponentDataArray<Health>(Allocator.TempJob);

            var hits = new NativeList<BulletHitResult>(bulletCount, Allocator.TempJob);

            var job = new DetectBulletHitsJob
            {
                BulletPositions = bulletPositions,
                BulletEntities = bulletEntities,
                BulletDatas = bulletDatas,
                EnemyPositions = enemyPositions,
                EnemyEntities = enemyEntities,
                EnemyHealths = enemyHealths,
                HitRadius = HitRadius,
                GroundY = GroundY,
                Hits = hits.AsParallelWriter()
            };

            state.Dependency = job.Schedule(bulletCount, 32, state.Dependency);

            // Complete job before main-thread work
            state.Dependency.Complete();

            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var damagePerEnemy = new NativeHashMap<Entity, float>(enemyCount, Allocator.Temp);

            foreach (var hit in hits)
            {
                if (hit.EnemyEntity == Entity.Null)
                {
                    ecb.DestroyEntity(hit.BulletEntity);
                    continue;
                }

                ecb.DestroyEntity(hit.BulletEntity);

                var prev = damagePerEnemy.TryGetValue(hit.EnemyEntity, out var d) ? d : 0f;
                damagePerEnemy[hit.EnemyEntity] = prev + hit.Damage;
            }

            foreach (var kv in damagePerEnemy)
            {
                var enemy = kv.Key;
                if (!em.HasComponent<Health>(enemy))
                    continue;

                var hp = em.GetComponentData<Health>(enemy);
                hp.Current -= kv.Value;
                ecb.SetComponent(enemy, hp);
            }

            ecb.Playback(em);
            ecb.Dispose();

            hits.Dispose();
            damagePerEnemy.Dispose();
            bulletPositions.Dispose();
            bulletEntities.Dispose();
            bulletDatas.Dispose();
            enemyPositions.Dispose();
            enemyEntities.Dispose();
            enemyHealths.Dispose();
        }
    }

    [BurstCompile]
    struct DetectBulletHitsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<LocalTransform> BulletPositions;
        [ReadOnly] public NativeArray<Entity> BulletEntities;
        [ReadOnly] public NativeArray<Bullet> BulletDatas;
        [ReadOnly] public NativeArray<LocalTransform> EnemyPositions;
        [ReadOnly] public NativeArray<Entity> EnemyEntities;
        [ReadOnly] public NativeArray<Health> EnemyHealths;
        public float HitRadius;
        public float GroundY;
        public NativeList<BulletHitResult>.ParallelWriter Hits;

        public void Execute(int i)
        {
            var bulletPos = BulletPositions[i].Position;

            if (bulletPos.y <= GroundY)
            {
                Hits.AddNoResize(new BulletHitResult
                {
                    BulletEntity = BulletEntities[i],
                    EnemyEntity = Entity.Null,
                    Damage = 0f
                });
                return;
            }

            for (var j = 0; j < EnemyPositions.Length; j++)
            {
                if (EnemyHealths[j].Current <= 0f)
                    continue;

                var dist = math.distance(bulletPos, EnemyPositions[j].Position);
                if (dist <= HitRadius)
                {
                    Hits.AddNoResize(new BulletHitResult
                    {
                        BulletEntity = BulletEntities[i],
                        EnemyEntity = EnemyEntities[j],
                        Damage = BulletDatas[i].Damage
                    });
                    return;
                }
            }
        }
    }
}
