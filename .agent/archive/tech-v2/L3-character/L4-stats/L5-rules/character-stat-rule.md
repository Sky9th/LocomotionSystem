# CharacterStatRule · 数值规则基类

> `Character/Stats/Rules/CharacterStatRule.cs` — abstract class，所有 Stat Rule 的基类

## 调用链

```
被谁调:
  CharacterStats.Update() → rule.Apply(stats, ctx, dt)

子类:
  BatchDamageRule → DamageRule
  DepleteChainRule → HungerDepleteRule
  PassiveGainRule
  ToggleModifierRule → SprintStaminaRule
```

## 抽象定义

```csharp
internal abstract class CharacterStatRule
{
    internal abstract void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt);
}
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterStats | 遍历调用 |
| 参数 | CharacterStats | 读取/修改 StatInstance |
| 参数 | CharacterFrameContext | 读取输入/运动状态 |

## 未来规划

无。
