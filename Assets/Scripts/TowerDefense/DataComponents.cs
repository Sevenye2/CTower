using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// ── Buff/Debuff type constants ──
public static class BuffType
{
    public const int Slow = 0;
    public const int AttackSpeed = 1;
    public const int GoldRush = 2;
    public const int DoubleDamage = 3;
    public const int LifeSpring = 4;
    public const int TowerShield = 5;
    public const int Count = 6;
}

public static class GameConst
{
    public const float GroundY = 0f;
}

// ── Entity type constants ──
public static class EntityKind
{
    public const byte Tower = 0;
    public const byte Enemy = 1;
    public const byte Bullet = 2;
    public const byte EnemyBullet = 3;
    public const byte HealthBar = 4;
    public const byte Barrier = 5;
}

/// <summary>统一实体类型标识，替换TowerTag/EnemyTag/BarrierTag/等。</summary>
public struct EntityType : IComponentData
{
    public byte Value;
}

public struct PlayerTag : IComponentData
{
    
}

/// <summary>眩晕状态（禁止移动/攻击/目标分配），有时长。</summary>
public struct Stunned : IComponentData
{
    public float Duration;
}

/// <summary>战斗期间创建的临时实体，结束时统一销毁。</summary>
public struct BattleEntity : IComponentData
{
}

public struct Health : IComponentData
{
    public float Current;
    public float Max;
    public float Shield;
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
    public bool IsRanged;
    public float BulletDuration;
    public float BulletMaxHeight;
}

public struct EnemyAttackState : IComponentData
{
    public float NextHitTime;
    public float LastHitTime;
}

public struct CurrentTarget : IComponentData
{
    public Entity Value;
    public float Distance;
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

public struct Bullet : IComponentData
{
    public float3 Velocity;
    public float Damage;
    public Entity Source;
}

/// <summary>聚合在实体上的效果修正（塔与敌人共用）。</summary>
public struct EntityModifiers : IComponentData
{
    public float DamageDealt;
    public float DamageTaken;
    public float AttackSpeed;
    public float AttackRange;
    public float Speed;
    public float GoldMultiplier;

    public static EntityModifiers Identity => new()
    {
        DamageDealt = 1f,
        DamageTaken = 1f,
        AttackSpeed = 1f,
        AttackRange = 1f,
        Speed = 1f,
        GoldMultiplier = 1f
    };
}



/// <summary>敌人抛物子弹的飞行数据。</summary>
public struct EnemyBullet : IComponentData
{
    public float3 StartPos;
    public float3 TargetPos;
    public float StartTime;
    public float Duration;
    public float MaxHeight;
    public float Damage;
    public Entity Target;
}
