# DepleteChainRule · 消耗链规则基类

> `Character/Stats/Rules/DepleteChainRule.cs` — abstract class，归零链模式 — A 归零时对 B 持续伤害

## 调用链

```
被谁调:
  CharacterStats.Update() → rule.Apply(stats, ctx, dt)

子类:
  HungerDepleteRule
```

## 抽象定义

```csharp
internal abstract class DepleteChainRule : CharacterStatRule
{
    protected abstract string SourcePath();        // 源 Stat 路径（如 "Vitals/Hunger"）
    protected abstract string TargetPath();        // 目标 Stat 路径（如 "Vitals/HP"）
    protected abstract float DamagePerSec();       // 每秒伤害值
}
```

### Apply()
```csharp
internal override void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt)
```
- **用途**: 如果 SourcePath 的 Current <= Min，则对 TargetPath 造成 DamagePerSec * dt 伤害
- **调用者**: CharacterStats.Update()

## 未来规划

无。
