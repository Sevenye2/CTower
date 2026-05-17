using UnityEngine;
using UnityEngine.UI;

namespace CardTower.UI
{
    public enum ShopItemType { Relic, Card, TowerAttack }

    public class ShopItemModel
    {
        public ShopItemType Type;
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public int BasePrice;
        public bool IsLocked;

        public int GetPrice(int level)
        {
            return Mathf.RoundToInt(BasePrice * (1f + (level - 1) * 0.2f));
        }
    }

    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [SerializeField] Image iconImage;
        [SerializeField] Text descriptionText;
        [SerializeField] Button lockButton;
        [SerializeField] Text lockButtonText;
        [SerializeField] Button buyButton;
        [SerializeField] Text buyButtonText;

        public ShopItemModel Model { get; private set; }

        System.Action<ShopItemModel> _onBuy;

        public void Setup(ShopItemModel model, int level, System.Action<ShopItemModel> onBuy)
        {
            Model = model;
            _onBuy = onBuy;

            if (nameText != null)
                nameText.text = model.DisplayName;

            if (iconImage != null)
            {
                var icon = !string.IsNullOrEmpty(model.IconPath)
                    ? Resources.Load<Sprite>(model.IconPath)
                    : null;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (descriptionText != null)
                descriptionText.text = BuildDescription(model);

            var price = model.GetPrice(level);
            if (buyButtonText != null)
                buyButtonText.text = "Buy|" + price;

            UpdateLockVisual();
        }

        public void RefreshPrice(int level)
        {
            if (Model == null || buyButtonText == null)
                return;

            buyButtonText.text = "Buy|" + Model.GetPrice(level);
        }

        public void OnLockClicked()
        {
            if (Model == null)
                return;

            Model.IsLocked = !Model.IsLocked;
            UpdateLockVisual();
        }

        public void OnBuyClicked()
        {
            _onBuy?.Invoke(Model);
        }

        void UpdateLockVisual()
        {
            if (lockButtonText == null)
                return;

            lockButtonText.text = Model != null && Model.IsLocked ? "Unlock" : "Lock";
        }

        static string BuildDescription(ShopItemModel model)
        {
            var tag = model.Type switch
            {
                ShopItemType.Card => "【卡片】",
                ShopItemType.Relic => "【遗物】",
                ShopItemType.TowerAttack => "【攻击】",
                _ => ""
            };

            return tag + "\n\n" + model.Description;
        }
    }
}
