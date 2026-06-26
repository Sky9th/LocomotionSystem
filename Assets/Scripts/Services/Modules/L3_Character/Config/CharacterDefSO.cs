using UnityEngine;
using RedDust.Container;
using RedDust.Properties;

namespace RedDust.Character
{
    /// <summary>
    /// 角色属性预设。继承 PropertyPresetSO。
    ///
    /// 身体槽位通过 OverridesJson 覆写 Entity 基树的 Common/Slots：
    ///   [{"Path":"Common/Slots","Value":"[{\"SlotId\":\"RightHand\",\"Capacity\":1},...]"}]
    ///
    /// 未覆写时使用 StandardBodySlots 兜底。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDef", menuName = "RedDust/Properties/Preset/Character")]
    public class CharacterDefSO : PropertyPresetSO, ISerializationCallbackReceiver
    {
        /// <summary>标准人形 9 身体槽位。</summary>
        public static readonly SlotDef[] StandardBodySlots =
        {
            new() { SlotId = "RightHand", Capacity = 1 },
            new() { SlotId = "LeftHand",  Capacity = 1 },
            new() { SlotId = "Head",      Capacity = 1 },
            new() { SlotId = "Body",      Capacity = 1 },
            new() { SlotId = "LeftLeg",   Capacity = 1 },
            new() { SlotId = "RightLeg",  Capacity = 1 },
            new() { SlotId = "LeftFoot",  Capacity = 1 },
            new() { SlotId = "RightFoot", Capacity = 1 },
            new() { SlotId = "Back",      Capacity = 1 },
        };

        // TODO: Entity Editor — 批量创建 CharacterDefSO 时接管此逻辑，支持海量物品+自定义槽位。

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(OverridesJson))
                OverridesJson = BuildOverridesJson();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() { }

        private static string BuildOverridesJson()
        {
            var sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < StandardBodySlots.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"SlotId\":\"");
                sb.Append(StandardBodySlots[i].SlotId);
                sb.Append("\",\"Capacity\":");
                sb.Append(StandardBodySlots[i].Capacity);
                sb.Append('}');
            }
            sb.Append(']');

            var escaped = sb.ToString().Replace("\"", "\\\"");
            return "{\"Overrides\":[{\"Path\":\"Common/Slots\",\"Value\":\"" + escaped + "\"}]}";
        }
    }
}
