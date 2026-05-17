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
        AddComponent<BulletTag>(entity);
        AddComponent<Bullet>(entity, new Bullet());

    }
}

