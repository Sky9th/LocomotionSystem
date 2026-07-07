using System.Collections;
using System.Collections.Generic;
using RedDust.Addressables;
using RedDust.Core;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Collects and runs IBootTasks sequentially, then loads the first content scene.
    ///
    /// Splits readiness into two flags:
    ///   BootTasksComplete — set after all tasks finish (TransitionGate gates on this).
    ///   IsReady — set after the first scene fully loads.
    ///
    /// v2: BootPipeline now loads ALL "boot" Addressables once, populates a
    /// BootAssetCatalog, and calls each task's Resolve(catalog) in order.
    /// Tasks no longer load assets independently.
    /// </summary>
    public class BootPipeline : IGameplaySessionHandler
    {
        private readonly List<IBootTask> _tasks = new();
        private AddressablesService _addressables;
        private TransitionGate _gate;
        private LoadProgress _progress;
        private LogChannel _log;

        public bool BootTasksComplete { get; private set; }
        public bool IsReady { get; private set; }

        public void Initialize(AddressablesService addressables, TransitionGate gate, LoadProgress progress)
        {
            _log = LogManager.GetChannel(GetType().Name);
            _addressables = addressables;
            _gate = gate;
            _progress = progress;
        }

        public void Register(IBootTask task) => _tasks.Add(task);

        public void RegisterAll(List<IBootTask> tasks)
        {
            foreach (var t in tasks)
                _tasks.Add(t);
        }

        /// <summary>
        /// 1. Init Addressables
        /// 2. Load all "boot" assets once
        /// 3. Build catalog → Resolve each task in order
        /// 4. Load the first scene
        /// </summary>
        public IEnumerator Run(SceneLoadConfigSO firstSceneConfig)
        {
            _log.Info("Boot pipeline started.");

            _progress.Publish("Initializing...", 0f);
            yield return _addressables.InitializeAsync();

            if (!_addressables.IsInitialized)
            {
                _log.Error("Addressables init failed. Proceeding with degraded functionality.");
                BootTasksComplete = true;
                yield break;
            }

            // ── Phase 1: Load all "boot" assets in one shot ──
            _progress.Publish("Loading boot assets...", 0.1f);
            var assets = new List<Object>();
            bool loadDone = false;
            var label = SceneAssetLabel.Boot.ToLabelStrings()[0];
            _addressables.LoadByLabel<Object>(label, r => { assets = r; loadDone = true; });
            while (!loadDone)
                yield return null;

            Debug.Log($"[BootPipeline] Loaded {assets.Count} boot assets.");

            // ── Phase 2: Build catalog + index by type ──
            var catalog = new BootAssetCatalog(assets);

            // Keep catalog alive on GameRegistry to maintain strong references against
            // Addressables native-side teardown during scene transitions.
            if (GameService.Instance != null)
                GameService.Instance.AssetRegistry.SetCatalog(catalog);

            // ── Phase 3: Resolve each task ──
            int total = _tasks.Count;
            for (int i = 0; i < total; i++)
            {
                var task = _tasks[i];
                _progress.Publish(task.Description, 0.1f + (0.8f * i / total));
                Debug.Log($"[BootPipeline] Running: {task.Description}");
                task.Resolve(catalog);
            }

            BootTasksComplete = true;

            // ── Phase 4: Load the first scene ──
            _progress.Publish("Loading scene...", 0.95f);
            yield return _gate.Begin(firstSceneConfig, null);

            IsReady = true;
            _log.Info("Boot pipeline complete.");
        }

        /// <summary>TransitionGate gates on this, not IsReady, to avoid deadlock.</summary>
        public IEnumerator WaitUntilTasksComplete()
        {
            while (!BootTasksComplete)
                yield return null;
        }

        public void OnGameplaySessionEnd() { }
    }
}
