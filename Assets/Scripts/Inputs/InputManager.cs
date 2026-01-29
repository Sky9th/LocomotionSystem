using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Central coordinator for all gameplay input helpers. It owns their lifecycle,
/// aggregates snapshots, and keeps the EventDispatcher wiring deterministic.
/// </summary>
[DisallowMultipleComponent]
public class InputManager : RuntimeServiceBase
{
    [SerializeField] private InputActionHandler[] actionHandlers;

    private EventDispatcher eventDispatcher;
    private bool actionsConfigured;

    public bool AreActionsConfigured => actionsConfigured;

    protected override bool OnRegister(GameContext context)
    {
        if (!context.TryResolveService(out EventDispatcher dispatcher))
        {
            Debug.LogError("InputManager requires EventDispatcher to be registered first.", this);
            return false;
        }

        eventDispatcher = dispatcher;
        context.RegisterService(this);
        ConfigureActions();

        if (isActiveAndEnabled)
        {
            EnableActions();
        }

        return true;
    }

    private void ConfigureActions()
    {
        if (actionHandlers == null || eventDispatcher == null)
        {
            return;
        }

        foreach (var handler in actionHandlers)
        {
            handler?.InitializeHandler(eventDispatcher);
        }

        actionsConfigured = true;
    }

    private void OnEnable()
    {
        if (IsRegistered)
        {
            EnableActions();
        }
    }

    private void OnDisable()
    {
        DisableActions();
    }

    private void OnDestroy()
    {

        if (actionHandlers == null)
        {
            return;
        }

        foreach (var handler in actionHandlers)
        {
            handler?.Dispose();
        }
    }

    private void EnableActions()
    {
        if (actionHandlers == null)
        {
            return;
        }

        foreach (var handler in actionHandlers)
        {
            handler?.Enable();
        }
    }

    private void DisableActions()
    {
        if (actionHandlers == null)
        {
            return;
        }

        foreach (var handler in actionHandlers)
        {
            handler?.Disable();
        }
    }
}
