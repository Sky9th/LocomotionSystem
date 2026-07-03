using Pathfinding;
using UnityEngine;
using RedDust.Core;
using RedDust.Character.Locomotion;

namespace RedDust.Character.Pathfinding
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Seeker), typeof(AIPath))]
    public sealed class PathfindingAgent : ModuleChildMono
    {
        private Seeker seeker;
        private AIPath ai;

        public Vector3 PathDirection
        {
            get
            {
                if (ai == null || !ai.hasPath || ai.reachedEndOfPath) return Vector3.zero;

                var dir = ai.steeringTarget - transform.position;
                dir.y = 0f;
                return dir.sqrMagnitude > Mathf.Epsilon ? dir.normalized : Vector3.zero;
            }
        }

        public bool HasPath => ai != null && ai.hasPath;
        public bool HasReachedDestination => ai != null && ai.reachedEndOfPath;
        public bool HasActivePath => HasPath && !HasReachedDestination;

        public Vector3 DesiredVelocity => ai != null ? ai.desiredVelocity : Vector3.zero;

        // ── Lifecycle ──

        public override void OnAssemble()
        {
            base.OnAssemble();
            seeker = GetComponent<Seeker>();
            ai = GetComponent<AIPath>();

            ai.updatePosition = false;
            ai.updateRotation = false;
            ai.slowWhenNotFacingTarget = false;

            Teleport(transform.position);
        }

        // ── API ──

        /// <summary>
        /// 同步 Locomotion 状态到 A*：设置有效最大速度并同步 Transform。
        /// </summary>
        public void SyncLocomotion(in SCharacterDiscrete discrete)
        {
            if (ai != null)
            {
                ai.maxSpeed = discrete.EffectiveMaxSpeed;
                ai.FinalizeMovement(transform.position, transform.rotation);
            }
        }

        public void SetDestination(Vector3 worldPoint)
        {
            if (ai == null) return;
            ai.destination = worldPoint;
            ai.SearchPath();
        }

        public void Stop()
        {
            if (ai != null)
                ai.isStopped = true;
        }

        public void Resume()
        {
            if (ai != null)
                ai.isStopped = false;
        }

        public void Teleport(Vector3 position)
        {
            if (ai != null)
                ai.Teleport(position, false);
        }
    }
}
