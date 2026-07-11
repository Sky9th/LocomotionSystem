using RedDust.Core.GameContext;
using System;
using System.Collections.Generic;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Core.GameContext
{
    [DisallowMultipleComponent]
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        [Header("Diagnostics")]
        [SerializeField] private LogLevel logLevel = LogLevel.Warning;

        private LogChannel Log;

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
            if (Log == null) Log = LogManager.GetChannel(nameof(GameContext), logLevel);

            if (isInitialized)
            {
                Log.Debug("Initialize called but already initialized.");
                return;
            }

            Instance = this;
            isInitialized = true;

            Log.Info($"Initialized. RegisteredServiceCount={RegisteredServiceCount}");
        }

        public void UpdateSnapshot<TSnapshot>(TSnapshot snapshot)
            where TSnapshot : struct
        {
            contextSnapshots[typeof(TSnapshot)] = snapshot;
            Log.Debug($"Snapshot updated: {typeof(TSnapshot).Name}");
        }

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

        public void ClearSnapshots()
        {
            contextSnapshots.Clear();
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

        /// <summary>检查服务是否已注册，用于避免重复注册（first-come-first-served）。</summary>
        public bool HasService<TService>()
            where TService : class
        {
            return serviceRegistry.ContainsKey(typeof(TService));
        }

        public void RegisterService<TService>(TService service)
            where TService : class
        {
            if (service == null) return;

            serviceRegistry[typeof(TService)] = service;
            Log.Debug($"Service registered: {typeof(TService).Name}");
        }
    }
}
