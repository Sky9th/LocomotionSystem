using UnityEngine;

namespace RedDust.Character
{
    [CreateAssetMenu(fileName = "CharacterProfileSO", menuName = "RedDust/Character/Character Profile")]
    public sealed class CharacterProfileSO : ScriptableObject
    {
        /// <summary>角色移动能力配置（posture×gait 速度矩阵）。</summary>
        public Locomotion.LocomotionProfileSO locomotion;

        /// <summary>角色物理/运动学配置（地面检测、障碍物、头部转动、转向阈值）。</summary>
        public KinematicProfileSO kinematic;
    }
}
