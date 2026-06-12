#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 共享导入/导出 UI。文件选择 + 预览卡片 + 按钮 + 结果卡片。
    /// 各编辑器分别提供 Importer（DTO + Import/Export 逻辑），UI 统一在此。
    /// </summary>
    public static class EditorImportExport
    {
        private const float Pad = 6f;

        public static void Draw(
            string title,
            string defaultDir,
            string fileExtension,
            ref string filePath,
            ref string previewText,
            ref string resultText,
            Action onImport,
            Action onExport)
        {
            var fp = filePath;
            var pv = previewText;
            var rs = resultText;

            EditorCard.Draw(Pad, title, () =>
            {
                // ── 文件选择 ──
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("File", GUILayout.Width(30));
                fp = EditorGUILayout.TextField(fp ?? "");
                if (EditorButton.Draw("...", size: EditorButtonSize.Small, width: 30f))
                {
                    var path = EditorUtility.OpenFilePanel($"Import {title}", defaultDir, fileExtension);
                    if (!string.IsNullOrEmpty(path))
                        fp = path;
                }
                EditorGUILayout.EndHorizontal();

                EditorCard.GapTight();

                // ── 预览 ──
                if (!string.IsNullOrEmpty(pv))
                {
                    EditorCard.DrawLight(Pad, () =>
                    {
                        EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);
                        var s = new GUIStyle(EditorStyles.miniLabel)
                            { normal = { textColor = Color.grey }, wordWrap = true };
                        EditorGUILayout.LabelField(pv, s);
                    });
                    EditorCard.GapTight();
                }

                // ── 按钮 ──
                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Draw("Import", EditorButtonStyle.Success, EditorButtonSize.Medium))
                    onImport();
                GUILayout.FlexibleSpace();
                if (EditorButton.Draw("Export", EditorButtonStyle.Primary, EditorButtonSize.Medium))
                    onExport();
                EditorGUILayout.EndHorizontal();

                // ── 结果 ──
                if (!string.IsNullOrEmpty(rs))
                {
                    EditorCard.GapTight();
                    EditorCard.DrawLight(Pad, () =>
                    {
                        EditorGUILayout.LabelField("Result", EditorStyles.miniBoldLabel);
                        var s = new GUIStyle(EditorStyles.miniLabel)
                            { normal = { textColor = Color.grey }, wordWrap = true };
                        EditorGUILayout.LabelField(rs, s);
                    });
                }
            });

            filePath = fp;
            previewText = pv;
            resultText = rs;
        }
    }
}
#endif
