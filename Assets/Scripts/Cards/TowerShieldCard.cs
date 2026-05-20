using CardTower.TowerDefense;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

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

        public override void Play(EntityManager em, Entity towerEntity)
        {
            var hp = em.GetComponentData<Health>(towerEntity);
            hp.Shield = ShieldAmount;
            em.SetComponentData(towerEntity, hp);

            em.GetBuffer<BuffInstance>(towerEntity).Add(Create(em, towerEntity));
        }

        [BurstCompile]
        unsafe static BuffInstance Create(EntityManager em, Entity towerEntity)
        {
            var data = (TowerShieldData*)UnsafeUtility.Malloc(sizeof(TowerShieldData), 4, Allocator.Persistent);
            data->RemainingTime = Duration;
            data->ShieldRemaining = ShieldAmount;

            return new BuffInstance
            {
                Data = data,
                OnTick = BurstCompiler.CompileFunctionPointer<OnTick>(OnTick),
                OnTakeDamage = BurstCompiler.CompileFunctionPointer<OnTakeDamage>(OnTakeDamage)
            };
        }

        struct TowerShieldData
        {
            public float RemainingTime;
            public float ShieldRemaining;
        }

        [BurstCompile]
        unsafe static void OnTick(ref BuffInstance self, ref TickContext ctx)
        {
            var data = (TowerShieldData*)self.Data;
            data->RemainingTime -= ctx.DT;

            if (data->RemainingTime > 0)
            {
                ctx.Health->Shield = data->ShieldRemaining;
                return;
            }

            ctx.Health->Shield = 0;
            UnsafeUtility.Free(self.Data, Allocator.Persistent);
            self.Data = null;
            self.IsExpired = true;
        }

        [BurstCompile]
        static unsafe void OnTakeDamage(ref DamageContext ctx, ref BuffInstance self)
        {
            var data = (TowerShieldData*)self.Data;
            var absorb = math.min(data->ShieldRemaining, ctx.Amount);
            data->ShieldRemaining -= absorb;
            ctx.Amount -= absorb;

            if (data->ShieldRemaining <= 0f)
                self.IsExpired = true;
        }
    }
}
