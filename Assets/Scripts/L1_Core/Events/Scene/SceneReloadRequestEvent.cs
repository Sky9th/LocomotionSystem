using RedDust.Core;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/SceneReloadRequest", fileName = "SceneReloadRequestEvent")]
    public sealed class SceneReloadRequestEvent : GameEvent<SReloadSceneRequest> { }
}
