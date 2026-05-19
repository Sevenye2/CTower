using CardTower.RuntimeEffects;
using CardTower.TowerDefense;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CardTower.Cards
{
    public sealed class GoldRushCard : CardBase
    {
        const float Duration = 15f;
        const float MultiplierBonus = 0.5f;

        public override CardConfig Config => new CardConfig
        {
            Id = "gold_rush",
            DisplayName = "淘金热",
            Description = "15秒内金币获取量+50%。",
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
            var data = (GoldRushData*)UnsafeUtility.Malloc(sizeof(GoldRushData), 4, Allocator.Persistent);
            data->RemainingTime = Duration;
            data->GoldBonus = MultiplierBonus;

            var modifiers = context.EntityManager.GetComponentData<EntityModifiers>(context.TowerEntity);
            modifiers.GoldMultiplier += MultiplierBonus;
            context.EntityManager.SetComponentData(context.TowerEntity, modifiers);

            return new BuffInstance
            {
                Data = data,
                OnTick = BurstCompiler.CompileFunctionPointer<OnTick>(OnTick)
            };
        }

        struct GoldRushData
        {
            public float RemainingTime;
            public float GoldBonus;
        }

        [BurstCompile]
        unsafe static void OnTick(ref BuffInstance self, ref TickContext ctx)
        {
            var data = (GoldRushData*)self.Data;
            data->RemainingTime -= ctx.DT;
            if (data->RemainingTime > 0) return;

            ctx.Modifiers->GoldMultiplier -= data->GoldBonus;
            UnsafeUtility.Free(self.Data, Allocator.Persistent);
            self.Data = null;
            self.IsExpired = true;
        }
    }
}