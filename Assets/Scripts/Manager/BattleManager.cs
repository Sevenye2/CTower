using System.Collections.Generic;
using CardTower.Config;
using CardTower.Relics;
using CardTower.UI;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public enum PlayCommitResult
{
    Failed,
    Committed,
    Targeting
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            var obj = new GameObject("BattleManager");
            DontDestroyOnLoad(obj);
            _instance = obj.AddComponent<BattleManager>();
            return _instance;
        }
    }

    static BattleManager _instance;


    // ── State ──
    public bool isBattling;

    // ── Timer ──
    public float remainTime { get; private set; }
    public float goldEarned;

    // ── Relics ──
    readonly List<RelicBase> _activeRelics = new();

    // ── Deck ──
    [Header("Deck")]
    [SerializeField] List<string> startingDeckIds = new();
    readonly Queue<string> _drawPile = new();
    readonly List<string> _discardPile = new();
    readonly List<string> _handCardIds = new();
    readonly Queue<string> _pendingDraws = new();

    // ── Draw ──
    [Header("Draw")]
    [Min(0f)]
    [SerializeField] float drawInterval = 2f;
    [Min(0)]
    [SerializeField] int handLimit = 7;
    float _drawElapsed;

    // ── Mana ──
    [Header("Mana")]
    [Min(0)]
    [SerializeField] int playCost = 1;
    [Min(0)]
    [SerializeField] int startingResource = 10;
    int _currentResource;

    // ── Targeting ──
    HandCardView _targetingCardView;

    // ── Accessors ──
    public IReadOnlyList<string> DiscardPile => _discardPile;
    public int CurrentResource => _currentResource;
    public int MaxResource => startingResource;
    public int HandLimit => handLimit;
    public int PlayCost => playCost;
    public bool HasPendingDraw => _pendingDraws.Count > 0;
    public float DrawProgress => Mathf.Clamp01(_drawElapsed / Mathf.Max(0.05f, drawInterval));
    public string DequeuePendingDraw() => _pendingDraws.Dequeue();

    // ════════════════════════════════════════════════════════════
    //  Lifecycle
    // ════════════════════════════════════════════════════════════

    public void Begin()
    {
        var em = World.EntityManager;

        // Tower health from save
        var maxHp = (float)SaveDataManager.instance.data.maxHealth;
        using var towerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<EntityType>());
        using var towers = towerQuery.ToEntityArray(Allocator.Temp);
        using var towerTypes = towerQuery.ToComponentDataArray<EntityType>(Allocator.Temp);

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        for (var i = 0; i < towers.Length; i++)
        {
            if (towerTypes[i].Value != EntityKind.Tower)
                continue;
            var tower = towers[i];
            if (em.HasComponent<Health>(tower))
                ecb.SetComponent(tower, new Health { Current = maxHp, Max = maxHp });
            else
                ecb.AddComponent(tower, new Health { Current = maxHp, Max = maxHp });
        }

        // Restore tower attacks
        var towerPos = FindTowerEntity(em) != Entity.Null
            ? em.GetComponentData<LocalToWorld>(FindTowerEntity(em)).Position
            : float3.zero;
        foreach (var a in SaveDataManager.instance.TowerAttacks)
            if (GameContentRegistry.Instance.TryGetTowerAttack(a.id, out var atk))
            {
                var atkEntity = ecb.CreateEntity();
                ecb.AddComponent(atkEntity, LocalTransform.FromPosition(towerPos));
                ecb.AddComponent<BattleEntity>(atkEntity);
                atk.Apply(ecb, em, atkEntity, a.level);
            }

        // Enemy spawners from level config
        var levelId = Mathf.Max(1, SaveDataManager.instance.data.level);
        var levelCfg = LevelConfigLoader.Load(levelId);
        remainTime = levelCfg.DurationSeconds;

        foreach (var entry in levelCfg.Timeline)
            foreach (var style in entry.Groups)
            {
                var spawnerEntity = ecb.CreateEntity();
                ecb.AddComponent<BattleEntity>(spawnerEntity);
                ecb.AddComponent(spawnerEntity, new SpawnerData
                {
                    EnemyPrefabId = style.EnemyPrefabId,
                    TotalCount = style.Count,
                    BatchSize = style.BatchSize,
                    Interval = style.Interval,
                    SpawnRadius = style.SpawnRadius,
                    StartTime = entry.Time,
                    OverrideHealth = style.OverrideHealth,
                    OverrideSpeed = style.OverrideSpeed,
                    OverrideScale = style.OverrideScale
                });
            }

        ecb.Playback(em);
        ecb.Dispose();

        // Relics
        var towerEntity = FindTowerEntity(em);
        DeactivateRelics();
        ClearTowerBuffs(em, towerEntity);
        ResetTowerModifiers(em, towerEntity);
        RebuildRelicsFromSave(em, towerEntity);
        foreach (var relic in _activeRelics)
            relic.Activate(em, towerEntity);

        // Deck & mana reset
        _currentResource = startingResource;
        _drawElapsed = 0f;
        goldEarned = 0f;
        BuildDrawPile();
        isBattling = true;
    }

    public void End()
    {
        isBattling = false;
        _targetingCardView = null;

        var em = World.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Relics + buffs
        var towerEntity = FindTowerEntity(em);

        // ── OnBattleEnd: tower buffs ──
        if (towerEntity != Entity.Null && em.HasBuffer<BuffInstance>(towerEntity))
        {
            var buffs = em.GetBuffer<BuffInstance>(towerEntity);
            unsafe
            {
                for (int i = 0; i < buffs.Length; i++)
                {
                    ref var buff = ref buffs.ElementAt(i);
                    if (buff.OnBattleEnd.IsCreated)
                    {
                        var e = towerEntity;
                        buff.OnBattleEnd.Invoke(ref e, ref buff);
                    }
                }
            }
        }

        DeactivateRelics();
        ClearTowerBuffs(em, towerEntity);
        ResetTowerModifiers(em, towerEntity);

        // Destroy all battle entities
        foreach (var e in QueryToArray<BattleEntity>(em))
            ecb.DestroyEntity(e);

        ecb.Playback(em);
        ecb.Dispose();

        // Gold & hand cleanup
        SaveDataManager.instance.AddGold((int)goldEarned);
        goldEarned = 0f;
        _handCardIds.Clear();
        _pendingDraws.Clear();

        // Clear hand card UI
        var cardContainer = UIManager.instance?.battleHUD?.cardContainer;
        if (cardContainer != null)
        {
            for (var i = cardContainer.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(cardContainer.transform.GetChild(i).gameObject);
        }
    }

    void Update()
    {
        if (!isBattling) return;

        var em = World.EntityManager;
        var dt = Time.deltaTime;

        remainTime -= dt;
        if (remainTime <= 0f)
        {
            End();
            UIManager.instance.shopUI.OpenUI();
            return;
        }

        var (current, max) = GetTowerHp();


        var hud = UIManager.instance.battleHUD;
        hud.RefreshHp(current, max);
        hud.SetDrawBar(DrawProgress);
        hud.SetMana(_currentResource, MaxResource);
        hud.SetGold(goldEarned);
        hud.SetTime(remainTime);
        hud.SyncHand(this);

        if (current <= 0f)
        {
            UIManager.instance.gameOverUI.Show();
            SaveDataManager.instance.Delete();
            End();
            return;
        }

        if (_drawElapsed < drawInterval)
            _drawElapsed += dt;
        if (_drawElapsed >= drawInterval)
        {
            var result = TryDrawOne();

            if (result)
                _drawElapsed = 0f;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  Deck / Draw / Play
    // ════════════════════════════════════════════════════════════

    void BuildDrawPile()
    {
        _drawPile.Clear();
        _handCardIds.Clear();
        _pendingDraws.Clear();
        _discardPile.Clear();

        var list = new List<string>();
        foreach (var c in SaveDataManager.instance.Cards)
            list.Add(c.id);

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        foreach (var id in list) _drawPile.Enqueue(id);
    }

    bool TryDrawOne()
    {
        if (_handCardIds.Count >= handLimit || _drawPile.Count == 0) return false;

        var cardId = _drawPile.Dequeue();
        _handCardIds.Add(cardId);
        _pendingDraws.Enqueue(cardId);
        return true;
    }

    public PlayCommitResult TryCommitPlay(string cardId, int manaCost, HandCardView cardView)
    {
        var cost = manaCost > 0 ? manaCost : playCost;
        if (_currentResource < cost) return PlayCommitResult.Failed;

        _currentResource -= cost;
        _discardPile.Add(cardId);
        _handCardIds.Remove(cardId);

        PlayEffect(cardId);
        DispatchCardPlayEffects();

        if (cardView.Config != null && cardView.Config.RequiresTargeting)
        {
            _targetingCardView = cardView;
            return PlayCommitResult.Targeting;
        }

        return PlayCommitResult.Committed;
    }

    public void CompleteTargetPlay()
    {
        if (_targetingCardView == null) return;
        _targetingCardView.OnTargetingComplete();
        _targetingCardView = null;
    }

    public void RefundCard(string cardId, int manaCost)
    {
        _currentResource += manaCost;

        if (_targetingCardView != null)
        {
            _targetingCardView.OnTargetingCancel();
            _targetingCardView = null;
            return;
        }

        _discardPile.Remove(cardId);
        _handCardIds.Add(cardId);
        _pendingDraws.Enqueue(cardId);
    }

    void PlayEffect(string cardId)
    {
        if (!GameContentRegistry.Instance.TryGetCardEffect(cardId, out var effect)) return;
        var em = World.EntityManager;
        effect.Play(em, FindTowerEntity(em));
    }

    void DispatchCardPlayEffects()
    {
    }

    // ════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════

    EntityQuery _towerQuery;
    bool _towerQueryCreated;

    (float current, float max) GetTowerHp()
    {
        var em = World.EntityManager;
        var tower = FindTowerEntity(em);
        var hp = em.GetComponentData<Health>(tower);
        return (hp.Current, hp.Max);
    }

    Entity FindTowerEntity(EntityManager em)
    {
        if (!_towerQueryCreated)
        {
            _towerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<EntityType>());
            _towerQueryCreated = true;
        }

        using var towers = _towerQuery.ToEntityArray(Allocator.Temp);
        using var types = _towerQuery.ToComponentDataArray<EntityType>(Allocator.Temp);
        for (var i = 0; i < towers.Length; i++)
            if (types[i].Value == EntityKind.Tower)
                return towers[i];
        return Entity.Null;
    }

    World World => World.DefaultGameObjectInjectionWorld;

    NativeArray<Entity> QueryToArray<T>(EntityManager em) where T : IComponentData
    {
        using var q = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return q.ToEntityArray(Allocator.Temp);
    }

    void RebuildRelicsFromSave(EntityManager em, Entity towerEntity)
    {
        DeactivateRelics();
        _activeRelics.Clear();
        foreach (var relicSave in SaveDataManager.instance.Relics)
        {
            GameContentRegistry.Instance.TryCreateRelic(relicSave.id, out var relic);
            relic ??= new UnknownRelic();
            _activeRelics.Add(relic);
            relic.OnOwned(em, towerEntity);
        }
    }

    void DeactivateRelics()
    {
        foreach (var relic in _activeRelics)
            relic.Deactivate();
    }

    unsafe void ClearTowerBuffs(EntityManager em, Entity towerEntity)
    {
        if (towerEntity == Entity.Null)
            return;
        if (!em.HasBuffer<BuffInstance>(towerEntity))
            return;

        var buffs = em.GetBuffer<BuffInstance>(towerEntity);
        for (var i = 0; i < buffs.Length; i++)
        {
            if (buffs[i].Data != null)
                UnsafeUtility.Free(buffs[i].Data, Allocator.Persistent);
        }
        buffs.Clear();
    }

    void ResetTowerModifiers(EntityManager em, Entity towerEntity)
    {
        if (towerEntity == Entity.Null)
            return;
        if (em.HasComponent<EntityModifiers>(towerEntity))
            em.SetComponentData(towerEntity, EntityModifiers.Identity);
    }
}
