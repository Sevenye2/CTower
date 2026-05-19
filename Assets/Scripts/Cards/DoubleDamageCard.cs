using CardTower.RuntimeEffects;
using CardTower.TowerDefense;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CardTower.Cards
{
    public sealed class DoubleDamageCard : CardBase
    {
        const float Duration = 5f;
        const float DamageBonus = 1f;

        public override CardConfig Config => new CardConfig
        {
            Id = "double_damage",
            DisplayName = "力量爆发",
            Description = "5秒内攻击力翻倍。",
            ManaCost = 1,
            Price = 8
        };

        public override void Play(RuntimeEffectContext context)
        {
            var buff = context.EntityManager.GetBuffer<BuffInstance>(context.TowerEntity);
            buff.Add(Create(context));
        }

        unsafe static BuffInstance Create(RuntimeEffectContext context)
        {
            var data = (DoubleDamageData*)UnsafeUtility.Malloc(sizeof(DoubleDamageData), 4, Allocator.Persistent);
            data->RemainingTime = Duration;
            data->DamageBonus = DamageBonus;

            var modifiers = context.EntityManager.GetComponentData<EntityModifiers>(context.TowerEntity);
            modifiers.DamageDealt += DamageBonus;
            context.EntityManager.SetComponentData(context.TowerEntity, modifiers);

            return new BuffInstance
            {
                Data = data,
                OnTick = BurstCompiler.CompileFunctionPointer<OnTick>(OnTick)
            };
        }

        struct DoubleDamageData
        {
            public float RemainingTime;
            public float DamageBonus;
        }

        [BurstCompile]
        unsafe static void OnTick(ref BuffInstance self, ref TickContext ctx)
        {
            var data = (DoubleDamageData*)self.Data;
            data->RemainingTime -= ctx.DT;
            if (data->RemainingTime > 0) return;

            ctx.Modifiers->DamageDealt -= data->DamageBonus;
            UnsafeUtility.Free(self.Data, Allocator.Persistent);
            self.Data = null;
            self.IsExpired = true;
        }
    }
}