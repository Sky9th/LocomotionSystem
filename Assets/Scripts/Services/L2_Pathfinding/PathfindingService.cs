using Pathfinding;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Pathfinding
{
    public class PathfindingService : BaseService, IGameplaySessionHandler
    {
        private AstarPath graph;

        // ── BaseService ──

        protected override bool OnRegister(GameContext context)
        {
            context.RegisterService(this);

            graph = GetComponent<AstarPath>();
            if (graph == null)
            {
                Log.Error("AstarPath component missing — attach it to the same GameObject as PathfindingService.");
                return false;
            }

            Log.Info("Registered.");
            return true;
        }

        protected override void OnServicesReady()
        {
            Scan();
        }

        protected override void OnSubscriptionsActivated() { }

        protected override void OnDispatcherAttached() { }

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
            Log.Debug("Session ended — graph updates flushed.");
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
