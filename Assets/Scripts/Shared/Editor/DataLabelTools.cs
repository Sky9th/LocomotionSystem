#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    public static class DataLabelTools
    {
        [MenuItem("RedDust/Data/Tag All Data as 'boot'")]
        public static void TagAllData()
        {
            TagFolder("Assets/Data", "boot");
        }

        [MenuItem("RedDust/Data/Tag Prototype Art as 'prototype-art'")]
        public static void TagPrototypeArt()
        {
            TagFolder("Assets/Art/PolygonPrototype", "prototype-art");
        }

        private static void TagFolder(string folder, string label)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[DataLabelTools] No AddressableAssetSettings found.");
                return;
            }

            var targetGroup = settings.DefaultGroup;
            var guids = AssetDatabase.FindAssets("t:Object", new[] { folder });
            int tagged = 0;

            foreach (var guid in guids)
            {
                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                    entry = settings.CreateOrMoveEntry(guid, targetGroup);
                if (!entry.labels.Contains(label))
                {
                    entry.SetLabel(label, true, true);
                    tagged++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DataLabelTools] Tagged {tagged} of {guids.Length} assets in '{folder}' with label '{label}'.");
        }

        /// <summary>
        /// Ensure a single asset is registered in Addressables and tagged with "boot".
        /// Called by importers after AssetDatabase.CreateAsset to make new assets available in builds.
        /// </summary>
        public static void EnsureBootLabel(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);

            if (!entry.labels.Contains("boot"))
                entry.SetLabel("boot", true, true);
        }
    }
}
#endif
