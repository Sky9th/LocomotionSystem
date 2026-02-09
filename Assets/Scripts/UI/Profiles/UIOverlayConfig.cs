using UnityEngine;

/// <summary>
/// ScriptableObject container for UIOverlayEntry definitions.
/// Allows overlay mappings to be configured as an asset
/// instead of only on the UIManager component.
/// </summary>
[CreateAssetMenu(menuName = "UI/Overlay Config", fileName = "UIOverlayConfig")]
public class UIOverlayConfig : ScriptableObject
{
    [SerializeField] private UIOverlayEntry[] overlays;

    public UIOverlayEntry[] Overlays => overlays;
}
