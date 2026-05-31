using System;

namespace RedDust.Character.Locomotion
{
    [Serializable]
    public readonly struct SCharacterDiscrete
    {
        public SCharacterDiscrete(ELocomotionPhase phase, EPosture posture, EMovementGait gait, bool isTurning, float motionSpeedScale = 1f, float effectiveMaxSpeed = 0f)
        {
            Phase = phase;
            Posture = posture;
            Gait = gait;
            IsTurning = isTurning;
            MotionSpeedScale = motionSpeedScale;
            EffectiveMaxSpeed = effectiveMaxSpeed;
        }

        public ELocomotionPhase Phase { get; }
        public EPosture Posture { get; }
        public EMovementGait Gait { get; }
        public bool IsTurning { get; }

        /// <summary>
        /// 当前有效速度与基础步态速度的比值 (0~∞)。
        /// 由 Locomotion 评估（地形/增益等），供 Animation 等下游使用。
        /// </summary>
        public float MotionSpeedScale { get; }

        /// <summary>
        /// 当前有效最大速度 (m/s) = gaitSpeed × MotionSpeedScale。
        /// Locomotion 计算的最终值，Pathfinding 直接设置 ai.maxSpeed。
        /// </summary>
        public float EffectiveMaxSpeed { get; }

        public static SCharacterDiscrete Default => new(
            ELocomotionPhase.GroundedIdle, EPosture.Standing, EMovementGait.Idle, false);
    }
}
