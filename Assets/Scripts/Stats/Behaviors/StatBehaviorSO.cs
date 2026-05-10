using UnityEngine;

namespace Game.Stats
{
    public abstract class StatBehaviorSO : ScriptableObject
    {
        public abstract void Bind(StatInstance instance);
        public virtual void Tick(float dt) { }
    }
}
