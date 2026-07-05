# Ability Instance — 技能运行时实例

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist

## Layer Position

L3 → L4。位于 `L3_Ability/` 根目录。

- `AbilityInstance.cs` — 运行时实例数据类
- `InstanceManager.cs` — 生命周期管理器

## 核心概念

**主动/被动/装备/天赋本质相同**——都是 AbilitySO 的运行时实例（AbilityInstance），区别仅在生命周期和触发方式。卡片是其隐喻：Activate = 插卡，Deactivate = 拔卡。

### AbilityInstance

```csharp
public sealed class AbilityInstance
{
    string Id;              // GUID 短码
    AbilitySO Definition;   // 数据定义
    object Source;          // "innate" | equipmentInstance | talent | "input"
    ELifecycle Lifecycle;   // OneShot | Persistent | Toggle
    ERefreshPolicy RefreshPolicy; // Refresh | Stack | Replace
    bool IsActive;          // Pull 清理依据
}
```

### InstanceManager

三索引管理实例生命周期：

| 索引 | Key | 用途 |
|------|-----|------|
| `_instances` | AbilityInstance | 活跃实例集合（ContainsKey 判活） |
| `_logicalIndex` | (AbilitySO, object) | RefreshPolicy 判重 |
| `_sourceIndex` | object | DeactivateBySource O(1) 查找 |
| `_triggerIndex` | ETriggerEvent | 事件匹配 |

## 副作用：Pull 模型

**Push（旧）**：InstanceManager 追踪 _affectedTables/_affectedTags，Deactivate 时遍历清理。需要知道"影响了谁"。

**Pull（新）**：副作用携带 Owner=AbilityInstance。Deactivate 置 IsActive=false。目标侧 Tick 自动检查：

```
FloatState.Tick → Owner.IsActive=false → 移除 adjunct
CleanupExpiredCooldowns → Owner.IsActive=false → 移除 tag
```

| 维度 | Push | Pull |
|------|------|------|
| 追踪结构 | _affectedTables + _affectedTags | 零 |
| 清理时机 | Deactivate 立刻 | Tick/查询（FloatState 每帧，Tag 每 0.5s） |
| 临时禁用 | 不支持 | IsActive=false → 暂停 → true → 恢复 |

## RefreshPolicy

| 策略 | 行为 |
|------|------|
| Refresh | 复用已有 Instance，调用方 RemoveAdjuncts(self) + 重新跑 FSM |
| Stack | 创建新 Instance，不写 _logicalIndex |
| Replace | Deactivate 旧 + 创建新 Instance |

## 与 AbilityExecutor 的关系

```csharp
// AbilityExecutor
InstanceManager _instances;          // 实例管理
AbilityPipeline _activePipeline;     // 主动技能 FSM（互斥）
List<AbilityPipeline> _runningPassives; // 被动技能 FSM（独立实例）
Queue _pendingPassiveStarts;         // 延迟启动（防 re-entrancy）

TryUse(ability, ...) → Activate(OneShot) → _activePipeline.Start
NotifyPassiveEvent(trigger, subject) → GetByTrigger → 入队 → FlushPendingPassives
SyncInstances(passives, source) → DeactivateBySource + Activate foreach
```

## 设计决策

| 决策 | 原因 |
|------|------|
| AbilityHandle 合并入 AbilityInstance | 用户指出两个 class 可以合并，Instance 自身就是 owner 和字典 key |
| Pull > Push | 目标侧各自 Tick 成本摊销；自然支持临时禁用/恢复；不需要跨实体追踪 |
| 拔卡不清冷却 | 防止插拔刷 CD |
| Deactivate 不打断 FSM | 6 帧窗口极窄，不加锁；只清副作用，Pipeline 自然完成 |
| Refresh 时 RemoveAdjuncts(self) + 重新 FSM | Pull 模型下无法遍历已注入的 adjunct 刷新 ExpiryTime——直接清+重建更简单 |
