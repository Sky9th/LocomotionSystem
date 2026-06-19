using UnityEngine;

namespace RedDust.Character.Locomotion
{
    [CreateAssetMenu(fileName = "LocomotionProfileSO", menuName = "RedDust/Character/Locomotion Profile")]
    public sealed class LocomotionProfileSO : ScriptableObject
    {
        /// <summary>
        /// 角色移动速度矩阵 (m/s)，按 posture × gait 组织。
        /// 与 AnimationModeConfigSO.animNativeSpeed 不同：
        /// - 这里的值 = 角色物理/A* 寻路的目标速度
        /// - animNativeSpeed = 动画在 Speed=1 时自然产生的速度
        /// 乘积 = gaitSpeed / animNativeSpeed 由 AnimationBrain 计算。
        /// speed=0 表示该姿势+步态组合不可用。
        /// </summary>
        [Header("Standing Speeds")]
        [Min(0f)] public float standWalk = 2f;
        [Min(0f)] public float standRun = 5f;
        [Min(0f)] public float standSprint = 8f;
        [Min(0f)] public float standCrawl = 1f;

        [Header("Crouching Speeds")]
        [Min(0f)] public float crouchWalk = 1.2f;
        [Min(0f)] public float crouchRun = 3f;
        [Min(0f)] public float crouchSprint;
        [Min(0f)] public float crouchCrawl = 0.8f;

        [Header("Prone Speeds")]
        [Min(0f)] public float proneWalk = 0.5f;
        [Min(0f)] public float proneRun;
        [Min(0f)] public float proneSprint;
        [Min(0f)] public float proneCrawl = 0.3f;

        [Header("Acceleration")]
        [Min(0f)] public float acceleration = 5f;

        /// <summary>返回指定 posture+gait 组合的速度 (m/s)。返回 0 表示该组合不可用。</summary>
        public float GetSpeed(EPosture posture, EMovementGait gait) => (posture, gait) switch
        {
            (EPosture.Standing, EMovementGait.Walk) => standWalk,
            (EPosture.Standing, EMovementGait.Run) => standRun,
            (EPosture.Standing, EMovementGait.Sprint) => standSprint,
            (EPosture.Standing, EMovementGait.Crawl) => standCrawl,
            (EPosture.Crouching, EMovementGait.Walk) => crouchWalk,
            (EPosture.Crouching, EMovementGait.Run) => crouchRun,
            (EPosture.Crouching, EMovementGait.Sprint) => crouchSprint,
            (EPosture.Crouching, EMovementGait.Crawl) => crouchCrawl,
            (EPosture.Prone, EMovementGait.Walk) => proneWalk,
            (EPosture.Prone, EMovementGait.Run) => proneRun,
            (EPosture.Prone, EMovementGait.Sprint) => proneSprint,
            (EPosture.Prone, EMovementGait.Crawl) => proneCrawl,
            _ => 0f,
        };

        /// <summary>该 posture+gait 组合是否可用（速度 > 0）。</summary>
        public bool IsAllowed(EPosture posture, EMovementGait gait) => GetSpeed(posture, gait) > 0f;
    }
}
