using CardTower.TowerDefense;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CardTower.Relics
{
    public sealed class UnstableEngineRelic : RelicBase
    {
        const float TickInterval = 2f;
        const float AttackSpeedPerStack = 0.02f;
        const float DamagePerStack = -0.01f;

        public override RelicRuntimeConfig Config => new RelicRuntimeConfig
        {
            Id = "unstable_engine",
            DisplayName = "不稳定引擎",
            Description = "每过2秒，攻击速度+2%，攻击力-1%。",
            IconPath = "Art/Relics/UnstableEngine",
            Rarity = "Rare",
            Price = 120
        };

        public override unsafe void Activate(EntityManager em, Entity towerEntity)
        {
            var data = (UnstableEngineData*)UnsafeUtility.Malloc(sizeof(UnstableEngineData), 4, Allocator.Persistent);
            data->Timer = 0f;
            data->Interval = TickInterval;

            var buf = em.GetBuffer<BuffInstance>(towerEntity);
            buf.Add(new BuffInstance
            {
                Data = data,
                OnTick = BurstCompiler.CompileFunctionPointer<OnTick>(OnTick)
            });
        }

        struct UnstableEngineData
        {
            public float Timer;
            public float Interval;
        }

        [BurstCompile]
        unsafe static void OnTick( ref BuffInstance self, ref TickContext ctx)
        {
            var data = (UnstableEngineData*)self.Data;
            data->Timer += ctx.DT;
            if (data->Timer < data->Interval) return;

            data->Timer -= data->Interval;
            ctx.Modifiers->AttackSpeed += AttackSpeedPerStack;
            ctx.Modifiers->DamageDealt += DamagePerStack;
        }
    }
}
