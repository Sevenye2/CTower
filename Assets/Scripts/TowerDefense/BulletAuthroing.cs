using UnityEngine;
using Unity.Entities;

public class BulletAuthroing : MonoBehaviour
{
}

public class BulletBaker : Baker<BulletAuthroing>
{
    public override void Bake(BulletAuthroing authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None);
        AddComponent(entity, new EntityType { Value = EntityKind.Bullet });
        AddComponent<Bullet>(entity, new Bullet());
    }
}
