# StatInstance · 运行时实例

> `Stats/StatInstance.cs` — Stat 的运行时实例，持有当前值、修改器列表、Tick 分派逻辑

## 调用链

```
被谁调:
  StatsTreeSO.ExtractLeaves()   → new StatInstance(def, overrideDefault)
  CharacterStats 构造函数       → tree.Resolve() 产出 list，填入 stats 字典
  CharacterStats.Update(dt)     → 遍历所有 StatInstance 调用 Tick(dt)
  ToggleModifierRule.Apply()    → AddModifier / RemoveByOwner
  DamageRule/PassiveGainRule    → Modify(delta) 直接加减
  外部系统（物品/技能/事件）    → Get(path) → AddModifier/Modify

调谁:
  TickConsume() / TickRestore() → CollectModifiers() → 遍历 Modifier 收集 ctx
  Modify()                      → OnZero?.Invoke() / OnChanged?.Invoke()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 02-character CharacterStats | 持有 Dictionary<string, StatInstance>，每帧 Tick |
| 被依赖 | 02-character Rules | 各 Rule 通过 AddModifier/RemoveByOwner/Modify 操作 |
| 依赖 | StatDefSO | 构造函数传入 Def，读取 Min/Max/Default 和能力标记 |
| 依赖 | StatModifier | modifiers 列表持有 Modifier 引用 |
| 依赖 | ModifierContext | CollectModifiers 产出 ctx |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Def` | `StatDefSO` | 所关联的 Stat 定义（只读） |
| `Path` | `string` | 树路径（如 "Vitals/HP"），由 StatsTreeSO 设置 |
| `Current` | `float` | 当前值（只读），通过 Modify 改变 |
| `ModifierCount` | `int` | 当前活跃修改器数量 |
| `OnZero` | `event Action` | Current 到达 Min 时触发 |
| `OnChanged` | `event Action<float>` | Current 变化超过 0.001 时触发，参数为当前值 |

## 方法

### StatInstance()
```csharp
internal StatInstance(StatDefSO def, float overrideDefault)
```
- **用途**: 构造运行时实例
- **参数**: `def` — Stat 定义；`overrideDefault` — >=0 时覆盖 Def.Default
- **调用者**: `StatsTreeSO.ExtractLeaves()` — Resolve 时构造
- **备注**: 仅限 internal，外部不直接构造

### AddModifier()
```csharp
public void AddModifier(StatModifier m)
```
- **用途**: 添加一个修改器到 modifiers 列表
- **参数**: `m` — 修改器实例
- **调用者**: `ToggleModifierRule.Apply()`、外部系统（物品/技能加效果时）
- **备注**: 不判重，可重复添加同一个 Modifier

### RemoveByOwner()
```csharp
public void RemoveByOwner(object owner)
```
- **用途**: 移除指定 Owner 的所有修改器
- **参数**: `owner` — 创建修改器时传入的 Owner 引用
- **调用者**: `ToggleModifierRule.Apply()`（deactivate 时）、外部效果结束时
- **备注**: 使用 List.RemoveAll，移除所有匹配的项

### HasModifier()
```csharp
public bool HasModifier(StatModifier m)
```
- **用途**: 检查特定修改器是否已添加
- **调用者**: 外部系统（防重复添加）

### Tick()
```csharp
public void Tick(float dt)
```
- **用途**: 帧驱动入口，根据 Def 能力分派消耗/恢复
- **参数**: `dt` — 帧时间增量
- **调用者**: `CharacterStats.Update(dt)`
- **备注**: 根据 Def.IsConsumable 和 Def.IsRestorable 分派到 TickConsume/TickRestore

### Modify()
```csharp
public void Modify(float delta)
```
- **用途**: 直接修改 Current 值，带 Min/Max clamp 和事件通知
- **参数**: `delta` — 变化量（正数增加，负数减少）
- **调用者**: TickConsume / TickRestore / DamageRule / 外部系统
- **备注**: 绝对值变化小于 0.001 时不触发 OnChanged

## 内部机制

### TickConsume(float dt)
- 如果 `consumeInterval > 0`：帧累加 timer，到达间隔后计算 ticks = (int)(timer / interval)，公式：`(-consumeRate + Addend) * Multiplier * ticks`
- 如果 `consumeInterval == 0`：每帧执行，公式：`(-consumeRate + Addend) * Multiplier * dt`
- TODO: 长间隔统一走帧累加，后续接入 TimeManager 再改为事件驱动

### TickRestore(float dt)
- 逻辑与 TickConsume 对称，公式：`(restoreRate + Addend) * Multiplier * ticks`（正方向）

### CollectModifiers()
- 遍历所有 modifiers，调用每个 m.Apply(stat, ctx)
- 返回累计的 ModifierContext { Addend, Multiplier }

## 使用规则

- Current 永远 clamp 在 [Def.Min, Def.Max] 区间
- 不直接设置 Current — 只能用 Modify()
- 外部系统通过 AddModifier/RemoveByOwner 注入影响，不直接调 Modify（伤害等一次性事件除外）
- OnZero 事件可用于触发 DepleteChainRule 等连锁逻辑

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 长间隔改事件驱动 | 远期 | TimeService | 代码 TODO (L42) |
