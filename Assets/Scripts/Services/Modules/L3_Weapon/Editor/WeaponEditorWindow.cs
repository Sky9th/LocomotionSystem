using System;
using RedDust.Entities.Editor;
using RedDust.Weapon;
using UnityEditor;

namespace RedDust.Weapon.Editor
{
    /// <summary>
    /// 武器编辑器。编辑 MeleeWeaponSO / RangedWeaponSO 预设。
    /// </summary>
    public class WeaponEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Weapon Editor", priority = 7)]
        private static void Open() => GetWindow<WeaponEditorWindow>("Weapon Editor");

        protected override Type GetTargetType() => typeof(WeaponDefSO);
        protected override string GetEditorTitle() => "Weapon Editor";
        protected override string GetBreadcrumb() => "L3_Weapon · Editor";
        protected override string GetAssetFilter() => "t:WeaponDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Melee Weapon", typeof(MeleeWeaponSO)),
            ("Ranged Weapon", typeof(RangedWeaponSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/Weapons";

        protected override string GetAssetDirForType(Type soType) => soType == typeof(MeleeWeaponSO)
            ? "Assets/Data/Entities/Weapons/Melee"
            : "Assets/Data/Entities/Weapons/Ranged";
        protected override Action OpenImportWindow() => WeaponImportWindow.Open;

        protected override (string label, string assetName)[] GetTemplatePresets(Type selectedType)
        {
            if (selectedType == typeof(MeleeWeaponSO))
                return new[] { ("Weapon Base", "WeaponBase"), ("Melee Weapon", "MeleeWeapon"), ("Axe", "Axe"), ("Blade", "Blade"), ("Blunt", "Blunt"), ("Polearm", "Polearm") };
            if (selectedType == typeof(RangedWeaponSO))
                return new[] { ("Weapon Base", "WeaponBase"), ("Ranged Weapon", "RangedWeapon"), ("Firearm", "Firearm"), ("Pistol", "Pistol"), ("Rifle", "Rifle"), ("Shotgun", "Shotgun"), ("Bow", "Bow"), ("Throwable", "Throwable") };
            return new[] { ("Weapon Base", "WeaponBase"), ("Melee Weapon", "MeleeWeapon"), ("Ranged Weapon", "RangedWeapon"), ("Firearm", "Firearm"), ("Pistol", "Pistol"), ("Rifle", "Rifle"), ("Shotgun", "Shotgun"), ("Bow", "Bow"), ("Throwable", "Throwable"), ("Axe", "Axe"), ("Blade", "Blade"), ("Blunt", "Blunt"), ("Polearm", "Polearm") };
        }
    }
}
