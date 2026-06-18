#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared
{
    /// <summary>
    /// 一次性工具：将 Protofactor 动画 FBX 设为 Humanoid，Avatar 指向 Protof-Actor。
    /// 菜单：RedDust > Fix Protofactor Avatar
    /// </summary>
    public static class ProtofactorAvatarFixer
    {
        const string Dir = "Assets/Art/Animations/Protofactor";
        const string ActorPath = Dir + "/SK_Protof-Actor.fbx";

        [MenuItem("RedDust/Fix Protofactor Avatar")]
        public static void Fix()
        {
            // 1. 先导入 Protof-Actor 为 Humanoid，生成 Avatar
            AssetDatabase.Refresh();
            SetHumanoid(ActorPath, createAvatar: true);
            AssetDatabase.ImportAsset(ActorPath);
            AssetDatabase.SaveAssets();

            // 获取生成的 Avatar
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(ActorPath);
            if (avatar == null)
            {
                // Avatar 是 FBX 的子资产，尝试从所有子资产中找
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(ActorPath);
                foreach (var a in allAssets)
                {
                    if (a is Avatar av) { avatar = av; break; }
                }
            }

            if (avatar == null)
            {
                Debug.LogError("[AvatarFixer] Failed to generate Avatar from Protof-Actor.");
                return;
            }

            Debug.Log($"[AvatarFixer] Protof-Actor Avatar: {avatar.name}");

            // 2. 所有动画 FBX 设为 Humanoid，Avatar 指向 Protof-Actor
            var fbxFiles = Directory.GetFiles(Dir, "Humanoid@*.fbx");
            foreach (var fbx in fbxFiles)
            {
                var assetPath = fbx.Replace("\\", "/");
                SetHumanoid(assetPath, createAvatar: false);
                AssetDatabase.ImportAsset(assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AvatarFixer] <b>Done: {fbxFiles.Length} animation FBX files configured as Humanoid.</b>");
        }

        static void SetHumanoid(string path, bool createAvatar)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[AvatarFixer] Not a ModelImporter: {path}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = createAvatar
                ? ModelImporterAvatarSetup.CreateFromThisModel
                : ModelImporterAvatarSetup.CopyFromOther;

            if (!createAvatar)
            {
                // 指向 Protof-Actor 的 Avatar
                var srcImporter = AssetImporter.GetAtPath(ActorPath) as ModelImporter;
                if (srcImporter != null)
                {
                    importer.sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(ActorPath);
                    if (importer.sourceAvatar == null)
                    {
                        var all = AssetDatabase.LoadAllAssetsAtPath(ActorPath);
                        foreach (var a in all)
                            if (a is Avatar av) { importer.sourceAvatar = av; break; }
                    }
                }
            }

            importer.SaveAndReimport();
        }
    }
}
#endif
