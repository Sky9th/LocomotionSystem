#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 表单字段。唯一渲染入口 <see cref="Draw"/>——Label 左 + Slot 右。
    /// Float/Int/Toggle/.../RawField/ObjectFieldWithTag/ArrayField 全部走 Draw。
    /// </summary>
    public static class EditorFormItem
    {
        // ═══════════════════════════════════════════════════
        // ★ 唯一渲染入口
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// 左右布局。左列固定宽 + wordWrap（Label 占整行，后续可加子说明/按钮），右边距 + Slot 右。
        /// </summary>
        public static void Draw(string label, Action drawSlot,
            float? labelWidth = null, string tooltip = null,
            Func<bool> visibleWhen = null)
        {
            if (visibleWhen?.Invoke() == false) return;

            var f = EditorForm.Current;
            var w = labelWidth ?? f.DefaultLabelWidth;
            var inGroup = f._inGroup.Count > 0;

            BeginRow();
            if (inGroup)
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(w * 2.5f));
            else
                EditorGUILayout.BeginHorizontal();
            // 左布局 — 固定宽度垂直容器
            EditorGUILayout.BeginVertical(GUILayout.Width(w));
            EditorLabel.Draw(label, w, tooltip, style: LabelStyle);
            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            drawSlot();
            EditorGUILayout.EndHorizontal();
            if (inGroup)
                GUILayout.Space(EditorTokens.Pad * 5);
            else
                EditorDivider.Draw();
        }

        // ═══════════════════════════════════════════════════
        // 反射字段
        // ═══════════════════════════════════════════════════

        public static void Float(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<float, float> onBeforeSet = null, Func<bool> visibleWhen = null)
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;

            Draw(Label(fieldName, label), () =>
            {
                var v = EditorInput.FloatField((float)(fd.Value ?? 0f));
                if (Mathf.Abs((float)fd.Value - v) > 0.001f)
                {
                    v = onBeforeSet?.Invoke(v) ?? v;
                    EditorForm.Current.NotifyFieldChanged(fd.Field, v);
                }
            }, labelWidth, Tooltip(tooltip, fd.Field), visibleWhen);
        }

        public static void Int(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<int, int> onBeforeSet = null, Func<bool> visibleWhen = null)
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;

            Draw(Label(fieldName, label), () =>
            {
                var v = EditorInput.IntField((int)(fd.Value ?? 0));
                if ((int)fd.Value != v)
                {
                    v = onBeforeSet?.Invoke(v) ?? v;
                    EditorForm.Current.NotifyFieldChanged(fd.Field, v);
                }
            }, labelWidth, Tooltip(tooltip, fd.Field), visibleWhen);
        }

        public static void Toggle(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null)
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;

            Draw(Label(fieldName, label), () =>
            {
                var v = EditorInput.Toggle(fd.Value is bool b && b);
                if ((bool)fd.Value != v)
                    EditorForm.Current.NotifyFieldChanged(fd.Field, v);
            }, labelWidth, Tooltip(tooltip, fd.Field), visibleWhen);
        }

        public static void ObjectField<T>(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null) where T : Object
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;

            Draw(Label(fieldName, label), () =>
            {
                var val = fd.Value is Object o ? o : null;
                var next = EditorInput.ObjectField(val, val?.GetType(), false);
                if (!object.ReferenceEquals(next, val))
                    EditorForm.Current.NotifyFieldChanged(fd.Field, next);
            }, labelWidth, Tooltip(tooltip, fd.Field), visibleWhen);
        }

        public static void Enum<T>(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null) where T : Enum
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;

            Draw(Label(fieldName, label), () =>
            {
                var v = EditorInput.EnumPopup(fd.Value is Enum e ? e : default(T));
                if (!EqualityComparer<T>.Default.Equals((T)fd.Value, (T)v))
                    EditorForm.Current.NotifyFieldChanged(fd.Field, v);
            }, labelWidth, Tooltip(tooltip, fd.Field), visibleWhen);
        }

        public static void TextField(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null)
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;

            Draw(Label(fieldName, label), () =>
            {
                var v = EditorInput.TextField((string)fd.Value ?? "");
                if (!string.Equals((string)fd.Value, v, StringComparison.Ordinal))
                    EditorForm.Current.NotifyFieldChanged(fd.Field, v);
            }, labelWidth, Tooltip(tooltip, fd.Field), visibleWhen);
        }

        public static void Slider(string fieldName, float min, float max,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<float, float> onBeforeSet = null, Func<bool> visibleWhen = null)
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;

            Draw(Label(fieldName, label), () =>
            {
                var v = EditorInput.Slider((float)(fd.Value ?? 0f), min, max);
                if (Mathf.Abs((float)fd.Value - v) > 0.001f)
                {
                    v = onBeforeSet?.Invoke(v) ?? v;
                    EditorForm.Current.NotifyFieldChanged(fd.Field, v);
                }
            }, labelWidth, Tooltip(tooltip, fd.Field), visibleWhen);
        }

        // ═══════════════════════════════════════════════════
        // ObjectField + TagPicker → 走 Draw（ref 桥接）
        // ═══════════════════════════════════════════════════

        public static void ObjectFieldWithTag<T>(string fieldName, ref Rect tagBtnRect,
            string label = null, float? labelWidth = null) where T : Object
        {
            var fd = Resolve(fieldName);
            if (fd.Field == null) return;
            var val = (T)fd.Value;
            var localRect = tagBtnRect; // ref → local，lambda 可捕获

            Draw(Label(fieldName, label), () =>
            {
                var next = EditorInput.ObjectFieldWithTagPicker(val, ref localRect,
                    onTagSelected: selected =>
                    {
                        if (!object.ReferenceEquals(val, selected))
                            EditorForm.Current.NotifyFieldChanged(fd.Field, selected);
                    });
                if (!object.ReferenceEquals(next, val))
                    EditorForm.Current.NotifyFieldChanged(fd.Field, next);
            }, labelWidth);

            tagBtnRect = localRect; // local → ref，回写
        }

        // ═══════════════════════════════════════════════════
        // 完全自定义 → 走 Draw（原 RawField）
        // ═══════════════════════════════════════════════════

        public static void RawField(
            string label, float? labelWidth,
            Func<object> getValue, Action<object> setValue,
            Func<object, object> drawFunc, Func<object, object, bool> equals,
            string tooltip = null, Func<bool> visibleWhen = null)
        {
            if (equals == null) equals = (a, b) => object.Equals(a, b);

            Draw(label, () =>
            {
                var oldValue = getValue();
                var newValue = drawFunc(oldValue);
                if (!equals(oldValue, newValue))
                {
                    setValue(newValue);
                    EditorForm.Current.NotifyChanged();
                }
            }, labelWidth, tooltip, visibleWhen);
        }

        // ═══════════════════════════════════════════════════
        // ArrayField → 多行数组，走 Draw（Label 左 + Slot 右）
        // ═══════════════════════════════════════════════════

        public static void ArrayField<T>(
            string label,
            Func<T[]> getValue,
            Action<T[]> setValue,
            Action<int, T> drawRow,
            Func<T> createDefault,
            EventHandler onChanged = null,
            string tooltip = null)
        {
            var f = EditorForm.Current;
            var arr = getValue();
            var len = arr?.Length ?? 0;
            int removeAt = -1;

            Draw($"{label} [{len}]", () =>
            {
                EditorGUILayout.BeginVertical();
                if (arr != null)
                {
                    for (var i = 0; i < arr.Length; i++)
                    {
                        if (i > 0) GUILayout.Space(EditorTokens.Pad / 3);
                        EditorGUILayout.BeginHorizontal(
                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        drawRow(i, arr[i]);
                        if (EditorButton.Delete())
                            removeAt = i;
                        EditorGUILayout.EndHorizontal();
                    }
                }

                PostRemove();
                GUILayout.Space(EditorTokens.Pad / 3);
                if (EditorButton.Default("+ Add", EditorButtonSize.Small))
                {
                    setValue(arr == null
                        ? new T[] { createDefault() }
                        : Append(arr, createDefault()));
                    onChanged?.Invoke(null, EventArgs.Empty);
                    f.NotifyChanged();
                }
                EditorGUILayout.EndVertical();

                // ---- local ----
                void PostRemove()
                {
                    if (removeAt < 0) return;
                    setValue(RemoveAt(arr, removeAt));
                    onChanged?.Invoke(null, EventArgs.Empty);
                    f.NotifyChanged();
                }
            }, tooltip: tooltip);
        }

        // ═══════════════════════════════════════════════════
        // Internal helpers
        // ═══════════════════════════════════════════════════

        private static GUIStyle _labelStyle;
        private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorLabel.DefaultStyle)
        {
            wordWrap = true,
        };

        /// <summary> spacing + _itemIndex++，Draw 和 ArrayField 共用 </summary>
        private static void BeginRow()
        {
            var f = EditorForm.Current;
            if (f._itemIndex > 0 && f._inGroup.Count == 0)
                GUILayout.Space(f.RowSpacing);
            f._itemIndex++;
        }

        private struct FieldDesc { public FieldInfo Field; public object Value; }

        private static FieldDesc Resolve(string fieldName)
        {
            var t = EditorForm.Current._target;
            if (t == null) return default;
            var field = t.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            return new FieldDesc { Field = field, Value = field?.GetValue(t) };
        }

        private static string Label(string fieldName, string explicitLabel)
            => explicitLabel ?? fieldName;

        private static string Tooltip(string explicitTooltip, FieldInfo field)
            => explicitTooltip ?? field?.GetCustomAttribute<TooltipAttribute>()?.tooltip;

        private static T[] RemoveAt<T>(T[] src, int index)
        {
            var dst = new T[src.Length - 1];
            for (int i = 0, j = 0; i < src.Length; i++)
                if (i != index) dst[j++] = src[i];
            return dst;
        }

        private static T[] Append<T>(T[] src, T elem)
        {
            var dst = new T[src.Length + 1];
            Array.Copy(src, dst, src.Length);
            dst[src.Length] = elem;
            return dst;
        }
    }
}
#endif
