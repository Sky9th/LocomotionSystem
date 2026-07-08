#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// Addressables label 管理工具。Import 流程中创建新资产后调用 EnsureBootLabel。
    /// </summary>
    public static class DataLabelTools
    {
        /// <summary>
        /// 将单个资产注册到 Addressables 并标记 "boot" label。
        /// Importer 在 AssetDatabase.CreateAsset 后调用，确保新资产在 Build 中可用。
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
