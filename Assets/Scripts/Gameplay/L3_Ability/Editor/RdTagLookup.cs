#if UNITY_EDITOR
using RedDust.Core.RdTag;
using System.Collections.Generic;
using RedDust.Core.Events;
using UnityEditor;

namespace RedDust.Gameplay.Ability.Editor
{
    /// <summary>
    /// Editor 共享工具 — 全量 RdTagDefSO 按 FullTag 查表。
    /// 消除 5 个 ImportExport 文件中的重复 BuildTagLookup()。
    /// </summary>
    internal static class RdTagLookup
    {
        public static Dictionary<string, RdTagDefSO> Build()
        {
            var dict = new Dictionary<string, RdTagDefSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:RdTagDefSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var t = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(path);
                if (t != null && !string.IsNullOrEmpty(t.FullTag))
                    dict[t.FullTag] = t;
            }
            return dict;
        }
    }
}
#endif
