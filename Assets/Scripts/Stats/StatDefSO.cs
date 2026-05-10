using UnityEngine;

namespace Game.Stats
{
    [CreateAssetMenu(fileName = "StatDef", menuName = "Game/Stats/Stat Definition")]
    public class StatDefSO : ScriptableObject
    {
        public string Id;
        public StatType Type;
        public float Min;
        public float Max = 100f;
        public float Default = 100f;
        public StatBehaviorSO[] Behaviors;
    }
}
