using Animancer;
using UnityEngine;
using RedDust.Character;

namespace RedDust.Character.Animation
{
    /// <summary>
    /// 单套 Locomotion 动画引用集合 — 仅包含 FSM 实际使用的字段。
    /// 每种握持姿态（Unarmed / OneHanded / Shield / TwoHanded / DualWield）各自一份资产。
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocomotionAnimationSetSO",
        menuName = "RedDust/Animation/Locomotion/Locomotion Animation Set")]
    public sealed class LocomotionAnimationSetSO : ScriptableObject
    {
        [Header("Idle")]
        public ClipTransition idleL;

        [Header("Move")]
        public MixerTransition2D walkMixer;
        public MixerTransition2D runMixer;
        public ClipTransition sprint;

        [Header("Move Native Speed")]
        [Tooltip("Walk 动画在 Speed=1.0 时的实际位移速度 (m/s)")]
        [Min(0.01f)] public float walkAnimNativeSpeed = 1.5f;
        [Tooltip("Run 动画在 Speed=1.0 时的实际位移速度 (m/s)")]
        [Min(0.01f)] public float runAnimNativeSpeed = 5f;
        [Tooltip("Sprint 动画在 Speed=1.0 时的实际位移速度 (m/s)")]
        [Min(0.01f)] public float sprintAnimNativeSpeed = 7f;
        [Tooltip("Crawl 动画在 Speed=1.0 时的实际位移速度 (m/s)")]
        [Min(0.01f)] public float crawlAnimNativeSpeed = 1f;

        /// <summary>返回该动画集在指定步态下的基础速度 (m/s)。0 表示不支持此步态。</summary>
        public float GetNativeSpeed(EMovementGait gait) => gait switch
        {
            EMovementGait.Walk => walkAnimNativeSpeed,
            EMovementGait.Run => runAnimNativeSpeed,
            EMovementGait.Sprint => sprintAnimNativeSpeed,
            EMovementGait.Crawl => crawlAnimNativeSpeed,
            _ => 0f,
        };

        [Header("Turn")]
        public ClipTransition turnInPlace90L;
        public ClipTransition turnInPlace90R;

        [Header("Air / Land")]
        public LinearMixerTransition airLight;
        public LinearMixerTransition airHard;
        public LinearMixerTransition landLight;
        public LinearMixerTransition landHard;
    }
}
