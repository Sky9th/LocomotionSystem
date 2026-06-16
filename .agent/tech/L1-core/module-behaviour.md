# ModuleBehaviour · MB 父模块基类

> `L1_Core/Modules/ModuleBehaviour.cs` — MonoBehaviour 父模块基类，自动管理 ModuleRegistry 和生命周期

## 调用链

```
被谁调:
  Unity 引擎                    → Awake(), Start()
  子类                          → override OnAssemble(), OnWire()

调谁:
  ModuleRegistry                → new, OnAssembleAll(), OnWireAll()
  IInitializable (子模块)       → 通过 Registry 遍历
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | MonoBehaviour | 继承 |
| 依赖 | IInitializable | 实现接口 |
| 依赖 | ModuleRegistry | 创建 + 持有 + 驱动 |
| 被依赖 | CharacterActor | 继承 |
| 被依赖 | 未来：GameService, L2 Service | 继承 |

## 公开属性

```csharp
internal ModuleRegistry Registry { get; private set; }
```

- **用途**: 子模块注册到的收集器。子模块构造传此引用。`internal`——同 assembly 的 `ModuleComponent` 可访问。

## 方法

### Awake()

```csharp
protected virtual void Awake()
```

- **用途**: 创建 Registry → 发现 MB IInitializable 子组件并注册 → 调 OnAssemble → 调 Registry.OnAssembleAll()
- **备注**: 子类 override 时在末尾调 `base.Awake()`，确保 SetupModel/ResolveComponents 在 OnAssemble 之前完成

### Start()

```csharp
protected virtual void Start()
```

- **用途**: 调 OnWire()
- **备注**: 子类通常不需要 override——OnWire 已覆盖连线需求

### OnAssemble()

```csharp
public virtual void OnAssemble()
```

- **用途**: 子类 override，创建 C# 子模块（构造里自注册到 Registry）
- **调用者**: `Awake()` 末尾自动调用

### OnWire()

```csharp
public virtual void OnWire()
```

- **用途**: 先调 `Registry.OnWireAll()` 遍历子模块，子类 override 在 base.OnWire() 前后加额外操作
- **调用者**: `Start()` 自动调用

## 内部机制

```
Awake:
  1. Registry = new ModuleRegistry()
  2. GetComponentsInChildren<IInitializable>() — 发现所有 MB 子模块
  3. 过滤掉自身（ModuleBehaviour），其余 Register
  4. OnAssemble() — 子类创建 C# 子模块
  5. Registry.OnAssembleAll() — 遍历所有子模块 OnAssemble

Start:
  1. OnWire() → base.OnWire() → Registry.OnWireAll()
```

## 使用规则

- 子类 `Awake` 在末尾调 `base.Awake()`——确保 SetupModel 等前置工作在 OnAssemble 之前
- 子类 `OnAssemble` 只创建 C# 子模块——MB 子模块由基类自动发现
- 子类 `OnWire` 调 `base.OnWire()` 后再做额外操作
