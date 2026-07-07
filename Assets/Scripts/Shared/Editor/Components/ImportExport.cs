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
        public static void Draw(
            string title,
            string subtitle,
            string defaultDir,
            string fileExtension,
            string defaultFileName,
            ref string filePath,
            ref string previewText,
            ref (int created, int updated, int skipped, List<string> errors) result,
            Func<string, string> buildPreview,
            Func<string, (int created, int updated, int skipped, List<string> errors)> onImport,
            Action<string> onExport)
        {
            var fp = filePath;
            var pv = previewText;

            EditorGUILayout.BeginHorizontal();
            EditorLabel.Draw(title, style: EditorTokens.HeaderTitleStyle);
            var subWidth = EditorTokens.BreadcrumbStyle.CalcSize(new GUIContent(subtitle ?? "")).x;
            EditorLabel.Draw(subtitle, subWidth, style: EditorTokens.BreadcrumbStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorCard.Gap(EditorTokens.Pad);

            // 文件选择
            EditorCard.Draw("JSON File", () =>
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

            EditorCard.Gap(EditorTokens.Pad);

            // 预览
            if (buildPreview != null)
            {
                EditorCard.Draw(() =>
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
                            var style = EditorTokens.RichLabelStyle;
                            var height = style.CalcHeight(content,
                                EditorGUIUtility.currentViewWidth - EditorTokens.Pad * 10);
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

                EditorCard.Gap(EditorTokens.Pad);
            }

            // 按钮
            DrawButtons(fp, defaultDir, defaultFileName, fileExtension, ref result, onImport, onExport);

            EditorCard.Gap(EditorTokens.Pad);

            // 结果
            DrawResultSection(result);

            filePath = fp;
            previewText = pv;
        }

        private static void DrawButtons(
            string filePath, string defaultDir, string defaultFileName, string fileExtension,
            ref (int created, int updated, int skipped, List<string> errors) result,
            Func<string, (int created, int updated, int skipped, List<string> errors)> onImport,
            Action<string> onExport)
        {
            var hasFile = File.Exists(filePath);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!hasFile);
            if (EditorButton.Draw("Import", EditorButtonType.Success,
                    EditorButtonSize.Medium))
            {
                result = onImport(filePath);
                AssetDatabase.Refresh();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            if (EditorButton.Draw("Export", EditorButtonType.Primary,
                    EditorButtonSize.Medium))
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
                EditorCard.Draw(() =>
                {
                    EditorGUILayout.LabelField("File not found. Select a JSON file to import.",
                        EditorUIUtility.GreyPlaceholder);
                });
            }
        }

        private static void DrawResultSection(
            (int created, int updated, int skipped, List<string> errors) result)
        {
            var (created, updated, skipped, errors) = result;
            if (created + updated + skipped == 0 && (errors == null || errors.Count == 0)) return;

            var hasErrors = errors != null && errors.Count > 0;

            EditorCard.Draw("Result", () =>
            {
                var okStyle = new GUIStyle(EditorTokens.SuccessLabelStyle)
                    { normal = { textColor = EditorTokens.ColorResultOk } };
                var errStyle = new GUIStyle(EditorTokens.ErrorLabelStyle)
                    { normal = { textColor = EditorTokens.ColorResultErr } };

                EditorGUILayout.LabelField(
                    $"Created: {created}  ·  Updated: {updated}  ·  Skipped: {skipped}" +
                    (hasErrors ? $"  |  Errors: {errors.Count}" : ""),
                    hasErrors ? errStyle : okStyle);

                if (hasErrors)
                {
                    EditorCard.GapTight();
                    var scrollPos = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.MaxHeight(160));
                    EditorGUILayout.TextArea(string.Join("\n", errors),
                        EditorStyles.miniLabel, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
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
