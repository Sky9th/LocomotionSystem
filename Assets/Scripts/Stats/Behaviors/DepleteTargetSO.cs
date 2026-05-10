using System;
using UnityEngine;

namespace Game.Stats
{
    [CreateAssetMenu(fileName = "DepleteTarget", menuName = "Game/Stats/Behavior/Deplete Target")]
    public class DepleteTargetSO : StatBehaviorSO
    {
        public string TargetId;
        public float Damage;
        public Action<float> OnDeplete;

        private StatInstance instance;
        private bool isDepleted;

        public override void Bind(StatInstance instance)
        {
            this.instance = instance;
            instance.OnZero += HandleDepleted;
            instance.OnChanged += HandleRestored;
        }

        private void HandleDepleted() => isDepleted = true;
        private void HandleRestored(float value) { if (value > 0f) isDepleted = false; }

        public override void Tick(float dt)
        {
            if (!isDepleted || Damage <= 0f) return;
            OnDeplete?.Invoke(Damage * dt);
        }
    }
}
