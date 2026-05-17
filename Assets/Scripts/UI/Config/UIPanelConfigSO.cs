using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PanelConfig", menuName = "Game/UI/Panel Config")]
public class UIPanelConfigSO : ScriptableObject
{
    [Serializable]
    public struct PanelEntry
    {
        public UIPanelId id;
        public EUIPanelType type;
        public GameObject prefab;
    }

    public PanelEntry[] panels;

    private Dictionary<UIPanelId, PanelEntry> lookup;

    public bool TryGetEntry(UIPanelId id, out PanelEntry entry)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out entry);
    }

    public void BuildLookup()
    {
        lookup = new Dictionary<UIPanelId, PanelEntry>();
        if (panels == null) return;
        foreach (var entry in panels)
            lookup[entry.id] = entry;
    }
}
