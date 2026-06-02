using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.UI
{

    [CreateAssetMenu(fileName = "UIPanelConfigSO", menuName = "RedDust/UI/Panel Config")]
    public class UIPanelConfigSO : ScriptableObject
    {
        [Serializable]
        public struct ScreenEntry { public UIScreenId id; public GameObject prefab; }
        [Serializable]
        public struct OverlayEntry { public UIOverlayId id; public GameObject prefab; }
        [Serializable]
        public struct ModalEntry { public UIModalId id; public GameObject prefab; }

        public ScreenEntry[] screens;
        public OverlayEntry[] overlays;
        public ModalEntry[] modals;

        private Dictionary<object, GameObject> lookup;

        public bool TryGetScreen(UIScreenId id, out GameObject prefab)
        {
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out prefab);
        }

        public bool TryGetOverlay(UIOverlayId id, out GameObject prefab)
        {
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out prefab);
        }

        public bool TryGetModal(UIModalId id, out GameObject prefab)
        {
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out prefab);
        }

        public void BuildLookup()
        {
            lookup = new Dictionary<object, GameObject>();
            if (screens != null)
                foreach (var e in screens) lookup[e.id] = e.prefab;
            if (overlays != null)
                foreach (var e in overlays) lookup[e.id] = e.prefab;
            if (modals != null)
                foreach (var e in modals) lookup[e.id] = e.prefab;
        }
    }
}
