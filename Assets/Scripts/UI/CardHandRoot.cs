using System.Collections.Generic;
using UnityEngine;

namespace CardTower.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class CardHandRoot : MonoBehaviour
    {
        [Header("Layout")]
        [Min(0f)]
        [SerializeField] float spacingPixels = 96f;
        [Min(0f)]
        [SerializeField] float hoverSpreadPixels = 24f;

        [Header("Hover")]
        [Min(1f)]
        [SerializeField] float hoverScale = 1.08f;
        [Min(0f)]
        [SerializeField] float hoverLiftPixels = 36f;
        [Min(0f)]
        [SerializeField] float hoverLiftPixelsPerSecond = 280f;
        [Min(0f)]
        [SerializeField] float hoverScaleUnitsPerSecond = 2f;

        [Header("Drag")]
        [Min(0f)]
        [SerializeField] float playDragThresholdPixels = 120f;

        [Header("Animation")]
        [SerializeField] Vector2 enterFromPosition = new Vector2(0f, -200f);
        [Min(0.01f)]
        [SerializeField] float enterDuration = 0.35f;
        [SerializeField] Vector2 playTargetPosition = new Vector2(200f, 400f);
        [Min(0.01f)]
        [SerializeField] float playDuration = 0.3f;

        [Header("Prefab")]
        [SerializeField] HandCardView cardPrefab;

        public float SpacingPixels => spacingPixels;
        public float HoverSpreadPixels => hoverSpreadPixels;
        public float HoverScale => hoverScale;
        public float HoverLiftPixels => hoverLiftPixels;
        public float HoverLiftPixelsPerSecond => hoverLiftPixelsPerSecond;
        public float HoverScaleUnitsPerSecond => hoverScaleUnitsPerSecond;
        public float PlayDragThresholdPixels => playDragThresholdPixels;
        public Vector2 EnterFromPosition => enterFromPosition;
        public float EnterDuration => enterDuration;
        public Vector2 PlayTargetPosition => playTargetPosition;
        public float PlayDuration => playDuration;

        public int? HoveredIndex { get; set; }
        public int HandCardCount => _cards.Count;

        readonly List<HandCardView> _cards = new();
        int _nextHandIndex;
        CanvasGroup _canvasGroup;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            foreach (var card in GetComponentsInChildren<HandCardView>())
            {
                if (card.IsConsumed) continue;
                card.HandIndex = _nextHandIndex++;
                card.onPlayed.AddListener(OnCardPlayed);
                _cards.Add(card);
            }
            foreach (var card in _cards)
                card.SnapToRestPosition();
        }

        public HandCardView AddCard(HandCardView prefab)
        {
            var prefabToUse = prefab != null ? prefab : cardPrefab;
            var instance = Instantiate(prefabToUse, transform);
            instance.HandIndex = _nextHandIndex++;
            instance.onPlayed.AddListener(OnCardPlayed);
            _cards.Add(instance);
            instance.BeginEnter(enterFromPosition);
            return instance;
        }

        void OnCardPlayed()
        {
            _cards.RemoveAll(c => c == null || c.IsConsumed);
            RenumberIndices();
        }

        void RenumberIndices()
        {
            _nextHandIndex = 0;
            foreach (var card in _cards)
                card.HandIndex = _nextHandIndex++;
        }

        public void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
