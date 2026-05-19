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
    List<ShopItemModel> _pool;
    int Level => SaveDataManager.instance.data.level;
    public void OpenUI()
    {
        _refreshCount = SaveDataManager.instance.data.shopRefreshCount;

        GenerateShop();
        UpdateGoldDisplay();
        UpdateRefreshCost();

        SaveDataManager.instance.data.isBattling = false;
        SaveDataManager.instance.Save();
        gameObject.SetActive(true);
    }

    public void Close()
    {

        SaveDataManager.instance.data.isBattling = true;
        SaveDataManager.instance.Save();
        gameObject.SetActive(false);
    }

    void GenerateShop()
    {
        var save = SaveDataManager.instance;
        var seed = save.data.seed + Level * 997 + _refreshCount * 7919;
        _pool = BuildItemPool();
        Random.InitState(seed);
        Shuffle(_pool);

        var items = new ShopItemModel[4];

        // Restore locked items from save data
        LoadSlots(items, _pool);

        var poolIdx = 0;
        for (var i = 0; i < 4 && i < itemViews.Length; i++)
        {
            if (items[i] != null)
                continue;

            while (poolIdx < _pool.Count && IsAlreadyInSlot(items, _pool[poolIdx]))
                poolIdx++;

            if (poolIdx < _pool.Count)
                items[i] = _pool[poolIdx++];
        }

        for (var i = 0; i < 4 && i < itemViews.Length; i++)
        {
            if (items[i] != null)
                itemViews[i].Setup(items[i], Level, OnBuyItem);
        }

        // Persist current lock state
        SaveSlots();
    }

    void LoadSlots(ShopItemModel[] items, List<ShopItemModel> pool)
    {
        var saved = SaveDataManager.instance.data.shopSlots;
        if (saved == null || saved.Count == 0)
            return;

        for (var i = 0; i < saved.Count && i < items.Length; i++)
        {
            var entry = saved[i];
            if (string.IsNullOrEmpty(entry.id))
                continue;

            for (var p = 0; p < pool.Count; p++)
            {
                if (pool[p].Id == entry.id && pool[p].Type.ToString() == entry.type)
                {
                    pool[p].IsLocked = entry.isLocked;
                    items[i] = pool[p];
                    break;
                }
            }
        }
    }

    public void SaveSlots()
    {
        var save = SaveDataManager.instance;
        save.data.shopSlots.Clear();

        for (var i = 0; i < itemViews.Length; i++)
        {
            var model = itemViews[i].Model;
            save.data.shopSlots.Add(new SaveDataManager.ShopSlotSaveData
            {
                type = model?.Type.ToString() ?? "",
                id = model?.Id ?? "",
                isLocked = model?.IsLocked ?? false
            });
        }

        save.Save();
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

        Debug.Log($"Shop pool generated with {pool.Count} items.");

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
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void OnBuyItem(ShopItemModel model)
    {
        var save = SaveDataManager.instance;
        var price = model.GetPrice(Level);

        if (!save.TrySpendGold(price))
            return;

        // Find the slot that holds this model
        var slot = -1;
        for (var i = 0; i < itemViews.Length; i++)
        {
            if (itemViews[i].Model == model)
            {
                slot = i;
                break;
            }
        }

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

        // Refill the slot with next available item from pool
        if (slot >= 0 && _pool != null)
        {
            for (var p = 0; p < _pool.Count; p++)
            {
                if (_pool[p].Id == model.Id && _pool[p].Type == model.Type)
                    continue; // skip the bought item itself

                if (IsAlreadyInSlotViews(_pool[p]))
                    continue;

                itemViews[slot].Setup(_pool[p], Level, OnBuyItem);
                break;
            }
        }

        SaveSlots();
        UpdateGoldDisplay();
        UpdateRefreshCost();
        RefreshAllAffordability();
    }

    bool IsAlreadyInSlotViews(ShopItemModel item)
    {
        for (var i = 0; i < itemViews.Length; i++)
        {
            var m = itemViews[i].Model;
            if (m != null && m.Id == item.Id && m.Type == item.Type)
                return true;
        }

        return false;
    }

    void RefreshAllAffordability()
    {
        foreach (var v in itemViews)
            v.RefreshAffordability();
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
        {
            UpdateGoldDisplay();
            return;
        }

        _refreshCount++;
        save.data.shopRefreshCount = _refreshCount;
        save.data.shopSlots.Clear();
        save.Save();
        GenerateShop();
        UpdateGoldDisplay();
        UpdateRefreshCost();
    }

    public void OnContinueClicked()
    {
        var save = SaveDataManager.instance;
        save.IncrementLevel();
        save.data.shopRefreshCount = 0;

        // Keep only locked slots across levels
        var slots = save.data.shopSlots;
        for (var i = slots.Count - 1; i >= 0; i--)
        {
            if (!slots[i].isLocked)
                slots.RemoveAt(i);
        }

        UIManager.instance.blackUI.DoFade(1, 1, () =>
        {

            BattleManager.instance.Begin();
            Close();
        });
    }

    int GetRefreshCost()
    {
        return 1 + _refreshCount + (Level - 1) * 2;
    }

    void UpdateGoldDisplay()
    {
        if (goldText != null)
            goldText.text = "金币 " + SaveDataManager.instance.GetGold().ToString();
    }

    void UpdateRefreshCost()
    {
        if (refreshCostText != null)
            refreshCostText.text = "刷新 " + GetRefreshCost().ToString();
    }
}
