#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    /// <summary>
    /// 标签工厂。强制非跨级链式创建，事务式（失败回滚）。
    /// </summary>
    public static class TagCreator
    {
        private const string TagRoot = "Assets/Data/Tags";

        /// <summary>
        /// 创建完整标签链，返回叶节点 SO。
        /// "Damage.Elemental.Fire" → 确保 Damage, Damage.Elemental 存在，创建 Fire。
        /// </summary>
        public static RdTagDefSO CreateTagChain(string fullTag)
        {
            if (string.IsNullOrEmpty(fullTag))
                throw new ArgumentException("fullTag is required");

            var segments = fullTag.Split('.');
            var created = new List<string>();

            // 确保目录存在 + 清理残留（必须在 StartAssetEditing 之前）
            for (int i = 0; i < segments.Length; i++)
            {
                var dir = GetAssetDirectory(segments, i);
                var fullDir = Path.Combine(Application.dataPath, dir.Substring("Assets/".Length));
                if (!Directory.Exists(fullDir))
                {
                    Directory.CreateDirectory(fullDir);
                    AssetDatabase.Refresh();
                }

                // 删除残留资产（上次失败可能留下的）
                var assetPath = $"{dir}/Tag_{segments[i]}.asset";
                var stale = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(assetPath);
                if (stale != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    AssetDatabase.Refresh();
                }
            }

            RdTagDefSO parent = null;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    var partialTag = string.Join(".", segments, 0, i + 1);
                    var existing = FindExistingTag(partialTag);
                    if (existing != null)
                    {
                        parent = existing;
                        continue;
                    }

                    var assetPath = $"{GetAssetDirectory(segments, i)}/Tag_{segments[i]}.asset";
                    var newTag = ScriptableObject.CreateInstance<RdTagDefSO>();
                    using (var serialized = new SerializedObject(newTag))
                    {
                        serialized.FindProperty("leafName").stringValue = segments[i];
                        serialized.FindProperty("parent").objectReferenceValue = parent;
                        serialized.ApplyModifiedProperties();
                    }
                    AssetDatabase.CreateAsset(newTag, assetPath);
                    created.Add(assetPath);
                    parent = newTag;
                }
            }
            catch (Exception)
            {
                // 回滚
                for (int i = created.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(created[i]);
                throw;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            return parent;
        }

        private static string GetAssetDirectory(string[] segments, int index)
        {
            var path = TagRoot;
            for (int i = 0; i < index; i++)
                path += $"/{segments[i]}";
            return path;
        }

        private static RdTagDefSO FindExistingTag(string fullTag)
        {
            var guids = AssetDatabase.FindAssets("t:RdTagDefSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tag = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(path);
                if (tag != null && tag.FullTag == fullTag)
                    return tag;
            }
            return null;
        }
    }
}
#endif
