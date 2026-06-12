#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    [CustomEditor(typeof(PassiveAbilitySO))]
    public class PassiveAbilitySOEditor : UnityEditor.Editor
    {
        private bool damageFoldout = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawDamagePreview();
        }

        private void DrawDamagePreview()
        {
            var def = (PassiveAbilitySO)target;
            if (def.targetEffects == null || def.targetEffects.Length == 0) return;

            var damages = new List<DamageEffectSO>();
            float instantTotal = 0f, dotTotal = 0f;
            foreach (var e in def.targetEffects)
            {
                if (e is DamageEffectSO dmg) { damages.Add(dmg); instantTotal += dmg.duration <= 0 ? dmg.baseValue : 0; dotTotal += dmg.duration > 0 ? dmg.baseValue * dmg.duration : 0; }
            }
            if (damages.Count == 0) return;

            EditorGUILayout.Space(6);
            damageFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(damageFoldout,
                $"Damage Preview — {instantTotal:F1} pts{(dotTotal > 0 ? $" + {dotTotal:F1} DoT" : "")}  ({damages.Count})");

            if (damageFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Type", EditorStyles.miniLabel, GUILayout.Width(56));
                EditorGUILayout.LabelField("Base", EditorStyles.miniLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("+Add", EditorStyles.miniLabel, GUILayout.Width(46));
                EditorGUILayout.LabelField("×Mult", EditorStyles.miniLabel, GUILayout.Width(46));
                EditorGUILayout.LabelField("Pri", EditorStyles.miniLabel, GUILayout.Width(24));
                EditorGUILayout.LabelField("Tag", EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Effective", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                foreach (var dmg in damages)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(dmg.duration <= 0 ? "Instant" : $"DoT {dmg.duration}s", GUILayout.Width(56));
                    EditorGUILayout.LabelField($"{dmg.baseValue:F1}", GUILayout.Width(50));
                    EditorGUILayout.LabelField(dmg.modAdd != 0 ? $"{dmg.modAdd:+0.#;-0.#}" : "—", GUILayout.Width(46));
                    EditorGUILayout.LabelField(Mathf.Abs(dmg.modMult - 1f) > 0.001f ? $"{dmg.modMult:F2}" : "—", GUILayout.Width(46));
                    EditorGUILayout.LabelField($"{dmg.priority}", GUILayout.Width(24));
                    EditorGUILayout.LabelField(dmg.effectTag != null ? dmg.effectTag.FullTag : "—", GUILayout.Width(100));
                    var eff = dmg.baseValue + dmg.modAdd;
                    if (Mathf.Abs(dmg.modMult - 1f) > 0.001f) eff *= dmg.modMult;
                    EditorGUILayout.LabelField($"{eff:F1}");
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
#endif