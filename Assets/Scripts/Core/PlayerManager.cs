using UnityEngine;

/// <summary>
/// Maintains high-level player wiring, including syncing the Player root with the animated Model child.
/// </summary>
[DisallowMultipleComponent]
public class PlayerManager : MonoBehaviour
{
    [Header("Rig References")]
    [SerializeField] private Transform modelRoot;

    [Header("Alignment")]
    [SerializeField] private bool keepPlayerAlignedWithModel = true;

    private void Awake()
    {
        if (modelRoot == null)
        {
            var model = transform.Find("Model");
            if (model != null)
            {
                modelRoot = model;
            }
        }
    }

    private void LateUpdate()
    {
        if (!keepPlayerAlignedWithModel || modelRoot == null)
        {
            return;
        }
    }
}
