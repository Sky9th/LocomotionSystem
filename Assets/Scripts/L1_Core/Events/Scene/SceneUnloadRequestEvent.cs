using RedDust.Core;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/SceneUnloadRequest", fileName = "SceneUnloadRequestEvent")]
    public sealed class SceneUnloadRequestEvent : GameEvent<SUnloadSceneRequest> { }
}
