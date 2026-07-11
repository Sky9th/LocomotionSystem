using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/Scene Transition", fileName = "SceneTransitionEvent")]
    public sealed class SceneTransitionEvent : GameEvent<SSceneTransition> { }
}
