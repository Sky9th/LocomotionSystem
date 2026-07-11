using System;

namespace RedDust.Services.ModService
{
    /// <summary>
    /// Serializable manifest matching manifest.json. Parsed by JsonUtility.
    /// S0 minimal fields.
    /// TODO S1: add dependencies[], loadPriority, content.
    /// NOTE: JsonUtility cannot parse top-level arrays (string[]);
    /// switch to Newtonsoft.Json when dependencies is added.
    /// </summary>
    [Serializable]
    public class ModManifest
    {
        public string modId;
        public string name;
        public string version;
        public string author;
        public string description;
    }
}
