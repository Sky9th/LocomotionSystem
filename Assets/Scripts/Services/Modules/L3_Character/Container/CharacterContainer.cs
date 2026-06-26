using RedDust.Container;
using RedDust.Core;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Character
{
    /// <summary>
    /// 角色身体容器适配层。
    ///
    /// 从 PropertyTree Common/Slots 读取身体槽位定义。
    /// 槽位由 CharacterDefSO 的 OverridesJson 覆写 Entity 基树默认的空数组。
    /// 未配置 → BodySlots 为空（非人形角色可能没有身体槽位）。
    ///
    /// ItemInstance 到位后加 CreateBodyContainer() → Container&lt;ItemInstance&gt;。
    /// </summary>
    internal class CharacterContainer : ModuleChild
    {
        private readonly CharacterBuildContext ctx;

        /// <summary>身体槽位定义。OnAssemble 后可用。</summary>
        public SlotDef[] BodySlots { get; private set; } = System.Array.Empty<SlotDef>();

        public CharacterContainer(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
        }

        public override void OnWire()
        {
            BodySlots = ctx.Agent.GetStructArray<SlotDef>("Common/Slots") ?? System.Array.Empty<SlotDef>();

            if (BodySlots.Length == 0)
                Debug.LogWarning($"[CharacterContainer] {ctx.Agent.name}: Common/Slots is empty — no body slots configured.");
            else
            {
                var names = new System.Text.StringBuilder();
                for (int i = 0; i < BodySlots.Length; i++)
                {
                    if (i > 0) names.Append(", ");
                    names.Append(BodySlots[i].SlotId);
                }
                Debug.Log($"[CharacterContainer] {ctx.Agent.name}: {BodySlots.Length} body slots — {names}");
            }
        }
    }
}
