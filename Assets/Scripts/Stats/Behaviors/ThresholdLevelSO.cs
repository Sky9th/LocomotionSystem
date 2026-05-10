using System;
using UnityEngine;

namespace Game.Stats
{
    [CreateAssetMenu(fileName = "ThresholdLevel", menuName = "Game/Stats/Behavior/Threshold Level")]
    public class ThresholdLevelSO : StatBehaviorSO
    {
        public float[] Thresholds;
        public int Level { get; private set; }
        public event Action<int> OnLevelUp;

        public override void Bind(StatInstance instance)
        {
            instance.OnChanged += OnValueChanged;
        }

        private void OnValueChanged(float current)
        {
            if (Thresholds == null || Thresholds.Length == 0) return;

            int newLevel = 0;
            for (int i = Thresholds.Length - 1; i >= 0; i--)
            {
                if (current >= Thresholds[i]) { newLevel = i + 1; break; }
            }

            if (newLevel > Level)
            {
                Level = newLevel;
                OnLevelUp?.Invoke(Level);
            }
        }
    }
}
