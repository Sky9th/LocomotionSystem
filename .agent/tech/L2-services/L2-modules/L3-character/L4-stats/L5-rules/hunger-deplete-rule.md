# HungerDepleteRule · 饥饿消耗规则

> `Character/Stats/Rules/HungerDepleteRule.cs` — sealed class，饥饿归零时扣 HP

## 调用链

```
被谁调:
  CharacterStats 构造 → new HungerDepleteRule()

调谁:
  继承 DepleteChainRule:
    Apply(stats, ctx, dt) → 每帧判断并扣减
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterStats | 每帧 Apply 遍历调用 |
| 继承 | DepleteChainRule | 父类 — 消耗链逻辑 |

## 实现

```csharp
internal class HungerDepleteRule : DepleteChainRule
{
    protected override string SourcePath() => "Vitals/Hunger";
    protected override string TargetPath() => "Vitals/HP";
    protected override float DamagePerSec() => 5f;   // TODO: Demo 阶段确定归零伤害值
}
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 归零伤害值配置化 | 待做 | 代码 TODO |
