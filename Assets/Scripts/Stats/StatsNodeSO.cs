using UnityEngine;

namespace Game.Stats
{
    [CreateAssetMenu(fileName = "StatsNode", menuName = "Game/Stats/Stats Node")]
    public class StatsNodeSO : ScriptableObject
    {
        public string Id;
        public bool IsEnabled = true;
        public bool IsFolder;
        public StatDefSO Def;
        public StatsNodeSO[] Children;
        public StatBehaviorSO[] CustomBehaviors;
        [Min(-1f)] public float OverrideValue = -1f;
    }
}
