using RedDust.Core;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/SceneLoadComplete", fileName = "SceneLoadCompleteEvent")]
    public sealed class SceneLoadCompleteEvent : GameEvent<SSceneLoadComplete> { }
}
