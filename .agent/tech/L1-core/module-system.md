# Module 系统 · 树形生命周期统一链路

> `L1_Core/Modules/` — 基于 `IInitializable` 的树形模块工具集，覆盖 C# / MB / 父 / 子全部角色

## 层级定位

L1。与 `IInitializable` 同级，是协议的上层工具封装。任何需要树形父子模块管理的节点均可使用。

## 调用链

```
ModuleBehaviour.Awake()
  ├── Registry = new ModuleRegistry()
  ├── GetComponentsInChildren<IInitializable>()  → 发现 MB 子模块，Registry.Register()
  ├── OnAssemble()                                → 子类 override：创建 C# 子模块（Module 构造里自注册）
  └── Registry.OnAssembleAll()                    → 遍历所有已注册子模块 OnAssemble()

ModuleBehaviour.Start()
  └── OnWire()
        ├── 子类 override：额外操作（如 agent.AddModifier）
        └── base.OnWire() → Registry.OnWireAll()  → 遍历所有子模块 OnWire()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | `IInitializable` | Module 系统是 IInitializable 的工具封装 |
| 被依赖 | CharacterActor | 继承 ModuleBehaviour |
| 被依赖 | PlayerDirector 等 C# 子模块 | 继承 Module |
| 被依赖 | LocomotionDriver 等 MB 子模块 | 继承 ModuleComponent |
| 被依赖 | BaseCharacterAnimationDriver | 继承 ModuleComponent，所有动画驱动自动获得注册能力 |

## 四个工具

| 类 | 定位 | 谁用 | 注册方式 |
|----|------|------|---------|
| `ModuleRegistry` | 收集器 | 任何树节点持有 | `Register(module)` |
| `Module` | C# 子模块基类 | 纯 C# 子模块 | 构造 `base(parent)` 自动注册 |
| `ModuleBehaviour` | MB 父模块基类 | MB 父节点 | Awake 创建 Registry + 自动发现 MB 子 + OnAssembleAll |
| `ModuleComponent` | MB 子模块基类 | MB 子模块（Animation 驱动等） | OnAssemble 自动向上查找父 Registry 注册 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 四个类共用一个 `Module` 前缀 | 一眼识别为同一系统 |
| C# 和 MB 分开两个基类 | C# 单继承限制——MB 必须继承 MonoBehaviour |
| `ModuleBehaviour.Awake` 自动发现 MB 子组件 | 父模块不应手动维护子模块列表 |
| `ModuleBehaviour.Awake` 末尾自动调 `OnAssembleAll` | 与 OnAssemble 对称，消除手动调用 |
| `ModuleBehaviour.Start` 自动调 `OnWire` | 与 OnAssemble 对称 |
| `ModuleRegistry.Register` 去重 | 防止 MB 发现 + ModuleComponent.OnAssemble 双注册 |
| `ModuleRegistry.Register` 为 `internal` | 仅同 assembly 的 ModuleBehaviour 创建和 Module/ModuleComponent 自注册使用 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| ~~BaseService 继承 ModuleBehaviour~~ | ~~v0.16.0 已删除 — Service 直接继承 ModuleComponent~~ | — | 统一 L1→L2 树形链路 |
| ~~GameService 继承 ModuleBehaviour~~ | ~~v0.16.0 已完成~~ | — | L1 根节点统一管理 |
| C# 父模块 Pattern 封装 | 观察中 | Module 添加内置 Registry | 如果 C# 父子模式频繁出现 |

## 子文档索引

| 文档 | 文件 |
|------|------|
| [module-registry.md](module-registry.md) | ModuleRegistry |
| [module.md](module.md) | Module |
| [module-behaviour.md](module-behaviour.md) | ModuleBehaviour |
| [module-component.md](module-component.md) | ModuleComponent |
