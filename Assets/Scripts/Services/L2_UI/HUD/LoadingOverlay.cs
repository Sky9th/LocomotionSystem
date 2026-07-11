using RedDust.Core.GameContext;
using RedDust.Core.Events;
using RedDust.Services.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedDust.Services.UI
{

    public class LoadingOverlay : UIOverlay
    {
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressPercent;

        public void SetPhase(string phase)
        {
            if (phaseText != null) phaseText.text = phase;
        }

        /// <summary>Set progress bar fill. p is clamped to [0, 1].</summary>
        public void SetProgress(float p)
        {
            float clamped = Mathf.Clamp01(p);
            if (progressFill != null) progressFill.fillAmount = clamped;
            if (progressPercent != null) progressPercent.text = $"{(int)(clamped * 100f)}%";
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (GameContext.Instance.TryResolveService(out EventHub hub))
                hub.Get<SceneProgressEvent>().Register(HandleProgress);
        }

        protected override void OnDestroy()
        {
            if (GameContext.Instance != null && GameContext.Instance.TryResolveService(out EventHub hub))
                hub?.Get<SceneProgressEvent>()?.Unregister(HandleProgress);

            base.OnDestroy();
        }

        private void HandleProgress(SLoadingProgress evt)
        {
            SetPhase(evt.PhaseName);
            SetProgress(evt.Progress);
        }
    }
}
