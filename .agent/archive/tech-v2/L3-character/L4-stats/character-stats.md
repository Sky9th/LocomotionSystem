# CharacterStats · 角色数值组件

> `Character/Stats/CharacterStats.cs` — 纯 C# 类，持有 StatsTree 实例和 Rule 列表，每帧 Tick

## 调用链

```
被谁调:
  CharacterActor.Awake()       → new CharacterStats(statsTree)
  CharacterActor.Update()      → stats.Update(ctx, dt)

调谁:
  tree.Resolve()               → StatsTreeSO 展开所有 StatInstance
  rule.Apply(this, ctx, dt)    → 遍历规则列表
  stat.Tick(dt)                → StatInstance 每帧更新
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | StatsTreeSO | Stat 树根节点 |
| 依赖 | StatInstance | 运行时数值实例 |
| 依赖 | CharacterStatRule | 规则列表 |
| 依赖 | CharacterFrameContext | 帧上下文（输入/运动状态） |

## 公开属性

```csharp
public IReadOnlyDictionary<string, StatInstance> All => stats;   // 所有 Stat 路径→实例
internal DamageRule DamageRule { get; private set; }              // 伤害规则引用
```

## 方法

### CharacterStats()
```csharp
internal CharacterStats(StatsTreeSO tree)
```
- **用途**: 构造 — 解析 StatsTree → 填充 stats 字典 → 注册 Rule
- **调用者**: `CharacterActor.Awake()`
- **备注**: tree 为 null 时不做任何事；当前注册 3 个 Rule（SprintStaminaRule / HungerDepleteRule / DamageRule）

### Get()
```csharp
public StatInstance Get(string path)
```
- **用途**: 按路径获取 StatInstance
- **参数**: `path` — 如 "Vitals/HP"
- **返回**: 找到则返回实例，否则 null
- **调用者**: 各 Rule 的 Apply()

### Update()
```csharp
internal void Update(CharacterFrameContext ctx, float dt)
```
- **用途**: 遍历所有 Rule.Apply() → 遍历所有 StatInstance.Tick()
- **调用者**: `CharacterActor.Update()`

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 注册更多 Rule（DamageRule 等） | 待做 | 代码 TODO |
| 数值和生效条件配置化（当前硬编码） | 待做 | 代码 TODO |
| 外部系统通过 EventDispatcher 通信（如 CombatSystem） | 待做 | 旧 stats-rule-system.md |
