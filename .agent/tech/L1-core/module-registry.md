# ModuleRegistry · 模块收集器

> `L1_Core/Modules/ModuleRegistry.cs` — 收集 IInitializable 子模块，提供统一遍历

## 调用链

```
被谁调:
  ModuleBehaviour.Awake()        → new ModuleRegistry()
  Module 构造                    → Registry.Register(this)
  ModuleComponent.OnAssemble()   → Registry.Register(this)
  ModuleBehaviour.Awake()        → Registry.OnAssembleAll()
  ModuleBehaviour.OnWire()       → Registry.OnWireAll()

调谁:
  IInitializable                 → OnAssemble(), OnWire()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | IInitializable | 存储 + 遍历 |
| 被依赖 | Module | 构造时注册 |
| 被依赖 | ModuleBehaviour | 创建 + 驱动 |
| 被依赖 | ModuleComponent | OnAssemble 时注册 |

## 公开方法

### Register(IInitializable)

```csharp
internal void Register(IInitializable module)
```

- **用途**: 注册子模块。去重——同一模块只注册一次。
- **调用者**: `Module` 构造、`ModuleComponent.OnAssemble`、`ModuleBehaviour.Awake`

### OnAssembleAll()

```csharp
public void OnAssembleAll()
```

- **用途**: 遍历所有已注册模块，调用 `OnAssemble()`
- **调用者**: `ModuleBehaviour.Awake()` 末尾自动调用

### OnWireAll()

```csharp
public void OnWireAll()
```

- **用途**: 遍历所有已注册模块，调用 `OnWire()`
- **调用者**: `ModuleBehaviour.OnWire()` 自动调用

## 内部机制

- 底层 `List<IInitializable>`，`Register` 去重（`Contains` 检查）
- 注册顺序即遍历顺序——C# 子模块先注册（构造时），MB 子模块后注册（Awake 发现）

## 使用规则

- `Register` 为 `internal`——仅同 assembly 的 Module 系统类使用
- 外部代码通过 `ModuleBehaviour.Registry` 获取实例
