using RedDust.Core;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/SceneLoadRequest", fileName = "SceneLoadRequestEvent")]
    public sealed class SceneLoadRequestEvent : GameEvent<SLoadSceneRequest> { }
}
