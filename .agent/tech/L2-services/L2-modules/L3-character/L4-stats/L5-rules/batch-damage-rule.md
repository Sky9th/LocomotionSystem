# BatchDamageRule · 批量伤害规则基类

> `Character/Stats/Rules/BatchDamageRule.cs` — abstract class，一次性事件模式 — 外部攒批每帧统一执行

## 调用链

```
被谁调:
  CharacterStats.Update() → rule.Apply(stats, ctx, dt)

被外部调:
  (通过 DamageRule.Add(amount) 继承接口)

子类:
  DamageRule
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterStats | 每帧 Apply 遍历调用 |
| 被继承 | DamageRule | 子类 — 具体伤害目标 |

## 公开方法

```csharp
public void Add(float amount)                    // 累积伤害值
protected abstract string TargetPath();          // 目标 Stat 路径
internal override void Apply(...)                // 每帧结算
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 外部系统调 Add() 的桥接 | 待做 | 代码 TODO |
