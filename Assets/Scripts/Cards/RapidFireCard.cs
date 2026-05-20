using CardTower.TowerDefense;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CardTower.Cards
{
    public sealed class RapidFireCard : CardBase
    {
        const float Duration = 15f;
        const float AttackSpeedBonus = 0.2f;

        public override CardConfig Config => new CardConfig
        {
            Id = "rapid_fire",
            DisplayName = "急速射击",
            Description = "15秒内塔攻速+20%。",
            ManaCost = 2,
            Price = 10
        };

        public override void Play(EntityManager em, Entity towerEntity)
        {
            em.GetBuffer<BuffInstance>(towerEntity).Add(Create(em, towerEntity));
        }

        unsafe static BuffInstance Create(EntityManager em, Entity towerEntity)
        {
            var data = (RapidFireData*)UnsafeUtility.Malloc(sizeof(RapidFireData), 4, Allocator.Persistent);
            data->RemainingTime = Duration;
            data->AttackSpeedBonus = AttackSpeedBonus;

            var modifiers = em.GetComponentData<EntityModifiers>(towerEntity);
            modifiers.AttackSpeed += AttackSpeedBonus;
            em.SetComponentData(towerEntity, modifiers);

            return new BuffInstance
            {
                Data = data,
                OnTick = BurstCompiler.CompileFunctionPointer<OnTick>(OnTick)
            };
        }

        struct RapidFireData
        {
            public float RemainingTime;
            public float AttackSpeedBonus;
        }

        [BurstCompile]
        unsafe static void OnTick(ref BuffInstance self, ref TickContext ctx)
        {
            var data = (RapidFireData*)self.Data;
            data->RemainingTime -= ctx.DT;
            if (data->RemainingTime > 0) return;

            ctx.Modifiers->AttackSpeed -= data->AttackSpeedBonus;
            UnsafeUtility.Free(self.Data, Allocator.Persistent);
            self.Data = null;
            self.IsExpired = true;
        }
    }
}
