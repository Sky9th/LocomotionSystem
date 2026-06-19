using RedDust.Properties;

namespace RedDust.Character.Kinematic
{
    /// <summary>
    /// 角色物理属性缓存 — Init 时从 PropertyAgent 读取一次，存为强类型字段供 hot path 零开销访问。
    /// 替代原有的 LocomotionProfileSO + KinematicProfileSO 两套 SO 配置。
    /// </summary>
    public struct CharacterPhysique
    {
        // ── Movement ──
        public float Acceleration;
        public float MaxSlopeAngle;

        // ── Body ──
        public float Height;
        public float ObstacleProbeVertical;
        public float ObstacleProbeDistance;
        public float ObstacleMinClimb;
        public float ObstacleMaxClimb;

        // ── Head ──
        public float MaxHeadYaw;
        public float MaxHeadPitch;

        /// <summary>
        /// 从 PropertyAgent 读取角色物理属性。
        /// TODO: Properties 接入更多属性后（负重、移速修正等）在此追加字段。
        /// </summary>
        public static CharacterPhysique FromAgent(IPropertyReader agent) => new()
        {
            Acceleration          = agent.GetFloat("Movement/Acceleration"),
            MaxSlopeAngle         = agent.GetFloat("Movement/MaxSlopeAngle"),
            Height                = agent.GetFloat("Body/Height"),
            ObstacleProbeVertical = agent.GetFloat("Body/ObstacleProbeVertical"),
            ObstacleProbeDistance = agent.GetFloat("Body/ObstacleProbeDistance"),
            ObstacleMinClimb      = agent.GetFloat("Body/ObstacleMinClimb"),
            ObstacleMaxClimb      = agent.GetFloat("Body/ObstacleMaxClimb"),
            MaxHeadYaw            = agent.GetFloat("Body/MaxHeadYaw"),
            MaxHeadPitch          = agent.GetFloat("Body/MaxHeadPitch"),
        };
    }
}
