using UnityEngine;

namespace RedDust.Stats
{
    [CreateAssetMenu(fileName = "StatsNodeSO", menuName = "RedDust/Stats/Stats Node")]
    public class StatsNodeSO : ScriptableObject
    {
        public string Id;
        public bool IsEnabled = true;
        public bool IsFolder;
        public StatDefinitionSO Def;
        public StatsNodeSO[] Children;
        [Min(-1f)] public float OverrideValue = -1f;

        [System.NonSerialized] public string Path;
    }
}
