using Pathfinding;
using RedDust.Core;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Pathfinding
{
    public class PathfindingService : ModuleComponent, IGameplaySessionHandler
    {
        private AstarPath graph;
        private LogChannel _log;
        private EventDispatcherService _dispatcher; // TODO: 替换为 EventHub — EventDispatcher 即将废弃

        public override void OnAssemble()
        {
            _log = LogManager.GetChannel(GetType().Name);

            graph = GetComponent<AstarPath>();
            if (graph == null)
            {
                _log.Error("AstarPath component missing — attach it to the same GameObject as PathfindingService.");
                return;
            }

            _log.Info("Assembled.");
        }

        public override void OnWire()
        {
            GameContext.Instance.RegisterService(this);
            GameContext.Instance.TryResolveService(out _dispatcher);
            Scan();

            GameService.Instance?.NotifyServiceWired();
        }

        private void OnDestroy()
        {
            graph = null;
        }

        // ── IGameplaySessionHandler ──

        public void OnGameplaySessionEnd()
        {
            if (graph != null && graph.IsAnyGraphUpdateQueued)
            {
                graph.FlushGraphUpdates();
            }
            _log.Debug("Session ended — graph updates flushed.");
        }

        // ── Public API ──

        public bool IsWalkable(Vector3 worldPoint)
        {
            if (graph == null || graph.graphs.Length == 0) return false;
            var nn = graph.GetNearest(worldPoint);
            return nn.node != null && nn.node.Walkable;
        }

        public void MarkObstacle(Bounds area)
        {
            if (graph == null) return;
            var guo = new GraphUpdateObject(area);
            graph.UpdateGraphs(guo);
        }

        public void Scan()
        {
            if (graph == null) return;
            graph.Scan();
        }

        public AstarPath Graph => graph;
    }
}
