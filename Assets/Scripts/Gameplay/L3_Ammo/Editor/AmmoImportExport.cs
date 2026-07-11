#if UNITY_EDITOR
using System; using System.Collections.Generic; using System.IO;
using RedDust.Entities.Editor; using RedDust.Shared.EditorUI;
using UnityEditor; using UnityEngine;

namespace RedDust.Ammo.Editor
{
    public class AmmoImportWindow : EditorWindow
    {
        private static readonly EntityImportConfig Config = new()
        {
            Category = "Ammo", Breadcrumb = "L3_Ammo · JSON ↔ .asset",
            DataRoot = "Assets/Data/Entities/Ammo", AssetFilter = "t:AmmoDefSO",
            DefaultFileName = "ammo_export",
            TypeMap = null,
            DefaultType = typeof(AmmoSO),
            BuildPreview = BuildPreview,
        };

        private string _filePath = "Assets/Data/Entities/Ammo/ammo_all.json", _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Ammo Import-Export", priority = 33)]
        public static void Open()
        { var w = GetWindow<AmmoImportWindow>("Ammo Import-Export"); w.minSize = new Vector2(520, 420); w.Show(); }

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
                return $"<b>{p.entities.Length}</b> ammo\nv{p.version} · {p.description ?? "-"}";
            }
            catch { return null; }
        }
    }
}
#endif
