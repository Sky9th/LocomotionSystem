#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    public static class AbilityListView
    {

        public static void DrawFilterCard(AbilityTypeFilter current, Action<AbilityTypeFilter> onChanged)
        {
            EditorCard.Draw(() =>
            {
                var next = EditorButtonGroup.Draw(current,
                    new[] { AbilityTypeFilter.All, AbilityTypeFilter.Active, AbilityTypeFilter.Passive },
                    new[] { "All", "Active", "Passive" });
                if (!EqualityComparer<AbilityTypeFilter>.Default.Equals(next, current))
                    onChanged(next);
            });
        }

        public static void DrawSearchCard(string current, Action<string> onChanged)
        {
            EditorCard.Draw(() =>
            {
                var s = EditorSearchBar.Draw(current, labelWidth: 45f);
                if (s != current) onChanged(s);
            });
        }

        public static void DrawCreateCard(Action<AbilitySO> onCreated)
        {
            EditorCard.Draw(() =>
            {
                if (EditorButton.Draw("+ Create New", EditorButtonType.Primary,
                        EditorButtonSize.Large, 160f))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Active Ability"), false,
                        () => CreateAbility<ActiveAbilitySO>("Ability_New", "Assets/Data/Ability/Actives", onCreated));
                    menu.AddItem(new GUIContent("Passive Ability"), false,
                        () => CreateAbility<PassiveAbilitySO>("Passive_New", "Assets/Data/Ability/Passives", onCreated));
                    menu.ShowAsContext();
                }
            });
        }

        public static void CreateAbility<T>(string prefix, string dir, Action<AbilitySO> onCreated) where T : AbilitySO
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{prefix}.asset");
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AbilityEditor] Created {path}");
            onCreated?.Invoke(instance);
        }
    }
}
#endif
