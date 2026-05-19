using Unity.Entities;
using UnityEngine;

namespace CardTower.TowerDefense
{
    [DisallowMultipleComponent]
    public class BarrierAuthoring : MonoBehaviour
    {
        [Min(1f)] public float MaxHealth = 500f;
    }

    public class BarrierAuthoringBaker : Baker<BarrierAuthoring>
    {
        public override void Bake(BarrierAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntityType { Value = EntityKind.Barrier });
            AddComponent(entity, new Health
            {
                Max = authoring.MaxHealth,
                Current = authoring.MaxHealth
            });
            AddComponent(entity, EntityModifiers.Identity);
            AddBuffer<BuffInstance>(entity);
        }
    }
}
