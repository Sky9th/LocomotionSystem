using UnityEngine;

namespace RedDust.Items
{
    /// <summary>
    /// 容器物品预设占位（背包等）。零 C# 字段。
    /// NestedContainer 由 EntityService 自动从 Slots/ 节点创建。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Container", fileName = "NewContainer")]
    public class ContainerSO : ItemDefSO { }
}
