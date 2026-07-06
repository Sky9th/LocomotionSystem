#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace RedDust.Properties.Editor
{
    /// <summary>
    /// One-shot editor tool: tags all PropertyDefSO assets under Assets/Data/Properties/Definitions/
    /// as Addressable with the "boot" label, so AddressablesService can load them at runtime.
    /// </summary>
    public static class PropertyDefLabelTool
    {
        private const string DefsFolder = "Assets/Data/Properties/Definitions";
        private const string Label = "boot";

        [MenuItem("RedDust/Properties/Tag All PropertyDefSO as 'boot'")]
        public static void TagAll()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[PropertyDefLabelTool] No AddressableAssetSettings found. Open Window > Asset Management > Addressables > Groups first.");
                return;
            }

            var targetGroup = settings.DefaultGroup;
            var guids = AssetDatabase.FindAssets("t:PropertyDefSO", new[] { DefsFolder });

            int tagged = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var entry = settings.FindAssetEntry(guid);

                // Create Addressable entry if not already Addressable
                if (entry == null)
                    entry = settings.CreateOrMoveEntry(guid, targetGroup);

                // Set label
                if (!entry.labels.Contains(Label))
                {
                    entry.SetLabel(Label, true, true);
                    tagged++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PropertyDefLabelTool] Tagged {tagged} of {guids.Length} PropertyDefSOs with label '{Label}'.");
        }
    }
}
#endif
