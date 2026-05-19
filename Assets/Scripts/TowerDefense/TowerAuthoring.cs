using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace CardTower.TowerDefense
{
    [DisallowMultipleComponent]
    public class TowerAuthoring : MonoBehaviour
    {
        [Min(1f)] public float MaxHealth = 500f;
    }

    public class TowerAuthoringBaker : Baker<TowerAuthoring>
    {
        public override void Bake(TowerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntityType { Value = EntityKind.Tower });
            AddComponent(entity,new PlayerTag());
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
