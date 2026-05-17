using Unity.Entities;
using Unity.Mathematics;

public struct TowerTag : IComponentData
{
}

public struct EnemyTag : IComponentData
{
}

/// <summary>卡牌等效果需要暂时禁止敌人朝塔推进与攻击时使用。</summary>
public struct SuctionStunTag : IComponentData
{
}

public struct Health : IComponentData
{
    public float Current;
    public float Max;
}

public struct BattleTag : IComponentData
{
}

public struct ProjectileTowerAttack : IComponentData
{
    public float AttackRange;
    public float DamagePerShot;
    public float ShotsPerSecond;
    public float NextShotTime;
}

public struct MoveSpeed : IComponentData
{
    public float Value;
}

public struct EnemyAttackConfig : IComponentData
{
    public float AttackRange;
    public float DamagePerHit;
    public float HitIntervalSeconds;
}

public struct EnemyAttackState : IComponentData
{
    public float NextHitTime;
}

public struct SpawnerData : IComponentData
{
    public int EnemyPrefabId;
    public int TotalCount;
    public int SpawnedCount;
    public int BatchSize;
    public float Interval;
    public float SpawnRadius;
    public float StartTime;
    public float Timer;
    public float3 FixedPosition;
    public float OverrideHealth;
    public float OverrideSpeed;
    public float OverrideScale;
    public bool IsActive;
}


public struct BulletTag : IComponentData
{
}

public struct Bullet : IComponentData
{
    public float3 Velocity;
    public float Damage;
}

public struct HealthBarTag : IComponentData
{
}


