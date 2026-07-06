using System.Collections;
using System.Collections.Generic;
using RedDust.Addressables;
using RedDust.Core;
using RedDust.Shared;

namespace RedDust.GameScene
{
    /// <summary>
    /// Collects and runs IBootTasks sequentially, then loads the first content scene.
    /// Splits readiness into two flags to avoid deadlock with TransitionGate:
    ///   BootTasksComplete — set after all tasks finish (TransitionGate gates on this).
    ///   IsReady — set after the first scene fully loads.
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
        /// Run all boot tasks → load scene-configs → build config registry → load first scene.
        /// Called once by SceneService.BeginPreload.
        /// </summary>
        public IEnumerator Run(SceneLoadConfigSO firstSceneConfig)
        {
            _log.Info("Boot pipeline started.");

            // Init Addressables
            _progress.Publish("Initializing...", 0f);
            yield return _addressables.InitializeAsync();

            if (!_addressables.IsInitialized)
            {
                _log.Error("Addressables init failed. Proceeding with degraded functionality.");
                BootTasksComplete = true;
                yield break;
            }

            // Run registered boot tasks
            int total = _tasks.Count;
            int current = 0;

            foreach (var task in _tasks)
            {
                _progress.Publish(task.Description, (float)current / total);
                yield return task.Execute();
                current++;
            }

            BootTasksComplete = true;

            // Load the first scene
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
