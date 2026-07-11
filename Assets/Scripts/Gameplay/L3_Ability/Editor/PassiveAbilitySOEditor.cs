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

            var damageMods = new List<DamageModifierEffectSO>();
            foreach (var e in def.targetEffects)
            {
                if (e is DamageModifierEffectSO mod) { damageMods.Add(mod); }
            }
            if (damageMods.Count == 0) return;

            EditorGUILayout.Space(6);
            damageFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(damageFoldout,
                $"Damage Modifiers — ({damageMods.Count})");

            if (damageFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Duration", EditorStyles.miniLabel, GUILayout.Width(56));
                EditorGUILayout.LabelField("Target", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField("+Add", EditorStyles.miniLabel, GUILayout.Width(46));
                EditorGUILayout.LabelField("%", EditorStyles.miniLabel, GUILayout.Width(46));
                EditorGUILayout.LabelField("Pri", EditorStyles.miniLabel, GUILayout.Width(24));
                EditorGUILayout.LabelField("Tag", EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Formula", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                foreach (var mod in damageMods)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(mod.duration <= 0 ? "Instant" : $"DoT {mod.duration}s", GUILayout.Width(56));
                    EditorGUILayout.LabelField(mod.targetTag != null ? mod.targetTag.name : "—", GUILayout.Width(120));
                    EditorGUILayout.LabelField(mod.modAdd != 0 ? $"{mod.modAdd:+0.#;-0.#}" : "—", GUILayout.Width(46));
                    EditorGUILayout.LabelField(mod.modPercent != 0 ? $"{mod.modPercent:+0%}" : "—", GUILayout.Width(46));
                    EditorGUILayout.LabelField($"{mod.priority}", GUILayout.Width(24));
                    EditorGUILayout.LabelField(mod.effectTag != null ? mod.effectTag.FullTag : "—", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"base × (1 + {mod.modPercent:P0}) + {mod.modAdd}");
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
#endif