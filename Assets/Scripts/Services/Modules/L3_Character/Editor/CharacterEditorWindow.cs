using System;
using RedDust.Character;
using RedDust.Entities.Editor;
using UnityEditor;

namespace RedDust.Character.Editor
{
    /// <summary>
    /// 角色编辑器。编辑 CharacterDefSO 预设。
    /// </summary>
    public class CharacterEditorWindow : EntityEditorWindow
    {
        [MenuItem("RedDust/Character Editor", priority = 6)]
        private static void Open() => GetWindow<CharacterEditorWindow>("Character Editor");

        protected override Type GetTargetType() => typeof(CharacterDefSO);
        protected override string GetEditorTitle() => "Character Editor";
        protected override string GetBreadcrumb() => "L3_Character · Editor";
        protected override string GetAssetFilter() => "t:CharacterDefSO";

        protected override (string label, Type soType)[] GetCreateMenuItems() => new[]
        {
            ("Character", typeof(CharacterDefSO)),
        };

        protected override string GetDefaultAssetDir() => "Assets/Data/Entities/Character";
        protected override Action OpenImportWindow() => CharacterImportWindow.Open;

        protected override (string label, string assetName)[] GetTemplatePresets(Type selectedType) => new[]
        {
            ("Actor", "Actor"),
            ("Human", "Human"),
            ("Zombie", "Zombie"),
        };
    }
}
