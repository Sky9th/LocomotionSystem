using UnityEngine;

namespace RedDust.Stats
{
    [CreateAssetMenu(fileName = "StatDef", menuName = "RedDust/Stats/Stat Definition")]
    public class StatDefSO : ScriptableObject
    {
        public string Id;
        public float Min;
        public float Max = 100f;
        public float Default = 100f;

        [Header("Capabilities")]
        public bool isConsumable;
        public float consumeRate;
        public float consumeInterval;

        public bool isRestorable;
        public float restoreRate;
        public float restoreInterval;

        public bool isCumulative;

        public bool IsConsumable => isConsumable && consumeRate > 0;
        public bool IsRestorable => isRestorable && restoreRate > 0;
    }
}
