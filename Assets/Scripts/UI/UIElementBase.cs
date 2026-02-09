using UnityEngine;

/// <summary>
/// Shared base class for all UI elements managed by UIManager.
/// Encapsulates a simple logical visibility flag and a unified
/// SetVisible implementation which controls the underlying GameObject.
///
/// UIScreenBase and UIOverlayBase both derive from this type.
/// </summary>
public abstract class UIElementBase : MonoBehaviour
{
    [Tooltip("Logical visibility flag used by higher-level UI flows.\n" +
             "UIManager will keep this in sync when showing or hiding the element.")]
    [SerializeField] private bool visible;

    /// <summary>
    /// Indicates whether this element is logically visible.
    /// </summary>
    public bool Visible
    {
        get => visible;
        protected set => visible = value;
    }

	[Header("Refresh")]
	[Tooltip("UI refresh interval in seconds. 0 means every frame.")]
	[SerializeField] protected float refreshInterval = 0.2f;

	private float refreshTimer;

    /// <summary>
    /// Shared GameContext reference for all UI elements.
    /// UIManager will assign this via Registered(GameContext) during bootstrap.
    /// </summary>
    protected GameContext GameContext { get; private set; }

    /// <summary>
    /// Called by UIManager once the global GameContext is available so that
    /// UI elements can cache it for later use (for example debug overlays
    /// reading snapshot data).
    /// </summary>
    public virtual void Registered(GameContext context)
    {
        GameContext = context;
    }

    /// <summary>
    /// Override this in derived classes to implement periodic UI updates.
    /// </summary>
    protected virtual void Refresh()
    {
    }

    /// <summary>
    /// Controls whether this element participates in the shared refresh loop.
    /// Overlays enable this by default; screens can opt in when needed.
    /// </summary>
    protected virtual bool ShouldAutoRefresh => false;

    /// <summary>
    /// Resets the internal refresh timer. Useful when an element is shown/hidden.
    /// </summary>
    protected void ResetRefreshTimer()
    {
        refreshTimer = 0f;
    }

    private void Update()
    {
        if (!ShouldAutoRefresh || !Visible)
        {
            return;
        }

        if (refreshInterval <= 0f)
        {
            Refresh();
            return;
        }

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            Refresh();
        }
    }

    /// <summary>
    /// Centralized visibility hook. Default implementation toggles the
    /// GameObject active state and updates the Visible flag.
    /// Derived classes can override this method (for example to drive
    /// a CanvasGroup) but should usually call base.SetVisible to keep
    /// the logical flag in sync.
    /// </summary>
    public virtual void SetVisible(bool isVisible)
    {
        if (gameObject.activeSelf != isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        visible = isVisible;
    }
}
