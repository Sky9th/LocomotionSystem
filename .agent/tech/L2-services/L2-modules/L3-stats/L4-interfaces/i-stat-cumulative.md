# IStatCumulative
> **源文件**: `Assets/Scripts/Stats/Interfaces/IStatCumulative.cs`

只增不减的累积能力接口契约（击杀得经验、采集得材料等）。

## 调用链

```
被谁调: (接口保留，无运行时调用)
调谁: (无)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| (保留) | StatDefSO | StatDefSO 不实现此接口，使用同名字段替代 |

## 公开属性

无。

```csharp
// 空接口 — 仅作为标记
public interface IStatCumulative { }
```

## 方法

无。

## 使用规则

- StatDefSO 不实现 IStatCumulative，改在 Inspector 勾选 `isCumulative`
- 外部调用 `StatInstance.Modify(delta)` 直接增加
- 无 Tick 自动消耗/恢复逻辑

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 接口无运行时用途，保留为文档 | — | 设计文档 stats-system.md |
