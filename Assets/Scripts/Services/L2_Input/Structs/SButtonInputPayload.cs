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

        /// <summary>同一 Action 内多 Binding 时的绑定索引。-1 = 单 Binding 或无意义。</summary>
        public readonly int BindingIndex;

        public SButtonInputPayload(bool isPressed, bool isRequested, bool isReleased, int bindingIndex = -1)
        {
            IsPressed = isPressed;
            IsRequested = isRequested;
            IsReleased = isReleased;
            BindingIndex = bindingIndex;
        }
    }
}
