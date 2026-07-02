using RedDust.Core;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Player/CameraSnapshot", fileName = "CameraSnapshotEvent")]
    public sealed class CameraSnapshotEvent : GameEvent<SCameraSnapshot> { }
}
