# ToggleModifierRule · 切换修改器规则基类

> `Character/Stats/Rules/ToggleModifierRule.cs` — abstract class，条件满足挂修改器、不满足撤

## 调用链

```
被谁调:
  CharacterStats.Update() → rule.Apply(stats, ctx, dt)

子类:
  SprintStaminaRule
```

## 抽象定义

```csharp
internal abstract class ToggleModifierRule : CharacterStatRule
{
    protected ToggleModifierRule(object owner, StatModifier m);
    protected abstract bool ShouldActivate(CharacterFrameContext ctx);  // 激活条件
    protected abstract string StatPath();  // 目标 Stat 路径
}
```

### Apply()
```csharp
internal override void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt)
```
- **用途**: 条件满足 && 未激活 → AddModifier；条件不满足 && 已激活 → RemoveByOwner
- **调用者**: CharacterStats.Update()

## 未来规划

无。
