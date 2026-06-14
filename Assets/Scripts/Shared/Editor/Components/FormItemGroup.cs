#if UNITY_EDITOR
namespace RedDust.Shared.EditorUI
{
    public enum FormGroupLayout { Vertical, Horizontal }

    /// <summary>
    /// FormItem 布局包装器。在渲染时包裹 FormItem，控制排列方式。
    /// 用法：FormItemGroup.Draw(Horizontal, () => { item1.Draw(); item2.Draw(); });
    /// </summary>
    public static class FormItemGroup
    {
        public static void Draw(FormGroupLayout layout, System.Action drawItems)
        {
            if (layout == FormGroupLayout.Horizontal)
                UnityEditor.EditorGUILayout.BeginHorizontal();
            drawItems();
            if (layout == FormGroupLayout.Horizontal)
                UnityEditor.EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
