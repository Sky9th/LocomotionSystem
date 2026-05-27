# DamageRule · 伤害规则

> `Character/Stats/Rules/DamageRule.cs` — sealed class，继承 BatchDamageRule，伤害 HP

## 调用链

```
被谁调:
  CharacterStats 构造 → new DamageRule()

调谁:
  继承 BatchDamageRule:
    Add(amount)  — 外部累积伤害
    Apply()      — 每帧扣减 HP
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterStats | 每帧 Apply 遍历调用 |
| 继承 | BatchDamageRule | 父类 — 累积伤害/批量结算 |

## 实现

```csharp
internal class DamageRule : BatchDamageRule
{
    protected override string TargetPath() => "Vitals/HP";
}
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 外部系统事件桥接 | 待做 | 旧 stats-rule-system.md |
