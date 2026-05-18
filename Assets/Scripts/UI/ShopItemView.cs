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
        [SerializeField] Text lockButtonText;
        [SerializeField] Text buyButtonText;

        static readonly Color AffordColor = new Color(0.2f, 0.2f, 0.2f);
        static readonly Color CantAffordColor = new Color(0.5f, 0.5f, 0.5f);

        public ShopItemModel Model { get; private set; }

        System.Action<ShopItemModel> _onBuy;
        int _level;

        public void Setup(ShopItemModel model, int level, System.Action<ShopItemModel> onBuy)
        {
            Model = model;
            _onBuy = onBuy;
            _level = level;

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
            {
                buyButtonText.text = "Buy|" + price;
                buyButtonText.color = SaveDataManager.instance.GetGold() >= price ? AffordColor : CantAffordColor;
            }

            UpdateLockVisual();
        }

        public void RefreshAffordability()
        {
            if (Model == null || buyButtonText == null)
                return;

            buyButtonText.color = SaveDataManager.instance.GetGold() >= Model.GetPrice(_level)
                ? AffordColor
                : CantAffordColor;
        }

        public void RefreshPrice(int level)
        {
            if (Model == null || buyButtonText == null)
                return;

            _level = level;
            buyButtonText.text = "Buy|" + Model.GetPrice(level);
        }

        public void OnLockClicked()
        {
            if (Model == null)
                return;

            Model.IsLocked = !Model.IsLocked;
            UpdateLockVisual();

            var shop = GetComponentInParent<ShopUI>();
            if (shop != null)
                shop.SaveSlots();
        }

        public void OnBuyClicked()
        {
            if (Model == null)
                return;

            if (SaveDataManager.instance.GetGold() < Model.GetPrice(_level))
                return;

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
