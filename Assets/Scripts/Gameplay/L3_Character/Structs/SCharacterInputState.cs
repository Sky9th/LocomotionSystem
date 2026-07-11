using UnityEngine;

namespace RedDust.Character
{
    /// <summary>
    /// 角色原始输入状态——外部系统（PlayerService/AIService）每帧写入的"旋钮面板"。
    ///
    /// 区分于 Command（离散一次性动作）和 locomotion 内部推导结果。
    /// 字段设计原则：只放原始输入信号，不放推导结果。
    ///   例如：放 WantsSprint（冲刺键按住），不放 DesiredGait（CharacterActor 内部推导）。
    /// </summary>
    internal struct SCharacterInputState
    {
        public Vector3 AimPoint;
        public bool HasAimPoint;
        public EPosture DesiredPosture;
        public bool WantsSprint;

        public static SCharacterInputState Default => new()
        {
            AimPoint = Vector3.zero,
            HasAimPoint = false,
            DesiredPosture = EPosture.Standing,
            WantsSprint = false,
        };
    }
}
