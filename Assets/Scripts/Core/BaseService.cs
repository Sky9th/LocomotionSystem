using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared base class for all runtime managers/services that the GameManager bootstraps.
/// Provides a deterministic Register(GameContext) entry point so each system can
/// bind into the global context without duplicating guard logic.
/// </summary>
public abstract class BaseService : MonoBehaviour
{
    public bool IsRegistered { get; private set; }
    protected GameContext GameContext { get; private set; }
    protected EventDispatcher Dispatcher { get; private set; }
    private readonly Dictionary<Type, object> serviceCache = new();
    private bool subscriptionsActivated;
    private bool isInitialized;

    /// <summary>
    /// Called by GameManager to hook this service into the GameContext.
    /// Derived classes should place their initialization logic inside OnRegister.
    /// </summary>
    public void Register(GameContext context)
    {
        if (context == null)
        {
            Debug.LogError($"{name} cannot register without a valid GameContext reference.", this);
            return;
        }

        if (IsRegistered)
        {
            return;
        }

        GameContext = context;
        serviceCache.Clear();
        Dispatcher = null;
        subscriptionsActivated = false;
        isInitialized = false;
        if (OnRegister(context))
        {
            IsRegistered = true;
        }
        else
        {
            GameContext = null;
            serviceCache.Clear();
        }
    }

    protected abstract bool OnRegister(GameContext context);

    /// <summary>
    /// Attempts to resolve the requested service from GameContext.
    /// </summary>
    protected bool TryResolveService<TService>(out TService service, bool logWarning = true)
        where TService : class
    {
        service = null;

        if (serviceCache.TryGetValue(typeof(TService), out var cached) && cached is TService cachedService)
        {
            service = cachedService;
            return true;
        }

        if (GameContext == null)
        {
            if (logWarning)
            {
                Debug.LogWarning($"{name} cannot resolve {typeof(TService).Name} because GameContext is missing.", this);
            }

            return false;
        }

        if (GameContext.TryResolveService(out service))
        {
            if (service != null)
            {
                serviceCache[typeof(TService)] = service;
            }
            return true;
        }

        if (logWarning)
        {
            Debug.LogWarning($"{name} could not resolve service {typeof(TService).Name}. Ensure it is registered before {GetType().Name}.", this);
        }

        return false;
    }

    /// <summary>
    /// Resolves the requested service or logs an error when it is unavailable.
    /// </summary>
    protected TService RequireService<TService>()
        where TService : class
    {
        if (TryResolveService(out TService service, logWarning: false))
        {
            return service;
        }

        Debug.LogError($"{name} requires service {typeof(TService).Name} but it has not been registered.", this);
        return null;
    }

    internal void AttachDispatcher(EventDispatcher dispatcher)
    {
        if (!IsRegistered)
        {
            Debug.LogWarning($"{name} cannot attach dispatcher before registration completes.", this);
            return;
        }

        if (dispatcher == null)
        {
            Debug.LogWarning($"{name} cannot attach a null dispatcher.", this);
            return;
        }

        if (Dispatcher == dispatcher)
        {
            return;
        }

        Dispatcher = dispatcher;
        subscriptionsActivated = false;
        OnDispatcherAvailable();
    }

    internal void ActivateSubscriptions()
    {
        if (!IsRegistered)
        {
            Debug.LogWarning($"{name} cannot activate subscriptions before registration completes.", this);
            return;
        }

        if (Dispatcher == null)
        {
            Debug.LogWarning($"{name} cannot activate subscriptions without a dispatcher reference.", this);
            return;
        }

        if (subscriptionsActivated)
        {
            return;
        }

        SubscribeToDispatcher();
        subscriptionsActivated = true;
    }

    /// <summary>
    /// Called by GameManager after all services have registered, attached a dispatcher,
    /// and activated their subscriptions. Use this for any final initialization that
    /// relies on other services being fully ready.
    /// </summary>
    protected virtual void OnInitialized()
    {
    }

    /// <summary>
    /// Internal entry point used by GameManager to notify services that the
    /// bootstrap sequence has completed.
    /// </summary>
    internal void NotifyInitialized()
    {
        if (!IsRegistered || isInitialized)
        {
            return;
        }

        OnInitialized();
        isInitialized = true;
    }

    protected virtual void OnDispatcherAvailable()
    {
    }

    protected virtual void SubscribeToDispatcher()
    {
    }
}
