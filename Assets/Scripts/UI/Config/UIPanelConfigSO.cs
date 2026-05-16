using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PanelConfig", menuName = "Game/UI/Panel Config")]
public class UIPanelConfigSO : ScriptableObject
{
    [Serializable]
    public struct PanelEntry
    {
        public string id;
        public EUIPanelType type;
        public GameObject prefab;
    }

    public PanelEntry[] panels;

    private Dictionary<string, PanelEntry> lookup;

    public bool TryGetEntry(string id, out PanelEntry entry)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out entry);
    }

    public void BuildLookup()
    {
        lookup = new Dictionary<string, PanelEntry>();
        if (panels == null) return;
        foreach (var entry in panels)
        {
            if (!string.IsNullOrEmpty(entry.id))
                lookup[entry.id] = entry;
        }
    }
}
