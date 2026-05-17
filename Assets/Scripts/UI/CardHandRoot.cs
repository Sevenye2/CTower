using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardTower.UI
{
    /// <summary>
    /// 手牌容器：内置排版与悬停/出牌参数；子物体上的 HandCardView 从此处读取数值。
    /// </summary>
    [DisallowMultipleComponent]
    public class CardHandRoot : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("手牌横向间距（像素）。")]
        [Min(0f)]
        [SerializeField] float spacingPixels = 96f;

        [Header("Hover")]
        [Tooltip("鼠标悬停时的缩放倍数（相对 resting local scale）。")]
        [Min(1f)]
        [SerializeField] float hoverScale = 1.08f;

        [Tooltip("悬停目标上移量（像素）。")]
        [Min(0f)]
        [SerializeField] float hoverLiftPixels = 36f;

        [Tooltip("上移/下移的线速度（像素/秒），用于悬停过渡。")]
        [Min(0f)]
        [SerializeField] float hoverLiftPixelsPerSecond = 280f;

        [Tooltip("缩放回目标的线速度（缩放倍数/秒），用于悬停过渡。")]
        [Min(0f)]
        [SerializeField] float hoverScaleUnitsPerSecond = 2f;

        [Header("Drag")]
        [Tooltip("从开始拖动位置算起，向上移动超过该像素视为出牌。")]
        [Min(0f)]
        [SerializeField] float playDragThresholdPixels = 120f;

        [Header("Prefab")]
        [SerializeField] HandCardView cardPrefab;

        public float SpacingPixels => spacingPixels;
        public float HoverScale => hoverScale;
        public float HoverLiftPixels => hoverLiftPixels;
        public float HoverLiftPixelsPerSecond => hoverLiftPixelsPerSecond;
        public float HoverScaleUnitsPerSecond => hoverScaleUnitsPerSecond;
        public float PlayDragThresholdPixels => playDragThresholdPixels;

        public int HandCardCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < transform.childCount; i++)
                {
                    if (transform.GetChild(i).GetComponent<HandCardView>() != null)
                        n++;
                }

                return n;
            }
        }

        readonly List<HandCardView> _cards = new List<HandCardView>();

        void Awake()
        {
            RefreshCardListFromChildren();
        }

        void OnEnable()
        {
            Reflow();
        }

        void OnValidate()
        {
            RefreshCardListFromChildren();
            Reflow();
        }

        public void RefreshCardListFromChildren()
        {
            _cards.Clear();
            for (var i = 0; i < transform.childCount; i++)
            {
                var card = transform.GetChild(i).GetComponent<HandCardView>();
                if (card != null)
                    _cards.Add(card);
            }

            foreach (var card in _cards)
                card.onPlayed.RemoveListener(OnCardPlayed);
            foreach (var card in _cards)
                card.onPlayed.AddListener(OnCardPlayed);
        }

        public HandCardView AddCard(HandCardView prefab)
        {
            var instance = Instantiate(prefab, transform);
            RefreshCardListFromChildren();
            Reflow();
            return instance;
        }

        public HandCardView AddCardFromPrefab()
        {
            return AddCard(cardPrefab);
        }

        void OnCardPlayed()
        {
            StartCoroutine(RefreshAfterDestroyFrame());
        }

        IEnumerator RefreshAfterDestroyFrame()
        {
            yield return null;
            RefreshCardListFromChildren();
            Reflow();
        }

        public void Reflow()
        {
            if (_cards.Count == 0)
                return;

            var spacing = spacingPixels;
            var totalWidth = spacing * Mathf.Max(0, _cards.Count - 1);
            var startX = -totalWidth * 0.5f;

            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                var rt = (RectTransform)card.transform;
                var pos = new Vector2(startX + i * spacing, rt.anchoredPosition.y);
                card.SetRestAnchoredPosition(pos);
            }
        }
    }
}
