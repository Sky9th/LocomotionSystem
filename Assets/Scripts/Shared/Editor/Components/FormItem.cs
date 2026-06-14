#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedDust.Shared.EditorUI
{
    public enum FieldType { Float, Int, Toggle, Object, Enum, Text, Slider }

    /// <summary>
    /// 表单字段——拼装 EditorLabel + EditorInput + 变更检测。
    /// 所有方法即时绘制，通过 EditorForm.Current 拿布局上下文。
    /// </summary>
    public static class EditorFormItem
    {
        public static void Float(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<float, float> onBeforeSet = null, Func<bool> visibleWhen = null)
            => DrawReflected(fieldName, FieldType.Float, label, tooltip, labelWidth, visibleWhen,
                (a, b) => Mathf.Abs((float)a - (float)b) <= 0.001f,
                minMax: null, onBeforeSet != null ? v => onBeforeSet((float)v) : null);

        public static void Int(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<int, int> onBeforeSet = null, Func<bool> visibleWhen = null)
            => DrawReflected(fieldName, FieldType.Int, label, tooltip, labelWidth, visibleWhen,
                (a, b) => (int)a == (int)b,
                onBeforeSet: onBeforeSet != null ? v => onBeforeSet((int)v) : (Func<object, object>)null);

        public static void Slider(string fieldName, float min, float max,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<float, float> onBeforeSet = null, Func<bool> visibleWhen = null)
            => DrawReflected(fieldName, FieldType.Slider, label, tooltip, labelWidth, visibleWhen,
                (a, b) => Mathf.Abs((float)a - (float)b) <= 0.001f,
                minMax: (min, max), onBeforeSet != null ? v => onBeforeSet((float)v) : null);

        public static void Toggle(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null)
            => DrawReflected(fieldName, FieldType.Toggle, label, tooltip, labelWidth, visibleWhen,
                (a, b) => (bool)a == (bool)b);

        public static void Enum<T>(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null) where T : Enum
            => DrawReflected(fieldName, FieldType.Enum, label, tooltip, labelWidth, visibleWhen,
                (a, b) => EqualityComparer<T>.Default.Equals((T)a, (T)b));

        public static void TextField(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null)
            => DrawReflected(fieldName, FieldType.Text, label, tooltip, labelWidth, visibleWhen,
                (a, b) => string.Equals((string)a, (string)b, StringComparison.Ordinal));

        public static void ObjectField<T>(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null) where T : Object
            => DrawReflected(fieldName, FieldType.Object, label, tooltip, labelWidth, visibleWhen,
                (a, b) => object.ReferenceEquals(a, b));

        // ═══════════════════════════════════════════════════
        // ObjectField + TagPicker
        // ═══════════════════════════════════════════════════

        public static void ObjectFieldWithTag<T>(string fieldName, ref Rect tagBtnRect,
            string label = null, float? labelWidth = null) where T : Object
        {
            var f = EditorForm.Current;
            var t = f._target;
            if (t == null) return;

            var field = t.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;

            var val = (T)field.GetValue(t);
            var labelText = label ?? fieldName;
            var w = labelWidth ?? f.DefaultLabelWidth;
            var h = EditorGUIUtility.singleLineHeight;

            if (f._itemIndex > 0 && f._inGroup.Count == 0)
                GUILayout.Space(f.RowSpacing);
            f._itemIndex++;

            EditorGUILayout.BeginHorizontal(GUILayout.Height(h));
            EditorLabel.Draw(labelText, w);
            var next = EditorInput.ObjectFieldWithTagPicker(val, ref tagBtnRect,
                onTagSelected: selected =>
                {
                    if (!object.ReferenceEquals(val, selected))
                        f.NotifyFieldChanged(field, selected);
                });
            if (!object.ReferenceEquals(next, val))
                f.NotifyFieldChanged(field, next);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        // RawField
        // ═══════════════════════════════════════════════════

        public static void RawField(
            string label, float? labelWidth,
            Func<object> getValue, Action<object> setValue,
            Func<object, object> drawFunc, Func<object, object, bool> equals,
            string tooltip = null, Func<bool> visibleWhen = null)
        {
            if (visibleWhen?.Invoke() == false) return;
            if (equals == null) equals = (a, b) => object.Equals(a, b);

            var f = EditorForm.Current;
            var oldValue = getValue();
            var w = labelWidth ?? f.DefaultLabelWidth;
            var h = EditorGUIUtility.singleLineHeight;

            if (f._itemIndex > 0 && f._inGroup.Count == 0)
                GUILayout.Space(f.RowSpacing);
            f._itemIndex++;

            EditorGUILayout.BeginHorizontal(GUILayout.Height(h));
            EditorLabel.Draw(label, w, tooltip);
            var newValue = drawFunc(oldValue);
            EditorGUILayout.EndHorizontal();

            if (!equals(oldValue, newValue))
            {
                setValue(newValue);
                f.NotifyChanged();
            }
        }

        // ═══════════════════════════════════════════════════
        // ArrayField
        // ═══════════════════════════════════════════════════

        public static void ArrayField<T>(
            string label,
            Func<T[]> getValue,
            Action<T[]> setValue,
            Action<int, T> drawRow,
            Func<T> createDefault,
            EventHandler onChanged = null)
        {
            var f = EditorForm.Current;
            var arr = getValue();
            var len = arr?.Length ?? 0;
            int removeAt = -1;

            if (f._itemIndex > 0) GUILayout.Space(f.RowSpacing);
            f._itemIndex++;

            EditorGUILayout.LabelField($"{label} [{len}]", EditorStyles.miniBoldLabel);

            if (arr != null)
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal(
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    drawRow(i, arr[i]);
                    if (EditorButton.Delete())
                        removeAt = i;
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (removeAt >= 0)
            {
                var newArr = new T[arr.Length - 1];
                for (int i = 0, j = 0; i < arr.Length; i++)
                    if (i != removeAt) newArr[j++] = arr[i];
                setValue(newArr);
                onChanged?.Invoke(null, EventArgs.Empty);
                f.NotifyChanged();
            }

            GUILayout.Space(2);
            if (EditorButton.Default("+ Add", EditorButtonSize.Small))
            {
                var newArr = arr == null
                    ? new T[] { createDefault() }
                    : CreateExpanded(arr, createDefault());
                setValue(newArr);
                onChanged?.Invoke(null, EventArgs.Empty);
                f.NotifyChanged();
            }
        }

        // ═══════════════════════════════════════════════════
        // Internal
        // ═══════════════════════════════════════════════════

        private static void DrawReflected(string fieldName, FieldType type,
            string label, string tooltip, float? labelWidth, Func<bool> visibleWhen,
            Func<object, object, bool> equals,
            (float min, float max)? minMax = null, Func<object, object> onBeforeSet = null)
        {
            if (visibleWhen?.Invoke() == false) return;

            var f = EditorForm.Current;
            var t = f._target;
            var field = t != null
                ? t.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                : null;

            var labelText = label ?? fieldName;
            var tooltipText = tooltip;
            if (tooltipText == null && field != null)
                tooltipText = field.GetCustomAttribute<TooltipAttribute>()?.tooltip;

            var oldValue = field?.GetValue(t);
            var w = labelWidth ?? f.DefaultLabelWidth;
            var h = EditorGUIUtility.singleLineHeight;

            if (f._itemIndex > 0 && f._inGroup.Count == 0)
                GUILayout.Space(f.RowSpacing);
            f._itemIndex++;

            EditorGUILayout.BeginHorizontal(GUILayout.Height(h));
            EditorLabel.Draw(labelText, w, tooltipText);
            var newValue = DrawInput(type, oldValue, minMax);
            EditorGUILayout.EndHorizontal();

            if (!equals(oldValue, newValue))
            {
                newValue = onBeforeSet?.Invoke(newValue) ?? newValue;
                f.NotifyFieldChanged(field, newValue);
            }
        }

        private static object DrawInput(FieldType type, object val, (float min, float max)? minMax)
        {
            return type switch
            {
                FieldType.Float   => EditorInput.FloatField((float)(val ?? 0f)),
                FieldType.Int     => EditorInput.IntField((int)(val ?? 0)),
                FieldType.Toggle  => EditorInput.Toggle(val is bool b && b),
                FieldType.Object  => val is Object o ? EditorInput.ObjectField(o, o?.GetType(), false) : val,
                FieldType.Enum    => val is Enum e ? EditorInput.EnumPopup(e) : val,
                FieldType.Text    => EditorInput.TextField((string)val ?? ""),
                FieldType.Slider  => EditorInput.Slider((float)(val ?? 0f), minMax?.min ?? 0f, minMax?.max ?? 1f),
                _ => val,
            };
        }

        private static T[] CreateExpanded<T>(T[] src, T newElem)
        {
            var dst = new T[src.Length + 1];
            Array.Copy(src, dst, src.Length);
            dst[src.Length] = newElem;
            return dst;
        }
    }
}
#endif
