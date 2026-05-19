using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TargetAssignSystem))]
    [UpdateBefore(typeof(EnemyAttackSystem))]
    public partial struct EnemyMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var job = new EnemyMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(EntityType))]
    [WithNone(typeof(Stunned))]
    partial struct EnemyMoveJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;

        void Execute(ref LocalTransform lt, in MoveSpeed speed,
                     in EnemyAttackConfig atkCfg, in EntityModifiers modifiers,
                     in CurrentTarget target, in EntityType selfType)
        {
            if (selfType.Value != EntityKind.Enemy)
                return;
            if (target.Value == Entity.Null || target.Distance <= atkCfg.AttackRange)
                return;

            var effectiveSpeed = speed.Value * modifiers.Speed;
            var targetPos = TransformLookup[target.Value].Position;
            var pos = lt.Position;
            var posXz = new float3(pos.x, 0f, pos.z);
            var txz = new float3(targetPos.x, 0f, targetPos.z);
            var dir = math.normalize(txz - posXz);
            pos.x += dir.x * effectiveSpeed * DeltaTime;
            pos.z += dir.z * effectiveSpeed * DeltaTime;
            lt.Position = pos;
        }
    }
}
