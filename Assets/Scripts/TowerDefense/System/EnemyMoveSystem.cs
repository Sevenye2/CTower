using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySpawnSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct EnemyMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {

            float3 towerPos = default;
            var hasTower = false;
            foreach (var lt in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<TowerTag>())
            {
                towerPos = lt.ValueRO.Position;
                hasTower = true;
                break;
            }

            if (!hasTower)
                return;

            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (lt, speed, atkCfg) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRO<EnemyAttackConfig>>()
                         .WithAll<EnemyTag>()
                         .WithNone<SuctionStunTag>())
            {
                var pos = lt.ValueRO.Position;
                var toTower = towerPos - pos;
                toTower.y = 0f;
                var dist = math.length(toTower);
                if (dist <= atkCfg.ValueRO.AttackRange)
                    continue;

                var dir = toTower / dist;
                pos += dir * (speed.ValueRO.Value * dt);
                var t = lt.ValueRO;
                t.Position = pos;
                lt.ValueRW = t;
            }
        }
    }
}
