using CardTower.TowerDefense;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CardTower.Relics
{
    public sealed class LifeSpringRelic : RelicBase
    {
        const float HealInterval = 10f;
        const float HealPercent = 0.1f;

        public override RelicRuntimeConfig Config => new RelicRuntimeConfig
        {
            Id = "life_spring",
            DisplayName = "生命之泉",
            Description = "每10秒恢复塔10%最大生命值。",
            IconPath = "Art/Relics/LifeSpring",
            Rarity = "Rare",
            Price = 100
        };

        public override void Activate(EntityManager em, Entity towerEntity)
        {
            unsafe
            {
             
                var data = (LifeSpringData*)UnsafeUtility.Malloc(sizeof(LifeSpringData), 4, Allocator.Persistent);
                data->Timer = 0f;
                data->Interval = HealInterval;
                data->HealPercent = HealPercent;

                var buf = em.GetBuffer<BuffInstance>(towerEntity);
                buf.Add(new BuffInstance
                {
                    Data = data,
                    OnTick = BurstCompiler.CompileFunctionPointer<OnTick>(OnTick)
                });   
            }
        }

        struct LifeSpringData
        {
            public float Timer;
            public float Interval;
            public float HealPercent;
        }

        [BurstCompile]
        unsafe static void OnTick(ref BuffInstance self, ref TickContext ctx)
        {
            var data = (LifeSpringData*)self.Data;
            data->Timer += ctx.DT;
            if (data->Timer < data->Interval) return;

            data->Timer -= data->Interval;
            ctx.Health->Current += ctx.Health->Max * data->HealPercent;
            if (ctx.Health->Current > ctx.Health->Max)
                ctx.Health->Current = ctx.Health->Max;
        }
    }
}
