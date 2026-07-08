#if UNITY_EDITOR
using System; using System.Collections.Generic; using System.IO;
using RedDust.Entities.Editor; using RedDust.SceneItem; using RedDust.Shared.EditorUI;
using UnityEditor; using UnityEngine;

namespace RedDust.SceneItem.Editor
{
    public class SceneItemImportWindow : EditorWindow
    {
        private static readonly EntityImportConfig Config = new()
        {
            Category = "SceneItem", Breadcrumb = "L3_SceneItem · JSON ↔ .asset",
            DataRoot = "Assets/Data/Entities/SceneItems", AssetFilter = "t:SceneItemDefSO",
            DefaultFileName = "sceneitems_export",
            TypeMap = null,
            DefaultType = typeof(SceneItemDefSO),
            BuildPreview = BuildPreview,
        };

        private string _filePath, _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Scene Item Import-Export", priority = 34)]
        public static void Open()
        { var w = GetWindow<SceneItemImportWindow>("Scene Item Import-Export"); w.minSize = new Vector2(520, 420); w.Show(); }

        private void OnGUI()
        {
            EditorImportExport.Draw(Config.Category + " Import-Export", Config.Breadcrumb,
                Config.DataRoot, "json", Config.DefaultFileName,
                ref _filePath, ref _previewText, ref _result,
                Config.BuildPreview,
                path => EntityImporter.ImportFromFile(path, Config),
                path => File.WriteAllText(path, EntityImporter.ExportToJson(Config)));
        }

        private static string BuildPreview(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var p = JsonUtility.FromJson<EntityExportFile>(File.ReadAllText(path));
                if (p?.entities == null || p.entities.Length == 0) return null;
                return $"<b>{p.entities.Length}</b> scene items\nv{p.version} · {p.description ?? "-"}";
            }
            catch { return null; }
        }
    }
}
#endif
