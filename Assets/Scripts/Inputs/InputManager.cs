using System;
using UnityEngine;

/// <summary>
/// Central coordinator for all gameplay input helpers. It owns their lifecycle,
/// aggregates snapshots, and keeps the EventDispatcher wiring deterministic.
/// </summary>
[DisallowMultipleComponent]
public class InputManager : BaseService
{
    [SerializeField] private InputActionHandler[] actionHandlers = Array.Empty<InputActionHandler>();

    private bool actionsConfigured;
    private EGameState currentGameState = EGameState.Initializing;
    private bool hasGameStateSnapshot;

    public bool AreActionsConfigured => actionsConfigured;

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);
        actionsConfigured = false;
        return true;
    }

    protected override void OnDispatcherAttached()
    {
        base.OnDispatcherAttached();
        ConfigureActions();

        if (isActiveAndEnabled)
        {
            EnableActions();
        }

        SyncInitialGameState();
    }

    protected override void OnSubscriptionsActivated()
    {
        Dispatcher.Subscribe<SGameState>(HandleGameStateChanged);
    }

    private void ConfigureActions()
    {
        if (actionsConfigured)
        {
            return;
        }

        foreach (var handler in actionHandlers)
        {
            handler?.InitializeHandler(Dispatcher);
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
        Dispatcher?.Unsubscribe<SGameState>(HandleGameStateChanged);

        foreach (var handler in actionHandlers)
        {
            handler?.Dispose();
        }
    }

    private void EnableActions()
    {
        if (!actionsConfigured)
        {
            return;
        }

        foreach (var handler in actionHandlers)
        {
            handler?.Enable();
        }

        EnforceHandlerStatePermissions();
    }

    private void DisableActions()
    {
        if (!actionsConfigured)
        {
            return;
        }

        foreach (var handler in actionHandlers)
        {
            handler?.Disable();
        }
    }

    private void HandleGameStateChanged(SGameState snapshot, MetaStruct meta)
    {
        ApplyGameState(snapshot.CurrentState);
    }

    private void SyncInitialGameState()
    {
        if (GameContext != null && GameContext.TryGetSnapshot(out SGameState snapshot))
        {
            ApplyGameState(snapshot.CurrentState, force: true);
        }
        else
        {
            ApplyGameState(EGameState.Initializing, force: true);
        }
    }

    private void ApplyGameState(EGameState nextState, bool force = false)
    {
        if (!force && hasGameStateSnapshot && nextState == currentGameState)
        {
            return;
        }

        currentGameState = nextState;
        hasGameStateSnapshot = true;

        if (!actionsConfigured)
        {
            return;
        }

        EnforceHandlerStatePermissions();
    }

    private void EnforceHandlerStatePermissions()
    {
        if (!actionsConfigured)
        {
            return;
        }

        bool canEnableHandlers = IsRegistered && isActiveAndEnabled;
        foreach (var handler in actionHandlers)
        {
            if (handler == null)
            {
                continue;
            }

            bool supportsState = hasGameStateSnapshot ? handler.SupportsState(currentGameState) : true;
            if (!supportsState || !canEnableHandlers)
            {
                handler.Disable();
            }
            else
            {
                handler.Enable();
            }
        }
    }

    protected override void OnServicesReady()
    {
    }
}
