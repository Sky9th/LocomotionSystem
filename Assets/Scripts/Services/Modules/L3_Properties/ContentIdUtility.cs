using RedDust.Properties;

namespace RedDust.Properties
{
    /// <summary>
    /// contentId 拼接工具。从 PropertyTable 读取 Common/Category (RdTag FullPath) + Common/Id (string)，
    /// 组合为 Mod 可寻址的稳定标识符。
    /// </summary>
    public static class ContentIdUtility
    {
        /// <summary>官方内容前缀。Mod 使用自己的命名空间。</summary>
        public const string OfficialPrefix = "rd.";

        /// <summary>
        /// 拼接 contentId = {prefix}.{categoryFullPath}.{id}
        /// 任一参数为空时返回 null。
        /// </summary>
        public static string BuildContentId(string prefix, string categoryFullPath, string id)
        {
            if (string.IsNullOrEmpty(categoryFullPath) || string.IsNullOrEmpty(id))
                return null;
            return $"{prefix}{categoryFullPath}.{id}";
        }

        /// <summary>使用官方前缀 "rd." 拼接 contentId。</summary>
        public static string BuildContentId(string categoryFullPath, string id)
            => BuildContentId(OfficialPrefix, categoryFullPath, id);

        /// <summary>从 PropertyTable 读取 Category + Id 并拼接 contentId。</summary>
        public static string GetContentId(PropertyTable props)
        {
            var catFullPath = props.GetRdTag("Common/Category");
            var id = props.GetString("Common/Id");
            return BuildContentId(catFullPath, id);
        }
    }
}
