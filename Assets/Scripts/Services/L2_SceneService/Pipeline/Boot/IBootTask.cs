using System.Collections;

namespace RedDust.GameScene
{
    /// <summary>
    /// A task that must complete before the first content scene activates.
    /// Registered with BootPipeline during OnWire. Typical uses: asset preloading,
    /// SDK init, shader warmup, config validation.
    /// </summary>
    public interface IBootTask
    {
        string Description { get; }
        IEnumerator Execute();
    }
}
