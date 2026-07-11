using System;
using RedDust.Services.EntityService.Editor;
using UnityEditor;

namespace RedDust.Gameplay.Ammo.Editor
{
    /// <summary>
    /// 弹药编辑器。编辑 AmmoSO 预设。
    /// </summary>
    public class AmmoEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Ammo Editor", priority = 9)]
        private static void Open() => GetWindow<AmmoEditorWindow>("Ammo Editor");

        protected override Type GetTargetType() => typeof(AmmoDefSO);
        protected override string GetEditorTitle() => "Ammo Editor";
        protected override string GetBreadcrumb() => "L3_Ammo · Editor";
        protected override string GetAssetFilter() => "t:AmmoDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Ammo", typeof(AmmoSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/Ammo";

        protected override Action OpenImportWindow() => AmmoImportWindow.Open;

        protected override (string label, string assetName)[] GetTemplatePresets(Type selectedType)
        {
            if (selectedType == typeof(AmmoSO))
                return new[] { ("Ammo Base", "AmmoBase"), ("Pistol Ammo", "PistolAmmo"), ("Rifle Ammo", "RifleAmmo"), ("Shotgun Shell", "ShotgunShell") };
            return null;
        }
    }
}
