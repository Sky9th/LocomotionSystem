namespace RedDust.Character.Combat
{
    /// <summary>
    /// 技能动画分层。映射 AnimationBrain 的 AnimancerLayer。
    /// </summary>
    public enum ESkillAnimationLayer
    {
        /// <summary>全身动画，锁定移动。ChannelMask = 1 &lt;&lt; 0。</summary>
        FullBody = 0,

        /// <summary>上半身动画，不锁移动。ChannelMask = 1 &lt;&lt; 1。</summary>
        UpperBody = 1
    }
}
