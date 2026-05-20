using CardTower.Cards;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardTower.UI
{
    public enum CardState
    {
        Entering,
        Idle,
        Targeting,
        Playing
    }

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
        int _handIndex;

        Vector3 _restLocalScale = Vector3.one;
        bool _hovered;
        bool _dragging;
        bool _consumed;
        Vector2 _dragStartAnchored;
        float _canvasScaleFactor = 1f;

        CardState _state = CardState.Idle;
        float _stateTimer;
        Vector2 _enterFromPosition;
        float _restY;
        bool _restYInitialized;

        float _animatedLift;
        float _animatedScaleMul = 1f;
        Vector2 _currentAnimatedPosition;

        public CardConfig Config => _config;
        public bool IsConsumed => _consumed;
        public int HandIndex { get => _handIndex; set => _handIndex = value; }

        bool CanActivate => !_consumed && _state == CardState.Idle;

        void Awake()
        {
            ConfigureFaceRaycastTargets();
            ApplyFrameColor();
            _restLocalScale = Rect.localScale;
            _currentAnimatedPosition = Rect.anchoredPosition;
            CacheRestY();
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
        }

        void Update()
        {
            var hand = _hand != null ? _hand : CacheHand();

            if (_state == CardState.Playing)
            {
                UpdatePlaying(hand);
                return;
            }

            if (_consumed) return;

            switch (_state)
            {
                case CardState.Entering:
                    UpdateEntering(hand);
                    break;
                case CardState.Idle:
                    UpdateIdle(hand);
                    break;
                case CardState.Targeting:
                    break;
            }
        }

        void UpdateEntering(CardHandRoot hand)
        {
            _stateTimer += Time.unscaledDeltaTime;
            var dur = hand.EnterDuration;
            var t = dur > 0f ? Mathf.Clamp01(_stateTimer / dur) : 1f;

            var restPos = ComputeRestPosition();
            var targetPos = Vector2.Lerp(_enterFromPosition, restPos, EaseOutQuad(t));
            var scale = Mathf.Lerp(0.3f, 1f, EaseOutQuad(t));
            var angle = Mathf.Lerp(20f, 0f, t);

            Rect.anchoredPosition = targetPos;
            Rect.localScale = _restLocalScale * scale;
            Rect.localEulerAngles = new Vector3(0f, 0f, angle);

            _currentAnimatedPosition = targetPos;

            if (t >= 1f)
            {
                _state = CardState.Idle;
                Rect.localEulerAngles = Vector3.zero;
                _animatedScaleMul = 1f;
                Rect.localScale = _restLocalScale;
            }
        }

        void UpdateIdle(CardHandRoot hand)
        {
            var restPos = ComputeRestPosition();
            var spread = ComputeHoverSpreadOffset();

            var targetLift = _hovered && !_dragging ? hand.HoverLiftPixels : 0f;
            var targetScale = _hovered && !_dragging ? hand.HoverScale : 1f;

            var dt = Time.unscaledDeltaTime;
            _animatedLift = Mathf.MoveTowards(_animatedLift, targetLift, hand.HoverLiftPixelsPerSecond * dt);
            _animatedScaleMul = Mathf.MoveTowards(_animatedScaleMul, targetScale, hand.HoverScaleUnitsPerSecond * dt);

            var targetPos = restPos + spread;
            _currentAnimatedPosition = Vector2.Lerp(_currentAnimatedPosition, targetPos, 12f * dt);

            if (!_dragging)
            {
                Rect.anchoredPosition = _currentAnimatedPosition + new Vector2(0f, _animatedLift);
                Rect.localScale = Vector3.Scale(_restLocalScale, Vector3.one * _animatedScaleMul);
            }
        }

        void UpdatePlaying(CardHandRoot hand)
        {
            _stateTimer += Time.unscaledDeltaTime;
            var dur = hand.PlayDuration;
            var t = dur > 0f ? Mathf.Clamp01(_stateTimer / dur) : 1f;

            var startPos = _currentAnimatedPosition;
            var targetPos = hand.PlayTargetPosition;
            var pos = Vector2.Lerp(startPos, targetPos, EaseInQuad(t));
            var scale = Mathf.Lerp(1f, 0f, t);
            var angle = Mathf.Lerp(0f, 15f, t);

            Rect.anchoredPosition = pos;
            Rect.localScale = _restLocalScale * scale;
            Rect.localEulerAngles = new Vector3(0f, 0f, angle);

            if (t >= 1f)
                Destroy(gameObject);
        }

        Vector2 ComputeRestPosition()
        {
            var hand = _hand != null ? _hand : CacheHand();
            var count = hand.HandCardCount;
            var spacing = hand.SpacingPixels;
            var totalWidth = spacing * Mathf.Max(0, count - 1);
            var startX = -totalWidth * 0.5f;
            return new Vector2(startX + _handIndex * spacing, _restY);
        }

        Vector2 ComputeHoverSpreadOffset()
        {
            var hand = _hand != null ? _hand : CacheHand();
            var hovered = hand.HoveredIndex;
            if (hovered == null || _handIndex == hovered.Value)
                return Vector2.zero;

            var diff = _handIndex - hovered.Value;
            var direction = Mathf.Sign(diff);
            var distance = Mathf.Abs(diff);
            var spread = hand.HoverSpreadPixels * direction / distance;
            return new Vector2(spread, 0f);
        }

        public void BeginEnter(Vector2 fromPosition)
        {
            CacheRestY();
            _state = CardState.Entering;
            _stateTimer = 0f;
            _enterFromPosition = fromPosition;
            Rect.anchoredPosition = fromPosition;
            _currentAnimatedPosition = fromPosition;
            Rect.localScale = _restLocalScale * 0.3f;
            Rect.localEulerAngles = new Vector3(0f, 0f, 20f);
        }

        public void SnapToRestPosition()
        {
            CacheRestY();
            var pos = ComputeRestPosition();
            _currentAnimatedPosition = pos;
            Rect.anchoredPosition = pos;
            _state = CardState.Idle;
        }

        public void Setup(CardConfig config)
        {
            _config = config;
            ApplyConfigToFace();
        }

        void CacheRestY()
        {
            if (!_restYInitialized)
            {
                _restY = Rect.anchoredPosition.y;
                _restYInitialized = true;
            }
        }

        CardHandRoot CacheHand()
        {
            _hand = GetComponentInParent<CardHandRoot>();
            return _hand;
        }

        void CacheCanvasScale()
        {
            var canvas = overrideCanvas != null ? overrideCanvas : GetComponentInParent<Canvas>();
            _canvasScaleFactor = canvas != null ? canvas.scaleFactor : 1f;
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

        // --- Easing helpers ---

        static float EaseOutQuad(float t) => t * (2f - t);

        static float EaseInQuad(float t) => t * t;

        // --- Pointer handlers ---

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_consumed) return;
            _hovered = true;
            var hand = _hand != null ? _hand : CacheHand();
            hand.HoveredIndex = _handIndex;
            if (!_dragging)
                transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_consumed) return;
            _hovered = false;
            var hand = _hand != null ? _hand : CacheHand();
            if (hand.HoveredIndex == _handIndex)
                hand.HoveredIndex = null;
            transform.SetSiblingIndex(_handIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanActivate) return;
            TryPlayCard();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanActivate) return;
            CacheCanvasScale();
            _dragging = true;
            _dragStartAnchored = Rect.anchoredPosition;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!CanActivate) return;
            Rect.anchoredPosition += eventData.delta / _canvasScaleFactor;
            _currentAnimatedPosition = Rect.anchoredPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!CanActivate) return;

            _dragging = false;

            var hand = _hand != null ? _hand : CacheHand();
            var upwardDelta = Rect.anchoredPosition.y - _dragStartAnchored.y;
            if (upwardDelta >= hand.PlayDragThresholdPixels)
            {
                TryPlayCard();
                return;
            }

            _currentAnimatedPosition = Rect.anchoredPosition;
            transform.SetSiblingIndex(_handIndex);
        }

        void TryPlayCard()
        {
            var bm = BattleManager.instance;
            if (bm == null)
            {
                _dragging = false;
                _currentAnimatedPosition = Rect.anchoredPosition;
                return;
            }

            var result = bm.TryCommitPlay(Config.Id, Config.ManaCost, this);
            switch (result)
            {
                case PlayCommitResult.Failed:
                    _dragging = false;
                    _currentAnimatedPosition = Rect.anchoredPosition;
                    break;
                case PlayCommitResult.Committed:
                    BeginPlay();
                    break;
                case PlayCommitResult.Targeting:
                    _state = CardState.Targeting;
                    break;
            }
        }

        void BeginPlay()
        {
            _consumed = true;
            _state = CardState.Playing;
            _stateTimer = 0f;
            _currentAnimatedPosition = Rect.anchoredPosition;
            onPlayed.Invoke();
        }

        public void OnTargetingComplete()
        {
            BeginPlay();
        }

        public void OnTargetingCancel()
        {
            _state = CardState.Idle;
            _hovered = false;
            _dragging = false;
            _animatedLift = 0f;
            _animatedScaleMul = 1f;
            Rect.localScale = _restLocalScale;
            var hand = _hand ?? CacheHand();
            if (hand.HoveredIndex == _handIndex)
                hand.HoveredIndex = null;
            transform.SetSiblingIndex(_handIndex);
        }
    }
}
