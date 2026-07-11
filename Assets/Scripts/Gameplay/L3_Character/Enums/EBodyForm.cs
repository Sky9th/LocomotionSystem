namespace RedDust.Character
{
    /// <summary>
    /// 战备形态——身体放松走动还是进入战斗戒备。
    /// 由 Director 产出，CharacterActor 缓存为 LastBodyForm。
    /// </summary>
    public enum EBodyForm
    {
        Relax = 0,
        Combat = 1
    }
}
