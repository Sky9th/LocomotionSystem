using UnityEngine;
using Animancer;

namespace Locomotion.Animation
{
    /// <summary>
    /// Configuration asset describing locomotion-related Animancer transitions.
    /// One profile can be reused across multiple characters sharing the same rig.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocomotionAnimationProfile",
        menuName = "Locomotion/Animancer/Locomotion Animation Profile")]
    public class LocomotionAnimationProfile : ScriptableObject
    {
        [Header("Core States")] 
        [Tooltip("Idle transition played when character speed is almost zero.")]
        public ClipTransition idle;

        [Tooltip("Forward locomotion transition when character is moving mostly forward.")]
        public ClipTransition moveForward;

        [Tooltip("Strafe-left locomotion transition when character is primarily moving left.")]
        public ClipTransition moveLeft;

        [Tooltip("Strafe-right locomotion transition when character is primarily moving right.")]
        public ClipTransition moveRight;

        [Tooltip("Backward locomotion transition when character is moving mostly backwards.")]
        public ClipTransition moveBackward;

        [Header("Turn In Place")] 
        [Tooltip("Turn-in-place transition for ~90 degree left turns (e.g. A_MOD_BL_Turn_Standing_90L_RM_Masc).")]
        public ClipTransition turnLeft90;

        [Tooltip("Turn-in-place transition for ~90 degree right turns (e.g. A_MOD_BL_Turn_Standing_90R_RM_Masc).")]
        public ClipTransition turnRight90;

        [Tooltip("Turn-in-place transition for ~180 degree left turns (e.g. A_MOD_BL_Turn_Standing_180L_RM_Masc).")]
        public ClipTransition turnLeft180;

        [Tooltip("Turn-in-place transition for ~180 degree right turns (e.g. A_MOD_BL_Turn_Standing_180R_RM_Masc).")]
        public ClipTransition turnRight180;

        [Header("Airborne")] 
        [Tooltip("Looped airborne/fall transition while character is not grounded.")]
        public ClipTransition airborneLoop;

        [Tooltip("Landing transition when character returns to ground.")]
        public ClipTransition land;

        [Header("Blend / Thresholds")] 
        [Tooltip("Speed (m/s) below which character is considered idle.")]
        [Min(0f)] public float idleSpeedThreshold = 0.1f;

        [Tooltip("Speed (m/s) above which character is treated as running instead of walking (for future extensions).")]
        [Min(0f)] public float runSpeedThreshold = 3.5f;

        [Tooltip("Angle in degrees above which large turn clips should be preferred.")]
        [Range(0f, 180f)] public float largeTurnAngleThreshold = 120f;

        [Tooltip("Angle in degrees below which we do not trigger turn-in-place animations.")]
        [Range(0f, 180f)] public float minTurnAngleToTrigger = 45f;

        [Tooltip("Additive look-up clip (e.g. A_MOD_BL_HeadLook_U_Additive_Neut).")]
        public ClipTransition lookUp;

        [Tooltip("Additive look-down clip (e.g. A_MOD_BL_HeadLook_D_Additive_Neut).")]
        public ClipTransition lookDown;

        [Tooltip("Additive look-right clip (e.g. A_MOD_BL_HeadLook_R_Additive_Neut).")]
        public ClipTransition lookRight;

        [Tooltip("Additive look-left clip (e.g. A_MOD_BL_HeadLook_L_Additive_Neut).")]
        public ClipTransition lookLeft;
    }
}
