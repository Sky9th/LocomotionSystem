#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using RedDust.Gameplay.Character;
using RedDust.Services.EntityService.Editor;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Gameplay.Character.Editor
{
    public class CharacterImportWindow : EditorWindow
    {
        private static readonly EntityImportConfig Config = new()
        {
            Category = "Character", Breadcrumb = "L3_Character · JSON ↔ .asset",
            DataRoot = "Assets/Data/Entities/Character", AssetFilter = "t:CharacterDefSO",
            DefaultFileName = "characters_export",
            TypeMap = null,
            DefaultType = typeof(CharacterDefSO),
            BuildPreview = BuildPreview,
        };

        private string _filePath = "Assets/Data/Entities/Character/character_all.json", _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Character Import-Export", priority = 32)]
        public static void Open()
        { var w = GetWindow<CharacterImportWindow>("Character Import-Export"); w.minSize = new Vector2(520, 420); w.Show(); }

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
                return $"<b>{p.entities.Length}</b> characters\nv{p.version} · {p.description ?? "-"}";
            }
            catch { return null; }
        }
    }
}
#endif
