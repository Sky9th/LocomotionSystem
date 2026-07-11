using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace RedDust.Gameplay.Properties.Editor
{
    /// <summary>
    /// Editor 工具—扫描所有 [PropertyStruct] 标记的 struct，提供下拉选择。
    /// </summary>
    [InitializeOnLoad]
    internal static class PropertyStructScanner
    {
        private static (string fullName, string typeName)[] _cache;

        static PropertyStructScanner() => _cache = null;

        /// <summary>获取所有标记了 [PropertyStruct] 的 struct 类型。（fullName, shortName）</summary>
        internal static (string fullName, string shortName)[] GetStructTypes()
        {
            if (_cache != null) return _cache;

            _cache = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (Exception) { return Type.EmptyTypes; }
                })
                .Where(t => t.IsValueType
                         && !t.IsEnum
                         && !t.IsPrimitive
                         && t.GetCustomAttributes(typeof(PropertyStructAttribute), false).Length > 0)
                .Select(t => (fullName: t.FullName, shortName: t.Name))
                .OrderBy(t => t.shortName)
                .ToArray();

            return _cache;
        }

        /// <summary>失效缓存——域重载后重建。</summary>
        internal static void Invalidate() => _cache = null;

        /// <summary>绘制 StructType 下拉框。返回选中的 type.FullName。</summary>
        internal static string DrawDropdown(string current, string label = "Struct Type")
        {
            var types = GetStructTypes();
            var names = new List<string> { "(none)" };
            int selected = 0;

            for (int i = 0; i < types.Length; i++)
            {
                names.Add(types[i].fullName);
                if (types[i].fullName == current) selected = i + 1;
            }

            int newIndex = EditorGUILayout.Popup(label, selected, names.ToArray());
            return newIndex > 0 ? types[newIndex - 1].fullName : "";
        }
    }
}
