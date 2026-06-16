# Module · C# 子模块基类

> `L1_Core/Modules/Module.cs` — 纯 C# 子模块基类，构造里自动注册到父 Registry

## 调用链

```
被谁调:
  父模块 OnAssemble              → new XxxModule(ctx, Registry)

调谁:
  ModuleRegistry.Register()      → 构造里自动注册
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | IInitializable | 实现接口 |
| 依赖 | ModuleRegistry | 构造时自注册 |
| 被依赖 | PlayerDirector, CharacterKinematic, CharacterCombat, GroundLocomotion, NpcDirector | 继承 |

## 公开方法

### 构造

```csharp
protected Module(ModuleRegistry parent)
```

- **用途**: 存储父 Registry 引用，自动 `parent.Register(this)`
- **调用者**: 子类 `: base(registry)` 调用

### OnAssemble()

```csharp
public virtual void OnAssemble()
```

- **用途**: 自初始化。默认空实现，子类按需 override
- **调用者**: `ModuleBehaviour.Awake → Registry.OnAssembleAll()`

### OnWire()

```csharp
public virtual void OnWire()
```

- **用途**: 跨模块连线。默认空实现，子类按需 override
- **调用者**: `ModuleBehaviour.OnWire → Registry.OnWireAll()`

## 使用规则

- 只给纯 C# 子模块用。MB 子模块用 `ModuleComponent`
- 构造里不做事——只存字段。初始化逻辑放 `OnAssemble()`
- 跨模块连线（事件订阅等）放 `OnWire()`
