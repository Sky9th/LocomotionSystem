using Pathfinding;
using RedDust.Core;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Pathfinding
{
    public class PathfindingService : ModuleChildMono, IGameplaySessionHandler
    {
        private AstarPath graph;
        private LogChannel _log;
        public override void OnAssemble()
        {
            _log = LogManager.GetChannel(GetType().Name);

            graph = GetComponent<AstarPath>();
            if (graph == null)
            {
                _log.Error("AstarPath component missing — attach it to the same GameObject as PathfindingService.");
                return;
            }

            graph.Scan();
            _log.Info("Assembled and graph scanned.");

            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire() { }

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
