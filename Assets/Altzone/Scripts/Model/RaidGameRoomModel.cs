using System;
using System.Collections.Generic;

namespace Altzone.Scripts.Model
{
    /// <summary>
    /// Serializable Room model for Raid mini-game that can be sent over network or saved somewhere (e.h. as JSON string).
    /// </summary>
    /// <remarks>
    /// Data coordinate system is: X = colum, Y = row, origo = top,left, zero based indexing.
    /// </remarks>
    [Serializable]
    public class RaidGameRoomModel
    {
        [Serializable]
        public class CoinLocation
        {
            public int _x;
            public int _y;
            public int _amount;

            public CoinLocation(int x, int y, int amount)
            {
                _x = x;
                _y = y;
                _amount = amount;
            }
        }

        [Serializable]
        public class FurnitureLocation
        {
            public int _x;
            public int _y;
            public int _furnitureId;

            public FurnitureLocation(int x, int y, int furnitureId)
            {
                _x = x;
                _y = y;
                _furnitureId = furnitureId;
            }
        }

        public int _id;
        public string _name;
        public int _width;
        public int _height;

        public List<CoinLocation> _coinLocations = new();
        public List<FurnitureLocation> _furnitureLocations = new();

        public RaidGameRoomModel(int id, string name, int width, int height)
        {
            _id = id;
            _name = name;
            _width = width;
            _height = height;
        }
    }
}