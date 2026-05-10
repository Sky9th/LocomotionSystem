using System;
using UnityEngine;

namespace Game.Stats
{
    [CreateAssetMenu(fileName = "ConsumeOverTime", menuName = "Game/Stats/Behavior/Consume Over Time")]
    public class ConsumeOverTimeSO : StatBehaviorSO
    {
        public string ConditionId;
        public float Rate;
        public Func<bool> Condition;

        private StatInstance instance;

        public override void Bind(StatInstance instance)
        {
            this.instance = instance;
        }

        public override void Tick(float dt)
        {
            if (Rate <= 0f || instance == null) return;
            if (Condition != null && !Condition()) return;

            instance.Modify(-Rate * dt);
        }
    }
}
