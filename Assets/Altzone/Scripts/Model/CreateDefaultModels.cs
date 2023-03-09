using System.Collections.Generic;
using Altzone.Scripts.Model.Poco;

namespace Altzone.Scripts.Model
{
    internal static class CreateDefaultModels
    {
        internal static List<CharacterClass> CreateCharacterClasses()
        {
            return new List<CharacterClass>
            {
                new(1, "Koulukiusaaja", Defence.Desensitisation, 3, 9, 7, 3),
                new(2, "Vitsiniekka", Defence.Deflection, 9, 3, 3, 4),
                new(3, "Pappi", Defence.Introjection, 5, 5, 4, 4),
                new(4, "Taiteilija", Defence.Projection, 4, 2, 9, 5),
                new(5, "Hodariläski", Defence.Retroflection, 3, 7, 2, 9),
                new(6, "Älykkö", Defence.Egotism, 6, 2, 6, 5),
                new(7, "Tytöt", Defence.Confluence, 5, 6, 2, 6)
            };
        }

        internal static List<CustomCharacter> CreateCustomCharacters()
        {
            return new List<CustomCharacter>
            {
                new(1, 1, "1", "Desensitisation", 0, 0, 0, 0),
                new(2, 2, "2", "Deflection", 0, 0, 0, 0),
                new(3, 3, "3", "Introjection", 0, 0, 0, 0),
                new(4, 4, "4", "Projection", 0, 0, 0, 0),
                new(5, 5, "5", "Retroflection", 0, 0, 0, 0),
                new(6, 6, "6", "Egotism", 0, 0, 0, 0),
                new(7, 7, "7", "Confluence", 0, 0, 0, 06)
            };
        }

        internal static List<PlayerData> CreatePlayerData()
        {
            return new List<PlayerData>
            {
                new(1, 0, 1, "Player", 0, string.Empty)
            };
        }
    }
}