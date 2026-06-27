namespace RedDust.GameInput
{
    /// <summary>
    /// 按钮型输入载荷。
    /// </summary>
    public readonly struct SButtonInputPayload
    {
        public readonly bool IsPressed;
        public readonly bool IsRequested;
        public readonly bool IsReleased;

        public SButtonInputPayload(bool isPressed, bool isRequested, bool isReleased)
        {
            IsPressed = isPressed;
            IsRequested = isRequested;
            IsReleased = isReleased;
        }
    }
}
