#if UNITY_EDITOR
using System; using System.Collections.Generic; using System.IO;
using RedDust.Entities.Editor; using RedDust.Prop; using RedDust.Shared.EditorUI;
using UnityEditor; using UnityEngine;

namespace RedDust.Prop.Editor
{
    public class PropImportWindow : EditorWindow
    {
        private static readonly EntityImportConfig Config = new()
        {
            Category = "Prop", Breadcrumb = "L3_Prop · JSON ↔ .asset",
            DataRoot = "Assets/Data/Entities/Props", AssetFilter = "t:PropDefSO",
            DefaultFileName = "props_export",
            TypeMap = new()
            {
                ["Armor"] = typeof(ArmorSO), ["Consumable"] = typeof(ConsumableSO),
                ["Ammo"] = typeof(AmmoSO), ["Tool"] = typeof(ToolSO),
                ["Container"] = typeof(ContainerSO), ["Material"] = typeof(MaterialSO),
            },
            DefaultType = typeof(MaterialSO),
            BuildPreview = BuildPreview,
        };

        private string _filePath, _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Prop Import-Export", priority = 31)]
        public static void Open()
        { var w = GetWindow<PropImportWindow>("Prop Import-Export"); w.minSize = new Vector2(520, 420); w.Show(); }

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
                var counts = new Dictionary<string, int>();
                foreach (var e in p.entities)
                { var t = e.entityType ?? "?"; counts[t] = counts.TryGetValue(t, out var c) ? c + 1 : 1; }
                var parts = new List<string>();
                foreach (var kv in counts) parts.Add($"<b>{kv.Value}</b> {kv.Key}");
                return $"<b>{p.entities.Length}</b> props ({string.Join(" / ", parts)})\nv{p.version} · {p.description ?? "-"}";
            }
            catch { return null; }
        }
    }
}
#endif
