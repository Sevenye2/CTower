using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySpawnSystem))]
    [UpdateBefore(typeof(EnemyMoveSystem))]
    public partial struct TargetAssignSystem : ISystem
    {
        EntityQuery _targetQuery;

        public void OnCreate(ref SystemState state)
        {
            _targetQuery = SystemAPI.QueryBuilder()
                .WithAll<EntityType, LocalTransform, Health>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var targets = _targetQuery.ToEntityArray(Allocator.TempJob);
            var targetTypes = _targetQuery.ToComponentDataArray<EntityType>(Allocator.TempJob);

            var job = new TargetAssignJob
            {
                Targets = targets,
                TargetTypes = targetTypes,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                HealthLookup = SystemAPI.GetComponentLookup<Health>(true)
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);

            state.Dependency.Complete();
            targets.Dispose();
            targetTypes.Dispose();
        }
    }

    [BurstCompile]
    [WithAll(typeof(EntityType))]
    [WithNone(typeof(Stunned))]
    partial struct TargetAssignJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> Targets;
        [ReadOnly] public NativeArray<EntityType> TargetTypes;
        [ReadOnly] [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<Health> HealthLookup;

        void Execute(ref CurrentTarget target, in LocalTransform lt, in EntityType selfType)
        {
            if (selfType.Value != EntityKind.Enemy)
                return;

            var posXz = new float3(lt.Position.x, 0f, lt.Position.z);

            var best = Entity.Null;
            var bestDist = float.MaxValue;
            for (var t = 0; t < Targets.Length; t++)
            {
                var kind = TargetTypes[t].Value;
                if (kind != EntityKind.Tower && kind != EntityKind.Barrier)
                    continue;
                if (!TransformLookup.TryGetComponent(Targets[t], out var tlt))
                    continue;
                if (!HealthLookup.TryGetComponent(Targets[t], out var hp) || hp.Current <= 0f)
                    continue;
                var txz = new float3(tlt.Position.x, 0f, tlt.Position.z);
                var d = math.distance(posXz, txz);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = Targets[t];
                }
            }

            target = new CurrentTarget { Value = best, Distance = bestDist };
        }
    }
}
