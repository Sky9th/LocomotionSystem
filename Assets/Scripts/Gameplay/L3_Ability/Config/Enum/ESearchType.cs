namespace RedDust.Ability
{
    /// <summary>
    /// 战斗命中检测的物理搜索形状。
    /// </summary>
    public enum ESearchType
    {
        /// <summary>扇形搜索。OverlapSphere + 前方角度过滤（横斩、霰弹）。</summary>
        Cone = 0,

        /// <summary>射线搜索。Raycast 指向 + 近线目标检测（手枪、步枪）。</summary>
        RayLine = 1,

        /// <summary>圆形搜索。OverlapSphere 自身周围（旋风斩、战吼）。Phase 4.2+ 实现。</summary>
        Circle = 2
    }
}
