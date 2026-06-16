# ModuleComponent · MB 子模块基类

> `L1_Core/Modules/ModuleComponent.cs` — MonoBehaviour 子模块基类，OnAssemble 自动向上查找父 Registry 注册

## 调用链

```
被谁调:
  ModuleBehaviour.Awake           → GetComponentsInChildren 发现 + Registry.Register
  ModuleBehaviour.Awake           → Registry.OnAssembleAll() → OnAssemble()

调谁:
  ModuleBehaviour.Registry        → Register(this)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | MonoBehaviour | 继承 |
| 依赖 | IInitializable | 实现接口 |
| 依赖 | ModuleBehaviour | GetComponentInParent 查找 + Registry.Register |
| 被依赖 | BaseCharacterAnimationDriver | 继承——所有动画驱动自动获得注册能力 |
| 被依赖 | 未来 MB 子模块 | 继承 |

## 公开方法

### OnAssemble()

```csharp
public virtual void OnAssemble()
```

- **用途**: 自动向上查找父 `ModuleBehaviour`，注册到其 Registry。子类 override 需调 `base.OnAssemble()`
- **调用者**: `ModuleBehaviour.Awake → Registry.OnAssembleAll()`
- **备注**: `_registered` 标志防重复注册

### OnWire()

```csharp
public virtual void OnWire()
```

- **用途**: 默认空实现。子类按需 override
- **调用者**: `ModuleBehaviour.OnWire → Registry.OnWireAll()`

## 内部机制

- 通过 `GetComponentInParent<ModuleBehaviour>()` 向上查找父节点
- 即使已被 `ModuleBehaviour.Awake` 的 `GetComponentsInChildren` 发现并注册，`_registered` 标志保证不重复注册
- `ModuleRegistry.Register` 本身也有去重，双重防护

## 使用规则

- 子类 override `OnAssemble` 必须调 `base.OnAssemble()`——设置 `_registered` 标志
- 无法继承 `ModuleComponent` 时（已有其他 MB 父类），改为实现 `IInitializable` 并在 OnAssemble 手动调 `GetComponentInParent<ModuleBehaviour>()?.Registry?.Register(this)`
