# 2026-06-16 — Service Module 标准化

## 目标

将 L1-L2 Service 层统一到 Module 树形生命周期，删除 BaseService 中间层。

## 改动概要

- **删除** `BaseService.cs`（223 行）— 10 个 L2 Service 直接继承 `ModuleComponent`
- **重写** `GameService.cs` — 继承 `ModuleBehaviour`，主动 `new GameObject("GameContext")`，移除 Bootstrap 手动序列
- **新增** `ModuleRegistry.Count` — GameService 验证全部 Service 加载
- **标记** `EventDispatcherService` 为 `[Obsolete]` — 未来替换为 EventHub
- **修复** `AnimationBrain.OnAnimatorMove` — `buildCtx == null` 空安全检查（动态实例化时间窗口）

## 架构变化

```
之前: Service → BaseService → ModuleComponent
现在: Service → ModuleComponent
```

- OnAssemble：自组装（Log 初始化、组件发现、内部状态）
- OnWire：自注册 + 解析依赖（Dispatcher、GameContext）+ 事件订阅

## 数据流

```
GameService.OnAssemble → GameContext 创建
GameService.OnWire → EventDispatcher 注册 → Registry.OnWireAll
  → 10 个 Service.OnWire（自注册 + 订阅）
  → 验证全部加载
  → Editor 自动加载
```

## 已知问题

- EventDispatcher 已标记 `[Obsolete]`，未来替换为 EventHub
- `AnimationBrain.OnAnimatorMove` 在 `Instantiate` 动态实例化时可能先于 `Start/OnWire` 触发（Unity 时序问题），已加空安全防御
