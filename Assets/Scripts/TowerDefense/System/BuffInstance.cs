using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ── Context / Event structs ──

/// <summary>OnTick 上下文：仅自我状态，零拷贝。</summary>
public unsafe struct TickContext
{
    public float DT;
    public EntityModifiers* Modifiers;
    public Health* Health;
}

/// <summary>伤害事件上下文。</summary>
public unsafe struct DamageContext
{
    public Entity Source;
    public Entity Target;
    public float Amount;
    public float3 SourcePosition;
    public float3 TargetPosition;
    public EntityCommandBuffer ECB;
}

/// <summary>死亡事件上下文，携带 AOE 查询能力。</summary>
public unsafe struct DeathContext
{
    public Entity Entity;
    public float3 Position;
    public EntityCommandBuffer ECB;
    public NativeArray<Entity> AllEnemies;
    public ComponentLookup<LocalTransform> TransformLookup;
    public BufferLookup<BuffInstance> BuffLookup;
}

/// <summary>击杀事件上下文。</summary>
public unsafe struct KillContext
{
    public Entity Killed;
    public float3 KilledPosition;
    public EntityCommandBuffer ECB;
}

// ── Delegates ──

public delegate void OnTick(ref BuffInstance self, ref TickContext ctx);
public delegate void OnEnd(ref BuffInstance self);
public delegate void OnDeath(ref DeathContext ctx, ref BuffInstance self);
public delegate void OnTakeDamage(ref DamageContext ctx, ref BuffInstance self);
public delegate void OnDealDamage(ref DamageContext ctx, ref BuffInstance self);
public delegate void OnKill(ref KillContext ctx, ref BuffInstance self);
public unsafe delegate void OnBattleEnd(ref Entity entity, ref BuffInstance self);

// ── BuffInstance ──

/// <summary>活跃的 Buff 实例，挂在实体 DynamicBuffer 上。</summary>
[ChunkSerializable]
public unsafe struct BuffInstance : IBufferElementData
{
    [MarshalAs(UnmanagedType.U1)] public bool IsExpired;

    /// <summary>自定义数据指针，由各 buff 自行分配释放。</summary>
    public void* Data;

    public FunctionPointer<OnTick> OnTick;
    public FunctionPointer<OnEnd> OnEnd;
    public FunctionPointer<OnDeath> OnDeath;
    public FunctionPointer<OnTakeDamage> OnTakeDamage;
    public FunctionPointer<OnDealDamage> OnDealDamage;
    public FunctionPointer<OnKill> OnKill;
    public FunctionPointer<OnBattleEnd> OnBattleEnd;
}
