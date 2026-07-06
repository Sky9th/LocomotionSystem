using System.Collections;
using System.Collections.Generic;
using RedDust.Addressables;
using RedDust.Core;
using RedDust.Core.Events;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Private helper owned by SceneService. Runs registered <see cref="IBootTask"/>s
    /// before the first content scene activates.
    ///
    /// Not a service — not registered in GameContext.
    /// </summary>
    public class LoadingOrchestrator : IGameplaySessionHandler
    {
        private readonly List<IBootTask> _tasks = new();
        private EventHub _eventHub;
        private AddressablesService _addressables;
        private LogChannel _log;

        public bool IsPreloadComplete { get; private set; }

        public void Initialize(EventHub eventHub, AddressablesService addressables)
        {
            _log = LogManager.GetChannel(GetType().Name);
            _eventHub = eventHub;
            _addressables = addressables;
        }

        /// <summary>
        /// Register a boot task. Called during OnWire. Tasks execute in registration order.
        /// </summary>
        public void Register(IBootTask task)
        {
            _tasks.Add(task);
        }

        public void BeginPreload(MonoBehaviour owner)
        {
            if (IsPreloadComplete) return;
            owner.StartCoroutine(RunPreloadPhase());
        }

        public IEnumerator WaitForPreload()
        {
            while (!IsPreloadComplete)
                yield return null;
        }

        private IEnumerator RunPreloadPhase()
        {
            _log.Info("Preload phase started.");
            int total = _tasks.Count + 1;
            int current = 0;

            // Step 1: Initialize Addressables
            PublishProgress("Initializing...", (float)current / total);
            yield return _addressables.InitializeAsync();

            if (!_addressables.IsInitialized)
            {
                _log.Error("Addressables initialization failed.");
                IsPreloadComplete = true;
                yield break;
            }
            current++;

            // Step 2: Run registered boot tasks
            foreach (var task in _tasks)
            {
                current++;
                PublishProgress(task.Description, (float)current / total);
                yield return task.Execute();
            }

            // Step 3: Complete
            PublishProgress("Ready", 1.0f);
            IsPreloadComplete = true;
            _log.Info($"Preload phase complete ({_tasks.Count} tasks).");
        }

        private void PublishProgress(string phaseName, float progress)
        {
            _eventHub?.Get<SceneProgressEvent>()?.Raise(new SLoadingProgress(phaseName, progress));
        }

        public void OnGameplaySessionEnd() { }
    }
}
