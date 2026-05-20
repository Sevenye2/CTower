using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace CardTower.Cards
{
    public static class GroundTargetingHelper
    {
        public static async UniTask<float3?> WaitForGroundTarget(float indicatorRadius, Color indicatorColor, string indicatorName)
        {
            var indicator = CreateGroundIndicator(indicatorRadius, indicatorColor, indicatorName);
            var handRoot = UIManager.instance?.battleHUD?.cardContainer;
            if (handRoot != null)
                handRoot.SetVisible(false);

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
                if (handRoot != null)
                    handRoot.SetVisible(true);
                if (indicator != null)
                    Object.Destroy(indicator);
            }
        }

        public static float3? GetGroundPosition()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var dist))
                return (float3)ray.GetPoint(dist);

            return null;
        }

        public static void UpdateIndicatorPosition(GameObject indicator)
        {
            if (indicator == null) return;
            var pos = GetGroundPosition();
            if (pos.HasValue)
                indicator.transform.position = (Vector3)pos.Value;
        }

        public static GameObject CreateGroundIndicator(float radius, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(radius * 2f, 0.12f, radius * 2f);
            var r = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
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
