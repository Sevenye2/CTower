using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileTowerAttackSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct BulletMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var job = new BulletMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(BulletTag))]
    partial struct BulletMoveJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(ref LocalTransform lt, in Bullet bullet)
        {
            lt.Position += bullet.Velocity * DeltaTime;
        }
    }
}
