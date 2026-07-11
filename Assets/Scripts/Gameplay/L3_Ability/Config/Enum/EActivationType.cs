namespace RedDust.Ability
{
    /// <summary>
    /// 技能激活方式。决定按下按键后技能的输入响应模型。
    /// </summary>
    public enum EActivationType
    {
        /// <summary>瞬发。按下即执行完整阶段序列（Windup→Fire→Recovery）。</summary>
        Instant = 0,

        /// <summary>蓄力。按住→蓄力→松手/满蓄→释放。Phase 4.2+。</summary>
        Charged = 1,

        /// <summary>按住持续。按下→进入 Fire→松手→结束。Phase 4.2+。</summary>
        Channel = 2,

        /// <summary>开关切换。按下→开启，再按→关闭。Phase 4.2+。</summary>
        Toggle = 3,
    }
}
