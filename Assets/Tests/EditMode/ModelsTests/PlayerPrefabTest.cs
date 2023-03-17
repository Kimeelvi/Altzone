using System;
using Altzone.Scripts;
using Altzone.Scripts.Config;
using NUnit.Framework;

namespace Tests.EditMode.ModelsTests
{
    [TestFixture]
    public class PlayerPrefabTest
    {
        [Test]
        public void GetCurrentPlayerPrefabTest()
        {
            Debug.Log($"test");
            var gameConfig = GameConfig.Get();
            var playerSettings = gameConfig.PlayerSettings;
            var playerGuid = playerSettings.PlayerGuid;
            var store = Storefront.Get();
            store.GetPlayerData(playerGuid, playerData =>
            {
                var currentCharacterModelId = playerData.CurrentCustomCharacterId;
                var prefabId = 0;
                try
                {
                    store.GetBattleCharacterTest(currentCharacterModelId, battleCharacter =>
                    {
                        Debug.Log($"{battleCharacter}");
                        Assert.IsFalse(string.IsNullOrWhiteSpace(battleCharacter.UnityKey));
                        prefabId = int.Parse(battleCharacter.UnityKey);
                        Assert.IsTrue(prefabId >= 0);
                        var playerPrefabs = gameConfig.PlayerPrefabs;
                        var playerPrefab = playerPrefabs.GetPlayerPrefab(prefabId);
                        Assert.IsNotNull(playerPrefab);
                    });
                }
                catch (Exception e)
                {
                    Debug.Log($"GetBattleCharacter failed {e.Message}");
                    Assert.Fail("Check that CustomCharacterModels exist or restart UNITY to reset Storefront");
                }
            });
        }

        [Test]
        public void GetAllPlayerPrefabsTest()
        {
            Debug.Log($"test");
            var playerPrefabs = GameConfig.Get().PlayerPrefabs;
            Storefront.Get().GetAllBattleCharactersTest(battleCharacters =>
            {
                foreach (var battleCharacter in battleCharacters)
                {
                    var prefabId = 0;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(battleCharacter.UnityKey));
                    prefabId = int.Parse(battleCharacter.UnityKey);
                    Assert.IsTrue(prefabId >= 0);
                    var playerPrefab = playerPrefabs.GetPlayerPrefab(prefabId);
                    Assert.IsNotNull(playerPrefab);
                }
            });
        }
    }
}