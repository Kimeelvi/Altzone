using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Altzone.Scripts.Model.Poco.Clan;
using Altzone.Scripts.Model.Poco.Game;
using Altzone.Scripts.Model.Poco.Player;
using Altzone.Scripts.Settings;
using Prg.Scripts.Common.Unity;
using UnityEngine;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif
using Debug = UnityEngine.Debug;

namespace Altzone.Scripts.Model
{
    /// <summary>
    /// Entry point to local POCO models JSON storage implementation with 'built-in' WebGL support.
    /// </summary>
    /// <remarks>
    /// WebGl builds have to manually flush changes to browser local storage/database after changes to be on the safe side.
    /// </remarks>
    internal class LocalModels
    {
        private const int WebGlFramesToWaitFlush = 10;
        private static readonly Encoding Encoding = new UTF8Encoding(false, false);

        private readonly string _storagePath;
        private readonly StorageData _storageData;

        #region WebGL support

#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void HelloWebGl();

        [DllImport("__Internal")]
        private static extern void FsSyncFs();

        private static void CallHelloWebGl()
        {
            Debug.Log("Call Javascript Library");
            try
            {
                HelloWebGl();
            }
            catch (Exception e)
            {
                Debug.Log($"Exception {e.GetType().FullName} {e.Message}");
            }
        }

        private static void CallFsSyncFs()
        {
            Debug.Log("Call Javascript Library");
            try
            {
                FsSyncFs();
            }
            catch (Exception e)
            {
                Debug.Log($"Exception {e.GetType().FullName} {e.Message}");
            }
        }

        private void InitFsSyncFs()
        {
            if (!AppPlatform.IsEditor)
            {
                // Javascript call.
                CallHelloWebGl();
            }
            _monoHelper = UnityMonoHelper.Instance;
            _fsSync = null;
        }
#endif
        private static UnityMonoHelper _monoHelper;
        private static Coroutine _fsSync;
        private static int _framesToWait;

        #endregion

        internal LocalModels(string storageFilename)
        {
#if UNITY_WEBGL
            InitFsSyncFs();
#endif
            // Files can only be in Application.persistentDataPath for WebGL compatibility! 
            _storagePath = Path.Combine(Application.persistentDataPath, storageFilename);
            if (AppPlatform.IsWindows)
            {
                _storagePath = AppPlatform.ConvertToWindowsPath(_storagePath);
            }
            Debug.Log($"StorageFilename {_storagePath}");
            _storageData = File.Exists(_storagePath)
                ? LoadStorage(_storagePath)
                : CreateDefaultStorage(_storagePath);
        }

        #region WebGL support for file system level sync aka flush data.

        [Conditional("UNITY_WEBGL")]
        private static void WebGlFsSyncFs()
        {
            if (AppPlatform.IsEditor)
            {
                return;
            }
            _framesToWait = WebGlFramesToWaitFlush;
            if (_fsSync != null)
            {
                Debug.Log("FsSyncFs - SKIP");
                return;
            }
            _fsSync = _monoHelper.StartCoroutine(FsSync());

            IEnumerator FsSync()
            {
                while (--_framesToWait > 0)
                {
                    yield return null;
                }
                _fsSync = null;
                Debug.Log("FsSyncFs - SYNC");
#if UNITY_WEBGL
                // Javascript call.
                CallFsSyncFs();
#endif
            }
        }

        #endregion

        #region PlayerData

        internal void GetPlayerData(string uniqueIdentifier, Action<PlayerData> callback)
        {
            var playerData = _storageData.PlayerData.FirstOrDefault(x => x.UniqueIdentifier == uniqueIdentifier);
            if (playerData != null)
            {
                // This storage is by no means a complete object model we want to serve.
                playerData.Patch(_GetAllBattleCharacters(), _storageData.CustomCharacters);
            }
            callback(playerData);
        }

        internal void SavePlayerData(PlayerData playerData, Action<PlayerData> callback)
        {
            var index = _storageData.PlayerData.FindIndex(x => x.Id == playerData.Id);
            if (index >= 0)
            {
                _storageData.PlayerData[index] = playerData;
            }
            else
            {
                if (string.IsNullOrEmpty(playerData.Id))
                {
                    playerData.Id = CreateDefaultModels.FakeMongoDbId();
                }
                _storageData.PlayerData.Add(playerData);
            }
            Debug.Log($"playerData {playerData}");
            SaveStorage(_storageData, _storagePath);
            callback?.Invoke(playerData);
        }

        #endregion

        #region ClanData

        internal void GetClanData(string id, Action<ClanData> callback)
        {
            callback(_storageData.ClanData.FirstOrDefault(x => x.Id == id));
        }

