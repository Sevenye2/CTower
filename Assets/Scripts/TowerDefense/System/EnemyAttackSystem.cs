using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMoveSystem))]
    public partial struct EnemyAttackSystem : ISystem
    {
        EntityQuery _barrierQuery;

        public void OnCreate(ref SystemState state)
        {
            _barrierQuery = SystemAPI.QueryBuilder()
                .WithAll<BarrierTag, Health, LocalTransform>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity towerEntity = Entity.Null;
            float3 towerPos = default;
            foreach (var (lt, e) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<TowerTag>().WithEntityAccess())
            {
                towerEntity = e;
                towerPos = lt.ValueRO.Position;
                break;
            }

            if (towerEntity == Entity.Null || !state.EntityManager.HasComponent<Health>(towerEntity))
                return;

            var time = (float)SystemAPI.Time.ElapsedTime;
            var hasBarriers = !_barrierQuery.IsEmptyIgnoreFilter;

            // Collect barrier positions once per frame
            NativeArray<Entity> barrierEntities = default;
            NativeArray<LocalTransform> barrierLts = default;
            NativeArray<Health> barrierHealths = default;
            if (hasBarriers)
            {
                barrierEntities = _barrierQuery.ToEntityArray(Allocator.Temp);
                barrierLts = _barrierQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
                barrierHealths = _barrierQuery.ToComponentDataArray<Health>(Allocator.Temp);
            }

            foreach (var (lt, atkCfg, atkState) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<EnemyAttackConfig>, RefRW<EnemyAttackState>>()
                         .WithAll<EnemyTag>()
                         .WithNone<SuctionStunTag>())
            {
                var enemyPos = lt.ValueRO.Position;
                var dist = math.distance(
                    new float3(enemyPos.x, 0f, enemyPos.z),
                    new float3(towerPos.x, 0f, towerPos.z));
                if (dist > atkCfg.ValueRO.AttackRange)
                    continue;

                ref var st = ref atkState.ValueRW;
                if (time < st.NextHitTime)
                    continue;

                st.NextHitTime = time + atkCfg.ValueRO.HitIntervalSeconds;
                var damage = atkCfg.ValueRO.DamagePerHit;

                // ── Barrier priority: attack nearest barrier in range ──
                if (hasBarriers)
                {
                    Entity closestBarrier = Entity.Null;
                    float closestBarrierDist = float.MaxValue;
                    for (var i = 0; i < barrierEntities.Length; i++)
                    {
                        if (barrierHealths[i].Current <= 0f)
                            continue;
                        var bpos = barrierLts[i].Position;
                        var bdist = math.distance(
                            new float3(enemyPos.x, 0f, enemyPos.z),
                            new float3(bpos.x, 0f, bpos.z));
                        if (bdist <= atkCfg.ValueRO.AttackRange && bdist < closestBarrierDist)
                        {
                            closestBarrierDist = bdist;
                            closestBarrier = barrierEntities[i];
                        }
                    }

                    if (closestBarrier != Entity.Null)
                    {
                        var bhp = state.EntityManager.GetComponentData<Health>(closestBarrier);
                        bhp.Current -= damage;
                        state.EntityManager.SetComponentData(closestBarrier, bhp);
                        continue; // skip attacking tower
                    }
                }

                // ── Tower shield absorbs damage first ──
                if (state.EntityManager.HasComponent<TowerShield>(towerEntity))
                {
                    var shield = state.EntityManager.GetComponentData<TowerShield>(towerEntity);
                    if (shield.Value > 0f)
                    {
                        if (shield.Value >= damage)
                        {
                            shield.Value -= damage;
                            state.EntityManager.SetComponentData(towerEntity, shield);
                            continue;
                        }
                        damage -= shield.Value;
                        shield.Value = 0f;
                        state.EntityManager.SetComponentData(towerEntity, shield);
                    }
                }

                // ── Damage tower health ──
                var hp = state.EntityManager.GetComponentData<Health>(towerEntity);
                hp.Current -= damage;
                state.EntityManager.SetComponentData(towerEntity, hp);
            }

            if (hasBarriers)
            {
                barrierEntities.Dispose();
                barrierLts.Dispose();
                barrierHealths.Dispose();
            }
        }
    }
}
