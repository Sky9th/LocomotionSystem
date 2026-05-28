#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SyntyPrototypeBrowser : EditorWindow
{
    private List<SyntyPrototypeMenu.CategoryData> _categories;
    private int _selectedCategoryIndex;
    private string _searchFilter = "";
    private Vector2 _categoryScroll;
    private Vector2 _prefabScroll;
    private readonly float _thumbnailSize = 80f;

    // Material variants
    private List<Material> _materials;
    private int _selectedMatIndex = -1; // -1 = keep original

    // Lazy-loaded references for AssetPreview
    private readonly Dictionary<string, GameObject> _prefabRefs = new();
    private readonly Dictionary<Material, Texture2D> _matPreviews = new();

    public static void Open(List<SyntyPrototypeMenu.CategoryData> categories)
    {
        var window = GetWindow<SyntyPrototypeBrowser>("Synty Prototype");
        window._categories = categories;
        window._selectedCategoryIndex = 0;
        window.minSize = new Vector2(480, 420);
        window.ScanMaterials();
    }

    private void OnGUI()
    {
        if (_categories == null || _categories.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No Synty prefabs found. Check that PolygonPrototype is at:\n" +
                "Assets/Art/PolygonPrototype/Prefabs/",
                MessageType.Warning);
            return;
        }

        DrawSearchBar();
        DrawMaterialBar();
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            DrawFilteredResults();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawCategorySidebar();
        DrawPrefabGrid();
        EditorGUILayout.EndHorizontal();
    }

    // ---- material scanning ----

    private void ScanMaterials()
    {
        _materials = new List<Material>();
        var matDir = "Assets/Art/PolygonPrototype/Materials";
        if (!Directory.Exists(matDir)) return;

        for (var i = 1; i <= 10; i++)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(
                $"{matDir}/PolygonPrototype_Global_Grid_{i:D2}.mat");
            if (mat != null) _materials.Add(mat);
        }
        for (var i = 1; i <= 10; i++)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(
                $"{matDir}/PolygonPrototype_Texture_{i:D2}.mat");
            if (mat != null) _materials.Add(mat);
        }
    }

    private void PlaceWithMaterial(string path)
    {
        SyntyPrototypeMenu.InstantiateByPath(path);
        if (_selectedMatIndex < 0 || _selectedMatIndex >= _materials.Count) return;

        var instance = Selection.activeGameObject;
        if (instance == null) return;

        var mat = _materials[_selectedMatIndex];
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = mat;
    }

    // ---- toolbar bars ----

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(24)))
        {
            _searchFilter = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMaterialBar()
    {
        if (_materials == null || _materials.Count == 0) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Mat:", GUILayout.Width(30));

        // "Original" button
        EditorGUI.BeginChangeCheck();
        var origActive = _selectedMatIndex == -1;
        GUI.backgroundColor = origActive ? Color.cyan : Color.white;
        if (GUILayout.Button("Orig", EditorStyles.toolbarButton, GUILayout.Width(36)))
            _selectedMatIndex = -1;
        GUI.backgroundColor = Color.white;

        // Grid materials (first 10)
        GUILayout.Space(4);
        for (var i = 0; i < _materials.Count; i++)
        {
            if (i == 10) GUILayout.Space(8); // gap between Grid and Texture

            var active = i == _selectedMatIndex;
            var preview = GetMatPreview(_materials[i]);
            var content = preview != null
                ? new GUIContent(preview, _materials[i].name)
                : new GUIContent(_materials[i].name[..4]);

            GUI.backgroundColor = active ? Color.cyan : Color.white;
            if (GUILayout.Button(content, EditorStyles.toolbarButton,
                    GUILayout.Width(24), GUILayout.Height(20)))
                _selectedMatIndex = i;
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();
    }

    private Texture2D GetMatPreview(Material mat)
    {
        if (!_matPreviews.TryGetValue(mat, out var tex) || tex == null)
        {
            tex = AssetPreview.GetAssetPreview(mat);
            _matPreviews[mat] = tex;
        }
        return tex;
    }

    // ---- category sidebar ----

    private void DrawCategorySidebar()
    {
        _categoryScroll = EditorGUILayout.BeginScrollView(_categoryScroll,
            GUILayout.Width(130), GUILayout.ExpandHeight(true));

        for (var i = 0; i < _categories.Count; i++)
        {
            var cat = _categories[i];
            var label = $"{cat.name}  ({cat.prefabs.Count})";
            var style = i == _selectedCategoryIndex
                ? EditorStyles.boldLabel
                : EditorStyles.label;

            if (GUILayout.Button(label, style, GUILayout.Height(24)))
            {
                _selectedCategoryIndex = i;
                _prefabScroll = Vector2.zero;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // ---- thumbnail grid ----

    private void DrawPrefabGrid()
    {
        if (_selectedCategoryIndex >= _categories.Count) return;

        var selected = _categories[_selectedCategoryIndex];
        _prefabScroll = GUILayout.BeginScrollView(_prefabScroll, GUILayout.ExpandHeight(true));

        var availableWidth = position.width - 140f;
        var cellWidth = _thumbnailSize + 12f;
        var columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / cellWidth));

        for (var i = 0; i < selected.prefabs.Count; i++)
        {
            if (i % columns == 0)
                EditorGUILayout.BeginHorizontal();

            DrawThumbnailCell(selected.prefabs[i]);

            if (i % columns == columns - 1 || i == selected.prefabs.Count - 1)
                EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void DrawThumbnailCell(SyntyPrototypeMenu.PrefabEntry entry)
    {
        var go = GetPrefabRef(entry.path);
        var preview = go != null ? AssetPreview.GetAssetPreview(go) : null;

        var label = entry.displayName;
        if (entry.isPolygon) label += " (P)";

        GUILayout.BeginVertical(GUILayout.Width(_thumbnailSize + 4));
        GUILayout.Space(4);

        if (preview != null)
        {
            if (GUILayout.Button(new GUIContent(preview, label), GUIStyle.none,
                    GUILayout.Width(_thumbnailSize), GUILayout.Height(_thumbnailSize)))
                PlaceWithMaterial(entry.path);
        }
        else
        {
            var rect = GUILayoutUtility.GetRect(_thumbnailSize, _thumbnailSize);
            GUI.Box(rect, entry.displayName, EditorStyles.helpBox);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                PlaceWithMaterial(entry.path);
        }

        GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel,
            GUILayout.Width(_thumbnailSize + 4));
        GUILayout.EndVertical();
    }

    // ---- filtered search results ----

    private void DrawFilteredResults()
    {
        _prefabScroll = GUILayout.BeginScrollView(_prefabScroll, GUILayout.ExpandHeight(true));

        var filter = _searchFilter.ToLowerInvariant();
        var availableWidth = position.width - 16f;
        var cellWidth = _thumbnailSize + 12f;
        var columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / cellWidth));

        foreach (var cat in _categories)
        {
            var matches = cat.prefabs
                .Where(e => e.displayName.ToLowerInvariant().Contains(filter))
                .ToList();
            if (matches.Count == 0) continue;

            GUILayout.Label(cat.name, EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < matches.Count; i++)
            {
                if (i > 0 && i % columns == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                DrawThumbnailCell(matches[i]);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        GUILayout.EndScrollView();
    }

    // ---- helpers ----

    private GameObject GetPrefabRef(string path)
    {
        if (!_prefabRefs.TryGetValue(path, out var go) || go == null)
        {
            go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            _prefabRefs[path] = go;
        }
        return go;
    }
}
#endif
