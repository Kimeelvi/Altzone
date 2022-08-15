using System.Collections.Generic;

namespace Altzone.Scripts.Model
{
    /// <summary>
    /// Utility class to load <c>ClanModel</c> models for runtime from external storage.
    /// </summary>
    /// <remarks>
    /// WIKI: https://github.com/Alt-Org/Altzone/wiki/ModelLoader
    /// </remarks>
    public static class ClanModelLoader
    {
        public static List<ClanModel> LoadModels()
        {
            return new List<ClanModel>()
            {
                new(1, "Alpha", "ALPHA"),
                new(2, "Beta", "BETA"),
                new(9, "Viewer", "VIEW", true)
            };
        }
    }
}