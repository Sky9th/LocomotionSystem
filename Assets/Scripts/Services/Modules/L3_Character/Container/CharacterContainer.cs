using System.Collections.Generic;
using RedDust.Container;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Character
{
    /// <summary>
    /// 角色身体容器适配层。
    ///
    /// 从 PropertyTree Slots/ 文件夹遍历子属性（每个子属性为独立 SlotDef Struct），
    /// 提取 SlotId（路径末段即 NodeId），创建运行时 Container。
    ///
    /// Entity 基树有空 Slots/ 文件夹。Human 树预设 9 个人形槽位，
    /// 各自引用专属 PropertyDefSO（RightHand/LeftHand/Head/Chest/...），自带 AcceptTags 默认值。
    /// 非人形角色（如 Zombie）的树若未声明 Slots/ 子节点 → 无槽位。
    /// </summary>
    internal class CharacterContainer : ModuleChild
    {
        private readonly CharacterBuildContext ctx;

        /// <summary>身体槽位定义。OnWire 后可用。</summary>
        public SlotDef[] BodySlots { get; private set; } = System.Array.Empty<SlotDef>();

        /// <summary>运行时身体容器——持有装备的 Entity。</summary>
        public Container.Container BodyContainer { get; private set; }

        public CharacterContainer(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
            ctx.CharacterContainer = this;
        }

        public override void OnWire()
        {
            var slotDefs = new List<SlotDef>();
            foreach (var path in ctx.Properties.GetChildren("Slots"))
            {
                var def = ctx.Properties.GetStruct<SlotDef>(path);
                def.SlotId = path.Substring(path.LastIndexOf('/') + 1);
                slotDefs.Add(def);
            }

            BodySlots = slotDefs.ToArray();

            if (BodySlots.Length == 0)
            {
                return;
            }

            BodyContainer = new Container.Container($"{ctx.Root.name}/Body", BodySlots);

            var names = new System.Text.StringBuilder();
            for (int i = 0; i < BodySlots.Length; i++)
            {
                if (i > 0) names.Append(", ");
                names.Append(BodySlots[i].SlotId);
            }
        }

    }
}
