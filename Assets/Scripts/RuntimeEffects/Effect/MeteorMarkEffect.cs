using CardTower.TowerDefense;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CardTower.RuntimeEffects
{
    public sealed class MeteorMarkEffect : RuntimeEffect
    {
        const int KillsPerMeteor = 5;
        const float BaseDamage = 30f;
        const float AoeRadius = 4f;
        const float MeteorStartHeight = 15f;
        const float MeteorFallDuration = 0.45f;
        const float MeteorStayDuration = 0.18f;

        int _killCount;

        public MeteorMarkEffect()
            : base("meteor_mark")
        {
        }

        public override void OnApply(RuntimeEffectContext context)
        {
            _ctx = context;
            RuntimeEffectManager.OnEnemyKilled += OnEnemyKilled;
        }

        public override void OnRemove(RuntimeEffectContext context)
        {
            RuntimeEffectManager.OnEnemyKilled -= OnEnemyKilled;
        }

        RuntimeEffectContext _ctx;

        void OnEnemyKilled()
        {
            _killCount++;
            if (_killCount < KillsPerMeteor)
                return;

            _killCount = 0;

            var em = _ctx.EntityManager;
            var enemy = PickRandomEnemy(em);
            if (enemy == Entity.Null)
                return;

            var lt = em.GetComponentData<LocalTransform>(enemy);
            var pos = lt.Position;

            // Visual
            var meteor = SpawnMeteorVisual(pos);

            // Delayed damage + cleanup
            _ = RunMeteorAsync(em, pos, meteor);
        }

        static async System.Threading.Tasks.Task RunMeteorAsync(EntityManager em, float3 pos, GameObject meteor)
        {
            var startTime = Time.time;
            while (Time.time - startTime < MeteorFallDuration)
            {
                if (meteor == null) break;
                var t = (Time.time - startTime) / MeteorFallDuration;
                var y = math.lerp(MeteorStartHeight, 0f, t * t);
                meteor.transform.position = new Vector3(pos.x, y, pos.z);
                await System.Threading.Tasks.Task.Yield();
            }

            if (meteor != null)
                meteor.transform.position = new Vector3(pos.x, 0f, pos.z);

            DealAoeDamage(em, pos, AoeRadius, BaseDamage);

            await System.Threading.Tasks.Task.Delay((int)(MeteorStayDuration * 1000f));
            if (meteor != null)
                Object.Destroy(meteor);
        }

        static Entity PickRandomEnemy(EntityManager em)
        {
            using var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Health>(),
                ComponentType.ReadOnly<LocalTransform>());
            using var entities = q.ToEntityArray(Allocator.Temp);
            using var healths = q.ToComponentDataArray<Health>(Allocator.Temp);

            var aliveCount = 0;
            for (var i = 0; i < entities.Length; i++)
            {
                if (healths[i].Current > 0f)
                    aliveCount++;
            }

            if (aliveCount == 0)
                return Entity.Null;

            var rng = new Random((uint)(Time.time * 10000 + entities.Length));
            var target = rng.NextInt(0, aliveCount);
            var idx = 0;
            for (var i = 0; i < entities.Length; i++)
            {
                if (healths[i].Current <= 0f)
                    continue;
                if (idx == target)
                    return entities[i];
                idx++;
            }

            return Entity.Null;
        }

        static void DealAoeDamage(EntityManager em, float3 center, float radius, float damage)
        {
            using var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadWrite<Health>(),
                ComponentType.ReadOnly<LocalTransform>());
            using var entities = q.ToEntityArray(Allocator.Temp);

            var cxz = new float2(center.x, center.z);
            var radiusSq = radius * radius;

            foreach (var e in entities)
            {
                var lt = em.GetComponentData<LocalTransform>(e);
                var pxz = new float2(lt.Position.x, lt.Position.z);
                if (math.lengthsq(pxz - cxz) > radiusSq)
                    continue;

                var hp = em.GetComponentData<Health>(e);
                hp.Current -= damage;
                em.SetComponentData(e, hp);
            }
        }

        static GameObject SpawnMeteorVisual(float3 position)
        {
            var prefab = VfxPrefabRef.MeteorVfx;
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab,
                    new Vector3(position.x, MeteorStartHeight, position.z),
                    Quaternion.identity);
                go.name = "MeteorVfx_Relic";
                return go;
            }

            // Fallback
            var fb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fb.name = "MeteorVfx_Relic";
            Object.Destroy(fb.GetComponent<Collider>());
            fb.transform.position = new Vector3(position.x, MeteorStartHeight, position.z);
            fb.transform.localScale = Vector3.one * 1.6f;
            var r = fb.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = new Color(0.55f, 0.18f, 0.08f, 1f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = mat;
            return fb;
        }
    }
}
