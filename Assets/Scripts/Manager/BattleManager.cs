using System.Collections.Generic;
using CardTower.Config;
using CardTower.Relics;
using CardTower.RuntimeEffects;
using CardTower.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
        using var towerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<TowerTag>());
        using var towers = towerQuery.ToEntityArray(Allocator.Temp);

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var tower in towers)
        {
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
                ecb.AddComponent<BattleTag>(atkEntity);
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
                ecb.AddComponent<BattleTag>(spawnerEntity);
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

        // Runtime effects
        var context = RuntimeEffectManager.Instance.CreateContext(em, FindTowerEntity(em));
        RuntimeEffectManager.Instance.Effects.Clear(context);
        RelicRuntimeManager.Instance.RebuildFromSave(context);
        RelicRuntimeManager.Instance.Relics.CreateAllEffects(context);
        RuntimeEffectManager.Instance.Effects.DispatchBattleStart(context);

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

        var em = World.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Runtime effects
        var context = RuntimeEffectManager.Instance.CreateContext(em, FindTowerEntity(em));
        RuntimeEffectManager.Instance.Effects.DispatchBattleEnd(context);
        RuntimeEffectManager.Instance.Effects.Clear(context);

        // Destroy all battle entities
        foreach (var e in QueryToArray<BattleTag>(em))
            ecb.DestroyEntity(e);

        ecb.Playback(em);
        ecb.Dispose();

        // Gold & hand cleanup
        SaveDataManager.instance.AddGold((int)goldEarned);
        goldEarned = 0f;
        _handCardIds.Clear();
        _pendingDraws.Clear();
    }

    void Update()
    {
        if (!isBattling) return;

        var em = World.EntityManager;
        var dt = Time.deltaTime;
        var context = RuntimeEffectManager.Instance.CreateContext(em, FindTowerEntity(em));

        RuntimeEffectManager.Instance.Effects.Update(context, dt);

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

        var list = new List<string>(startingDeckIds);
        list.Add("meteor_strike");
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

    public bool TryCommitPlay(string cardId, int manaCost)
    {
        var cost = manaCost > 0 ? manaCost : playCost;
        if (_currentResource < cost) return false;

        _currentResource -= cost;
        _discardPile.Add(cardId);
        _handCardIds.Remove(cardId);

        PlayEffect(cardId);
        DispatchCardPlayEffects();
        return true;
    }

    public void RefundCard(string cardId, int manaCost)
    {
        _currentResource += manaCost;
        _discardPile.Remove(cardId);
        _handCardIds.Add(cardId);
        _pendingDraws.Enqueue(cardId);
    }

    void PlayEffect(string cardId)
    {
        if (!GameContentRegistry.Instance.TryGetCardEffect(cardId, out var effect)) return;
        effect.Play(RuntimeEffectManager.Instance.CreateContext(World.EntityManager));
    }

    void DispatchCardPlayEffects()
    {
        RuntimeEffectManager.Instance.Effects.DispatchCardPlay(
            RuntimeEffectManager.Instance.CreateContext(World.EntityManager));
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
            _towerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<TowerTag>());
            _towerQueryCreated = true;
        }

        using var towers = _towerQuery.ToEntityArray(Allocator.Temp);
        return towers.Length > 0 ? towers[0] : Entity.Null;
    }

    World World => World.DefaultGameObjectInjectionWorld;

    NativeArray<Entity> QueryToArray<T>(EntityManager em) where T : IComponentData
    {
        using var q = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return q.ToEntityArray(Allocator.Temp);
    }
}
