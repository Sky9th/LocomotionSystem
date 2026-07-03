# LocomotionLayerFsmState · FSM 状态基类

> `Character/Animation/Drivers/Locomotion/LocomotionLayerFsmState.cs` — 抽象类，继承 Animancer.FSM.State，添加 Tick()

## 调用链

```
被谁调:
  BaseLayer 构造 → 子类实例化
  Animancer FSM  → CanEnterState/CanExitState/OnEnterState/OnExitState (继承自 State)
  BaseLayer.Update → CurrentState.Tick()

子类:
  BaseIdleState
  BaseMovingState
  BaseTurnInPlaceState
  BaseAirLoopState
  BaseAirLandState
```

## 抽象定义

```csharp
internal abstract class LocomotionLayerFsmState<TOwner> : State
{
    protected readonly TOwner Owner;
    protected LocomotionLayerFsmState(TOwner owner) { Owner = owner; }
    public abstract void Tick();
}
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | Animancer.FSM.State | 提供 CanEnter/CanExit/OnEnter/OnExit |
| 泛型 TOwner | BaseLayer | 每个 State 持有 BaseLayer 引用 |

## 未来规划

无。
