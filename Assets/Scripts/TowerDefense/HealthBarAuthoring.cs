using Unity.Entities;
using UnityEngine;

namespace CardTower.TowerDefense
{
    public class HealthBarAuthoring : MonoBehaviour
    {
    }

    public class HealthBarBaker : Baker<HealthBarAuthoring>
    {
        public override void Bake(HealthBarAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent<HealthBarTag>(entity);
        }
    }
}
