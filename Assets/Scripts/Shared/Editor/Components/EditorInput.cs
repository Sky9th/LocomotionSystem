#if UNITY_EDITOR
using System;
using RedDust.Core;
using RedDust.Core.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 纯 Input 控件——只画输入区 + 可选侧边按钮，不画 Label。
    /// Label 由 EditorLabel 负责，FormItem 负责拼装。
    /// </summary>
    public static class EditorInput
    {
        public static T ObjectField<T>(T value, float? width = null) where T : Object
        {
            return (T)EditorGUILayout.ObjectField(value, typeof(T), false,
                WithWidth(width));
        }

        public static Object ObjectField(Object value, Type type, bool allowSceneObjects, float? width = null)
        {
            return EditorGUILayout.ObjectField(value, type, allowSceneObjects,
                WithWidth(width));
        }

        public static float FloatField(float value, float? width = null)
        {
            return EditorGUILayout.FloatField(value, WithWidth(width));
        }

        public static int IntField(int value, float? width = null)
        {
            return EditorGUILayout.IntField(value, WithWidth(width));
        }

        public static string TextField(string value, float? width = null)
        {
            return EditorGUILayout.TextField(value ?? "", WithWidth(width));
        }

        public static string TextFieldWithClear(string value, float? width = null)
        {
            var result = EditorGUILayout.TextField(value ?? "", WithWidth(width));
            if (EditorButton.Danger("✕", EditorButtonSize.Small, width: EditorTokens.SizeMd, enabled: !string.IsNullOrEmpty(result)))
            {
                result = "";
                GUI.FocusControl(null);
            }
            return result;
        }

        public static bool Toggle(bool value, float? width = null)
        {
            return EditorGUILayout.Toggle(value, WithWidth(width));
        }

        public static float Slider(float value, float min, float max, float? width = null)
        {
            return EditorGUILayout.Slider(value, min, max, WithWidth(width));
        }

        public static Enum EnumPopup(Enum value, float? width = null)
        {
            return EditorGUILayout.EnumPopup(value, WithWidth(width));
        }

        // ═══════════════════════════════════════════════════
        // 侧边按钮
        // ═══════════════════════════════════════════════════

        /// <summary>ObjectField 右侧 TagPicker 按钮。返回是否按下。</summary>
        public static bool TagButton(ref Rect rect)
        {
            var clicked = EditorButton.Default("Tag", EditorButtonSize.Small, width: 35);
            if (Event.current.type == EventType.Repaint)
                rect = GUILayoutUtility.GetLastRect();
            return clicked;
        }

        /// <summary>ObjectField + TagPicker 按钮。Tag 按钮点击 → 弹出 TagPicker → onTagSelected 回调。</summary>
        public static T ObjectFieldWithTagPicker<T>(T value, ref Rect tagBtnRect,
            Action<T> onTagSelected = null) where T : Object
        {
            var next = (T)EditorGUILayout.ObjectField(value, typeof(T), false);

            if (TagButton(ref tagBtnRect))
            {
                var currentTag = value as GameplayTagDefinitionSO;
                TagPicker.Show(tagBtnRect, allowCreate: true,
                    currentFullTag: currentTag?.FullTag,
                    onSelected: t => onTagSelected?.Invoke(t as T));
            }

            return next;
        }

        // ═══════════════════════════════════════════════════

        private static GUILayoutOption[] WithWidth(float? w)
            => w.HasValue ? new[] { GUILayout.Width(w.Value) } : Array.Empty<GUILayoutOption>();
    }
}
#endif
