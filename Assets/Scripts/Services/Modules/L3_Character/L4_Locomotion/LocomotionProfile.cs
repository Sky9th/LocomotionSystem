using UnityEngine;

namespace RedDust.Character.Locomotion
{
    [CreateAssetMenu(fileName = "LocomotionProfile", menuName = "RedDust/Character/Locomotion Profile")]
    public sealed class LocomotionProfile : ScriptableObject
    {
        /// <summary>
        /// 角色期望移速 (m/s)。按角色类型配置（强化人 10m/s，老人 2m/s）。
        /// 与 LocomotionModeProfile.animNativeSpeed（动画原生速度）不同：
        /// - gaitSpeed = 角色物理/A* 寻路的目标速度
        /// - animNativeSpeed = 动画在 Speed=1 时自然产生的速度
        /// 乘积 = gaitSpeed / animNativeSpeed 由 AnimationBrain 计算。
        /// </summary>
        [Header("Motion - Gait Speeds")]
        [Min(0f)] public float walkSpeed = 2f;
        [Min(0f)] public float runSpeed = 5f;
        [Min(0f)] public float sprintSpeed = 8f;
        [Min(0f)] public float crawlSpeed = 1f;
        [Min(0f)] public float acceleration = 5f;

        /// <summary>返回指定步态对应的最大速度</summary>
        public float GetSpeedForGait(EMovementGait gait) => gait switch
        {
            EMovementGait.Walk => walkSpeed,
            EMovementGait.Run => runSpeed,
            EMovementGait.Sprint => sprintSpeed,
            EMovementGait.Crawl => crawlSpeed,
            _ => runSpeed,
        };

        [Header("Abilities")]
        public bool canSprint = true;
        public bool canCrouch = true;
        public bool canProne = true;

        [Header("Turning")]
        [Range(0f, 180f)] public float turnEnterAngle = 65f;
        [Range(0f, 25f)] public float turnCompletionAngle = 5f;
    }
}
