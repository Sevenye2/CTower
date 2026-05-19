using System.Collections.Generic;
using CardTower.RuntimeEffects;
using CardTower.TowerDefense;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace CardTower.Cards
{
    public sealed class BlackHoleCard : CardBase
    {
        const float DefaultDurationSeconds = 1.35f;
        const float HoleHeightAboveTower = 4.5f;
        const float PullStrength = 18f;

        public override CardConfig Config => new CardConfig
        {
            Id = "black_hole",
            DisplayName = "黑洞",
            Description = "在塔顶生成黑洞，吸引敌人并造成清场效果。",
            ManaCost = 3,
            Price = 15
        };

        public override void Play(RuntimeEffectContext context)
        {
            Run().Forget();
        }

        async UniTaskVoid Run()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var em = world.EntityManager;
            if (!TryGetTowerWorldPosition(em, out var towerPos))
                return;

            var holePos = (Vector3)towerPos + Vector3.up * HoleHeightAboveTower;
            var holeVisual = CreateHoleVisual(holePos);

            var enemies = CollectLivingEnemies(em);
            foreach (var e in enemies)
            {
                if (em.Exists(e) && !em.HasComponent<Stunned>(e))
                    em.AddComponent<Stunned>(e);
            }

            var holeF3 = (float3)holePos;
            var elapsed = 0f;
            while (elapsed < DefaultDurationSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / DefaultDurationSeconds);
                if (holeVisual != null)
                {
                    var pulse = 1f + 0.12f * Mathf.Sin(elapsed * 8f);
                    holeVisual.transform.localScale = Vector3.one * (3.2f + 0.6f * t) * pulse;
                }

                PullEnemiesToward(em, holeF3, Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            foreach (var e in enemies)
            {
                if (!em.Exists(e) || !em.HasComponent<Health>(e))
                    continue;
                var hp = em.GetComponentData<Health>(e);
                hp.Current = 0f;
                em.SetComponentData(e, hp);
            }

            if (holeVisual != null)
                Object.Destroy(holeVisual);
        }

        static bool TryGetTowerWorldPosition(EntityManager em, out float3 towerWorld)
        {
            towerWorld = default;
            using var q = em.CreateEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<EntityType>());
            using var arr = q.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using var types = q.ToComponentDataArray<EntityType>(Allocator.Temp);
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i].Value == EntityKind.Tower)
                {
                    towerWorld = arr[i].Position;
                    return true;
                }
            }
            return false;
        }

        static List<Entity> CollectLivingEnemies(EntityManager em)
        {
            var list = new List<Entity>();
            using var q = em.CreateEntityQuery(ComponentType.ReadOnly<EntityType>(), ComponentType.ReadWrite<Health>());
            using var ents = q.ToEntityArray(Allocator.Temp);
            using var types = q.ToComponentDataArray<EntityType>(Allocator.Temp);
            for (var i = 0; i < ents.Length; i++)
            {
                if (types[i].Value != EntityKind.Enemy)
                    continue;
                var e = ents[i];
                var hp = em.GetComponentData<Health>(e);
                if (hp.Current <= 0f)
                    continue;
                list.Add(e);
            }

            return list;
        }

        static void PullEnemiesToward(EntityManager em, float3 hole, float dt)
        {
            using var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<EntityType>(),
                ComponentType.ReadOnly<Stunned>(),
                ComponentType.ReadWrite<LocalTransform>());
            using var ents = q.ToEntityArray(Allocator.Temp);
            foreach (var e in ents)
            {
                if (!em.HasComponent<LocalTransform>(e))
                    continue;
                var lt = em.GetComponentData<LocalTransform>(e);
                var p = lt.Position;
                var toHole = hole - p;
                var dist = math.max(0.05f, math.length(toHole));
                var step = math.normalize(toHole) * (PullStrength * dt / math.sqrt(dist));
                if (math.lengthsq(step) > math.lengthsq(toHole))
                    p = hole;
                else
                    p += step;
                lt.Position = p;
                var u = lt.Scale;
                u = math.max(0.08f, u - dt * 1.1f);
                lt.Scale = u;
                em.SetComponentData(e, lt);
            }
        }

        static GameObject CreateHoleVisual(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "CardBlackHoleVfx";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 3.2f;
            var r = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.black);
            else
                mat.color = Color.black;
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.85f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.75f);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = mat;
            return go;
        }
    }
}
