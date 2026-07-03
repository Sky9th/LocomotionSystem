using RedDust.Properties;

namespace RedDust.Character.Kinematic
{
    /// <summary>
    /// 临时方案：角色物理属性缓存。从 PropertyTable 字符串路径手动提取 9 个值。
    ///
    /// 问题：手动维护字段映射，新增属性需改代码；角色域逻辑散落在 Actor/Physique 中。
    /// 应由专门的子模块接管——统一持有所有角色属性路径映射、
    /// 注入名字/生命/饥饿变化等角色域逻辑。届时删除此 struct，消费者读 Attributes。
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
        /// 从 PropertyTable 读取角色物理属性。临时方案——字段需手动维护与属性路径的映射。
        /// </summary>
        public static CharacterPhysique From(PropertyTable props) => new()
        {
            Acceleration          = props.GetFloat(CharacterConst.PropertyPath.Movement.Acceleration),
            MaxSlopeAngle         = props.GetFloat(CharacterConst.PropertyPath.Movement.MaxSlopeAngle),
            Height                = props.GetFloat(CharacterConst.PropertyPath.Body.Height),
            ObstacleProbeVertical = props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleProbeVertical),
            ObstacleProbeDistance = props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleProbeDistance),
            ObstacleMinClimb      = props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleMinClimb),
            ObstacleMaxClimb      = props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleMaxClimb),
            MaxHeadYaw            = props.GetFloat(CharacterConst.PropertyPath.Body.MaxHeadYaw),
            MaxHeadPitch          = props.GetFloat(CharacterConst.PropertyPath.Body.MaxHeadPitch),
        };
    }
}
