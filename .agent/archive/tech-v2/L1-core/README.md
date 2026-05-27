# L1-core · GameManager 层

> 根节点层 — 持有并管理所有 L2 Service，不包含业务逻辑。

## 定位

L1 是架构的根。GameService 做 Bootstrap，GameContext 做数据总线，BaseService 定义所有 Service 的契约。

**向下管理 L2**：GameService 发现、注册、初始化所有 BaseService 子类。
**不向上依赖**：L1 不依赖任何上层模块。

## 调用链

```
GameService.Awake() [order=-500, DontDestroyOnLoad]
  │
  └── Bootstrap()
      ├── [1] GameContext.Initialize()
      ├── [2] 发现所有 L2 BaseService → Register(ctx)
      │       └── OnRegister() → ctx.RegisterService(this)
      ├── [3] AttachDispatcherToServices() → OnDispatcherAttached()
      ├── [4] ActivateServiceSubscriptions() → OnSubscriptionsActivated()
      └── [5] InitializeServices() → OnServicesReady()
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| GameService | — | 根节点，不被任何模块依赖 |
| GameContext | 所有 L2 Service、L3 Module | Service Registry + Snapshot Store |
| BaseService | 所有 L2 Service | 继承此基类 |
| GameService | L2 全部 Service | 持有、发现、注册、销毁 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 四阶段 Service 初始化 | 解耦注册顺序，OnServicesReady 时所有依赖就绪 |
| PublishState 原子写入 | Snapshot 和 Event 同步，避免数据不一致 |
| DOTween unscaledTime | Time.timeScale=0 时 UI 动画不冻结 |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [gamecontext.md](gamecontext.md) | GameContext — Service Registry + Snapshot Store，每个方法 |
| [gameservice.md](gameservice.md) | GameService — Bootstrap 五步启动 + TeardownSession |
| [baseservice.md](baseservice.md) | BaseService — 四阶段生命周期 + TryResolveService + PublishState |
| [structs.md](structs.md) | MetaStruct + 所有 Core Context Struct |
