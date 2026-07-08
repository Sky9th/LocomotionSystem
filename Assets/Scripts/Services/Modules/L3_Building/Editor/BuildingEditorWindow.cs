using System;
using RedDust.Building;
using RedDust.Entities.Editor;
using UnityEditor;

namespace RedDust.Building.Editor
{
    /// <summary>
    /// 建筑编辑器。编辑 BuildingDefSO 预设。
    /// </summary>
    public class BuildingEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Building Editor", priority = 9)]
        private static void Open() => GetWindow<BuildingEditorWindow>("Building Editor");

        protected override Type GetTargetType() => typeof(BuildingDefSO);
        protected override string GetEditorTitle() => "Building Editor";
        protected override string GetBreadcrumb() => "L3_Building · Editor";
        protected override string GetAssetFilter() => "t:BuildingDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Building", typeof(BuildingDefSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/Building";
        protected override Action OpenImportWindow() => BuildingImportWindow.Open;

        protected override (string label, string assetName)[] GetTemplatePresets(Type selectedType) => new[]
        {
            ("Building", "Building"),
        };
    }
}
