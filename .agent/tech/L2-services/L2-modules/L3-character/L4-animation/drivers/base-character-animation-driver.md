# BaseCharacterAnimationDriver · 驱动基类

> `Character/Animation/Drivers/BaseCharacterAnimationDriver.cs` — MonoBehaviour，ICharacterAnimationDriver 抽象基类

## 调用链

```
被谁调:
  Unity 生命周期:
    → OnEnable()  — 自注册到 AnimationBrain
    → OnDisable() — 自注销

子类覆写:
  LocomotionDriver / TraversalDriver

调谁:
  AnimationBrain.RegisterDriver/UnregisterDriver
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | AnimationBrain | 父组件查找，注册/注销 |
| 实现 | ICharacterAnimationDriver | 接口实现 |
| 继承 | LocomotionDriver | 移动动画驱动 |
| 继承 | TraversalDriver | 攀爬动画驱动 |

## 公开属性

```csharp
protected AnimationBrain brain;      // 缓存的 AnimationBrain 引用
public abstract int ChannelMask { get; }  // 子类实现
```

## 方法

### OnEnable()
```csharp
protected virtual void OnEnable()
```
- **用途**: 查找父级 AnimationBrain 并自注册
- **调用者**: Unity 生命周期（子类 Override 时必须 base.OnEnable()）

### OnDisable()
```csharp
protected virtual void OnDisable()
```
- **用途**: 自注销
- **调用者**: Unity 生命周期

### 抽象方法
```csharp
public abstract void Evaluate(in CharacterFrameContext ctx, float dt);
public abstract void Drive(in CharacterFrameContext ctx, float dt);
public abstract void OnStarted();
public abstract void OnCompleted();
public abstract void OnInterrupted(AnimationRequest by);
public abstract void OnResumed();
```

## 未来规划

无。
