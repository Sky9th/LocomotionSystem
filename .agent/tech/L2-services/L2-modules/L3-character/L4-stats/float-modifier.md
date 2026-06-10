# FloatModifier — 持久帧级修改器

> `L3_Character/Stats/FloatModifier.cs` · 技术文档 · 2026-06-10

## 层级定位

L4 Character 子系统。由 Properties 模块定义标准的持久修改器。所有需要持续影响 Float 属性值的子系统遵守此约定注入。

是对旧 Stats 模块 `StatModifier` + `ModifierContext` 的重构——保留速率修改模式，新增定时 Delta 和自定义 Tick 能力。

## 调用链

```
被谁调:
  消费者创建 new FloatModifier { ... } → PropertyComponent.AddModifier(mod)
  FloatState.Tick() → m.OnApplyRate / m.CustomTick

调谁:
  不主动调用——被 FloatState 回调
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被消费 | FloatState | Tick 时读取 OnApplyRate / CustomTick / Delta |
| 被消费 | EntityProperties | AddModifier / RemoveModifiers |
| 被消费 | PropertyComponent | 代理 AddModifier / RemoveModifiers |
| 被消费 | 所有子系统 | 创建 Modifier 注入 |

## 字段

```csharp
public object Owner;                     // 创建者，批量移除用
public string TargetPath;                // 目标属性路径，如 "Vitals/Stamina"
public float Interval;                   // 执行间隔（秒），0 = 每帧
```

### 类型 A：速率影响

```csharp
public Action<RateContext> OnApplyRate;
```
- **用途**: 在 FloatState 计算消耗/恢复量时被调用。修改 ctx.Addend / ctx.Multiplier
- **示例**: `ctx => ctx.Multiplier = 3f` → 体力消耗 3 倍

### 类型 B：值直接修改

```csharp
public float Delta;              // 每次修改量（正=增，负=减）
public Func<bool> Condition;     // 执行条件，null = 无条件
```
- **用途**: 以 Interval 频率直接将 Delta 作用于 Current
- **示例**: `Interval=2f, Delta=-5f` → 每 2 秒扣 5 点

### 类型 C：自定义行为

```csharp
public Action<FloatState, float> CustomTick;
```
- **用途**: 完全接管 Tick 逻辑。FloatState 内置消耗/恢复不会被调用
- **适用**: 无法用 A+B 表达的复杂持久行为
- **参数**: FloatState 引用 + dt（或 Interval 值）

---

# RateContext — 速率上下文

```csharp
public class RateContext
{
    public float Addend;              // 加到基础速率上
    public float Multiplier = 1f;     // 乘到基础速率上
}
```

在 FloatState.Tick 计算消耗/恢复量时，遍历所有 Modifier 的 OnApplyRate 回调，累积 Addend（加性偏移）和 Multiplier（乘性倍数）。最终公式：

```
消耗: (-baseRate + Addend) * Multiplier * dt
恢复: (baseRate + Addend) * Multiplier * dt
```

## 用法示例

```csharp
// 冲刺：体力消耗翻 3 倍（类型 A）
var sprintMod = new FloatModifier {
    Owner = this,
    TargetPath = "Vitals/Stamina",
    Interval = 0,
    OnApplyRate = ctx => ctx.Multiplier = 3f
};
props.AddModifier(sprintMod);

// 中毒：每 2 秒扣 5 HP（类型 B）
var poisonMod = new FloatModifier {
    Owner = this,
    TargetPath = "Vitals/HP",
    Interval = 2f,
    Delta = -5f
};
props.AddModifier(poisonMod);

// 饥饿连锁扣血（类型 B + 条件）
var hungerMod = new FloatModifier {
    Owner = this,
    TargetPath = "Vitals/HP",
    Interval = 1f,
    Delta = -5f,
    Condition = () => props.GetFloat("Vitals/Hunger") <= props.GetMin("Vitals/Hunger")
};
props.AddModifier(hungerMod);
```

## 设计决策

| 决策 | 原因 |
|------|------|
| 三类而非统一回调 | A（速率）和 B（定时 Delta）覆盖 90% 场景，C（自定义）是逃生舱 |
| Owner 模式 | 创建者负责生命周期，RemoveModifiers(owner) 批量清理 |
| Interval 用秒数而非枚举 | 支持任意间隔（0.5s、3s、120s），不限制预设值 |
| OnApplyRate 的 Addend/Multiplier 并行 | 加法和乘法各自独立，多 Modifier 叠加不冲突 |
| Delta 区分每帧和定时间隔 | Interval=0 时 Delta*dt 实现平滑变化，>0 时按间隔脉冲 |
| public 字段而非属性 | 简化构造，与 Unity Inspector 使用模式一致 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| Modifier 优先级（排序） | 远期 | 需求驱动 | — |
| Modifier Tag（叠加规则） | 远期 | 需求驱动 | 同名 Modifier 去重 |
