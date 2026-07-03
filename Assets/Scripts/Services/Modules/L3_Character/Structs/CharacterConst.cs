namespace RedDust.Character
{
    /// <summary>
    /// Character 模块全局常量 — PropertyTree 路径 / RdTag FullTag / 槽位 ID。
    ///
    /// 目的：消除散落在各子模块中的硬编码字符串，
    /// PropertyTree 结构调整时只需改这一个文件。
    /// </summary>
    public static class CharacterConst
    {
        /// <summary>PropertyTree 路径常量 — 对齐 PropertyTreeSO 节点结构</summary>
        public static class PropertyPath
        {
            public const string CommonTags = "Common/Tags";
            public const string Slots      = "Slots";

            public static class Vitals
            {
                public const string HP     = "Vitals/HP";
                public const string Hunger = "Vitals/Hunger";
            }

            public static class Attributes
            {
                public const string Endurance = "Attributes/Endurance";
            }

            public static class Movement
            {
                public const string Acceleration  = "Movement/Acceleration";
                public const string MaxSlopeAngle = "Movement/MaxSlopeAngle";
            }

            public static class Body
            {
                public const string Height                = "Body/Height";
                public const string ObstacleProbeVertical = "Body/ObstacleProbeVertical";
                public const string ObstacleProbeDistance = "Body/ObstacleProbeDistance";
                public const string ObstacleMinClimb      = "Body/ObstacleMinClimb";
                public const string ObstacleMaxClimb      = "Body/ObstacleMaxClimb";
                public const string MaxHeadYaw            = "Body/MaxHeadYaw";
                public const string MaxHeadPitch          = "Body/MaxHeadPitch";
            }
        }

        /// <summary>
        /// 装备槽位 ID — 对齐 PropertyTree Slots/ 节点名。
        /// CharacterContainer.OnWire() 以 NodeId 作为 SlotId。
        /// </summary>
        public static class Slot
        {
            public const string RightHand     = "RightHand";
            public const string LeftHand      = "LeftHand";
            public const string Head          = "Head";
            public const string Chest         = "Chest";
            public const string Back          = "Back";
            public const string RightLeg      = "RightLeg";
            public const string LeftLeg       = "LeftLeg";
            public const string RightFoot     = "RightFoot";
            public const string LeftFoot      = "LeftFoot";

            /// <summary>Container 内部通用槽位键 — bpContainer.FindItem/Place/Remove</summary>
            public const string ContainerSlot = "ContainerSlot";
        }

        /// <summary>
        /// 握持标签 — 对齐 RdTagDefSO 资产的 FullTag 值。
        ///
        /// Tag 层级（证自 Assets/Data/Tags/）:
        ///   Grip (root)
        ///     ├─ Grip.Melee
        ///     │   ├─ Grip.Melee.OneHanded   ← Tag_OneHanded
        ///     │   ├─ Grip.Melee.TwoHanded   ← Tag_TwoHanded
        ///     │   └─ Grip.Melee.Unarmed    ← Tag_Unarmed
        ///     └─ Grip.Ranged
        ///         ├─ Grip.Ranged.Pistol2H  ← Tag_Pistol2H
        ///         ├─ Grip.Ranged.Rifle     ← Tag_Rifle
        ///         └─ ...
        /// </summary>
        public static class GripTag
        {
            public const string OneHanded = "Grip.Melee.OneHanded";
            public const string Pistol2H  = "Grip.Ranged.Pistol2H";
        }
    }
}
