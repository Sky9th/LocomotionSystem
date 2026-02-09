using UnityEngine;

/// <summary>
/// ScriptableObject container for UIScreenEntry definitions.
/// Allows UI screen mappings to be configured as an asset
/// instead of only on the UIManager component.
/// </summary>
[CreateAssetMenu(menuName = "UI/Screen Config", fileName = "UIScreenConfig")]
public class UIScreenConfig : ScriptableObject
{
    [SerializeField] private UIScreenEntry[] screens;

    public UIScreenEntry[] Screens => screens;
}
