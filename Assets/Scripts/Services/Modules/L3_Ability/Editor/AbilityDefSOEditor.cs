#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    [CustomEditor(typeof(AbilityDefSO))]
    public class AbilityDefSOEditor : UnityEditor.Editor
    {
        private bool damageFoldout = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawDamagePreview();
        }

        private void DrawDamagePreview()
        {
            var def = (AbilityDefSO)target;
            if (def.targetEffects == null || def.targetEffects.Length == 0) return;

            var damages = new List<DamageEffectSO>();
            float instantTotal = 0f, dotTotal = 0f;
            foreach (var e in def.targetEffects)
            {
                if (e is DamageEffectSO dmg) { damages.Add(dmg); instantTotal += dmg.duration <= 0 ? dmg.baseDamage : 0; dotTotal += dmg.duration > 0 ? dmg.baseDamage * dmg.duration : 0; }
            }
            if (damages.Count == 0) return;

            EditorGUILayout.Space(6);
            damageFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(damageFoldout,
                $"Damage Preview — {instantTotal:F1} pts{(dotTotal > 0 ? $" + {dotTotal:F1} DoT" : "")}  ({damages.Count})");

            if (damageFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Type", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("Value", EditorStyles.miniLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField("Tag", EditorStyles.miniLabel, GUILayout.Width(130));
                EditorGUILayout.LabelField("Penetration", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Range", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                foreach (var dmg in damages)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(dmg.duration <= 0 ? "Instant" : $"DoT {dmg.duration}s", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{dmg.baseDamage:F1} pts", GUILayout.Width(70));
                    EditorGUILayout.LabelField(dmg.effectTag != null ? dmg.effectTag.FullTag : "—", GUILayout.Width(130));

                    var pen = "";
                    if (dmg.armorPenetration > 0) pen += $"AP:{dmg.armorPenetration:P0}  ";
                    if (dmg.shieldPenetration > 0) pen += $"SP:{dmg.shieldPenetration:P0}";
                    if (!string.IsNullOrEmpty(pen)) EditorGUILayout.LabelField(pen);

                    if (dmg.minDamage > 0 || dmg.maxDamage > 0)
                        EditorGUILayout.LabelField($"Min:{dmg.minDamage:F1}  Max:{(dmg.maxDamage > 0 ? dmg.maxDamage : "—")}");

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
#endif