using UnityEngine;
using UnityEngine.UI;

namespace RedDust.UI
{

    [ExecuteAlways]
    public class UIPanel : MonoBehaviour
    {
        [SerializeField] private UIColorStyle style = UIColorStyle.Normal;
        [SerializeField] private UIThemeSO theme;
        [SerializeField] private Image background;

        // TODO: drag support
        // [SerializeField] private bool isDraggable;
        // [SerializeField] private RectTransform dragHandle;
        // private UIPanelDragHandler dragHandler;

        // TODO: resize support
        // [SerializeField] private bool isResizable;

        // TODO: close support
        // [SerializeField] private Button closeButton;
        // public event Action OnClose;

        private void Awake()
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (theme == null || background == null) return;
            var cs = theme.GetColorSet(style);
            background.color = cs.surface;
        }
    }
}
