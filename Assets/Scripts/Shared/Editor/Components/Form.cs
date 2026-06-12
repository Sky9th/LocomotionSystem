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
    /// 声明式 Editor 表单组件。绑定一个 ScriptableObject，通过 fluent API 定义字段列表，
    /// Draw() 时自动渲染带 Label + Tooltip + 变更检测 + SetDirty 的标准表单行。
    ///
    /// 使用范式（延迟构建）：
    ///   if (EditorForm.NeedsRebuild(_form, _selected)) { _form = BuildForm(_selected); }
    ///   _form?.Draw();
    /// </summary>
    public class EditorForm
    {
        public ScriptableObject Target { get; private set; }
        public float DefaultLabelWidth { get; set; } = 90f;
        /// <summary>FormItem 之间的垂直间距（类似 Element UI el-form-item margin-bottom）。</summary>
        public float RowSpacing { get; set; } = 4f;
        public event Action OnAnyChange;

        private readonly List<EditorFormItem> _items = new();

        public EditorForm(ScriptableObject target)
        {
            Target = target;
        }

        /// <summary>form 为 null 或 target 已变更时需要重建。</summary>
        public static bool NeedsRebuild(EditorForm form, ScriptableObject target)
            => form == null || form.Target != target;

        /// <summary>切换目标 SO，清空 Items 并重新绑定。</summary>
        public void SetTarget(ScriptableObject target)
        {
            Target = target;
            Clear();
        }

        /// <summary>清空所有 Items 和事件订阅。调用后需重新构建。</summary>
        public void Clear()
        {
            _items.Clear();
            OnAnyChange = null;
        }

        // ═══════════════════════════════════════════════════
        // 字段构建方法
        // ═══════════════════════════════════════════════════

        public EditorForm Float(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<float, float> onBeforeSet = null, Func<bool> visibleWhen = null)
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.FloatField((float)v),
                (a, b) => Mathf.Abs((float)a - (float)b) <= 0.001f,
                onBeforeSet != null ? v => onBeforeSet((float)v) : null);
            return this;
        }

        public EditorForm Int(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<int, int> onBeforeSet = null, Func<bool> visibleWhen = null)
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.IntField((int)v),
                (a, b) => (int)a == (int)b,
                onBeforeSet != null ? v => onBeforeSet((int)v) : null);
            return this;
        }

        public EditorForm Slider(string fieldName, float min, float max,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<float, float> onBeforeSet = null, Func<bool> visibleWhen = null)
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.Slider((float)v, min, max),
                (a, b) => Mathf.Abs((float)a - (float)b) <= 0.001f,
                onBeforeSet != null ? v => onBeforeSet((float)v) : null);
            return this;
        }

        public EditorForm Toggle(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null)
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.Toggle((bool)v),
                (a, b) => (bool)a == (bool)b);
            return this;
        }

        public EditorForm Enum<T>(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null) where T : Enum
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.EnumPopup((Enum)v),
                (a, b) => EqualityComparer<T>.Default.Equals((T)a, (T)b));
            return this;
        }

        public EditorForm TextField(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null)
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.TextField((string)v ?? ""),
                (a, b) => string.Equals((string)a, (string)b, StringComparison.Ordinal));
            return this;
        }

        public EditorForm TextArea(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null)
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.TextArea((string)v ?? "", GUILayout.Height(48)),
                (a, b) => string.Equals((string)a, (string)b, StringComparison.Ordinal));
            return this;
        }

        public EditorForm ObjectField<T>(string fieldName,
            string label = null, string tooltip = null, float? labelWidth = null,
            Func<bool> visibleWhen = null) where T : Object
        {
            AddItem(fieldName, label, tooltip, labelWidth, visibleWhen,
                v => EditorGUILayout.ObjectField((T)v, typeof(T), false),
                (a, b) => object.ReferenceEquals(a, b));
            return this;
        }

        // ═══════════════════════════════════════════════════
        // 修饰方法
        // ═══════════════════════════════════════════════════

        public EditorForm ReadOnly()
        {
            if (_items.Count > 0) _items[^1].IsReadOnly = true;
            return this;
        }

        public EditorForm HelpText(string text)
        {
            if (_items.Count > 0) _items[^1].HelpText = text;
            return this;
        }

        public EditorForm PostInput(Action drawExtra)
        {
            if (_items.Count > 0) _items[^1].PostInputDraw = drawExtra;
            return this;
        }

        /// <summary>覆盖最后一个 FormItem 的输入控件。</summary>
        public EditorForm CustomDraw(Func<object, object> drawFunc)
        {
            if (_items.Count > 0) _items[^1].DrawField = drawFunc;
            return this;
        }

        /// <summary>
        /// 覆盖最后一个 FormItem 的变更处理器（替代默认的 SetValue + SetDirty）。
        /// 返回 true 表示变更已被处理，仍会触发 OnChanged 事件。
        /// 返回 false 表示变更被拒绝，OnChanged 不触发。
        /// </summary>
        public EditorForm CustomOnChange(Func<object, object, bool> onChange)
        {
            if (_items.Count > 0) _items[^1].CustomOnChange = onChange;
            return this;
        }

        /// <summary>覆盖最后一个 FormItem 的等值比较。</summary>
        public EditorForm CustomEquals(Func<object, object, bool> equals)
        {
            if (_items.Count > 0) _items[^1].AreEqual = equals;
            return this;
        }

        /// <summary>
        /// 自定义字段（不使用反射 FieldInfo）。用于 Object.name 等非 SO 字段值。
        /// </summary>
        public EditorForm RawField(
            string label, float? labelWidth,
            Func<object> getValue, Action<object> setValue,
            Func<object, object> drawFunc, Func<object, object, bool> equals,
            string tooltip = null, Func<bool> visibleWhen = null)
        {
            if (equals == null)
                equals = (a, b) => object.Equals(a, b);

            var item = new EditorFormItem
            {
                LabelText = label,
                Tooltip = tooltip,
                LabelWidth = labelWidth ?? DefaultLabelWidth,
                GetValue = getValue,
                SetValue = setValue,
                DrawField = drawFunc,
                AreEqual = equals,
                VisibleWhen = visibleWhen,
            };
            item.OnChanged += () => OnAnyChange?.Invoke();
            _items.Add(item);
            return this;
        }

        // ═══════════════════════════════════════════════════
        // Draw
        // ═══════════════════════════════════════════════════

        public void Draw()
        {
            if (Target == null) return;
            for (int i = 0; i < _items.Count; i++)
            {
                if (i > 0 && RowSpacing > 0)
                    GUILayout.Space(RowSpacing);
                _items[i].Draw(Target);
            }
        }

        /// <summary>分组分隔线（类似 Element UI el-divider）。</summary>
        public EditorForm Divider(string title = null)
        {
            var item = new EditorFormItem { IsDivider = true, LabelText = title };
            _items.Add(item);
            return this;
        }

        // ═══════════════════════════════════════════════════
        // Internal
        // ═══════════════════════════════════════════════════

        private void AddItem(string fieldName, string label, string tooltip,
            float? labelWidth, Func<bool> visibleWhen,
            Func<object, object> drawField, Func<object, object, bool> equals,
            Func<object, object> onBeforeSet = null)
        {
            var field = Target?.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.Instance);

            if (field == null && Target != null)
                Debug.LogWarning($"[EditorForm] Field '{fieldName}' not found on {Target.GetType().Name}");

            var labelText = label ?? fieldName;
            var tooltipText = tooltip;
            if (tooltipText == null && field != null)
            {
                var attr = field.GetCustomAttribute<TooltipAttribute>();
                tooltipText = attr?.tooltip;
            }

            var item = new EditorFormItem
            {
                Field = field,
                LabelText = labelText,
                Tooltip = tooltipText,
                LabelWidth = labelWidth ?? DefaultLabelWidth,
                VisibleWhen = visibleWhen,
                DrawField = drawField,
                AreEqual = equals,
                OnBeforeSet = onBeforeSet,
            };

            item.OnChanged += () => OnAnyChange?.Invoke();
            _items.Add(item);
        }

    }
}
#endif
