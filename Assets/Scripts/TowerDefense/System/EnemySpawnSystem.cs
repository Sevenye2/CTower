using CardTower.TowerDefense;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EnemySpawnSystem : ISystem
{
    const float BarHeight = 2.5f;

    NativeHashMap<int, Entity> prefabs;
    Entity _barPrefab;
    bool _isCreated;
    Random rng;
    float _time;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PrefabComponentData>();
        state.RequireForUpdate<EnemyPrefabBufferData>();
        state.RequireForUpdate<SpawnerData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!_isCreated)
        {
            var singleton = SystemAPI.GetSingleton<PrefabComponentData>();
            _barPrefab = singleton.HealthBarPrefab;
            var buffer = SystemAPI.GetBuffer<EnemyPrefabBufferData>(
                SystemAPI.GetSingletonEntity<PrefabComponentData>());
            prefabs = new NativeHashMap<int, Entity>(buffer.Length, Allocator.Persistent);
            foreach (var d in buffer) prefabs[d.Index] = d.Prefab;
            rng = new Random(math.max(1u, 0xC0FFEE));
            _isCreated = true;
        }

        var dt = SystemAPI.Time.DeltaTime;
        _time += dt;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawnerRw, entity) in SystemAPI
                     .Query<RefRW<SpawnerData>>().WithAll<BattleTag>().WithEntityAccess())
        {
            ref var s = ref spawnerRw.ValueRW;

            // Not yet active — wait for start time
            if (!s.IsActive)
            {
                if (_time >= s.StartTime)
                    s.IsActive = true;
                else
                    continue;
            }

            // Done spawning
            if (s.SpawnedCount >= s.TotalCount)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            // Tick timer
            s.Timer -= dt;
            if (s.Timer > 0f)
                continue;

            s.Timer = s.Interval;

            var prefab = prefabs[s.EnemyPrefabId];
            var batch = math.min(s.BatchSize, s.TotalCount - s.SpawnedCount);

            for (var i = 0; i < batch; i++)
            {
                var pos = GetSpawnPosition(ref s);
                var instance = ecb.Instantiate(prefab);
                ecb.AddComponent<BattleTag>(instance);
                ecb.SetComponent(instance, LocalTransform.FromPosition(pos));

                // Health override
                if (s.OverrideHealth > 0f)
                {
                    ecb.SetComponent(instance, new Health
                    {
                        Current = s.OverrideHealth,
                        Max = s.OverrideHealth
                    });
                }

                // Speed override
                if (s.OverrideSpeed > 0f)
                {
                    ecb.SetComponent(instance, new MoveSpeed { Value = s.OverrideSpeed });
                }

                // Scale override
                if (s.OverrideScale > 0f)
                {
                    var lt = new LocalTransform { Position = pos, Scale = s.OverrideScale };
                    ecb.SetComponent(instance, lt);
                }

                // Health bar
                var barEntity = ecb.Instantiate(_barPrefab);
                ecb.AddComponent<BattleTag>(barEntity);
                ecb.AddComponent(barEntity, new Parent { Value = instance });
                ecb.SetComponent(barEntity, LocalTransform.FromPosition(
                    new float3(0f, BarHeight, 0f)));
            }

            s.SpawnedCount += batch;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    float3 GetSpawnPosition(ref SpawnerData s)
    {
        var radius = s.SpawnRadius > 0f ? s.SpawnRadius : 35f;
        var angle = rng.NextFloat() * math.PI * 2f;
        return new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (prefabs.IsCreated) prefabs.Dispose();
    }
}
