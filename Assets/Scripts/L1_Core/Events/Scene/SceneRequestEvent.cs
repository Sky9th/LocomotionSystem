using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/Scene Request", fileName = "SceneRequestEvent")]
    public sealed class SceneRequestEvent : GameEvent<SSceneRequest> { }
}
