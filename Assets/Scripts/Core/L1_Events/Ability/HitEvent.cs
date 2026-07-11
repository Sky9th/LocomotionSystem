using RedDust.Gameplay.Ability;
using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Ability/Hit", fileName = "HitEvent")]
    public sealed class HitEvent : GameEvent<SDamageInfo> { }
}
