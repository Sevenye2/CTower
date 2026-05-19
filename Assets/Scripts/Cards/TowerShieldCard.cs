using CardTower.RuntimeEffects;
using CardTower.TowerDefense;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CardTower.Cards
{
    public sealed class TowerShieldCard : CardBase
    {
        const float Duration = 15f;
        const float ShieldAmount = 200f;

        public override CardConfig Config => new CardConfig
        {
            Id = "tower_shield",
            DisplayName = "护盾",
            Description = "为塔生成200护盾，持续15秒。",
            ManaCost = 1,
            Price = 8
        };

        public override void Play(RuntimeEffectContext context)
        {
            var buff = context.EntityManager.GetBuffer<BuffInstance>(context.TowerEntity);
            buff.Add(Create(context));
        }

        [BurstCompile]
        unsafe static BuffInstance Create(RuntimeEffectContext context)
        {
            var data = (TowerShieldData*)UnsafeUtility.Malloc(sizeof(TowerShieldData), 4, Allocator.Persistent);
            data->RemainingTime = Duration;
            data->ShieldAmount = ShieldAmount;

            var tower = context.TowerEntity;
            var em = context.EntityManager;
            if (em.HasComponent<Health>(tower))
            {
                var hp = em.GetComponentData<Health>(tower);
                hp.Shield += ShieldAmount;
                em.SetComponentData(tower, hp);
            }

            return new BuffInstance
            {
                Data = data,
                OnTick = BurstCompiler.CompileFunctionPointer<OnTick>(OnTick)
            };
        }

        struct TowerShieldData
        {
            public float RemainingTime;
            public float ShieldAmount;
        }

        [BurstCompile]
        static unsafe void OnTick(ref BuffInstance self, ref TickContext ctx)
        {
            var data = (TowerShieldData*)self.Data;
            data->RemainingTime -= ctx.DT;
            if (data->RemainingTime > 0) return;

            ctx.Health->Shield-= data->ShieldAmount;
            if(ctx.Health->Shield < 0) ctx.Health->Shield = 0;
            UnsafeUtility.Free(self.Data, Allocator.Persistent);
            self.Data = null;
            self.IsExpired = true;
        }
    }
}
