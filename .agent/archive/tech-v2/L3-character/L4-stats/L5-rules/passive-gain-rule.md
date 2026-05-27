# PassiveGainRule · 被动恢复规则基类

> `Character/Stats/Rules/PassiveGainRule.cs` — abstract class，被动增加模式 — 外部积累每帧统一执行

## 调用链

```
被谁调:
  CharacterStats.Update() → rule.Apply(stats, ctx, dt)

子类:
  (预留) 体力/生命自然恢复
```

## 公开方法

```csharp
public void Gain(float amount)                     // 累积恢复值
protected abstract string TargetPath();            // 目标 Stat 路径
internal override void Apply(...)                  // 每帧结算
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 体力和生命自然恢复的具体实现 | 待做 | 代码预留 |
