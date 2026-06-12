#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>EditorForm 的一个表单行。封装 label + input + 变更检测 + SetDirty。</summary>
    internal class EditorFormItem
    {
        public FieldInfo Field;
        public string LabelText;
        public string Tooltip;
        public float LabelWidth;
        public bool IsReadOnly;
        public string HelpText;
        public Action PostInputDraw;
        public Func<bool> VisibleWhen;
        public Func<object, object> DrawField;
        public Func<object, object, bool> AreEqual;
        public Func<object, object> OnBeforeSet;
        public Func<object, object, bool> CustomOnChange;
        public bool IsDivider;

        // RawField
        public Func<object> GetValue;
        public Action<object> SetValue;

        public event Action OnChanged;

        public void Draw(ScriptableObject target)
        {
            if (IsDivider)
            {
                DrawDivider();
                return;
            }

            if (VisibleWhen?.Invoke() == false) return;

            var oldValue = GetValue != null
                ? GetValue()
                : Field?.GetValue(target);
            if (target == null && GetValue == null) return;

            EditorGUILayout.BeginHorizontal(
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            var wasEnabled = GUI.enabled;
            if (IsReadOnly) GUI.enabled = false;

            var guiContent = string.IsNullOrEmpty(Tooltip)
                ? new GUIContent(LabelText)
                : new GUIContent(LabelText, Tooltip);
            EditorGUILayout.LabelField(guiContent, EditorStyles.label,
                GUILayout.Width(LabelWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));

            var newValue = DrawField(oldValue);
            PostInputDraw?.Invoke();

            GUI.enabled = wasEnabled;
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(HelpText))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(LabelWidth);
                var s = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.grey } };
                EditorGUILayout.LabelField(HelpText, s);
                EditorGUILayout.EndHorizontal();
            }

            if (!AreEqual(oldValue, newValue))
            {
                bool applied = true;
                if (CustomOnChange != null)
                {
                    applied = CustomOnChange(oldValue, newValue);
                }
                else
                {
                    newValue = OnBeforeSet?.Invoke(newValue) ?? newValue;
                    if (SetValue != null)
                        SetValue(newValue);
                    else if (Field != null && target != null)
                        Field.SetValue(target, newValue);
                    if (target != null)
                        EditorUtility.SetDirty(target);
                }
                if (applied)
                    OnChanged?.Invoke();
            }
        }

        private void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label,
                GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            rect.y += 7f;
            EditorGUI.DrawRect(rect, new Color(0.137f, 0.137f, 0.137f, 0.3f)); // #232323 — Unity default-border
            if (!string.IsNullOrEmpty(LabelText))
            {
                var labelRect = new Rect(rect.x, rect.y - 2f, rect.width, 18f);
                var s = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.grey },
                      alignment = TextAnchor.MiddleLeft };
                GUI.Label(labelRect, LabelText, s);
            }
            GUILayout.Space(8f);
        }
    }
}
#endif
