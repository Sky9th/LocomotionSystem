using System;

namespace RedDust.Gameplay.Container
{
    /// <summary>
    /// 容器槽位轻量定位符——用于 L2_ItemService 索引和跨容器 Transfer。
    /// 存字符串 ID 而非 C# 引用，兼容网络传输。
    /// </summary>
    [Serializable]
    public struct ContainerSlotRef
    {
        /// <summary>
        /// 容器所有者的唯一 ID。
        /// 格式：char/{netId} | world/{uniqueId} | item/{instanceId}
        /// </summary>
        public string OwnerId;

        /// <summary>
        /// 容器内槽位标识。
        /// 平级容器中等于 SlotId，嵌套容器中带路径前缀（如 "Backpack/Main"）。
        /// </summary>
        public string SlotKey;
    }
}
