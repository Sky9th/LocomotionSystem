using System;

namespace RedDust.Services.ModService
{
    /// <summary>
    /// Marks a class as a Mod entry point. The loader invokes IModEntry.Initialize()
    /// on the marked class after loading the assembly.
    /// S0: lives in Assembly-CSharp. S1: moves to RedDust.Modding.dll.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ModEntryAttribute : Attribute { }
}
