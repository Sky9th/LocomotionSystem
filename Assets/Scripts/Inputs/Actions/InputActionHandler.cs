using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base class for input helper ScriptableObjects. It owns shared context
/// (camera transforms, stance references, etc.) and exposes a unified
/// Enable/Disable lifecycle that the InputManager can drive deterministically.
/// </summary>
public abstract class InputActionHandler : ScriptableObject
{

    [Header("Action Source")]
    [SerializeField] 
    private InputActionReference inputAction;
    private InputAction runtimeAction;

    protected EventDispatcher eventDispatcher;

    protected bool IsContextBound { get; private set; }
    protected bool IsEnabled { get; private set; }

    internal bool SupportsState(EGameState state)
    {
        return OnSupportsState(state);
    }

    /// <summary>
    /// Public entry point used by InputManager to supply the current shared
    /// context (camera transform, stance references, etc.).
    /// </summary>
    public void InitializeHandler(EventDispatcher dispatcher)
    {
        if (dispatcher == null)
        {
            Debug.LogError("InputActionHandler requires a valid EventDispatcher reference.");
            return;
        }

        if (IsContextBound)
        {
            return;
        }

        eventDispatcher = dispatcher;
        runtimeAction = inputAction?.action;

        if (runtimeAction == null)
        {
            Debug.LogError($"InputActionHandler '{name}' is missing an InputAction reference.");
            return;
        }

        runtimeAction.performed += Execute;
        runtimeAction.canceled += Execute;
        IsContextBound = true;
    }

    public void Enable()
    {
        if (!IsContextBound || IsEnabled || runtimeAction == null)
        {
            return;
        }

        runtimeAction.Enable();
        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled || runtimeAction == null)
        {
            return;
        }

        runtimeAction.Disable();
        IsEnabled = false;
    }

    public void Dispose()
    {
        Disable();
        if (runtimeAction != null)
        {
            runtimeAction.performed -= Execute;
            runtimeAction.canceled -= Execute;
        }

        IsContextBound = false;
        runtimeAction = null;
        eventDispatcher = null;
    }

    protected abstract void Execute (InputAction.CallbackContext context);

    protected virtual bool OnSupportsState(EGameState state)
    {
        return true;
    }
}
