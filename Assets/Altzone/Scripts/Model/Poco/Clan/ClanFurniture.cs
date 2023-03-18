using System;
using System.Diagnostics.CodeAnalysis;

namespace Altzone.Scripts.Model.Poco.Clan
{
    [Serializable, SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ClanFurniture
    {
        public int Id;
        public string GameFurnitureId;

        public ClanFurniture(int id, string gameFurnitureId)
        {
            Id = id;
            GameFurnitureId = gameFurnitureId;
        }
    }
}