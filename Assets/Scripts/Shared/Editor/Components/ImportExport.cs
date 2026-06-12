#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 导入/导出共享面板。Header + 文件选择 + 预览 + 按钮 + 结果。
    /// 遵循项目 Editor UI 约定：EditorCard / EditorButton / EditorCard.Gap。
    /// </summary>
    public static class EditorImportExport
    {
        private const float Pad = 6f;

        public static void Draw(
            string title,
            string subtitle,
            string defaultDir,
            string fileExtension,
            string defaultFileName,
            ref string filePath,
            ref string previewText,
            ref (int created, int skipped, List<string> errors) result,
            Func<string, string> buildPreview,
            Func<string, (int created, int skipped, List<string> errors)> onImport,
            Action<string> onExport)
        {
            var fp = filePath;
            var pv = previewText;

            DrawHeader(title, subtitle);

            EditorCard.Gap(Pad);

            // 文件选择
            EditorCard.Draw(Pad, "JSON File", () =>
            {
                EditorGUILayout.BeginHorizontal();
                fp = EditorGUILayout.TextField(fp ?? "");
                if (EditorButton.Draw("…", size: EditorButtonSize.Small, width: 30f))
                {
                    var selected = EditorUtility.OpenFilePanel("Select JSON File", defaultDir, fileExtension);
                    if (!string.IsNullOrEmpty(selected))
                    {
                        var projectPath = Path.GetDirectoryName(Application.dataPath);
                        fp = selected.StartsWith(projectPath!)
                            ? selected.Substring(projectPath.Length + 1).Replace('\\', '/')
                            : selected;
                    }
                }
                EditorGUILayout.EndHorizontal();
            });

            EditorCard.Gap(Pad);

            // 预览
            if (buildPreview != null)
            {
                EditorCard.DrawLight(Pad, () =>
                {
                    EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                    if (!File.Exists(fp))
                    {
                        EditorCard.GapTight();
                        EditorGUILayout.LabelField("File not found.", EditorUIUtility.GreyPlaceholder);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(pv))
                        {
                            try { pv = buildPreview(fp); }
                            catch { pv = null; }
                        }

                        if (!string.IsNullOrEmpty(pv))
                        {
                            EditorCard.GapTight();
                            var content = new GUIContent(pv);
                            var style = new GUIStyle(EditorStyles.label)
                                { richText = true, wordWrap = true };
                            var height = style.CalcHeight(content,
                                EditorGUIUtility.currentViewWidth - 60f);
                            EditorGUILayout.LabelField(content, style,
                                GUILayout.Height(Mathf.Max(height, EditorGUIUtility.singleLineHeight)));
                        }
                        else
                        {
                            EditorCard.GapTight();
                            EditorGUILayout.LabelField("JSON is empty or parse failed.",
                                EditorUIUtility.GreyPlaceholder);
                        }
                    }
                });

                EditorCard.Gap(Pad);
            }

            // 按钮
            DrawButtons(fp, defaultDir, defaultFileName, fileExtension, ref result, onImport, onExport);

            EditorCard.Gap(Pad);

            // 结果
            DrawResultSection(result);

            filePath = fp;
            previewText = pv;
        }

        private static void DrawHeader(string title, string subtitle)
        {
            EditorCard.Draw(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(title, EditorStyles.largeLabel,
                    GUILayout.ExpandWidth(true));
                var sub = new GUIStyle(EditorStyles.label)
                    { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(subtitle, sub, GUILayout.Width(230));
                EditorGUILayout.EndHorizontal();
            });
        }

        private static void DrawButtons(
            string filePath, string defaultDir, string defaultFileName, string fileExtension,
            ref (int created, int skipped, List<string> errors) result,
            Func<string, (int created, int skipped, List<string> errors)> onImport,
            Action<string> onExport)
        {
            var hasFile = File.Exists(filePath);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!hasFile);
            if (EditorButton.Draw("Import", EditorButtonStyle.Success,
                    EditorButtonSize.Large, 120f))
            {
                result = onImport(filePath);
                AssetDatabase.Refresh();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            if (EditorButton.Draw("Export", EditorButtonStyle.Primary,
                    EditorButtonSize.Large, 120f))
            {
                var outPath = EditorUtility.SaveFilePanel(
                    "Export JSON", defaultDir, defaultFileName, fileExtension);
                if (!string.IsNullOrEmpty(outPath))
                    onExport(outPath);
            }

            EditorGUILayout.EndHorizontal();

            if (!hasFile)
            {
                EditorCard.GapTight();
                EditorCard.DrawLight(Pad, () =>
                {
                    EditorGUILayout.LabelField("File not found. Select a JSON file to import.",
                        EditorUIUtility.GreyPlaceholder);
                });
            }
        }

        private static void DrawResultSection(
            (int created, int skipped, List<string> errors) result)
        {
            var (created, skipped, errors) = result;
            if (created + skipped == 0 && (errors == null || errors.Count == 0)) return;

            var hasErrors = errors != null && errors.Count > 0;

            EditorCard.Draw(Pad, "Result", () =>
            {
                var okStyle = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
                var errStyle = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = new Color(0.9f, 0.3f, 0.2f) } };

                EditorGUILayout.LabelField(
                    $"Created: {created}  ·  Skipped: {skipped}" +
                    (hasErrors ? $"  |  Errors: {errors.Count}" : ""),
                    hasErrors ? errStyle : okStyle);

                if (hasErrors)
                {
                    EditorCard.GapTight();
                    EditorGUILayout.TextArea(string.Join("\n", errors),
                        EditorStyles.miniLabel, GUILayout.MinHeight(40));
                }
                else
                {
                    EditorCard.GapTight();
                    EditorGUILayout.TextArea("No errors.", EditorStyles.miniLabel,
                        GUILayout.MinHeight(20));
                }
            });
        }
    }
}
#endif
