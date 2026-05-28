using TMPro;
using UnityEngine;

namespace RedDust.UI
{

    public class LoadingOverlay : UIOverlay
    {
        [SerializeField] private TMP_Text phaseText;

        // TODO: [SerializeField] private Image progressFill;
        // public void SetProgress(float p) { ... }

        public void SetPhase(string phase)
        {
            if (phaseText != null) phaseText.text = phase;
        }
    }
}
