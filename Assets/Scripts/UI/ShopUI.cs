using System.Collections.Generic;
using CardTower.Config;
using CardTower.UI;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    const int MaxAttackSlots = 6;

    [SerializeField] ShopItemView[] itemViews;
    [SerializeField] Text refreshCostText;
    [SerializeField] Text goldText;

    int _refreshCount;
    int Level => SaveDataManager.instance.data.level;
    public void OpenUI()
    {
        GenerateShop();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void GenerateShop()
    {
        var pool = BuildItemPool();
        Shuffle(pool);

        var items = new ShopItemModel[4];

        // Preserve locked items from previous slots
        for (var i = 0; i < 4 && i < itemViews.Length; i++)
        {
            if (itemViews[i].Model != null && itemViews[i].Model.IsLocked)
            {
                items[i] = itemViews[i].Model;
            }
        }

        var poolIdx = 0;
        for (var i = 0; i < 4 && i < itemViews.Length; i++)
        {
            if (items[i] != null)
                continue;

            while (poolIdx < pool.Count && IsAlreadyInSlot(items, pool[poolIdx]))
                poolIdx++;

            if (poolIdx < pool.Count)
                items[i] = pool[poolIdx++];
        }

        for (var i = 0; i < 4 && i < itemViews.Length; i++)
        {
            if (items[i] != null)
                itemViews[i].Setup(items[i], Level, OnBuyItem);
        }

        UpdateGoldDisplay();
        UpdateRefreshCost();
    }

    List<ShopItemModel> BuildItemPool()
    {
        var pool = new List<ShopItemModel>();
        var save = SaveDataManager.instance;
        var registry = GameContentRegistry.Instance;

        foreach (var relic in registry.GetAllRelics())
        {
            if (IsRelicOwned(relic.Config.Id))
                continue;

            pool.Add(new ShopItemModel
            {
                Type = ShopItemType.Relic,
                Id = relic.Config.Id,
                DisplayName = relic.Config.DisplayName,
                Description = relic.Config.Description,
                IconPath = relic.Config.IconPath,
                BasePrice = relic.Config.Price
            });
        }

        foreach (var card in registry.GetAllCards())
        {
            pool.Add(new ShopItemModel
            {
                Type = ShopItemType.Card,
                Id = card.Config.Id,
                DisplayName = card.Config.DisplayName,
                Description = card.Config.Description,
                IconPath = "Art/Cards/" + card.Config.Id,
                BasePrice = card.Config.Price
            });
        }

        foreach (var attack in registry.GetAllTowerAttacks())
        {
            if (!CanBuyOrMergeTowerAttack(attack.Config.Id))
                continue;

            pool.Add(new ShopItemModel
            {
                Type = ShopItemType.TowerAttack,
                Id = attack.Config.Id,
                DisplayName = attack.Config.DisplayName,
                Description = attack.Config.Description,
                IconPath = null,
                BasePrice = attack.Config.Price
            });
        }

        return pool;
    }

    bool IsRelicOwned(string relicId)
    {
        foreach (var r in SaveDataManager.instance.Relics)
        {
            if (r.id == relicId)
                return true;
        }

        return false;
    }

    bool CanBuyOrMergeTowerAttack(string attackId)
    {
        var attacks = SaveDataManager.instance.TowerAttacks;
        if (attacks.Count < MaxAttackSlots)
            return true;

        foreach (var atk in attacks)
        {
            if (atk.id == attackId && atk.level == 1)
                return true;
        }

        return false;
    }

    static bool IsAlreadyInSlot(ShopItemModel[] slots, ShopItemModel item)
    {
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].Id == item.Id && slots[i].Type == item.Type)
                return true;
        }

        return false;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void OnBuyItem(ShopItemModel model)
    {
        var save = SaveDataManager.instance;
        var price = model.GetPrice(Level);

        if (!save.TrySpendGold(price))
            return;

        switch (model.Type)
        {
            case ShopItemType.Relic:
                save.AddRelic(model.Id);
                break;
            case ShopItemType.Card:
                save.AddCard(model.Id);
                break;
            case ShopItemType.TowerAttack:
                BuyTowerAttack(model.Id);
                break;
        }

        UpdateGoldDisplay();
        UpdateRefreshCost();
    }

    void BuyTowerAttack(string id)
    {
        var save = SaveDataManager.instance;
        var attacks = save.TowerAttacks;

        if (attacks.Count < MaxAttackSlots)
        {
            save.AddOrUpgradeTowerAttack(id, 1);
            return;
        }

        for (var i = 0; i < attacks.Count; i++)
        {
            if (attacks[i].id == id && attacks[i].level == 1)
            {
                save.AddOrUpgradeTowerAttack(id, 2);
                return;
            }
        }
    }

    public void OnRefreshClicked()
    {
        var cost = GetRefreshCost();
        var save = SaveDataManager.instance;

        if (!save.TrySpendGold(cost))
            return;

        _refreshCount++;
        GenerateShop();
    }

    public void OnContinueClicked()
    {
        SaveDataManager.instance.IncrementLevel();

        if (BlackUI.instance != null)
        {
            BlackUI.instance.DoFade(1, 1, () =>
            {
                BattleManager.instance.Begin();
                gameObject.SetActive(false);
            });
        }
        else
        {
            BattleManager.instance.Begin();
            gameObject.SetActive(false);
        }
    }

    int GetRefreshCost()
    {
        return 1 + _refreshCount + (Level - 1) * 2;
    }

    void UpdateGoldDisplay()
    {
        if (goldText != null)
            goldText.text = SaveDataManager.instance.GetGold().ToString();
    }

    void UpdateRefreshCost()
    {
        if (refreshCostText != null)
            refreshCostText.text = GetRefreshCost().ToString();
    }
}
