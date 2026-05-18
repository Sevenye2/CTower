using CardTower.RuntimeEffects;
using CardTower.TowerDefense;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CardTower.Cards
{
    public sealed class MeteorStrikeCard : CardBase
    {
        const float BaseDamage = 30f;
        const float AoeRadius = 4f;
        const float MeteorStartHeight = 15f;
        const float MeteorFallDuration = 0.45f;
        const float MeteorStayDuration = 0.18f;

        public override CardConfig Config => new CardConfig
        {
            Id = "meteor_strike",
            DisplayName = "陨石天降",
            Description = "选择一片区域，召唤陨石对范围内敌人造成伤害。右键取消返还法力。",
            ManaCost = 1,
            Price = 5
        };

        public override void Play(RuntimeEffectContext context)
        {
            Run(context).Forget();
        }

        async UniTaskVoid Run(RuntimeEffectContext context)
        {
            var em = context.EntityManager;

            // ── Phase 1: targeting ──
            var target = await WaitForGroundTarget();
            if (target == null)
            {
                BattleManager.instance.RefundCard(Config.Id, Config.ManaCost);
                return;
            }

            var impactPos = target.Value;

            // ── Phase 2: meteor fall ──
            var meteor = CreateMeteorVisual(impactPos);
            var startTime = Time.time;
            while (Time.time - startTime < MeteorFallDuration)
            {
                if (meteor == null) break;
                var t = (Time.time - startTime) / MeteorFallDuration;
                var y = math.lerp(MeteorStartHeight, 0f, t * t);
                meteor.transform.position = new Vector3(impactPos.x, y, impactPos.z);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (meteor != null)
                meteor.transform.position = new Vector3(impactPos.x, 0f, impactPos.z);

            // ── Phase 3: damage ──
            DealAoeDamage(em, impactPos, AoeRadius, BaseDamage);

            // ── Phase 4: brief linger then destroy ──
            await UniTask.Delay((int)(MeteorStayDuration * 1000f));
            if (meteor != null)
                Object.Destroy(meteor);
        }

        static async UniTask<float3?> WaitForGroundTarget()
        {
            var indicator = CreateGroundIndicator();

            try
            {
                while (true)
                {
                    if (!BattleManager.instance.isBattling)
                        return null;

                    if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                        return null;

                    UpdateIndicatorPosition(indicator);

                    if (Input.GetMouseButtonDown(0))
                    {
                        var pos = GetGroundPosition();
                        if (pos.HasValue)
                            return pos.Value;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }
            finally
            {
                if (indicator != null)
                    Object.Destroy(indicator);
            }
        }

        static float3? GetGroundPosition()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var dist))
                return (float3)ray.GetPoint(dist);

            return null;
        }

        static void UpdateIndicatorPosition(GameObject indicator)
        {
            if (indicator == null) return;
            var pos = GetGroundPosition();
            if (pos.HasValue)
                indicator.transform.position = (Vector3)pos.Value;
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

        static GameObject CreateGroundIndicator()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "MeteorIndicator";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(AoeRadius * 2f, 0.15f, AoeRadius * 2f);
            var r = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(1f, 0.35f, 0.1f, 0.6f));
            else
                mat.color = new Color(1f, 0.35f, 0.1f, 0.6f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = mat;
            return go;
        }

        static GameObject CreateMeteorVisual(Vector3 position)
        {
            var prefab = TowerDefense.VfxPrefabRef.MeteorVfx;
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab,
                    new Vector3(position.x, MeteorStartHeight, position.z),
                    Quaternion.identity);
                go.name = "MeteorVfx";
                return go;
            }

            // Fallback
            var fb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fb.name = "MeteorVfx";
            Object.Destroy(fb.GetComponent<Collider>());
            fb.transform.position = new Vector3(position.x, MeteorStartHeight, position.z);
            fb.transform.localScale = Vector3.one * 1.6f;
            var r = fb.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(0.55f, 0.18f, 0.08f, 1f));
            else
                mat.color = new Color(0.55f, 0.18f, 0.08f, 1f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = mat;
            return fb;
        }
    }
}
