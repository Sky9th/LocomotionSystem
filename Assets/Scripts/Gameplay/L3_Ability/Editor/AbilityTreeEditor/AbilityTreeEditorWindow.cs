#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// AbilityTree Import-Export 窗口。使用共享 EditorImportExport 组件。
    /// </summary>
    public class AbilityTreeImportWindow : EditorWindow
    {
        private string _filePath = "Assets/Data/Ability/AbilityTrees/abilityTrees_all.json";
        private string _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Ability Tree Import-Export", priority = 21)]
        public static void Open()
        {
            var window = GetWindow<AbilityTreeImportWindow>("Ability Tree Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Ability Tree Import-Export",
                subtitle: "L3_Ability · AbilityTreeSO · JSON ↔ .asset",
                defaultDir: "Assets/Data/Ability/AbilityTrees",
                fileExtension: "json",
                defaultFileName: "abilityTrees",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    if (!File.Exists(path))
                        return (0, 0, 0, new List<string> { $"File not found: {path}" });
                    return AbilityTreeImporter.ImportFromJson(File.ReadAllText(path));
                },
                onExport: path => File.WriteAllText(path, AbilityTreeImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            AbilityTreeExportFile preview;
            try { preview = JsonUtility.FromJson<AbilityTreeExportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview?.trees == null || preview.trees.Length == 0) return null;

            int newCount = 0, existCount = 0;
            foreach (var entry in preview.trees)
            {
                var assetPath = $"Assets/Data/Ability/AbilityTrees/{entry.treeId}.asset";
                if (File.Exists(assetPath)) existCount++;
                else newCount++;
            }

            return $"<b>{preview.trees.Length}</b> trees\n" +
                   $"v{preview.version} · {preview.description ?? "-"}\n" +
                   $"<color=#66CC66>New {newCount}</color>  <color=#888888>Exist {existCount}</color>";
        }
    }
}
#endif
