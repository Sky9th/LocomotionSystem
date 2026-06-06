using Animancer;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能「怎么放」的完整定义。封装激活方式、动画、阶段时机。
    ///
    /// 核心设计：动画就是阶段机的时间轴本体。
    /// Phase Markers 描述动画本身的自然阶段（以 speed=1.0 为基准），
    /// 不命令动画走多快。animationSpeed 是唯一的调参旋钮——
    /// 实际阶段时间 = marker / animationSpeed。
    ///
    /// AbilityDefSO 持有此资产引用，AbilityDriver 消费。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Activation", fileName = "Activation_")]
    public sealed class AbilityActivationSO : ScriptableObject
    {
        [Header("Activation Type")]
        [Tooltip("技能输入响应模型。")]
        public EActivationType activationType;

        [Tooltip("蓄力最大时长（秒）。>0 且 Charged 型时有效。Phase 4.2+ 实现。")]
        public float maxChargeTime;

        [Tooltip("蓄满后自动释放，不等松手。Phase 4.2+ 实现。")]
        public bool autoReleaseAtFullCharge;

        [Header("Animation")]
        [Tooltip("Animancer StringAsset 动画引用。动画本身就是技能阶段的时间轴。")]
        public StringAsset animationAsset;

        [Tooltip("动画播放层。FullBody 锁定移动（全身动画），UpperBody 不锁（上半身动画）。")]
        public EAbilityAnimationLayer animationLayer;

        [Range(0.1f, 3f)]
        [Tooltip("动画播放速度倍率。唯一调参旋钮。1=原速，1.2=全体快 20%，0.8=全体慢 20%。")]
        public float animationSpeed = 1f;

        [Tooltip("是否使用动画根运动驱动角色位移。")]
        public bool rootMotion;

        [Header("Phase Markers — 描述动画本身的天然阶段（speed=1.0 基准）")]
        [Tooltip("前摇时长（秒）。动画从开始到进入激发窗口的时间。")]
        public float windupDuration;

        [Tooltip("激发窗口时长（秒）。此期间 AbilityDriver 每帧执行命中检测。")]
        public float fireWindowDuration;

        [Tooltip("前摇期间是否可被打断（翻滚/格挡）。")]
        public bool canCancelWindup;

        [Tooltip("后摇期间是否可被下一技能/翻滚打断。")]
        public bool canCancelRecovery;

        // ── Recovery: 由 AbilityDriver 运行时从 AnimationClip.length 计算 ──
        // recovery = clipLength / animationSpeed - (windup + fire) / animationSpeed
    }
}
