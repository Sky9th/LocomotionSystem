using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central repository for runtime scene context. Lives under GameManager and
/// exposes shared references (cameras, player roots, services) to the rest of the
/// project through a controlled API.
/// </summary>
[DisallowMultipleComponent]
public class GameContext : MonoBehaviour
{
    public static GameContext Instance { get; private set; }

    [Header("Diagnostics")]
    [SerializeField] private bool logDebugInfo;

    private readonly Dictionary<Type, object> serviceRegistry = new();
    private readonly Dictionary<Type, object> contextSnapshots = new();
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public int RegisteredServiceCount => serviceRegistry.Count;
    public int SnapshotCount => contextSnapshots.Count;
    public IEnumerable<Type> RegisteredServiceTypes => serviceRegistry.Keys;
    public IEnumerable<Type> SnapshotStructTypes => contextSnapshots.Keys;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        serviceRegistry.Clear();
        isInitialized = false;
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;

        if (logDebugInfo)
        {
            Debug.Log("GameContext initialized.", this);
        }
    }

    /// <summary>
    /// Stores or refreshes a snapshot struct pushed from another subsystem (e.g. camera, player, AI).
    /// </summary>
    public void UpdateSnapshot<TSnapshot>(TSnapshot snapshot)
        where TSnapshot : struct
    {
        contextSnapshots[typeof(TSnapshot)] = snapshot;

        if (logDebugInfo)
        {
            Debug.Log($"Snapshot updated: {typeof(TSnapshot).Name}", this);
        }
    }

    /// <summary>
    /// Attempts to retrieve the latest snapshot for the requested struct type.
    /// </summary>
    public bool TryGetSnapshot<TSnapshot>(out TSnapshot snapshot)
        where TSnapshot : struct
    {
        if (contextSnapshots.TryGetValue(typeof(TSnapshot), out var boxed) && boxed is TSnapshot typed)
        {
            snapshot = typed;
            return true;
        }

        snapshot = default;
        return false;
    }

    public bool TryResolveService<TService>(out TService service)
        where TService : class
    {
        if (serviceRegistry.TryGetValue(typeof(TService), out var boxed) && boxed is TService typed)
        {
            service = typed;
            return true;
        }

        service = null;
        return false;
    }

    public void RegisterService<TService>(TService service)
        where TService : class
    {
        if (service == null)
        {
            return;
        }

        serviceRegistry[typeof(TService)] = service;
    }
}
