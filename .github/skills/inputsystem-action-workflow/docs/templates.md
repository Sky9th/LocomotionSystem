# 最小模板

这些模板只保留最核心骨架，不应替代真实代码。真正实现前，仍应根据当前仓库风格做最小核对。

## Button Action Handler 模板

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Inputs/Player/IA Player Example")]
public class IAPlayerExample : InputActionHandler
{
    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        bool isPressed = context.ReadValueAsButton();
        SExampleIAction payload = new SExampleIAction(isPressed, context.phase);
        eventDispatcher.Publish(payload);
    }

    protected override bool OnSupportsState(EGameState state)
    {
        return state == EGameState.Playing;
    }
}
```

## Value Action Handler 模板

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Inputs/Player/IA Player Example Value")]
public class IAPlayerExampleValue : InputActionHandler
{
    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        Vector2 rawInput = context.ReadValue<Vector2>();
        SExampleValueIAction payload = new SExampleValueIAction(rawInput);
        eventDispatcher.Publish(payload);
    }

    protected override bool OnSupportsState(EGameState state)
    {
        return state == EGameState.Playing;
    }
}
```

## 一次性 Direct Event Action 模板

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Inputs/System/IA Example Trigger")]
public class IAExampleTrigger : InputActionHandler
{
    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled || !context.performed)
        {
            return;
        }

        SExampleTriggerIAction payload = new SExampleTriggerIAction();
        eventDispatcher.Publish(payload);
    }
}
```

## Payload Struct 模板

```csharp
using System;
using UnityEngine.InputSystem;

[Serializable]
public struct SExampleIAction
{
    public SExampleIAction(bool hasInput, InputActionPhase phase)
    {
        HasInput = hasInput;
        Phase = phase;
    }

    public bool HasInput { get; }
    public InputActionPhase Phase { get; }

    public static SExampleIAction None => new SExampleIAction(false, InputActionPhase.Disabled);
}
```

## Locomotion 聚合接入模板

```csharp
private SExampleIAction exampleAction;

RegisterAction<SExampleIAction>();

exampleAction = SExampleIAction.None;

if (typeof(TPayload) == typeof(SExampleIAction))
{
    exampleAction = (SExampleIAction)(object)payload;
    return;
}
```

SLocomotionInputActions 至少需要补这些点：

```csharp
public SLocomotionInputActions(
    ...
    SExampleIAction exampleAction)
{
    ...
    ExampleAction = exampleAction;
}

public SExampleIAction ExampleAction { get; }
```

## 何时仍然需要读取真实代码

- 需要保持某个模块的精确命名风格。
- 需要复用已有 payload，而不是新建 payload。
- 需要接入 locomotion 的具体消费者逻辑。
- 需要判断同类 Action 是否已经有特殊状态门控。
- 需要确认某个现有 Action 的 phase 语义。