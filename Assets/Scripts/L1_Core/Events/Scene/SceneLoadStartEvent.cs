using RedDust.Core;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/SceneLoadStart", fileName = "SceneLoadStartEvent")]
    public sealed class SceneLoadStartEvent : GameEvent<SSceneLoadStart> { }
}
