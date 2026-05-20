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
        static readonly Color IndicatorColor = new(0.2f, 0.4f, 1f, 0.5f);

        public override CardConfig Config => new CardConfig
        {
            Id = "slow_field",
            DisplayName = "减速场",
            Description = "选择一片区域，范围内敌人减速20%持续10秒。右键取消返还法力。",
            ManaCost = 1,
            Price = 6,
            RequiresTargeting = true
        };

        public override void Play(EntityManager em, Entity towerEntity)
        {
            Run(em).Forget();
        }

        async UniTaskVoid Run(EntityManager em)
        {

            // Phase 1: targeting
            var target = await GroundTargetingHelper.WaitForGroundTarget(AoeRadius, IndicatorColor, "SlowFieldIndicator");
            if (target == null)
            {
                BattleManager.instance.RefundCard(Config.Id, Config.ManaCost);
                return;
            }

            BattleManager.instance.CompleteTargetPlay();

            var center = target.Value;
            var indicator = CreateSlowFieldIndicator(center);
            var affectedEntities = new NativeList<Entity>(Allocator.Temp);

            // Phase 2: apply slow to enemies in range
            using (var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<EntityType>(),
                ComponentType.ReadOnly<LocalTransform>()))
            {
                using var entities = q.ToEntityArray(Allocator.Temp);
                using var types = q.ToComponentDataArray<EntityType>(Allocator.Temp);
                var cxz = new float2(center.x, center.z);
                var radiusSq = AoeRadius * AoeRadius;

                for (var j = 0; j < entities.Length; j++)
                {
                    if (types[j].Value != EntityKind.Enemy)
                        continue;

                    var e = entities[j];
                    var lt = em.GetComponentData<LocalTransform>(e);
                    var pxz = new float2(lt.Position.x, lt.Position.z);
                    if (math.lengthsq(pxz - cxz) > radiusSq)
                        continue;

                    var buf = em.GetBuffer<BuffInstance>(e);
                    // buf.Add(new BuffInstance { Type = BuffType.Slow, Value = 1f - SlowFactor });
                    affectedEntities.Add(e);
                }
            }

            // Phase 3: wait for duration
            await UniTask.Delay((int)(Duration * 1000f));

            // Phase 4: remove slow
            foreach (var e in affectedEntities)
            {
                if (em.Exists(e) && em.HasBuffer<BuffInstance>(e))
                {
                    var buf = em.GetBuffer<BuffInstance>(e);
                    for (var j = buf.Length - 1; j >= 0; j--)
                    {
          
                    }
                }
            }

            affectedEntities.Dispose();
            if (indicator != null)
                Object.Destroy(indicator);
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
