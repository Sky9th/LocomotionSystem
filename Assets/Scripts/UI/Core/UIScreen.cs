using DG.Tweening;
using UnityEngine;

public abstract class UIScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    protected UIManager uiManager;

    public void Initialize(UIManager manager)
    {
        uiManager = manager;
        OnInitialize();
    }

    public virtual Sequence PlayEnterSequence(object args = null)
    {
        if (canvasGroup == null) return null;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        return DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutCubic))
            .OnComplete(OnEnterFinished);
    }

    public virtual Sequence PlayExitSequence()
    {
        if (canvasGroup == null) return null;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        return DOTween.Sequence()
            .Append(canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InCubic))
            .OnComplete(OnExitFinished);
    }

    public virtual void OnPause()
    {
        if (canvasGroup != null) canvasGroup.interactable = false;
    }

    public virtual void OnResume()
    {
        if (canvasGroup != null) canvasGroup.interactable = true;
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnEnterFinished() { }
    protected virtual void OnExitFinished() { }

    protected virtual void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}
