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
    public sealed class SlowFieldCard : CardBase
    {
        const float AoeRadius = 5f;
        const float SlowFactor = 0.2f;
        const float Duration = 10f;

        public override CardConfig Config => new CardConfig
        {
            Id = "slow_field",
            DisplayName = "减速场",
            Description = "选择一片区域，范围内敌人减速20%持续10秒。右键取消返还法力。",
            ManaCost = 1,
            Price = 6
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

            var center = target.Value;
            var indicator = CreateSlowFieldIndicator(center);
            var affectedEntities = new NativeList<Entity>(Allocator.Temp);

            // ── Phase 2: apply slow to enemies in range ──
            using (var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadWrite<MoveSpeed>(),
                ComponentType.ReadOnly<LocalTransform>()))
            {
                using var entities = q.ToEntityArray(Allocator.Temp);
                var cxz = new float2(center.x, center.z);
                var radiusSq = AoeRadius * AoeRadius;

                foreach (var e in entities)
                {
                    var lt = em.GetComponentData<LocalTransform>(e);
                    var pxz = new float2(lt.Position.x, lt.Position.z);
                    if (math.lengthsq(pxz - cxz) > radiusSq)
                        continue;

                    if (!em.HasComponent<SlowTag>(e))
                    {
                        em.AddComponentData(e, new SlowTag { SlowFactor = SlowFactor });
                    }
                    else
                    {
                        var st = em.GetComponentData<SlowTag>(e);
                        st.SlowFactor = math.max(st.SlowFactor, SlowFactor);
                        em.SetComponentData(e, st);
                    }

                    affectedEntities.Add(e);
                }
            }

            // ── Phase 3: wait for duration ──
            await UniTask.Delay((int)(Duration * 1000f));

            // ── Phase 4: remove slow ──
            foreach (var e in affectedEntities)
            {
                if (em.Exists(e) && em.HasComponent<SlowTag>(e))
                {
                    var st = em.GetComponentData<SlowTag>(e);
                    st.SlowFactor = 0f;
                    em.RemoveComponent<SlowTag>(e);
                }
            }

            affectedEntities.Dispose();
            if (indicator != null)
                Object.Destroy(indicator);
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

        static GameObject CreateGroundIndicator()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SlowFieldIndicator";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(AoeRadius * 2f, 0.12f, AoeRadius * 2f);
            var r = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(0.2f, 0.4f, 1f, 0.5f));
            else
                mat.color = new Color(0.2f, 0.4f, 1f, 0.5f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = mat;
            return go;
        }

        static GameObject CreateSlowFieldIndicator(float3 center)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SlowFieldZone";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = (Vector3)center + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(AoeRadius * 2f, 0.08f, AoeRadius * 2f);
            var r = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(0.15f, 0.35f, 0.9f, 0.35f));
            else
                mat.color = new Color(0.15f, 0.35f, 0.9f, 0.35f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = mat;
            return go;
        }
    }
}
