# 日志格式规范化

> 状态：待执行 | 依赖：Ability Pipeline 完成后

## 目标

统一项目中所有 `Debug.Log` 的格式和开关控制，使日志可追踪来源、可按模块过滤、不残留测试日志。

## 格式规范

```
[L1][L2][L3][子模块] 消息内容
```

### 层级示例

```
[Core][PlayerService][Ability][ActivePipeline] Gating → Cost
[Core][PlayerService][Ability][GatingState] Passed: Blade_LightCut
[Core][EntityService] Spawned entity: test_backpack
[Core][PlayerService][Character][CharacterContainer] BodyContainer created with 9 slots
```

### 规则

| 维度 | 说明 |
|------|------|
| L1 | 服务根，恒为 `Core`（当前阶段） |
| L2 | 归属哪个 L2 Service。非 Service 管理的模块可省略 |
| L3 | 归属哪个 L3 模块 |
| 子模块 | 具体类/文件——`ActivePipeline`、`GatingState`、`CharacterContainer` 等 |
| L4/L5 | **不参与日志标签**，归入所属 L3 |

### 反例

```
[ActivePipeline] Cost → Completed                    ← 缺 L1/L2/L3
[Ability] TryActivate: Fireball                      ← 缺 L1/L2/子模块
[ProcessEquipInput] No backpack found.               ← 缺全部层级
```

## 等级开关

```
Error   — 始终输出，不可关闭
Info    — 非错误但有用的运行信息（启动/销毁/关键状态变更）
Debug   — 开发期临时日志，提交前必须删除或降级
```

**强制规则**：Debug 级别日志不应残留于生产代码。完成功能验证后立即删除。

## 模块开关

| 层级 | 开关粒度 | 示例 |
|------|---------|------|
| L1 | Core 全局开关 | 关闭所有日志 |
| L2 | 按 Service | `PlayerService`、`EntityService` |
| L3 | 按模块 | `Ability`、`Character`、`Properties`、`Container` |
| L4/L5 | **不设独立开关** | 跟随上级 L3 模块开关 |

### 开关实现

每个 L2/L3 模块提供一个 `bool EnableLog` 开关（或从配置文件读取），日志输出前检查：

```csharp
if (LogChannels.Ability.EnableDebug)
    Debug.Log($"[Core][PlayerService][Ability][GatingState] Passed: {name}");
```

## 执行计划

1. 定义 `LogChannels` 结构或类，集中管理各模块开关
2. 逐模块替换现有 `Debug.Log` → 新格式
3. 清理所有遗留的临时 Debug 日志
4. 验证：按模块关闭开关后对应日志不再输出

## 注意事项

- 不改动 `Shared/Logging/` 现有日志系统——新增的模块开关作为补充层
- `Debug.LogWarning` / `Debug.LogError` 保留格式化，不受 Debug 开关影响
