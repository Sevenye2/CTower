using CardTower.Config;
using CardTower.Relics;
using UnityEngine;

namespace CardTower.UI
{
    [DisallowMultipleComponent]
    public class RelicUIRoot : MonoBehaviour
    {
        [SerializeField] RelicUIView relicViewPrefab;

        void OnEnable()
        {
            RefreshFromSave();
        }

        public void RefreshFromSave()
        {
            if (relicViewPrefab == null)
                return;

            ClearViews();
            foreach (var relicSave in SaveDataManager.instance.Relics)
            {
                GameContentRegistry.Instance.TryCreateRelic(relicSave.id, out var relic);
                relic ??= new UnknownRelic();
                var view = Instantiate(relicViewPrefab, transform);
                view.Setup(relic);
            }
        }

        void ClearViews()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }
    }
}
