using System.Collections.Generic;

namespace RedDust.GameScene
{
    /// <summary>
    /// 定义 boot 阶段 Task 列表和顺序。新增数据领域只改这一个文件。
    /// Tasks no longer take AddressablesService — the Pipeline loads everything.
    /// </summary>
    public static class BootTaskComposer
    {
        public static List<IBootTask> CreateAll()
        {
            return new List<IBootTask>
            {
                new TagBootTask(),                // Tag 最先 — FullTag 缓存重建
                new PropertyBootTask(),
                new PropertyTreeBootTask(),
                new AbilityBootTask(),
                new ItemBootTask(),
                new CharacterBootTask(),
                new ConfigBootTask(),
                new TagFinalizeTask(),            // 最后 — 全量 Tag FullTag 重建
            };
        }
    }
}
