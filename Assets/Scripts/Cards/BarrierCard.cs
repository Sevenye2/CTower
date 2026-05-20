using CardTower.TowerDefense;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.Cards
{
    public sealed class BarrierCard : CardBase
    {
        const float BarrierRadius = 6f;
        const int BarrierCount = 6;
        const float BarHeight = 2f;

        public override CardConfig Config => new CardConfig
        {
            Id = "barrier",
            DisplayName = "护栏",
            Description = "在塔周围生成6个500HP护栏阻挡敌人。",
            ManaCost = 2,
            Price = 12
        };

        public override void Play(EntityManager em, Entity towerEntity)
        {

            Entity barrierPrefab = Entity.Null;
            Entity barPrefab = Entity.Null;

            using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<PrefabComponentData>()))
            {
                using var arr = q.ToComponentDataArray<PrefabComponentData>(Allocator.Temp);
                if (arr.Length > 0)
                {
                    barrierPrefab = arr[0].BarrierPrefab;
                    barPrefab = arr[0].BarrierHealthBarPrefab;
                }
            }


            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (var i = 0; i < BarrierCount; i++)
            {
                var angle = (float)i / BarrierCount * math.PI * 2f;
                var pos = new float3(
                    math.cos(angle) * BarrierRadius, 0f, math.sin(angle) * BarrierRadius);

                var barrier = ecb.Instantiate(barrierPrefab);
                ecb.AddComponent<BattleEntity>(barrier);
                ecb.SetComponent(barrier, LocalTransform.FromPosition(pos));
                ecb.SetComponent(barrier, new LocalToWorld
                {
                    Value = float4x4.TRS(pos, quaternion.identity, new float3(1f, 1f, 1f))
                });

                var barEntity = ecb.Instantiate(barPrefab);
                ecb.AddComponent<BattleEntity>(barEntity);
                ecb.AddComponent(barEntity, new Parent { Value = barrier });
                ecb.SetComponent(barEntity,
                    LocalTransform.FromPosition(new float3(0f, BarHeight, 0f)));
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

    }
}
