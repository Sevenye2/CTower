using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


public class SaveDataManager
{
    [Serializable]
    public struct TowerAttackSaveData
    {
        public string id;
        public int level;
    }

    [Serializable]
    public struct RelicSaveData
    {
        public string id;
    }

    [Serializable]
    public struct CardSaveData
    {
        public string id;
        public int count;
    }

    [Serializable]
    public struct ShopSlotSaveData
    {
        public string type;
        public string id;
        public bool isLocked;
    }

    [Serializable]
    public struct SaveData
    {
        public bool isBattling;
        public int seed;
        public int level;
        public int maxHealth;
        public int gold;
        public List<TowerAttackSaveData> towerAttacks;
        public List<RelicSaveData> relics;
        public List<CardSaveData> cards;
        public List<ShopSlotSaveData> shopSlots;
        public int shopRefreshCount;
    }


    public static SaveDataManager instance
    {
        get
        {
            _instance ??= new SaveDataManager();
            return _instance;
        }
    }

    private static SaveDataManager _instance;

    public SaveData data ;
    public bool isPlaying { get; private set; }

    private string DataPath =>
        CardTower.UI.GameSavePaths.AbsolutePath;

    private SaveDataManager()
    {
        if (!File.Exists(DataPath)) return;
        data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(DataPath));
        EnsureListData();
        isPlaying = true;
    }

    public IReadOnlyList<TowerAttackSaveData> TowerAttacks => data.towerAttacks;
    public IReadOnlyList<RelicSaveData> Relics => data.relics;
    public IReadOnlyList<CardSaveData> Cards => data.cards;

    public void Save()
    {
        EnsureListData();
        Debug.Log($"Saving...:{DataPath}");
        File.WriteAllText(DataPath, JsonConvert.SerializeObject(data, Formatting.Indented));
    }

    public void New()
    {
        data = CreateDefaultSaveData();
        Save();
        isPlaying = true;
    }

    public void Delete()
    {
        if (File.Exists(DataPath))
            File.Delete(DataPath);
        isPlaying = false;
    }

    public void AddOrUpgradeTowerAttack(string id, int levelDelta = 1)
    {
        EnsureListData();
        var attacks = data.towerAttacks;
        for (var i = 0; i < attacks.Count; i++)
        {
            var attack = attacks[i];
            if (attack.id != id)
                continue;

            attack.level = Math.Max(1, attack.level + levelDelta);
            attacks[i] = attack;
            Save();
            return;
        }

        attacks.Add(new TowerAttackSaveData
        {
            id = id,
            level = Math.Max(1, levelDelta)
        });
        Save();
    }

    public void AddRelic(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        EnsureListData();
        var relics = data.relics;
        for (var i = 0; i < relics.Count; i++)
        {
            if (relics[i].id == id)
                return;
        }

        relics.Add(new RelicSaveData
        {
            id = id
        });
        Save();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        EnsureData();
        data.gold += amount;
        Save();
    }

    public bool TrySpendGold(int amount)
    {
        EnsureData();
        if (data.gold < amount)
            return false;

        data.gold -= amount;
        Save();
        return true;
    }

    public void IncrementLevel()
    {
        EnsureData();
        data.level++;
        Save();
    }

    public int GetGold()
    {
        EnsureData();
        return data.gold;
    }

    void EnsureData()
    {
        if (data.towerAttacks == null)
            EnsureListData();
    }

    public void AddCard(string id, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(id) || count <= 0)
            return;

        EnsureListData();
        var cards = data.cards;
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card.id != id)
                continue;

            card.count += count;
            cards[i] = card;
            Save();
            return;
        }

        cards.Add(new CardSaveData
        {
            id = id,
            count = count
        });
        Save();
    }

    static SaveData CreateDefaultSaveData()
    {
        return new SaveData
        {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
            level = 1,
            maxHealth = 500,
            towerAttacks = new List<TowerAttackSaveData>
            {
                new TowerAttackSaveData
                {
                    id = "projectile",
                    level = 1
                }
            },
            relics = new List<RelicSaveData>(),
            cards = new List<CardSaveData>
            {
                new CardSaveData { id = "meteor_strike", count = 1 }
            },
            shopSlots = new List<ShopSlotSaveData>()
        };
    }

    void EnsureListData()
    {
        data.towerAttacks ??= new List<TowerAttackSaveData>();
        data.relics ??= new List<RelicSaveData>();
        data.cards ??= new List<CardSaveData>();
        data.shopSlots ??= new List<ShopSlotSaveData>();
    }
}
