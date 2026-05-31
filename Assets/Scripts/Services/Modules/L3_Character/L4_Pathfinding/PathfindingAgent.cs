using Pathfinding;
using UnityEngine;
using RedDust.Character.Locomotion;

namespace RedDust.Character.Pathfinding
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Seeker), typeof(AIPath))]
    public sealed class PathfindingAgent : MonoBehaviour
    {
        private Seeker seeker;
        private AIPath ai;
        private LocomotionProfile locomotionProfile;
        private EMovementGait currentGait = EMovementGait.Run;

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

        public Vector3 DesiredVelocity => ai != null ? ai.desiredVelocity : Vector3.zero;

        /// <summary>
        /// 归一化速度乘数 (0~1)。A* desiredVelocity 与配置最大速度的比值，
        /// 供 Locomotion/Animation 等下游系统使用。
        /// 接近路径终点时 A* 自动减速，此值会平滑下降。
        /// </summary>
        public float DesiredSpeedMultiplier
        {
            get
            {
                if (ai == null || locomotionProfile == null) return 1f;
                // 无路径或已到达 → 不干预，让 Motor 自行决定
                if (!ai.hasPath || ai.reachedEndOfPath) return 1f;
                float maxSpeed = locomotionProfile.GetSpeedForGait(currentGait);
                if (maxSpeed <= 0f) return 1f;
                return Mathf.Clamp01(ai.desiredVelocity.magnitude / maxSpeed);
            }
        }

        // ── Mono ──

        private void Awake()
        {
            seeker = GetComponent<Seeker>();
            ai = GetComponent<AIPath>();

            ai.updatePosition = false;
            ai.updateRotation = false;
            ai.slowWhenNotFacingTarget = false;

            var actor = GetComponent<CharacterActor>();
            if (actor != null)
            {
                locomotionProfile = actor.LocomotionProfile;
                if (locomotionProfile != null)
                    ai.maxSpeed = locomotionProfile.GetSpeedForGait(currentGait);
            }
        }

        private void Start()
        {
            Teleport(transform.position);
        }

        // ── API ──

        /// <summary>每帧更新当前步态，同步 AIPath.maxSpeed</summary>
        public void UpdateGaitSpeed(EMovementGait gait)
        {
            currentGait = gait;
            if (ai != null && locomotionProfile != null)
                ai.maxSpeed = locomotionProfile.GetSpeedForGait(gait);
        }

        public void SyncPosition()
        {
            if (ai != null)
                ai.FinalizeMovement(transform.position, transform.rotation);
        }

        public void SetDestination(Vector3 worldPoint)
        {
            if (seeker == null) return;
            seeker.StartPath(transform.position, worldPoint, null);
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
