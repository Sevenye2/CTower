using CardTower.Config;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace CardTower.UI
{
    public class BattleHUD : MonoBehaviour
    {

        [Header("HP")]
        [SerializeField] Image hpImg;
        [SerializeField] Text hpText;

        [Header("Draw")]
        [SerializeField] Image drawImg;

        [Header("Mana")]
        [SerializeField] Image manaImg;
        [SerializeField] Text manaText;

        [Header("Gold & Time")]
        [SerializeField] Text goldText;
        [SerializeField] Text timeText;

        [Header("Hand")]
        public CardHandRoot cardContainer;
        public HandCardView cardPrefab;

        void Start()
        {
        }

        public void RefreshHp(float hp, float max)
        {
            var ratio = Mathf.Clamp01(hp / max);
            if (hpImg != null)
                hpImg.fillAmount = ratio;
            if (hpText != null)
                hpText.text = "HP " + Mathf.CeilToInt(hp) + "/" + Mathf.CeilToInt(max);
        }

        public void SetDrawBar(float drawProgress)
        {
            if (drawImg != null)
                drawImg.fillAmount = drawProgress;
        }

        public void SetMana(int current, int max)
        {
            if (manaText != null)
                manaText.text = current.ToString();
            if (manaImg != null)
                manaImg.fillAmount = (float)current / Mathf.Max(1, max);
        }

        public void SetGold(float gold)
        {
            if (goldText != null)
                goldText.text = "Gold " + Mathf.FloorToInt(gold);
        }

        public void SetTime(float remain)
        {
            if (timeText == null)
                return;

            var r = Mathf.Max(0f, remain);
            var m = Mathf.FloorToInt(r / 60f);
            var s = Mathf.FloorToInt(r % 60f);
            timeText.text = m.ToString("D2") + ":" + s.ToString("D2");
        }

        public void SyncHand(BattleManager bm)
        {
            if (cardContainer == null || cardPrefab == null)
                return;

            while (bm.HasPendingDraw && cardContainer.HandCardCount < bm.HandLimit)
            {
                var cardId = bm.DequeuePendingDraw();
                if (GameContentRegistry.Instance.TryGetCardConfig(cardId, out var config))
                {
                    var view = cardContainer.AddCard(cardPrefab);
                    view.Setup(config);
                }
            }
        }

        Entity FindTower(EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<EntityType>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var types = query.ToComponentDataArray<EntityType>(Unity.Collections.Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
                if (types[i].Value == EntityKind.Tower)
                    return entities[i];
            return Entity.Null;
        }


    }
}
