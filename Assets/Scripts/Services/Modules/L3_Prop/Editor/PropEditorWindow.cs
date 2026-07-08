using System;
using RedDust.Entities.Editor;
using RedDust.Prop;
using UnityEditor;

namespace RedDust.Prop.Editor
{
    /// <summary>
    /// 道具编辑器。编辑 ArmorSO / ConsumableSO / AmmoSO / ToolSO / ContainerSO / MaterialSO 预设。
    /// </summary>
    public class PropEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Prop Editor", priority = 8)]
        private static void Open() => GetWindow<PropEditorWindow>("Prop Editor");

        protected override Type GetTargetType() => typeof(PropDefSO);
        protected override string GetEditorTitle() => "Prop Editor";
        protected override string GetBreadcrumb() => "L3_Prop · Editor";
        protected override string GetAssetFilter() => "t:PropDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Armor", typeof(ArmorSO)),
            ("Consumable", typeof(ConsumableSO)),
            ("Ammo", typeof(AmmoSO)),
            ("Tool", typeof(ToolSO)),
            ("Container", typeof(ContainerSO)),
            ("Material", typeof(MaterialSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/Props";
        protected override Action OpenImportWindow() => PropImportWindow.Open;
    }
}
