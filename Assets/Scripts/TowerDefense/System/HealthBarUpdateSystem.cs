using CardTower.RuntimeEffects;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct HealthBarUpdateSystem : ISystem
    {
        EntityQuery _enemyQuery;
        EntityQuery _barrierQuery;

        public void OnCreate(ref SystemState state)
        {
            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyTag, Health>()
                .Build();
            _barrierQuery = SystemAPI.QueryBuilder()
                .WithAll<BarrierTag, Health>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var camRot = (quaternion)UnityEngine.Camera.main.transform.rotation;
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var goldMultiplier = RuntimeEffectManager.Instance.Effects.CollectTowerModifiers().GoldMultiplier;

            // ── Destroy dying enemies (main thread: gold is managed) ──
            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyHealths = _enemyQuery.ToComponentDataArray<Health>(Allocator.TempJob);

            for (var i = 0; i < enemyEntities.Length; i++)
            {
                if (enemyHealths[i].Current <= 0f)
                {
                    ecb.DestroyEntity(enemyEntities[i]);
                    BattleManager.instance.goldEarned += 1f * goldMultiplier;
                    RuntimeEffectManager.NotifyEnemyKilled();
                }
            }

            // ── Destroy dead barriers ──
            var barrierEntities = _barrierQuery.ToEntityArray(Allocator.TempJob);
            var barrierHealths = _barrierQuery.ToComponentDataArray<Health>(Allocator.TempJob);

            for (var i = 0; i < barrierEntities.Length; i++)
            {
                if (barrierHealths[i].Current <= 0f)
                    ecb.DestroyEntity(barrierEntities[i]);
            }

            barrierEntities.Dispose();
            barrierHealths.Dispose();

            enemyEntities.Dispose();
            enemyHealths.Dispose();

            // ── Update bars + destroy orphans (Burst IJobEntity) ──
            var healthLookup = SystemAPI.GetComponentLookup<Health>();
            var job = new HealthJob
            {
                ECB = ecb.AsParallelWriter(),
                HealthLookup = healthLookup,
                CamRot = camRot
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(em);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    [WithAll(typeof(HealthBarTag))]
    partial struct HealthJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        [ReadOnly]
        public ComponentLookup<Health> HealthLookup;

        public quaternion CamRot;

        void Execute(ref LocalToWorld ltw, in Parent parent, in Entity entity, [ChunkIndexInQuery] int sortKey)
        {
            if (!HealthLookup.TryGetComponent(parent.Value, out var health))
            {
                ECB.DestroyEntity(sortKey, entity);
                return;
            }

            var ratio = math.clamp(health.Current / math.max(0.001f, health.Max), 0f, 1f);
            var pos = ltw.Position;
            var sca = new float3(ratio * 2.6f, 0.4f, 1f);
            var mat = float4x4.TRS(pos, CamRot, sca);

            ltw = new LocalToWorld { Value = mat };
        }
    }
}
