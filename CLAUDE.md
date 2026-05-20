该项目是一个 Unity DOTS (ECS) 塔防 + 卡牌游戏。

## 核心原则

- 尽量使用通用组件，避免在特定类中对其他特定类做单独判断。
- Buff 的行为通过函数指针承载，不在 System 中按 Type 做 switch。
- 如无必要，不在方法中添加过多参数。
- 如无必要，不做过多的判空。信任内部代码和框架保证。
- 尽量发挥Dots的优势，使用并行的方式进行编写。
- 提炼逻辑相同的方法，尽量复用可复用的方法。
- 表现层不使用程序化方式生成物体，使用预制体代替。同时提醒用户配置。

## 坐标约定

- 塔位置为 **(0, 0, 0)**。
- 地面高度为 **0**。

## 架构分层

### ECS 层 (`Assets/Scripts/TowerDefense/System/`)

| System | 职责 |
|--------|------|
| `BuffProcessSystem` | 每帧遍历实体 `DynamicBuffer<BuffInstance>`，调用 `OnTick`，移除 `IsExpired` |
| `BulletHitSystem` | 子弹命中检测 + 伤害结算 + 分发 `OnTakeDamage` / `OnDealDamage` / `OnDeath` / `OnKill` |
| `TowerAttackSystem` | 塔攻击逻辑，生成子弹（写入 `Bullet.Source = towerEntity`） |
| `EnemyMoveSystem` | 敌人移动 |
| `EnemyAttackSystem` | 敌人攻击塔（近战/远程） |
| `EnemySpawnSystem` | 敌人生成 |
| `HealthBarUpdateSystem` | **仅**销毁死亡实体 + 更新血条 + 追加金币。不做事件分发 |
| `TargetAssignSystem` | 索敌分配 |
| `BulletMoveSystem` | 子弹移动 |
| `EnemyBulletSystem` | 敌人子弹飞行 |

### Buff 系统 (`BuffInstance`)

```cs
// 7 个触发器，定义在 BuffInstance.cs
OnTick       // 逐帧，BuffProcessSystem 分发
OnEnd        // 移除时清理内存
OnDeath      // 本实体死亡，BulletHitSystem 分发（带 AllEnemies 查询能力）
OnTakeDamage // 受到伤害，BulletHitSystem 分发
OnDealDamage // 造成伤害，BulletHitSystem 分发
OnKill       // 击杀实体，BulletHitSystem 分发
OnBattleEnd  // 战斗结束，BattleManager.End() 分发
```

**分发原则**：事件在伤害结算点立即分发，而非在 HealthBarUpdateSystem 延迟分发。

**内存管理**：Buff 通过 `UnsafeUtility.Malloc(Allocator.Persistent)` 分配自定义数据（`void* Data`），在 `OnEnd` 中 `UnsafeUtility.Free`。战斗结束时 `BattleManager.ClearTowerBuffs` 统一清理残留。

### Card 层 (`Assets/Scripts/Cards/`)

- `CardBase` — 抽象基类，`Play(EntityManager, Entity)` 为入口
- 每个 Card 负责往 tower 的 `DynamicBuffer<BuffInstance>` 添加 buff
- 无持续时间的卡片（如 `SlowFieldCard`）直接操作 ECS 实体
- `GroundTargetingHelper` — 地面选择通用辅助

### Relic 层 (`Assets/Scripts/Relics/`)

- `RelicBase` — 抽象基类，`Activate(EntityManager, Entity)` / `Deactivate()` 为入口
- BattleManager 内联管理 `List<RelicBase> _activeRelics`
- 已删除 `RelicRuntimeManager` 和 `RelicRuntimeContainer`

### 战斗流程

```
BattleManager.Begin()
  → DeactivateRelics() + ClearTowerBuffs() + ResetTowerModifiers()
  → RebuildRelicsFromSave() → 逐个 Activate()
  → isBattling = true

每帧 Update()
  → ECS Systems 运行（按 UpdateBefore/After 排序）
  → UI 刷新

BattleManager.End()
  → isBattling = false
  → 分发 OnBattleEnd 到 tower buffs
  → DeactivateRelics() + ClearTowerBuffs() + ResetTowerModifiers()
  → 销毁所有 BattleEntity
```

## 当前生效的文件

### Data & System
- `Assets/Scripts/TowerDefense/DataComponents.cs` — 所有 ECS 组件定义
- `Assets/Scripts/TowerDefense/System/BuffInstance.cs` — Buff 结构体 + Context 定义 + 委托
- `Assets/Scripts/TowerDefense/System/BuffProcessSystem.cs`

### Cards
- `Assets/Scripts/Cards/CardBase.cs` + `CardConfig`
- `Assets/Scripts/Cards/RapidFireCard.cs` (Tick)
- `Assets/Scripts/Cards/GoldRushCard.cs` (Tick)
- `Assets/Scripts/Cards/DoubleDamageCard.cs` (Tick)
- `Assets/Scripts/Cards/TowerShieldCard.cs` (Tick)
- `Assets/Scripts/Cards/SlowFieldCard.cs` (手动管理)
- `Assets/Scripts/Cards/MeteorStrikeCard.cs` (地面选择 + 视觉)
- `Assets/Scripts/Cards/BlackHoleCard.cs`
- `Assets/Scripts/Cards/BarrierCard.cs`
- `Assets/Scripts/Cards/GroundTargetingHelper.cs`

### Relics
- `Assets/Scripts/Relics/RelicBase.cs`
- `Assets/Scripts/Relics/LifeSpringRelic.cs` (Tick)
- `Assets/Scripts/Relics/UnstableEngineRelic.cs` (Tick)
- `Assets/Scripts/Relics/MeteorMarkRelic.cs` (OnKill)
- `Assets/Scripts/Relics/GoldHarvestRelic.cs` (Deactivate)
- `Assets/Scripts/Relics/UnknownRelic.cs`
