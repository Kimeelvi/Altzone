using System;
using System.Diagnostics.CodeAnalysis;

namespace Altzone.Scripts.Model.Poco.Clan
{
    [Serializable, SuppressMessage("ReSharper", "InconsistentNaming")]
    public class RaidRoomFurniture
    {
        public int Id;
        public string GameFurnitureId;
        public int Row;
        public int Col;

        public RaidRoomFurniture(int id, string gameFurnitureId, int row, int col)
        {
            Id = id;
            GameFurnitureId = gameFurnitureId;
            Row = row;
            Col = col;
        }
    }
}