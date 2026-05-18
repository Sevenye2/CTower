using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System;

namespace CardTower.TowerDefense
{
    [Serializable]
    public class PrefabAuthoring : MonoBehaviour
    {
        public GameObject[] enemyPrefab;
        public GameObject bulletPrefab;
        public GameObject healthBarPrefab;
        public GameObject barrierPrefab;
        public GameObject barrierHealthBarPrefab;
        public GameObject meteorVfxPrefab;

        void Awake()
        {
            VfxPrefabRef.MeteorVfx = meteorVfxPrefab;
        }
    }


    public class PrefabBaker : Baker<PrefabAuthoring>
    {
        public override void Bake(PrefabAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);


            AddComponent(entity, new PrefabComponentData()
            {
                BulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic),
                HealthBarPrefab = GetEntity(authoring.healthBarPrefab, TransformUsageFlags.Dynamic),
                BarrierPrefab = GetEntity(authoring.barrierPrefab, TransformUsageFlags.Dynamic),
                BarrierHealthBarPrefab = GetEntity(authoring.barrierHealthBarPrefab, TransformUsageFlags.Dynamic)
            });

            var buffer = AddBuffer<EnemyPrefabBufferData>(entity);
            for (var i = 0; i < authoring.enemyPrefab.Length; i++)
            {
                var prefab = authoring.enemyPrefab[i];
                var pe = GetEntity(prefab, TransformUsageFlags.Dynamic);

                buffer.Add(new EnemyPrefabBufferData()
                {
                    Index = i,
                    Prefab = pe
                });
            }
        }
    }

    public struct PrefabComponentData : IComponentData
    {
        public Entity BulletPrefab;
        public Entity HealthBarPrefab;
        public Entity BarrierPrefab;
        public Entity BarrierHealthBarPrefab;
    }

    public struct EnemyPrefabBufferData : IBufferElementData
    {
        public int Index;
        public Entity Prefab;
    }
}
