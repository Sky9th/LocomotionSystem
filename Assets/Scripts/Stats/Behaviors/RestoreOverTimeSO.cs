using System;
using UnityEngine;

namespace Game.Stats
{
    [CreateAssetMenu(fileName = "RestoreOverTime", menuName = "Game/Stats/Behavior/Restore Over Time")]
    public class RestoreOverTimeSO : StatBehaviorSO
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

            instance.Modify(Rate * dt);
        }
    }
}
