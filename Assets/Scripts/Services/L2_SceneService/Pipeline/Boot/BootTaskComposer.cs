using System.Collections.Generic;
using RedDust.Addressables;

namespace RedDust.GameScene
{
    /// <summary>
    /// 定义 boot 阶段 Task 列表和顺序。新增数据领域只改这一个文件。
    /// Scene 层 Task 在 <see cref="Scene.SceneTaskComposer"/>。
    /// </summary>
    public static class BootTaskComposer
    {
        public static List<IBootTask> CreateAll(AddressablesService addressables)
        {
            return new List<IBootTask>
            {
                new TagBootTask(addressables),          // Tag 最先 — 其他 SO 可能引用 Tag.FullTag
                new PropertyBootTask(addressables),
                new AbilityBootTask(addressables),
                new ItemBootTask(addressables),
                new CharacterBootTask(addressables),
            };
        }
    }
}
