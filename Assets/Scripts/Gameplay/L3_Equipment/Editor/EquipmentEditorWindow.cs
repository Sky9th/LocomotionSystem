using System;
using RedDust.Services.EntityService.Editor;
using UnityEditor;

namespace RedDust.Gameplay.Equipment.Editor
{
    /// <summary>
    /// 装备编辑器。编辑 MeleeWeaponSO / RangedWeaponSO / ArmorSO / ToolSO / ContainerSO 预设。
    /// </summary>
    public class EquipmentEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Equipment Editor", priority = 7)]
        private static void Open() => GetWindow<EquipmentEditorWindow>("Equipment Editor");

        protected override Type GetTargetType() => typeof(EquipmentDefSO);
        protected override string GetEditorTitle() => "Equipment Editor";
        protected override string GetBreadcrumb() => "L3_Equipment · Editor";
        protected override string GetAssetFilter() => "t:EquipmentDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Melee Weapon", typeof(MeleeWeaponSO)),
            ("Ranged Weapon", typeof(RangedWeaponSO)),
            ("Armor", typeof(ArmorSO)),
            ("Tool", typeof(ToolSO)),
            ("Container", typeof(ContainerSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/Equipment";

        protected override string GetAssetDirForType(Type soType) => soType switch
        {
            _ when soType == typeof(MeleeWeaponSO) => "Assets/Data/Entities/Equipment/Weapon/Melee",
            _ when soType == typeof(RangedWeaponSO) => "Assets/Data/Entities/Equipment/Weapon/Ranged",
            _ when soType == typeof(ArmorSO) => "Assets/Data/Entities/Equipment/Armor",
            _ when soType == typeof(ToolSO) => "Assets/Data/Entities/Equipment/Tool",
            _ when soType == typeof(ContainerSO) => "Assets/Data/Entities/Equipment/Container",
            _ => "Assets/Data/Entities/Equipment",
        };

        protected override Action OpenImportWindow() => EquipmentImportWindow.Open;

        protected override (string label, string assetName)[] GetTemplatePresets(Type selectedType)
        {
            if (selectedType == typeof(MeleeWeaponSO))
                return new[] { ("Weapon Base", "WeaponBase"), ("Melee Weapon", "MeleeWeapon"), ("Axe", "Axe"), ("Blade", "Blade"), ("Blunt", "Blunt"), ("Polearm", "Polearm") };
            if (selectedType == typeof(RangedWeaponSO))
                return new[] { ("Weapon Base", "WeaponBase"), ("Ranged Weapon", "RangedWeapon"), ("Firearm", "Firearm"), ("Pistol", "Pistol"), ("Rifle", "Rifle"), ("Shotgun", "Shotgun"), ("Bow", "Bow"), ("Throwable", "Throwable") };
            if (selectedType == typeof(ArmorSO))
                return new[] { ("Armor Base", "ArmorBase"), ("Body Armor", "BodyArmor"), ("Head Armor", "HeadArmor"), ("Leg Armor", "LegArmor") };
            if (selectedType == typeof(ToolSO))
                return new[] { ("Tool Base", "ToolBase"), ("Repair Kit", "RepairKit") };
            if (selectedType == typeof(ContainerSO))
                return new[] { ("Backpack", "Backpack") };
            return null;
        }
    }
}
