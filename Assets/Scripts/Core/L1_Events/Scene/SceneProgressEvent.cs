using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Scene/Scene Progress", fileName = "SceneProgressEvent")]
    public sealed class SceneProgressEvent : GameEvent<SLoadingProgress> { }
}
