using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMoveSystem))]
    public partial struct EnemyAttackSystem : ISystem
    {
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

            foreach (var (lt, atkCfg, atkState) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<EnemyAttackConfig>, RefRW<EnemyAttackState>>()
                         .WithAll<EnemyTag>()
                         .WithNone<SuctionStunTag>())
            {
                var dist = math.distance(
                    new float3(lt.ValueRO.Position.x, 0f, lt.ValueRO.Position.z),
                    new float3(towerPos.x, 0f, towerPos.z));
                if (dist > atkCfg.ValueRO.AttackRange)
                    continue;

                ref var st = ref atkState.ValueRW;
                if (time < st.NextHitTime)
                    continue;

                st.NextHitTime = time + atkCfg.ValueRO.HitIntervalSeconds;

                var hp = state.EntityManager.GetComponentData<Health>(towerEntity);
                hp.Current -= atkCfg.ValueRO.DamagePerHit;
                state.EntityManager.SetComponentData(towerEntity, hp);
            }
        }
    }
}
