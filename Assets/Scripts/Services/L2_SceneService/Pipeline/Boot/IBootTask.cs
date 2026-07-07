namespace RedDust.GameScene
{
    /// <summary>
    /// A boot-phase task that extracts its required assets from the catalog
    /// and performs initialization (e.g. registries, caches).
    ///
    /// Tasks no longer load assets themselves; the BootPipeline loads all
    /// "boot" Addressables once and passes the catalog to each task in order.
    /// </summary>
    public interface IBootTask
    {
        string Description { get; }
        void Resolve(BootAssetCatalog catalog);
    }
}
