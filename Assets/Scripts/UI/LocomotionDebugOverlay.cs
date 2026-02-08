using UnityEngine;
using TMPro;

/// <summary>
/// Debug overlay for visualizing the current player locomotion snapshot.
/// Intended for development and tuning; not shown in shipping builds by default.
/// </summary>
public class LocomotionDebugOverlay : UIOverlayBase
{
    [Header("Text Outputs")]
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI velocityText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI groundText;

    private GameContext gameContext;

    private void Awake()
    {
        // Prefer the GameContext reference provided via GameManager.
        if (GameManager.Instance != null)
        {
            gameContext = GameManager.Instance.Context;
        }

        if (gameContext == null)
        {
            gameContext = GameContext.Instance;
        }

        EnsureTextOutputs();
    }

    private void Update()
    {
        if (!IsVisible)
        {
            return;
        }

        if (gameContext == null)
        {
            // Lazy-resolve GameContext in case service bootstrap has not finished
            // when Awake was executed.
            if (GameManager.Instance != null)
            {
                gameContext = GameManager.Instance.Context;
            }

            if (gameContext == null)
            {
                gameContext = GameContext.Instance;
            }
        }

        if (gameContext == null)
        {
            return;
        }

        if (!gameContext.TryGetSnapshot(out SPlayerLocomotion locomotion))
        {
            return;
        }

        UpdateTexts(locomotion);
    }

    private void EnsureTextOutputs()
    {
        // If any of the text fields are not wired in the Inspector, create them at runtime.
        CreateLabelIfNeeded(ref positionText, "PositionText", new Vector2(10f, -10f));
        CreateLabelIfNeeded(ref velocityText, "VelocityText", new Vector2(10f, -30f));
        CreateLabelIfNeeded(ref speedText, "SpeedText", new Vector2(10f, -50f));
        CreateLabelIfNeeded(ref stateText, "StateText", new Vector2(10f, -70f));
        CreateLabelIfNeeded(ref groundText, "GroundText", new Vector2(10f, -90f));
    }

    private void CreateLabelIfNeeded(ref TextMeshProUGUI label, string objectName, Vector2 anchoredPosition)
    {
        if (label != null)
        {
            return;
        }

        // Try to reuse an existing child first.
        var existing = transform.Find(objectName);
        if (existing != null)
        {
            label = existing.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                return;
            }
        }

        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;

        label = go.AddComponent<TextMeshProUGUI>();
        label.fontSize = 14f;
        label.color = Color.white;
        label.text = objectName;
    }

    private void UpdateTexts(SPlayerLocomotion locomotion)
    {
        if (positionText != null)
        {
            var pos = locomotion.Position;
            positionText.text = $"Pos: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}";
        }

        if (velocityText != null)
        {
            var vel = locomotion.Velocity;
            velocityText.text = $"Vel: {vel.x:F2}, {vel.y:F2}, {vel.z:F2}";
        }

        if (speedText != null)
        {
            speedText.text = $"Speed: {locomotion.Speed:F2}";
        }

        if (stateText != null)
        {
            stateText.text = $"State: {locomotion.State}";
        }

        if (groundText != null)
        {
            var contact = locomotion.GroundContact;
            groundText.text = contact.IsGrounded
                ? $"Grounded @ {contact.ContactPoint.x:F2}, {contact.ContactPoint.y:F2}, {contact.ContactPoint.z:F2}"
                : "Grounded: false";
        }
    }
}