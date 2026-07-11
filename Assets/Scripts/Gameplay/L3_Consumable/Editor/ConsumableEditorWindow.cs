using System;
using RedDust.Services.EntityService.Editor;
using UnityEditor;

namespace RedDust.Gameplay.Consumable.Editor
{
    /// <summary>
    /// 消耗品编辑器。编辑 ConsumableSO / MaterialSO 预设。
    /// </summary>
    public class ConsumableEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Consumable Editor", priority = 8)]
        private static void Open() => GetWindow<ConsumableEditorWindow>("Consumable Editor");

        protected override Type GetTargetType() => typeof(ConsumableDefSO);
        protected override string GetEditorTitle() => "Consumable Editor";
        protected override string GetBreadcrumb() => "L3_Consumable · Editor";
        protected override string GetAssetFilter() => "t:ConsumableDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Consumable", typeof(ConsumableSO)),
            ("Material", typeof(MaterialSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/Consumable";

        protected override string GetAssetDirForType(Type soType) => soType switch
        {
            _ when soType == typeof(ConsumableSO) => "Assets/Data/Entities/Consumable",
            _ when soType == typeof(MaterialSO) => "Assets/Data/Entities/Consumable",
            _ => "Assets/Data/Entities/Consumable",
        };

        protected override Action OpenImportWindow() => ConsumableImportWindow.Open;

        protected override (string label, string assetName)[] GetTemplatePresets(Type selectedType)
        {
            if (selectedType == typeof(ConsumableSO))
                return new[] { ("Consumable Base", "ConsumableBase"), ("Food", "Food"), ("Medical", "Medical") };
            if (selectedType == typeof(MaterialSO))
                return new[] { ("Material", "Material"), ("Seed", "Seed") };
            return null;
        }
    }
}
