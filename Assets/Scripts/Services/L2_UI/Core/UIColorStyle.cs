using System;
using UnityEngine;

namespace RedDust.Services.UI
{

    [Serializable]
    public struct UIColorSet
    {
        public Color primary;
        public Color primaryHover;
        public Color primaryPressed;
        public Color onPrimary;

        public Color surface;
        public Color surfaceAlt;
        public Color onSurface;
        public Color onSurfaceMuted;

        public Color border;
    }

    public enum UIColorStyle
    {
        Normal = 0,
        Primary = 1,
        Danger = 2,
        Warning = 3,
        Success = 4,
    }
}
