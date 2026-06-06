using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 命中事件通道。AbilityReactor 结算完成后发布，Audio/VFX/UI 等系统订阅。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Event/Hit", fileName = "HitEventSO")]
    public sealed class HitEventSO : GameEvent<SDamageInfo> { }
}
