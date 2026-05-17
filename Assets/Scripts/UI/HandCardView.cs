using CardTower.Cards;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardTower.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HandCardView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Drag")]
        [SerializeField] Canvas overrideCanvas;

        [Header("Card Face")]
        [SerializeField] Text costText;
        [SerializeField] Image artImage;
        [SerializeField] Text bodyText;
        [SerializeField] Image frameImage;

        [Header("Fallback Visuals")]
        [SerializeField] Color frameColor = new Color(0.11f, 0.09f, 0.14f, 1f);
        [SerializeField] Color missingArtColor = new Color(0.15f, 0.12f, 0.2f, 1f);

        public UnityEvent onPlayed;

        RectTransform Rect => _rect ??= GetComponent<RectTransform>();
        RectTransform _rect;

        CardConfig _config;
        CardHandRoot _hand;

        Vector2 _restAnchoredPosition;
        Vector3 _restLocalScale = Vector3.one;
        bool _hovered;
        bool _dragging;
        bool _consumed;
        Vector2 _dragStartAnchored;
        float _canvasScaleFactor = 1f;

        float _animatedLift;
        float _animatedScaleMul = 1f;

        public CardConfig Config => _config;

        void Awake()
        {
            ConfigureFaceRaycastTargets();
            ApplyFrameColor();
            _restLocalScale = Rect.localScale;
            CacheHand();
            CacheCanvasScale();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            ConfigureFaceRaycastTargets();
            ApplyFrameColor();
        }
#endif

        void OnEnable()
        {
            CacheHand();
            SyncVisualFromAnimatedState();
        }

        void Update()
        {
            if (_consumed)
                return;

            var hand = _hand != null ? _hand : CacheHand();
            var targetLift = _hovered && !_dragging ? hand.HoverLiftPixels : 0f;
            var targetScale = _hovered && !_dragging ? hand.HoverScale : 1f;

            var dt = Time.unscaledDeltaTime;
            _animatedLift = Mathf.MoveTowards(_animatedLift, targetLift, hand.HoverLiftPixelsPerSecond * dt);
            _animatedScaleMul = Mathf.MoveTowards(_animatedScaleMul, targetScale, hand.HoverScaleUnitsPerSecond * dt);

            Rect.localScale = Vector3.Scale(_restLocalScale, Vector3.one * _animatedScaleMul);
            if (!_dragging)
                Rect.anchoredPosition = _restAnchoredPosition + new Vector2(0f, _animatedLift);
        }

        public void Setup(CardConfig config)
        {
            _config = config;
            ApplyConfigToFace();
        }

        CardHandRoot CacheHand()
        {
            _hand = GetComponentInParent<CardHandRoot>();
            return _hand;
        }

        public void SetRestAnchoredPosition(Vector2 anchoredPosition)
        {
            _restAnchoredPosition = anchoredPosition;
        }

        void CacheCanvasScale()
        {
            var canvas = overrideCanvas != null ? overrideCanvas : GetComponentInParent<Canvas>();
            _canvasScaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        }

        void SyncVisualFromAnimatedState()
        {
            Rect.anchoredPosition = _restAnchoredPosition + new Vector2(0f, _animatedLift);
            Rect.localScale = Vector3.Scale(_restLocalScale, Vector3.one * _animatedScaleMul);
        }

        void ApplyConfigToFace()
        {
            if (_config == null)
                return;

            if (costText != null)
                costText.text = Mathf.Max(0, _config.ManaCost).ToString();

            if (artImage != null)
            {
                var art = Resources.Load<Sprite>($"Art/Cards/{_config.Id}");
                artImage.sprite = art;
                artImage.color = art != null ? Color.white : missingArtColor;
            }

            if (bodyText != null)
            {
                var title = string.IsNullOrEmpty(_config.DisplayName) ? _config.Id : _config.DisplayName;
                var desc = _config.Description ?? string.Empty;
                bodyText.text = string.IsNullOrEmpty(desc) ? title : $"{title}\n<size=11><color=#cccccc>{desc}</color></size>";
            }
        }

        void ConfigureFaceRaycastTargets()
        {
            var rootImage = GetComponent<Image>();
            if (rootImage != null)
                rootImage.raycastTarget = true;

            if (frameImage != null)
                frameImage.raycastTarget = false;
            if (artImage != null)
                artImage.raycastTarget = false;
            if (costText != null)
                costText.raycastTarget = false;
            if (bodyText != null)
                bodyText.raycastTarget = false;
        }

        void ApplyFrameColor()
        {
            var rootImage = GetComponent<Image>();
            if (rootImage != null)
                rootImage.color = new Color(0.18f, 0.14f, 0.22f, 1f);

            if (frameImage != null)
                frameImage.color = frameColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            if (!_dragging)
                transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_consumed || _dragging)
                return;
            PlayCard();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_consumed)
                return;
            CacheCanvasScale();
            _dragging = true;
            _dragStartAnchored = Rect.anchoredPosition;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_consumed)
                return;
            Rect.anchoredPosition += eventData.delta / _canvasScaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_consumed)
                return;

            _dragging = false;

            var hand = _hand != null ? _hand : CacheHand();
            var upwardDelta = Rect.anchoredPosition.y - _dragStartAnchored.y;
            if (upwardDelta >= hand.PlayDragThresholdPixels)
            {
                PlayCard();
                return;
            }
        }

        void PlayCard()
        {
            if (_consumed)
                return;

            var bm = BattleManager.instance;
            if (bm != null && !bm.TryCommitPlay(Config.Id, Config.ManaCost))
            {
                _dragging = false;
                SyncVisualFromAnimatedState();
                return;
            }

            _consumed = true;
            onPlayed.Invoke();
            Destroy(gameObject);
        }
    }
}
