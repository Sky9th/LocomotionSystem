namespace RedDust.Shared
{
    /// <summary>
    /// Centralized naming constants for common child transforms and rig elements.
    /// Keeping these in one place avoids hard-coded strings scattered across
    /// locomotion, camera, and other systems.
    /// </summary>
    public static class CommonConstants
    {
        public const string ModelChildName = "Model";
        public const string FollowAnchorName = "Anchor";

        /// <summary>官方内容命名空间前缀。Mod 内容使用自己的命名空间，实现隔离。</summary>
        public const string OfficialNamespace = "rd.";
    }
}
