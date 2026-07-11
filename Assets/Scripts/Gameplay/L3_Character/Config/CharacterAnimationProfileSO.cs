using UnityEngine;

namespace RedDust.Gameplay.Character.Animation
{
    /// <summary>
    /// 角色动画总领资产。一份资产涵盖该角色类型的所有动画配置与引用集。
    /// 挂到 CharacterActor 上，策划只需拖一个东西。
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterAnimationProfileSO",
        menuName = "RedDust/Animation/Character Animation Profile")]
    public sealed class CharacterAnimationProfileSO : ScriptableObject
    {
        [Header("Config")]
        public LocomotionAnimationConfigSO locomotionConfig;
        public AnimationModeConfigSO[] modeProfiles;

        [Header("Sets")]
        public LocomotionAnimationSetSO defaultLocomotionSet;
        public GripAnimationTableSO gripTable;
    }
}
