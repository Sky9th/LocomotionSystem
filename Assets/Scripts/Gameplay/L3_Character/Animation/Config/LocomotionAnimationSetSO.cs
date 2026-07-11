using Animancer;
using UnityEngine;
using RedDust.Gameplay.Character;

namespace RedDust.Gameplay.Character.Animation
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

        [Header("Crouch")]
        public ClipTransition crouchIdle;
        public MixerTransition2D crouchMixer;

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

        [Header("Traversal - Climb Up")]
        public ClipTransition climbUpHalfMeter;
        public ClipTransition climbUp1meter;
        public ClipTransition climbUp2meter;

        [Header("Traversal - Climb Down")]
        public ClipTransition climbDown1meter;
        public ClipTransition climbDown2meter;

        [Header("Traversal - Land")]
        public ClipTransition landFromWall;

        [Header("Hit Reaction — 轻受击")]
        [Tooltip("轻受击 4 方向混合 (Flinch)。X=左右, Y=前后 (相对角色朝向)。")]
        public MixerTransition2D hitReactionFlinch;

        [Header("Hit Reaction — 重受击")]
        [Tooltip("重受击 4 方向混合 (Stagger)。")]
        public MixerTransition2D hitReactionStagger;

        [Header("Hit Reaction — 击倒")]
        [Tooltip("击倒倒地 4 方向混合。")]
        public MixerTransition2D hitReactionKnockdown;

        [Header("Hit Reaction — 起身")]
        [Tooltip("倒地起身 4 方向混合。")]
        public MixerTransition2D hitReactionGetUp;

        /// <summary>
        /// 是否具备完整 locomotion。animset 无 walk/run 时 BaseLayer 不 swap，仅用 idleL 做 UpperBody 覆盖。
        /// 注意：MixerTransition2D 是内联序列化对象，永远非 null，需检查内部是否实际分配了动画。
        /// </summary>
        public bool HasFullLocomotion =>
            idleL.Clip != null &&
            walkMixer.Animations is { Length: > 0 } &&
            runMixer.Animations is { Length: > 0 };
    }
}
