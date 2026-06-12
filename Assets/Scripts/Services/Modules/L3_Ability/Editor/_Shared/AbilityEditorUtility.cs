#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using RedDust.Core;
using RedDust.Properties;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// Ability Editor 共用工具。树操作、数组操作、摘要函数、Search 工具。
    /// </summary>
    public static class AbilityEditorUtility
    {
        // ═══════════════════════════════════════════════════
        // 树操作（替换 EffectEditorWindow / AbilityEditorModel 中的本地函数）
        // ═══════════════════════════════════════════════════

        /// <summary>递归排序树：文件夹优先，再按 DisplayName 字母序。</summary>
        public static void SortTreeRecursive(List<AbilityTreeNode> nodes)
        {
            nodes.Sort((a, b) =>
            {
                if (a.IsFolder != b.IsFolder) return a.IsFolder ? -1 : 1;
                return string.CompareOrdinal(a.DisplayName, b.DisplayName);
            });
            foreach (var n in nodes) SortTreeRecursive(n.Children);
        }

        /// <summary>递归计算各文件夹的 AbilityCount。返回根节点总数。</summary>
        public static int ComputeTreeCounts(List<AbilityTreeNode> roots)
        {
            int CountRecursive(AbilityTreeNode node)
            {
                if (!node.IsFolder) return 1;
                var total = 0;
                foreach (var c in node.Children) total += CountRecursive(c);
                node.AbilityCount = total;
                return total;
            }
            var grandTotal = 0;
            foreach (var root in roots) grandTotal += CountRecursive(root);
            return grandTotal;
        }

        // ═══════════════════════════════════════════════════
        // 数组操作
        // ═══════════════════════════════════════════════════

        /// <summary>从数组中移除指定索引的元素，返回新数组。</summary>
        public static T[] RemoveAt<T>(T[] array, int index)
        {
            if (array == null || index < 0 || index >= array.Length) return array ?? System.Array.Empty<T>();
            var result = new T[array.Length - 1];
            for (int i = 0, j = 0; i < array.Length; i++)
                if (i != index) result[j++] = array[i];
            return result;
        }

        /// <summary>在数组末尾追加一个元素，返回新数组。</summary>
        public static T[] Append<T>(T[] array, T item)
        {
            var old = array ?? System.Array.Empty<T>();
            var result = new T[old.Length + 1];
            System.Array.Copy(old, result, old.Length);
            result[old.Length] = item;
            return result;
        }

        // ═══════════════════════════════════════════════════
        // 摘要函数（统一 MiddlePanel 与 SubAssetPickerView）
        // ═══════════════════════════════════════════════════

        public static string GetActivationSummary(AbilityActivationSO a)
            => a == null ? null : $"{a.activationType} · speed:{a.animationSpeed:F1}";

        public static string GetSearchSummary(AbilitySearchSO s)
            => s == null ? null : $"{s.searchType} · range:{s.range:F1} · max:{s.maxTargets}";

        /// <summary>
        /// Effect 摘要。includeDuration 控制 Damage 类型是否带 duration（MiddlePanel 需要，Picker 不需要）。
        /// </summary>
        public static string GetEffectSummary(EffectSO e, bool includeDuration = true)
        {
            if (e == null) return null;
            if (e is DamageEffectSO d)
            {
                var durPart = includeDuration ? $" · dur:{e.duration:F1}s" : "";
                return $"Damage · {e.effectTag?.FullTag ?? "—"} · base:{d.baseValue:F0}{durPart}";
            }
            if (e is ImpactEffectSO i)
                return $"Impact · {e.effectTag?.FullTag ?? "—"} · stagger:{i.staggerValue:F0}";
            if (e is ExecuteEffectSO x)
                return $"Execute · {e.effectTag?.FullTag ?? "—"} · threshold:{x.hpThreshold:P0}";
            if (e is CostEffectSO c)
                return $"Cost · {c.def?.name ?? "—"} · amount:{c.amount:F0}";
            return $"{e.GetType().Name.Replace("EffectSO", "")} · {e.effectTag?.FullTag ?? "—"}";
        }

        public static string GetNoiseSummary(NoiseEventSO n)
            => n == null ? null : $"level:{n.level:F0} · decay:{n.decayRadius:F1}m";

        public static string GetEffectIcon(EffectSO e)
        {
            if (e == null) return "?";
            if (e is DamageEffectSO) return "Dmg";
            if (e is ImpactEffectSO) return "Imp";
            if (e is ExecuteEffectSO) return "Exe";
            if (e is CostEffectSO) return "Cost";
            return "*";
        }

        public static int GetEffectTypeOrder(EffectSO e)
        {
            if (e is DamageEffectSO) return 0;
            if (e is ImpactEffectSO) return 1;
            if (e is ExecuteEffectSO) return 2;
            if (e is CostEffectSO) return 3;
            return 99;
        }

        // ═══════════════════════════════════════════════════
        // Search 工具
        // ═══════════════════════════════════════════════════

        public static string GetSearchTypeDisplayName(ESearchType t) => t switch
        {
            ESearchType.Cone => "Cone",
            ESearchType.RayLine => "Ray",
            ESearchType.Circle => "Circle",
            _ => t.ToString(),
        };

        public static bool SearchMatchesFilter(AbilitySearchSO s, SearchTypeFilter f) => f switch
        {
            SearchTypeFilter.Cone => s.searchType == ESearchType.Cone,
            SearchTypeFilter.Ray => s.searchType == ESearchType.RayLine,
            SearchTypeFilter.Circle => s.searchType == ESearchType.Circle,
            _ => true,
        };

        // ═══════════════════════════════════════════════════
        // 资产引用检查
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// 查找所有引用了 target 的资产（.asset / .prefab / .unity）。
        /// 通过 GUID 匹配搜索文件内容。
        /// </summary>
        public static List<string> FindReferencers(ScriptableObject target)
        {
            var result = new List<string>();
            if (target == null) return result;

            var targetPath = AssetDatabase.GetAssetPath(target);
            var targetGuid = AssetDatabase.AssetPathToGUID(targetPath);
            if (string.IsNullOrEmpty(targetGuid)) return result;

            var searchDirs = new[] { "Assets/Data/", "Assets/Prefabs/", "Assets/Scenes/" };
            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".asset" && ext != ".prefab" && ext != ".unity") continue;
                    if (file == targetPath) continue;

                    var text = File.ReadAllText(file);
                    if (text.Contains(targetGuid))
                        result.Add(file);
                }
            }
            return result;
        }

        /// <summary>
        /// 删除资产（带引用检查确认）。返回 true 表示已删除。
        /// </summary>
        public static bool DeleteAssetWithConfirm(ScriptableObject asset, string typeLabel)
        {
            if (asset == null) return false;

            var refs = FindReferencers(asset);
            var message = $"Delete '{asset.name}'?";
            if (refs.Count > 0)
            {
                message += $"\n\n⚠ Referenced by {refs.Count} asset(s):";
                foreach (var r in refs)
                    message += $"\n  • {Path.GetFileNameWithoutExtension(r)}";
                message += "\n\nDeleting may break these references.";
            }
            message += "\n\nThis cannot be undone.";

            if (!UnityEditor.EditorUtility.DisplayDialog($"Delete {typeLabel}", message, "Delete", "Cancel"))
                return false;

            var path = AssetDatabase.GetAssetPath(asset);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
#endif
