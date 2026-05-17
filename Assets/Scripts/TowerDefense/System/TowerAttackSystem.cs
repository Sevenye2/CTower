using CardTower.RuntimeEffects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CardTower.TowerDefense
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAttackSystem))]
    public partial struct ProjectileTowerAttackSystem : ISystem
    {
        const float BulletSpeed = 30f;
        const float BulletSpawnHeight = 2.5f;

        Entity _bulletPrefab;
        EntityQuery _enemyQuery;
        bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabComponentData>();

            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyTag, LocalTransform, Health>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_initialized)
            {
                _bulletPrefab = SystemAPI.GetSingleton<PrefabComponentData>().BulletPrefab;
                _initialized = true;
            }

            var time = (float)SystemAPI.Time.ElapsedTime;
            var em = state.EntityManager;
            var modifiers = RuntimeEffectManager.Instance.Effects.CollectTowerModifiers();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (atkRw, atkLt) in SystemAPI
                         .Query<RefRW<ProjectileTowerAttack>, RefRO<LocalTransform>>()
                         .WithAll<BattleTag>())
            {
                ref var atk = ref atkRw.ValueRW;
                var finalShotsPerSecond = atk.ShotsPerSecond * modifiers.AttackSpeedMultiplier;
                if (time < atk.NextShotTime || finalShotsPerSecond <= 0f)
                    continue;

                var towerPos = atkLt.ValueRO.Position;
                var finalAttackRange = atk.AttackRange * modifiers.AttackRangeMultiplier;
                var enemy = FindNearestEnemyInRange(towerPos, finalAttackRange);
                if (enemy == Entity.Null)
                    continue;

                var enemyPos = em.GetComponentData<LocalTransform>(enemy).Position;
                var muzzlePos = new float3(towerPos.x, towerPos.y + BulletSpawnHeight, towerPos.z);
                var dir = math.normalize(enemyPos - muzzlePos);
                var finalDamage = atk.DamagePerShot * modifiers.DamageMultiplier;

                var bulletEntity = ecb.Instantiate(_bulletPrefab);
                ecb.AddComponent<BattleTag>(bulletEntity);
                ecb.SetComponent(bulletEntity, new Bullet
                {
                    Velocity = dir * BulletSpeed,
                    Damage = finalDamage
                });
                ecb.SetComponent(bulletEntity, LocalTransform.FromPosition(muzzlePos));

                atk.NextShotTime = time + 1f / finalShotsPerSecond;
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        Entity FindNearestEnemyInRange(float3 towerPosition, float attackRange)
        {
            var best = Entity.Null;
            var bestDist = attackRange;

            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.Temp);
            var enemyLts = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var enemyHealths = _enemyQuery.ToComponentDataArray<Health>(Allocator.Temp);

            for (var i = 0; i < enemyEntities.Length; i++)
            {
                if (enemyHealths[i].Current <= 0f)
                    continue;

                var enemyPosition = enemyLts[i].Position;
                var dist = math.distance(
                    new float3(enemyPosition.x, 0f, enemyPosition.z),
                    new float3(towerPosition.x, 0f, towerPosition.z));
                if (dist <= attackRange && dist < bestDist)
                {
                    bestDist = dist;
                    best = enemyEntities[i];
                }
            }

            enemyEntities.Dispose();
            enemyLts.Dispose();
            enemyHealths.Dispose();

            return best;
        }
    }
}
