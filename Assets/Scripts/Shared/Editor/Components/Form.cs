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
    /// 声明式 Editor 表单。回调模式——每帧创建 + 绘制，对标 EditorCard。
    ///
    /// 用法：
    ///   EditorForm.Draw(target, form => {
    ///       form.Float("cooldownDuration", "冷却");
    ///       form.Toggle("stackable");
    ///       form.BeginGroup(Horizontal);
    ///       form.Float("a"); form.Float("b");
    ///       form.EndGroup();
    ///   });
    /// </summary>
    public class EditorForm
    {
        internal static EditorForm Current { get; private set; }

        public float DefaultLabelWidth { get; set; } = 90f;
        public float RowSpacing { get; set; } = 6f;
        public event Action OnChange;
        public event Action OnSubmit;
        internal void NotifyChanged() => OnChange?.Invoke();

        public void Submit() => OnSubmit?.Invoke();

        internal void NotifyFieldChanged(System.Reflection.FieldInfo field, object newValue)
        {
            if (field != null && _target != null)
            {
                field.SetValue(_target, newValue);
                EditorUtility.SetDirty(_target);
            }
            NotifyChanged();
        }

        internal readonly ScriptableObject _target;
        internal readonly Stack<bool> _inGroup = new();
        internal int _itemIndex;

        // ═══════════════════════════════════════════════════
        // 静态入口
        // ═══════════════════════════════════════════════════

        public static void Draw(object target, Action<EditorForm> build,
            float defaultLabelWidth = 90f, float rowSpacing = 6f)
        {
            var prev = Current;
            var form = new EditorForm(target)
            {
                DefaultLabelWidth = defaultLabelWidth,
                RowSpacing = rowSpacing,
            };
            Current = form;
            build(form);
            Current = prev;
        }

        private EditorForm(object target) { _target = target as ScriptableObject; }

        // ═══════════════════════════════════════════════════
        // Layout
        // ═══════════════════════════════════════════════════

        public EditorForm BeginGroup(FormGroupLayout layout)
        {
            if (layout == FormGroupLayout.Horizontal)
                EditorGUILayout.BeginHorizontal();
            _inGroup.Push(layout == FormGroupLayout.Horizontal);
            return this;
        }

        public EditorForm EndGroup()
        {
            if (_inGroup.Count == 0) return this;
            var wasHorizontal = _inGroup.Pop();
            if (wasHorizontal)
                EditorGUILayout.EndHorizontal();
            return this;
        }

    }
}
#endif
