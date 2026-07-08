using System;
using RedDust.Entities.Editor;
using RedDust.SceneItem;
using UnityEditor;

namespace RedDust.SceneItem.Editor
{
    /// <summary>
    /// 场景物品编辑器。编辑 SceneItemDefSO 预设（家具/装饰物/场景物体）。
    /// </summary>
    public class SceneItemEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Scene Item Editor", priority = 10)]
        private static void Open() => GetWindow<SceneItemEditorWindow>("Scene Item Editor");

        protected override Type GetTargetType() => typeof(SceneItemDefSO);
        protected override string GetEditorTitle() => "Scene Item Editor";
        protected override string GetBreadcrumb() => "L3_SceneItem · Editor";
        protected override string GetAssetFilter() => "t:SceneItemDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Scene Item", typeof(SceneItemDefSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/SceneItems";
        protected override Action OpenImportWindow() => SceneItemImportWindow.Open;

        protected override (string label, string assetName)[] GetTemplatePresets(Type selectedType) => new[]
        {
            ("Entity", "Entity"),
            ("Environment", "Environment"),
            ("Equipment", "Equipment"),
        };
    }
}