        internal void SaveClanData(ClanData clanData, Action<ClanData> callback)
        {
            var index = _storageData.ClanData.FindIndex(x => x.Id == clanData.Id);
            if (index >= 0)
            {
                _storageData.ClanData[index] = clanData;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(clanData.Id))
                {
                    var id = _storageData.ClanData.Count + 1001;
                    clanData.Id = id.ToString();
                }
                _storageData.ClanData.Add(clanData);
            }
            Debug.Log($"clanData {clanData}");
            SaveStorage(_storageData, _storagePath);
            callback?.Invoke(clanData);
        }

        #endregion

        #region Temporary Test API

        internal void GetAllCustomCharacterModelsTest(Action<List<CustomCharacter>> callback)
        {
            callback(_storageData.CustomCharacters);
        }

        internal void GetBattleCharacterTest(string customCharacterId, Action<BattleCharacter> callback)
        {
            callback(_GetBattleCharacter(customCharacterId));
        }

        internal void GetAllBattleCharactersTest(Action<List<BattleCharacter>> callback)
        {
            callback(_GetAllBattleCharacters());
        }

        private List<BattleCharacter> _GetAllBattleCharacters()
        {
            var battleCharacters = new List<BattleCharacter>();
            foreach (var customCharacter in _storageData.CustomCharacters)
            {
                battleCharacters.Add(_GetBattleCharacter(customCharacter.Id));
            }
            return battleCharacters;
        }

        private BattleCharacter _GetBattleCharacter(string customCharacterId)
        {
            var customCharacter = _storageData.CustomCharacters.FirstOrDefault(x => x.Id == customCharacterId);
            if (customCharacter == null)
            {
                throw new UnityException($"CustomCharacter not found for {customCharacterId}");
            }
            var characterClass =
                _storageData.CharacterClasses.FirstOrDefault(x => x.Id == customCharacter.CharacterClassId);
            if (characterClass == null)
            {
                // Create fake CharacterClass so we can return 'valid' object even character class has been deleted.
                characterClass = CharacterClass.CreateDummyFor(customCharacter.CharacterClassId);
            }
            return BattleCharacter.Create(customCharacter, characterClass);
        }

        #endregion

        #region Game non-mutable internal static data created by game designers

        internal void GetAllCharacterClassModels(Action<ReadOnlyCollection<CharacterClass>> callback)
        {
            callback(new ReadOnlyCollection<CharacterClass>(_storageData.CharacterClasses));
        }

        internal void GetAllGameFurniture(Action<ReadOnlyCollection<GameFurniture>> callback)
        {
            callback(new ReadOnlyCollection<GameFurniture>(_storageData.GameFurniture));
        }

        #endregion

        #region Setters for bulk data updates for base models.

        internal void Set(List<CharacterClass> characterClasses, Action<bool> callback)
        {
            _storageData.CharacterClasses = characterClasses;
            SaveStorage(_storageData, _storagePath);
            callback?.Invoke(true);
        }

        internal void Set(List<CustomCharacter> customCharacters, Action<bool> callback)
        {
            _storageData.CustomCharacters = customCharacters;
            SaveStorage(_storageData, _storagePath);
            callback?.Invoke(true);
        }

        internal void Set(List<GameFurniture> gameFurniture, Action<bool> callback)
        {
            _storageData.GameFurniture = gameFurniture;
            SaveStorage(_storageData, _storagePath);
            callback?.Invoke(true);
        }

        #endregion

        private static StorageData CreateDefaultStorage(string storagePath)
        {
            var storageData = new StorageData();

            storageData.CharacterClasses.AddRange(CreateDefaultModels.CreateCharacterClasses());
            storageData.CustomCharacters.AddRange(CreateDefaultModels.CreateCustomCharacters());
            storageData.GameFurniture.AddRange(CreateDefaultModels.CreateGameFurniture());

            var playerGuid = new PlayerSettings().PlayerGuid;
            var clanGuid = playerGuid;
            var customCharacterId = storageData.CustomCharacters[0].Id;
            storageData.PlayerData.Add(CreateDefaultModels.CreatePlayerData(playerGuid, clanGuid, customCharacterId));
            storageData.ClanData.Add(CreateDefaultModels.CreateClanData(clanGuid, storageData.GameFurniture));

            SaveStorage(storageData, storagePath);
            return storageData;
        }

        private static StorageData LoadStorage(string storagePath)
        {
            var jsonText = File.ReadAllText(storagePath, Encoding);
            var storageData = JsonUtility.FromJson<StorageData>(jsonText);
            return storageData;
        }

        private static void SaveStorage(StorageData storageData, string storagePath)
        {
            var jsonText = JsonUtility.ToJson(storageData);
            File.WriteAllText(storagePath, jsonText, Encoding);
            WebGlFsSyncFs();
        }
    }

    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    internal class StorageData
    {
        public List<CharacterClass> CharacterClasses = new();
        public List<CustomCharacter> CustomCharacters = new();
        public List<GameFurniture> GameFurniture = new();
        public List<PlayerData> PlayerData = new();
        public List<ClanData> ClanData = new();
    }
}
