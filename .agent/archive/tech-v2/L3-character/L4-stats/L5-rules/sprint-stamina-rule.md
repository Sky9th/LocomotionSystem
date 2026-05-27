# SprintStaminaRule · 冲刺体力消耗规则

> `Character/Stats/Rules/SprintStaminaRule.cs` — sealed class，冲刺时体力的消耗倍率

## 调用链

```
被谁调:
  CharacterStats 构造 → new SprintStaminaRule(this)

调谁:
  继承 ToggleModifierRule:
    ShouldActivate(ctx) → 判断 Gait==Sprint
    Apply() → 添加/移除 Modifier
```

## 实现

```csharp
internal class SprintStaminaRule : ToggleModifierRule
{
    internal SprintStaminaRule(object owner) : base(owner, new StatModifier
    {
        Apply = (s, ctx) => ctx.Multiplier = 3f   // 消耗倍率 3x
    }) { }

    protected override string StatPath() => "Vitals/Stamina";
    protected override bool ShouldActivate(CharacterFrameContext ctx)
        => ctx.Discrete.Gait == EMovementGait.Sprint;
}
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 冲刺体力倍率配置化（当前硬编码 3x） | 待做 | 代码 TODO |
