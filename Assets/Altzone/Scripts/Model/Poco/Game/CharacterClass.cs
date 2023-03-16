using System;
using System.Diagnostics.CodeAnalysis;

namespace Altzone.Scripts.Model.Poco.Game
{
    /// <summary>
    /// Non-mutable <c>CharacterClass</c> that acts as base archetype for player created characters.
    /// </summary>
    [Serializable, SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CharacterClass
    {
        public int CharacterClassId;
        public Defence MainDefence;
        public string Name;
        public int Speed;
        public int Resistance;
        public int Attack;
        public int Defence;

        public CharacterClass(int characterClassId, Defence mainDefence, string name, int speed, int resistance, int attack, int defence)
        {
            CharacterClassId = characterClassId;
            MainDefence = mainDefence;
            Name = name;
            Speed = speed;
            Resistance = resistance;
            Attack = attack;
            Defence = defence;
        }

        public override string ToString()
        {
            return $"{nameof(CharacterClassId)}: {CharacterClassId}, {nameof(MainDefence)}: {MainDefence}, {nameof(Name)}: {Name}" +
                   $", {nameof(Speed)}: {Speed}, {nameof(Resistance)}: {Resistance}, {nameof(Attack)}: {Attack}, {nameof(Defence)}: {Defence}";
        }
    }
}