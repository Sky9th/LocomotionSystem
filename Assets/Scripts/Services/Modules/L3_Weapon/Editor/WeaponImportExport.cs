#if UNITY_EDITOR
using System; using System.Collections.Generic; using System.IO;
using RedDust.Entities.Editor; using RedDust.Shared.EditorUI;
using RedDust.Weapon; using UnityEditor; using UnityEngine;

namespace RedDust.Weapon.Editor
{
    public class WeaponImportWindow : EditorWindow
    {
        private static readonly EntityImportConfig Config = new()
        {
            Category = "Weapon", Breadcrumb = "L3_Weapon · JSON ↔ .asset",
            DataRoot = "Assets/Data/Entities/Weapons", AssetFilter = "t:WeaponDefSO",
            DefaultFileName = "weapons_export",
            TypeMap = new() { ["MeleeWeapon"] = typeof(MeleeWeaponSO), ["RangedWeapon"] = typeof(RangedWeaponSO) },
            DefaultType = typeof(MeleeWeaponSO),
            BuildPreview = BuildPreview,
        };

        private string _filePath, _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Weapon Import-Export", priority = 30)]
        public static void Open()
        { var w = GetWindow<WeaponImportWindow>("Weapon Import-Export"); w.minSize = new Vector2(520, 420); w.Show(); }

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
                int melee = 0, ranged = 0;
                foreach (var e in p.entities)
                { if (e.entityType == "RangedWeapon") ranged++; else melee++; }
                var parts = new List<string>();
                if (melee > 0) parts.Add($"<b>{melee}</b> Melee");
                if (ranged > 0) parts.Add($"<b>{ranged}</b> Ranged");
                return $"<b>{p.entities.Length}</b> weapons ({string.Join(" / ", parts)})\nv{p.version} · {p.description ?? "-"}";
            }
            catch { return null; }
        }
    }
}
#endif
