# FloatState — Float 属性运行时状态

> **Last Verified**: 2026-07-06 | **Verification**: All referenced files exist, signatures match
> `L3_Properties/Instance/FloatState.cs` · `L3_Properties/Instance/FloatAdjunct.cs`

## 层级定位

L4 Character 子系统。单个 Float 属性的运行时引擎——可变 Current + Min/Max 钳制 + 消耗/恢复 Tick + Modifier 管理 + 事件广播。

由 PropertyTable 内部持有和管理，外部不可直接访问。是对旧 Stats 模块 `StatInstance` 的重构——保留 Tick/Modifier/Event 逻辑，移除对 StatDefSO 的依赖。

## 调用链

```
被谁调:
  PropertyTable.InitFloatStates()     → new FloatState(...)
  PropertyTable.EnsureFloatState()    → new FloatState(...)
  PropertyTable.Tick(dt)              → .Tick(dt)
  PropertyTable.Set(path, float)      → .SetCurrent / .SetCurrentSilent
  PropertyTable.AddModifier / Remove  → .AddModifier / .RemoveModifiers

调谁:
  FloatModifier                          → OnApplyRate / CustomTick 回调
  RateContext                            → 速率上下文
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | FloatModifier | 管理 Modifier 列表 |
| 依赖 | RateContext | 速率计算上下文 |
| 被消费 | PropertyTable | 唯一持有方 |

## 公开属性

```csharp
public string Path { get; }             // 属性路径，如 "Vitals/HP"
public float Current { get; private set; }  // 当前值
public float Min { get; }               // 下限
public float Max { get; }               // 上限
public bool IsConsumable { get; }        // 是否消耗型
public float ConsumeRate { get; }        // 每秒消耗量
public float ConsumeInterval { get; }    // 消耗间隔（秒）
public bool IsRestorable { get; }        // 是否恢复型
public float RestoreRate { get; }        // 每秒恢复量
public float RestoreInterval { get; }    // 恢复间隔（秒）
public int ModifierCount { get; }        // 当前 Modifier 数量
```

## 方法

### FloatState() (构造)
```csharp
internal FloatState(string path, float min, float max, float initialValue,
    bool isConsumable, float consumeRate, float consumeInterval,
    bool isRestorable, float restoreRate, float restoreInterval)
```
- **用途**: 构造，注入所有运行时配置。不依赖任何 SO
- **备注**: isConsumable 需要 consumeRate > 0 才生效（同名计算属性）

### Modify()
```csharp
public void Modify(float delta)
```
- **用途**: 增量修改 Current。钳制到 [Min, Max]，触发 OnChanged / OnZero 事件
- **调用者**: PropertyTable.Modify / TickConsume / TickRestore / Modifier 回调

### SetCurrent()
```csharp
public void SetCurrent(float value)
```
- **用途**: 直接覆写 Current（非增量）。触发事件
- **调用者**: PropertyTable.Set (Float)

### SetCurrentSilent()
```csharp
public void SetCurrentSilent(float value)
```
- **用途**: 静默覆写 Current。不触发事件
- **调用者**: PropertyTable.Load（读档）

### Tick()
```csharp
public void Tick(float dt)
```
- **用途**: 每帧驱动。执行顺序：C（CustomTick）→ A（速率修改 + consume/restore）→ B（定时 Delta）
- **调用者**: PropertyTable.Tick
- **备注**: 无 Modifier 且无 consume/restore 时直接返回

### AddModifier() / RemoveModifiers()
```csharp
public void AddModifier(FloatModifier m)
public void RemoveModifiers(object owner)
```
- **用途**: 管理持久修改器。RemoveModifiers 按 Owner 批量移除

## 事件

```csharp
public event Action OnZero;                           // Current 到达 Min
public event Action<string, float, float> OnChanged;  // path, old, new
```
- **订阅者**: PropertyTable（桥接到公开事件）

## 内部机制

### Tick 三阶段执行

1. **C 类 CustomTick**：完全自定义行为，每 Interval 秒调用一次（Interval=0 则每帧）
2. **A 类 + consume/restore**：收集 OnApplyRate → RateContext → 计算最终消耗/恢复量 → Modify
3. **B 类 Delta**：timer 累积，达 Interval 后检查 Condition → Modify(Delta)

### 消耗/恢复公式

```
消耗: (-ConsumeRate + RateContext.Addend) * RateContext.Multiplier * dt(或ticks)
恢复: (RestoreRate + RateContext.Addend) * RateContext.Multiplier * dt(或ticks)
```

有 Interval 时使用累计 ticks 而非 dt，确保非每帧间隔的精度。

### Effective（只读值）
```csharp
public float Effective
```
- **用途**: 叠加所有 FloatAdjunct 后的有效值
- **公式**: `clamp((Current + ΣValueAdd) × ΠValueMultiply, Min, effectiveMax)`
- **effectiveMax**: `Max × ΠMaxMultiply + ΣMaxAdd`（v0.38.3 新增，支持属性上限扩容）
- **调用者**: PropertyTable.GetEffective / 外部读取
- **备注**: 不修改 Current——修饰只在读取时叠加。过期或 Owner 失效的 adjunct 自动跳过

### FloatAdjunct — 只读修正
`FloatAdjunct.cs` 字段：
| 字段 | 默认 | 用途 |
|------|------|------|
| ValueAdd | 0 | 固定偏移 |
| ValueMultiply | 1 | 乘数（1=不变） |
| MaxAdd | 0 | Max 固定扩展（v0.38.3） |
| MaxMultiply | 1 | Max 乘数扩展（v0.38.3） |
| ExpiryTime | -1 | 过期时间，-1 永续 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 构造函数注入所有配置，不依赖 SO | FloatState 不绑定 StatDefSO 或 PropertyDefSO |
| internal 访问 | 只由 PropertyTable 创建和管理 |
| Addend + Multiplier 并行槽位 | 多个 Modifier 各自独立修改，不互相覆盖 |
| C→A→B 执行顺序 | 自定义行为优先执行，速率修改其次，定时修改最后 |
| SetCurrentSilent 独立方法 | Load（读档）不需要逐个触发事件 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 事件节流（高频修改合并） | 远期 | — | 性能优化 |
