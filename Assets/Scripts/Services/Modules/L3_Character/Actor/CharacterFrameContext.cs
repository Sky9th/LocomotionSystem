using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;
using RedDust.Character.Locomotion;

namespace RedDust.Character
{
    public struct CharacterFrameContext
    {
        public SCharacterIntent Intent;
        public SCharacterKinematic Kinematic;
        public SCharacterMotor Motor;
        public SCharacterDiscrete Discrete;

        /// <summary>
        /// 角色物理速度配置。Locomotion 模块据此获取 gaitSpeed。
        /// </summary>
        public LocomotionProfileSO LocomotionProfile;

        /// <summary>
        /// 动画原生速度配置。Locomotion 模块据此获取 animNativeSpeed 并计算 MotionSpeedScale。
        /// </summary>
        public LocomotionAnimationConfigSO LocomotionAnimationProfile;

        /// <summary>
        /// 角色物理/运动学配置（地面、障碍物、头部、转向）。
        /// </summary>
        public KinematicProfileSO KinematicProfile;
    }
}
