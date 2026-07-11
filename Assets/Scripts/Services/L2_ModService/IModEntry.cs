namespace RedDust.Services.ModService
{
    /// <summary>
    /// Contract for mod entry points. Mod authors implement this on a class
    /// marked with [ModEntry]. Called once after the mod assembly is loaded.
    /// </summary>
    public interface IModEntry
    {
        void Initialize();
    }
}
