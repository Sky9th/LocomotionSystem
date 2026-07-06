using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.GameScene
{
    public enum LoadMode { FullTransition, AdditiveWithFade, Streaming }
    public enum CurtainType { LoadingScreen, BriefFade, None }

    /// <summary>
    /// Valid Addressables labels for scene loading. Add entries here as needed.
    /// Drawn as a multi-select dropdown in the inspector via [Flags].
    /// </summary>
    [Flags]
    public enum SceneAssetLabel
    {
        None              = 0,
        Boot              = 1 << 0,   // System: PropertyDefSO, core configs
        SceneOpenWorld    = 1 << 1,
        SceneMainMenu     = 1 << 2,
        SceneBossAct2     = 1 << 3,
        SceneUnderground  = 1 << 4,
        SceneShelter      = 1 << 5,
        SceneStory        = 1 << 6,
        ChunkForest       = 1 << 7,
        ChunkWetland      = 1 << 8,
        ChunkCity         = 1 << 9,
        ChunkRuins        = 1 << 10,
        ChunkSnow         = 1 << 11,
        ChunkDesert       = 1 << 12,
        SharedCharacters  = 1 << 13,
        SharedVfx         = 1 << 14,
        SharedAudioBoss   = 1 << 15,
    }

    public static class SceneAssetLabelExtensions
    {
        private static readonly Dictionary<SceneAssetLabel, string> _map = new()
        {
            { SceneAssetLabel.Boot,             "boot" },
            { SceneAssetLabel.SceneOpenWorld,   "scene-openworld" },
            { SceneAssetLabel.SceneMainMenu,    "scene-mainmenu" },
            { SceneAssetLabel.SceneBossAct2,    "scene-boss-act2" },
            { SceneAssetLabel.SceneUnderground, "scene-underground" },
            { SceneAssetLabel.SceneShelter,     "scene-shelter" },
            { SceneAssetLabel.SceneStory,       "scene-story" },
            { SceneAssetLabel.ChunkForest,      "chunk-forest" },
            { SceneAssetLabel.ChunkWetland,     "chunk-wetland" },
            { SceneAssetLabel.ChunkCity,        "chunk-city" },
            { SceneAssetLabel.ChunkRuins,       "chunk-ruins" },
            { SceneAssetLabel.ChunkSnow,        "chunk-snow" },
            { SceneAssetLabel.ChunkDesert,      "chunk-desert" },
            { SceneAssetLabel.SharedCharacters, "shared-characters" },
            { SceneAssetLabel.SharedVfx,        "shared-vfx" },
            { SceneAssetLabel.SharedAudioBoss,  "shared-audio-boss" },
        };

        /// <summary>Convert a flags enum to the list of Addressables label strings.</summary>
        public static List<string> ToLabelStrings(this SceneAssetLabel labels)
        {
            var result = new List<string>();
            foreach (var kv in _map)
                if (labels.HasFlag(kv.Key))
                    result.Add(kv.Value);
            return result;
        }
    }

    /// <summary>Fixed scene identifiers matching Assets/Scenes/.</summary>
    public enum SceneId
    {
        MainMenu,
        NewGame,
        SampleScene,
        PathFinding,
    }

    public static class SceneIdExtensions
    {
        private static readonly Dictionary<SceneId, string> _paths = new()
        {
            { SceneId.MainMenu,     "Assets/Scenes/MainMenu.unity" },
            { SceneId.NewGame,      "Assets/Scenes/NewGame.unity" },
            { SceneId.SampleScene,  "Assets/Scenes/SampleScene.unity" },
            { SceneId.PathFinding,  "Assets/Scenes/PathFinding.unity" },
        };

        public static string GetPath(this SceneId id) => _paths[id];
        public static string GetName(this SceneId id) => id.ToString();
    }

    [CreateAssetMenu(fileName = "SceneLoadConfig", menuName = "RedDust/Scene/Load Config")]
    public class SceneLoadConfigSO : ScriptableObject
    {
        public SceneId Scene = SceneId.MainMenu;

        public string SceneName => Scene.GetName();
        public string ScenePath => Scene.GetPath();

        public SceneAssetLabel AssetLabels = SceneAssetLabel.None;

        public LoadMode Mode = LoadMode.FullTransition;

        [Min(0f)]
        public float MinDisplayTime = 0.5f;

        public CurtainType Curtain = CurtainType.LoadingScreen;
    }
}
