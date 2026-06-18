#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared
{
    /// <summary>
    /// 将 HumanTransitions.asset 重映射到 Protofactor 独立动画 FBX。
    /// 菜单：RedDust > Remap HumanTransitions to Protofactor
    /// </summary>
    public static class ProtofactorAnimationRemapper
    {
        const string AssetPath = "Assets/Data/Animancer/HumanTransitions.asset";
        const string FbxDir = "Assets/Art/Animations/Protofactor/";

        // HumanTransitions m_Name → 独立 FBX 文件名
        static readonly Dictionary<string, string> SimpleMap = new()
        {
            // ── Simple Clips (Basic Locomotion) ──
            ["Sprint"]              = "Humanoid@SprintForwardUnarmed_RM.FBX",
            ["TurnInSprint180L"]    = "Humanoid@RunFastTurnLeftUnarmed_RM.fbx",
            ["TurnInSprint180R"]    = "Humanoid@RunFastTurnRightUnarmed_RM.fbx",
            ["TurnInWalk180L"]      = "Humanoid@WalkUTurnLeftUnarmed_RM.fbx",
            ["TurnInWalk180R"]      = "Humanoid@WalkUTurnRightUnarmed_RM.fbx",
            ["TurnInRun180L"]       = "Humanoid@RunUTurnLeftUnarmed_RM.fbx",
            ["TurnInRun180R"]       = "Humanoid@RunUTurnRightUnarmed_RM.fbx",
            ["TurnInPlaceL90"]      = "Humanoid@Turn90LeftUnarmed_RM.fbx",
            ["TurnInPlaceR90"]      = "Humanoid@Turn90RightUnarmed_RM.fbx",
            ["IdleL"]               = "Humanoid@Turn90LeftUnarmed_RM.fbx",
            ["IdleR"]               = "Humanoid@Turn90RightUnarmed_RM.fbx",
            ["IdleToRun180L"]       = "Humanoid@Turn180LeftUnarmed_RM.fbx",
            ["IdleToRun180R"]       = "Humanoid@Turn180RightUnarmed_RM.fbx",
            ["AirLoop"]             = "Humanoid@FallingUnarmed.FBX",
            ["LandLight"]           = "Humanoid@LandingLightUnarmed.FBX",
            ["LandMedium"]          = "Humanoid@LandingMediumUnarmed.fbx",
            ["LandHard"]            = "Humanoid@LandingHeavyUnarmed.FBX",

            // ── Climbing ──
            ["ClimbUpHalfMeter"]    = "Humanoid@ClimbUpHalfMeterObstacleLeftUnarmed.fbx",
            ["ClimbUp1meter"]       = "Humanoid@Pass1MeterObstacleLeftUnarmed_RM.FBX",
            ["ClimbUp2meter"]       = "Humanoid@WallClimbUp_RM.fbx",
            ["LandFromWall"]        = "Humanoid@ExitDropFromWall.fbx",
        };

        // Direction maps for 2D Mixers: independent FBX filenames
        static readonly string[] WalkDirections =
        {
            "Humanoid@WalkForwardUnarmed2_RM.fbx",       // (0, 1)
            "Humanoid@WalkForwardRightUnarmed_RM.fbx",   // (0.5, 0.5)
            "Humanoid@WalkRightUnarmed_RM.fbx",          // (1, 0)
            "Humanoid@WalkBackwardsRightUnarmed_RM.fbx", // (0.5, -0.5)
            "Humanoid@WalkBackwardsUnarmed_RM.fbx",      // (0, -1)
            "Humanoid@WalkBackwardsLeftUnarmed_RM.fbx",  // (-0.5, -0.5)
            "Humanoid@WalkLeftUnarmed_RM.fbx",           // (-1, 0)
            "Humanoid@WalkForwardLeftUnarmed_RM.fbx",    // (-0.5, 0.5)
            "Humanoid@IdleUnarmed.FBX",                  // (0, 0)
        };

        static readonly string[] RunDirections =
        {
            "Humanoid@RunForward2Unarmed_RM.fbx",        // (0, 1)
            "Humanoid@RunForwardRightUnarmed_RM.fbx",    // (0.5, 0.5)
            "Humanoid@RunRightUnarmed_RM.fbx",           // (1, 0)
            "Humanoid@RunBackwardsRightUnarmed_RM.fbx",  // (0.5, -0.5)
            "Humanoid@RunBackwardsUnarmed_RM.fbx",       // (0, -1)
            "Humanoid@RunBackwardsLeftUnarmed_RM.fbx",   // (-0.5, -0.5)
            "Humanoid@RunLeftUnarmed_RM.fbx",            // (-1, 0)
            "Humanoid@RunForwardLeftUnarmed_RM.fbx",     // (-0.5, 0.5)
            "Humanoid@IdleUnarmed.FBX",                  // (0, 0)
        };

        static readonly string[] LookDirections =
        {
            "Humanoid@IdleUnarmed.FBX",                  // (0, 1)
            "Humanoid@IdleUnarmed.FBX",                  // (0, -1)
            "Humanoid@Turn90RightUnarmed_RM.fbx",        // (1, 0)
            "Humanoid@Turn90LeftUnarmed_RM.fbx",         // (-1, 0)
        };

        static readonly Dictionary<string, string[]> MixerMap = new()
        {
            ["WalkMixer"] = WalkDirections,
            ["RunMIxer"]  = RunDirections,
            ["LookMixer"] = LookDirections,
        };

        [MenuItem("RedDust/Remap HumanTransitions to Protofactor")]
        public static void Remap()
        {
            if (!File.Exists(AssetPath))
            {
                Debug.LogError($"[Remapper] Not found: {AssetPath}");
                return;
            }

            var text = File.ReadAllText(AssetPath);
            var original = text;
            int remapped = 0;
            var errors = new List<string>();

            // ── Simple Clips ──
            foreach (var (entryName, fbxFile) in SimpleMap)
            {
                var path = FbxDir + fbxFile;
                var (guid, fileId) = FindAnyClip(path);
                if (guid == null)
                {
                    errors.Add($"No clip found in {fbxFile}");
                    continue;
                }

                var oldRef = FindClipRef(text, entryName);
                if (oldRef == null)
                {
                    errors.Add($"Entry '{entryName}' not found in asset");
                    continue;
                }

                var newRef = $"{{fileID: {fileId}, guid: {guid}, type: 3}}";
                text = ReplaceFirst(text, oldRef, newRef);
                remapped++;
                Debug.Log($"[Remapper] {entryName} → {fbxFile}");
            }

            // ── 2D Mixers ──
            foreach (var (mixerName, directions) in MixerMap)
            {
                var animRefs = FindAllAnimRefs(text, mixerName);
                if (animRefs == null || animRefs.Count != directions.Length)
                {
                    errors.Add($"Mixer '{mixerName}': expected {directions.Length} refs, found {animRefs?.Count ?? 0}");
                    continue;
                }

                for (int i = 0; i < directions.Length; i++)
                {
                    var fbxFile = directions[i];
                    var path = FbxDir + fbxFile;
                    var (guid, fileId) = FindAnyClip(path);
                    if (guid == null)
                    {
                        errors.Add($"Mixer '{mixerName}' dir {i}: no clip found in {fbxFile}");
                        continue;
                    }

                    var newRef = $"{{fileID: {fileId}, guid: {guid}, type: 3}}";
                    text = ReplaceFirst(text, animRefs[i], newRef);
                    remapped++;
                }
                Debug.Log($"[Remapper] {mixerName}: {directions.Length} directions remapped");
            }

            // ── Write ──
            if (text != original)
            {
                File.WriteAllText(AssetPath, text);
                AssetDatabase.Refresh();
                Debug.Log($"[Remapper] <b>Done: {remapped} clips remapped.</b>");
            }

            if (errors.Count > 0)
                Debug.LogWarning($"[Remapper] <b>{errors.Count} errors:</b>\n{string.Join("\n", errors)}");
        }

        // ═══════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════

        /// <summary>Find the single AnimationClip in an individual FBX (no sub-asset name needed).</summary>
        static (string guid, long fileId) FindAnyClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip
                    && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out var guid, out long fid))
                    return (guid, fid);
            }
            return (null, 0);
        }

        static string FindClipRef(string text, string entryName)
        {
            var nameIdx = text.IndexOf($"m_Name: {entryName}");
            if (nameIdx < 0) return null;
            var clipIdx = text.IndexOf("_Clip:", nameIdx, StringComparison.Ordinal);
            if (clipIdx < 0) return null;
            var open = text.IndexOf('{', clipIdx);
            var close = text.IndexOf('}', open);
            if (open < 0 || close < 0) return null;
            return text[open..(close + 1)];
        }

        static List<string> FindAllAnimRefs(string text, string mixerName)
        {
            var nameIdx = text.IndexOf($"m_Name: {mixerName}");
            if (nameIdx < 0) return null;
            var animsIdx = text.IndexOf("_Animations:", nameIdx, StringComparison.Ordinal);
            if (animsIdx < 0) return null;
            var endIdx = text.IndexOf("_Speeds:", animsIdx, StringComparison.Ordinal);
            if (endIdx < 0)
            {
                endIdx = text.IndexOf("---", animsIdx, StringComparison.Ordinal);
                if (endIdx < 0) endIdx = text.Length;
            }
            var block = text[animsIdx..endIdx];
            var refs = new List<string>();
            int pos = 0;
            while (pos < block.Length)
            {
                var open = block.IndexOf("{fileID:", pos, StringComparison.Ordinal);
                if (open < 0) break;
                var close = block.IndexOf('}', open);
                if (close < 0) break;
                refs.Add(block[open..(close + 1)]);
                pos = close + 1;
            }
            return refs;
        }

        static string ReplaceFirst(string text, string old, string @new)
        {
            var idx = text.IndexOf(old, StringComparison.Ordinal);
            if (idx < 0) return text;
            return text[..idx] + @new + text[(idx + old.Length)..];
        }
    }
}
#endif
