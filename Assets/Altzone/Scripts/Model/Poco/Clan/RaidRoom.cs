using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Altzone.Scripts.Model.Poco.Attributes;
using UnityEngine.Assertions;

namespace Altzone.Scripts.Model.Poco.Clan
{
    [MongoDbEntity, Serializable, SuppressMessage("ReSharper", "InconsistentNaming")]
    public class RaidRoom
    {
        [PrimaryKey] public string Id;
        [ForeignKey(nameof(ClanMember)), Mandatory] public string ClanMemberId;
        public RaidRoomType Type;
        public int RowCount;
        public int ColCount;
        public List<RaidRoomFurniture> Furniture = new();

        public RaidRoom(string id, string clanMemberId, RaidRoomType type, int rowCount, int colCount)
        {
            Assert.IsTrue(id.IsPrimaryKey());
            Assert.IsTrue(clanMemberId.IsMandatory());
            Assert.AreNotEqual(RaidRoomType.None, type);
            Assert.IsTrue(rowCount >= 0);
            Assert.IsTrue(colCount >= 0);
            Id = id;
            ClanMemberId = clanMemberId;
            Type = type;
            RowCount = rowCount;
            ColCount = colCount;
        }
    }
}