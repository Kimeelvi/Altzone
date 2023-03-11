using System;
using System.Collections.Generic;
using Altzone.Scripts.Model;
using Altzone.Scripts.Model.Poco;
using Altzone.Scripts.Model.Poco.Clan;
using Altzone.Scripts.Model.Poco.Game;
using Altzone.Scripts.Model.Poco.Player;
using UnityEngine;

namespace Altzone.Scripts
{
    /// <summary>
    /// Factory class for our <c>DataStore</c> implementation.
    /// </summary>
    public static class Storefront
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Manual reset if UNITY Domain Reloading is disabled.
            _instance = null;
        }

        private const string StorageFilename = "LocalModels.json";
        
        private static DataStore _instance;

        /// <summary>
        /// Gets or creates an <c>DataStore</c> static singleton instance. 
        /// </summary>
        public static DataStore Get() => _instance ??= new DataStore(StorageFilename);
    }

    /// <summary>
    /// General Data Store for game data.<br />
    /// Data can be local, in our own hosted server or in some cloud based service.
    /// </summary>
    public class DataStore
    {
        private readonly LocalModels _localModels;

        public DataStore(string storageFilename)
        {
            _localModels = new LocalModels(storageFilename);
        }

        #region Public API

        // PLayer

        public void GetPlayerData(string uniqueIdentifier, Action<PlayerData> callback) => _localModels.GetPlayerData(uniqueIdentifier, callback);

        public void SavePlayerData(PlayerData playerData, Action<PlayerData> callback) => _localModels.SavePlayerData(playerData, callback);

        // Clan

        public void GetClanData(int id, Action<ClanData> callback) => _localModels.GetClanData(id, callback);

        public void SaveClanData(ClanData clanData, Action<ClanData> callback) => _localModels.SaveClanData(clanData, callback);

        // Game

        public void GetBattleCharacter(int customCharacterId, Action<BattleCharacter> callback) =>
            _localModels.GetBattleCharacter(customCharacterId, callback);

        public void GetAllBattleCharacters(Action<List<BattleCharacter>> callback) => _localModels.GetAllBattleCharacters(callback);

        public void GetAllCharacterClasses(Action<List<CharacterClass>> callback) => _localModels.GetAllCharacterClassModels(callback);

        public void GetAllCustomCharacters(Action<List<CustomCharacter>> callback) => _localModels.GetAllCustomCharacterModels(callback);

        #endregion

        #region Internal API

        internal int CharacterClassesVersion
        {
            get => _localModels.CharacterClassesVersion;
            set => _localModels.CharacterClassesVersion = value;
        }

        internal int CustomCharactersVersion
        {
            get => _localModels.CustomCharactersVersion;
            set => _localModels.CustomCharactersVersion = value;
        }

        internal void Set(List<CharacterClass> characterClasses) => new NotImplementedException();

        internal void Set(List<CustomCharacter> characterClasses) => new NotImplementedException();

        #endregion
    }
}