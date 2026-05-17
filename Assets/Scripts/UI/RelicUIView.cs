using CardTower.Relics;
using UnityEngine;
using UnityEngine.UI;

namespace CardTower.UI
{
    [DisallowMultipleComponent]
    public class RelicUIView : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] Text nameText;
        [SerializeField] Text levelText;
        [SerializeField] Text descriptionText;

        public void Setup(RelicBase relic)
        {
            var cfg = relic?.Config;

            if (iconImage != null)
            {
                var icon = !string.IsNullOrEmpty(cfg?.IconPath)
                    ? Resources.Load<Sprite>(cfg.IconPath)
                    : null;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
                nameText.text = cfg != null ? cfg.DisplayName : "Unknown Relic";

            if (levelText != null)
                levelText.gameObject.SetActive(false);

            if (descriptionText != null)
                descriptionText.text = cfg != null ? cfg.Description : string.Empty;
        }
    }
}
