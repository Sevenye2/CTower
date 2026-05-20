using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    struct BulletHit
    {
        public Entity Bullet;
        public Entity Target;
        public float Damage;
        public Entity Source;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMoveSystem))]
    public partial struct BulletHitSystem : ISystem
    {
        const float HitRadius = 1.5f;

        EntityQuery _bulletQuery;
        EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            _bulletQuery = SystemAPI.QueryBuilder()
                .WithAll<EntityType, LocalTransform, Bullet>()
                .Build();
            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EntityType, LocalTransform, Health>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var bulletCount = _bulletQuery.CalculateEntityCount();
            var enemyCount = _enemyQuery.CalculateEntityCount();
            if (bulletCount == 0 || enemyCount == 0)
                return;

            var hits = CollectHits(bulletCount);
            if (hits.Length == 0)
                return;

            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var killedThisFrame = new NativeHashSet<Entity>(enemyCount, Allocator.Temp);
            var allEnemies = _enemyQuery.ToEntityArray(Allocator.Temp);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var buffLookup = SystemAPI.GetBufferLookup<BuffInstance>(false);

            foreach (var hit in hits)
            {
                if (hit.Target == Entity.Null)
                {
                    ecb.DestroyEntity(hit.Bullet);
                    continue;
                }

                ecb.DestroyEntity(hit.Bullet);

                if (killedThisFrame.Contains(hit.Target))
                    continue;

                var hp = em.GetComponentData<Health>(hit.Target);
                if (hp.Current <= 0f)
                    continue;

                var rawDamage = hit.Damage
                    * em.GetComponentData<EntityModifiers>(hit.Target).DamageTaken;

                var targetPos = em.GetComponentData<LocalTransform>(hit.Target).Position;
                var sourcePos = GetPosition(em, hit.Source);

                var dmgCtx = new DamageContext
                {
                    Source = hit.Source,
                    Target = hit.Target,
                    Amount = rawDamage,
                    SourcePosition = sourcePos,
                    TargetPosition = targetPos,
                    ECB = ecb
                };

                DispatchDamageEvents(em, ref dmgCtx);

                if (dmgCtx.Amount <= 0f)
                    continue; // fully absorbed

                hp.Current -= dmgCtx.Amount;
                ecb.SetComponent(hit.Target, hp);

                if (hp.Current > 0f)
                    continue;

                killedThisFrame.Add(hit.Target);
                DispatchDeathEvents(em, ref ecb, hit.Target, targetPos, allEnemies, transformLookup, buffLookup);
                DispatchKillEvent(em, ref ecb, hit.Source, hit.Target, targetPos);
            }

            ecb.Playback(em);
            ecb.Dispose();
            killedThisFrame.Dispose();
            allEnemies.Dispose();
        }

        NativeList<BulletHit> CollectHits(int bulletCount)
        {
            var bulletPositions = _bulletQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var bulletEntities = _bulletQuery.ToEntityArray(Allocator.TempJob);
            var bulletDatas = _bulletQuery.ToComponentDataArray<Bullet>(Allocator.TempJob);
            var enemyPositions = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyHealths = _enemyQuery.ToComponentDataArray<Health>(Allocator.TempJob);
            var enemyTypes = _enemyQuery.ToComponentDataArray<EntityType>(Allocator.TempJob);

            var hits = new NativeList<BulletHit>(bulletCount, Allocator.TempJob);

            var job = new DetectHitsJob
            {
                BulletPositions = bulletPositions,
                BulletEntities = bulletEntities,
                BulletDatas = bulletDatas,
                EnemyPositions = enemyPositions,
                EnemyEntities = enemyEntities,
                EnemyHealths = enemyHealths,
                EnemyTypes = enemyTypes,
                HitRadius = HitRadius,
                GroundY = GameConst.GroundY,
                Hits = hits.AsParallelWriter()
            };

            job.Schedule(bulletCount, 32).Complete();

            var result = new NativeList<BulletHit>(hits.Length, Allocator.Temp);
            result.CopyFrom(hits);

            hits.Dispose();
            bulletPositions.Dispose();
            bulletEntities.Dispose();
            bulletDatas.Dispose();
            enemyPositions.Dispose();
            enemyEntities.Dispose();
            enemyHealths.Dispose();
            enemyTypes.Dispose();

            return result;
        }

        static float3 GetPosition(EntityManager em, Entity entity)
        {
            return entity != Entity.Null
                ? em.GetComponentData<LocalTransform>(entity).Position
                : default;
        }

        static void DispatchDamageEvents(EntityManager em, ref DamageContext ctx)
        {
            var buffs = em.GetBuffer<BuffInstance>(ctx.Target);
            for (int i = 0; i < buffs.Length; i++)
            {
                ref var buff = ref buffs.ElementAt(i);
                if (buff.OnTakeDamage.IsCreated)
                    buff.OnTakeDamage.Invoke(ref ctx, ref buff);
            }

            if (ctx.Source != Entity.Null)
            {
                buffs = em.GetBuffer<BuffInstance>(ctx.Source);
                for (int i = 0; i < buffs.Length; i++)
                {
                    ref var buff = ref buffs.ElementAt(i);
                    if (buff.OnDealDamage.IsCreated)
                        buff.OnDealDamage.Invoke(ref ctx, ref buff);
                }
            }
        }

        static void DispatchDeathEvents(EntityManager em, ref EntityCommandBuffer ecb,
            Entity dying, float3 pos, NativeArray<Entity> allEnemies,
            ComponentLookup<LocalTransform> transformLookup, BufferLookup<BuffInstance> buffLookup)
        {
            var ctx = new DeathContext
            {
                Entity = dying,
                Position = pos,
                ECB = ecb,
                AllEnemies = allEnemies,
                TransformLookup = transformLookup,
                BuffLookup = buffLookup
            };

            var buffs = em.GetBuffer<BuffInstance>(dying);
            for (int i = 0; i < buffs.Length; i++)
            {
                ref var buff = ref buffs.ElementAt(i);
                if (buff.OnDeath.IsCreated)
                    buff.OnDeath.Invoke(ref ctx, ref buff);
                if (buff.OnEnd.IsCreated)
                    buff.OnEnd.Invoke(ref buff);
            }
        }

        static void DispatchKillEvent(EntityManager em, ref EntityCommandBuffer ecb,
            Entity source, Entity killed, float3 killedPos)
        {
            if (source == Entity.Null)
                return;

            var ctx = new KillContext { Killed = killed, KilledPosition = killedPos, ECB = ecb };
            var buffs = em.GetBuffer<BuffInstance>(source);
            for (int i = 0; i < buffs.Length; i++)
            {
                ref var buff = ref buffs.ElementAt(i);
                if (buff.OnKill.IsCreated)
                    buff.OnKill.Invoke(ref ctx, ref buff);
            }
        }
    }

    [BurstCompile]
    struct DetectHitsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<LocalTransform> BulletPositions;
        [ReadOnly] public NativeArray<Entity> BulletEntities;
        [ReadOnly] public NativeArray<Bullet> BulletDatas;
        [ReadOnly] public NativeArray<LocalTransform> EnemyPositions;
        [ReadOnly] public NativeArray<Entity> EnemyEntities;
        [ReadOnly] public NativeArray<Health> EnemyHealths;
        [ReadOnly] public NativeArray<EntityType> EnemyTypes;
        public float HitRadius;
        public float GroundY;
        public NativeList<BulletHit>.ParallelWriter Hits;

        public void Execute(int i)
        {
            var bulletPos = BulletPositions[i].Position;

            if (bulletPos.y <= GroundY)
            {
                Hits.AddNoResize(new BulletHit
                {
                    Bullet = BulletEntities[i],
                    Target = Entity.Null,
                    Source = BulletDatas[i].Source
                });
                return;
            }

            for (var j = 0; j < EnemyPositions.Length; j++)
            {
                if (EnemyTypes[j].Value != EntityKind.Enemy)
                    continue;
                if (EnemyHealths[j].Current <= 0f)
                    continue;

                if (math.distance(bulletPos, EnemyPositions[j].Position) <= HitRadius)
                {
                    Hits.AddNoResize(new BulletHit
                    {
                        Bullet = BulletEntities[i],
                        Target = EnemyEntities[j],
                        Damage = BulletDatas[i].Damage,
                        Source = BulletDatas[i].Source
                    });
                    return;
                }
            }
        }
    }
}
