using System.Collections.Generic;
using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Audio;
using RedDust.Character.Kinematic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Boot task: registers character config SOs (animation profile, ground system, audio).
    /// </summary>
    public class ConfigBootTask : IBootTask
    {
        public string Description => "Registering character configs...";

        public void Resolve(BootAssetCatalog catalog)
        {
            var animProfiles = catalog.Get<CharacterAnimationProfileSO>();
            var groundConfigs = catalog.Get<GroundSystemConfigSO>();
            var audioConfigs = catalog.Get<CharacterAudioConfigSO>();

            var reg = GameService.Instance.AssetRegistry;
            reg.InitAnimProfiles(animProfiles);
            reg.InitGroundConfigs(groundConfigs);
            reg.InitAudioConfigs(audioConfigs);

            Debug.Log($"[ConfigBootTask] === Configs: AnimationProfile={animProfiles.Count}  GroundSystem={groundConfigs.Count}  Audio={audioConfigs.Count} ===");
        }
    }
}
