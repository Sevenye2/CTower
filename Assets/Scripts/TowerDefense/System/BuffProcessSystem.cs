using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySpawnSystem))]
    [UpdateBefore(typeof(EnemyMoveSystem))]
    public partial struct BuffProcessSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var job = new BuffProcessJob { Time = SystemAPI.Time.DeltaTime };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(EntityModifiers), typeof(Health))]
    partial struct BuffProcessJob : IJobEntity
    {
        [ReadOnly] public float Time;

        unsafe void Execute(ref DynamicBuffer<BuffInstance> buffs, Entity entity,
                     ref EntityModifiers modifiers, ref Health health)
        {
            var ctx = new TickContext
            {
                DT = Time,
                Modifiers = (EntityModifiers*)UnsafeUtility.AddressOf(ref modifiers),
                Health = (Health*)UnsafeUtility.AddressOf(ref health)
            };

            for (int i = buffs.Length - 1; i >= 0; i--)
            {
                ref var buff = ref buffs.ElementAt(i);
                if (buff.OnTick.IsCreated)
                    buff.OnTick.Invoke(ref buff, ref ctx);

                if (!buff.IsExpired) continue;

                if (buff.OnEnd.IsCreated)
                    buff.OnEnd.Invoke(ref buff);
                buffs.RemoveAtSwapBack(i);
            }
        }
    }
}
