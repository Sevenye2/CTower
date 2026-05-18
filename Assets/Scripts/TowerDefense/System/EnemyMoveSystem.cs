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

            // ── Normal-speed enemies ──
            foreach (var (lt, speed, atkCfg) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRO<EnemyAttackConfig>>()
                         .WithAll<EnemyTag>()
                         .WithNone<SuctionStunTag, SlowTag>())
            {
                MoveToward(lt, speed.ValueRO.Value, atkCfg.ValueRO.AttackRange, towerPos, dt);
            }

            // ── Slowed enemies ──
            foreach (var (lt, speed, atkCfg, slow) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRO<EnemyAttackConfig>, RefRO<SlowTag>>()
                         .WithAll<EnemyTag>()
                         .WithNone<SuctionStunTag>())
            {
                var effectiveSpeed = speed.ValueRO.Value * (1f - slow.ValueRO.SlowFactor);
                MoveToward(lt, effectiveSpeed, atkCfg.ValueRO.AttackRange, towerPos, dt);
            }
        }

        static void MoveToward(RefRW<LocalTransform> lt, float speed, float attackRange, float3 towerPos, float dt)
        {
            var pos = lt.ValueRO.Position;
            var toTower = towerPos - pos;
            toTower.y = 0f;
            var dist = math.length(toTower);
            if (dist <= attackRange)
                return;

            var dir = toTower / dist;
            pos += dir * (speed * dt);
            var t = lt.ValueRO;
            t.Position = pos;
            lt.ValueRW = t;
        }
    }
}
