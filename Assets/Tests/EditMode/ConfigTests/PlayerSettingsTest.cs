using System;
using System.Diagnostics.CodeAnalysis;
using Altzone.Scripts.Config;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.ConfigTests
{
    /// <summary>
    /// Note that these tests should not change, add or delete existing data which makes these a bit poor tests.
    /// </summary>
    /// <remarks>
    /// If <c>IsFirstTimePlaying</c> is true and <c>IsAccountVerified</c> is false we can do destructive testing!<br />
    /// Also note that <c>PlayerDataCache</c> is cached and will not reset 'easily'.
    /// </remarks>
    [TestFixture]
    public class PlayerSettingsTest
    {
        [Test, SuppressMessage("ReSharper", "UnusedVariable")]
        public void NonDestructiveTest1()
        {
            Debug.Log($"test");
            var playerDataCache = GameConfig.Get().PlayerSettings;

            // Execute every getter.
            var playerGuid = playerDataCache.PlayerGuid;
            var language = playerDataCache.Language;
            var isDebugFlag = playerDataCache.IsDebugFlag;
            var isTosAccepted = playerDataCache.IsTosAccepted;
            var isFirstTimePlaying = playerDataCache.IsFirstTimePlaying;
            var isAccountVerified = playerDataCache.IsAccountVerified;
        }

        [Test]
        public void DestructiveTest()
        {
            var playerDataCache = GameConfig.Get().PlayerSettings;
            if (!playerDataCache.IsFirstTimePlaying && playerDataCache.IsAccountVerified)
            {
                Debug.Log($"test SKIPPED");
                return;
            }
            Debug.Log($"test");
            var language = playerDataCache.Language;
            if (language == SystemLanguage.English)
            {
                playerDataCache.Language = SystemLanguage.Finnish;
                Assert.AreEqual(SystemLanguage.Finnish, playerDataCache.Language);
            }
            else
            {
                playerDataCache.Language = SystemLanguage.English;
                Assert.AreEqual(SystemLanguage.English, playerDataCache.Language);
            }
            var isDebugFlag = !playerDataCache.IsDebugFlag;
            playerDataCache.IsDebugFlag = isDebugFlag;
            Assert.AreEqual(isDebugFlag, playerDataCache.IsDebugFlag);

            var isTosAccepted = !playerDataCache.IsTosAccepted;
            playerDataCache.IsTosAccepted = isTosAccepted;
            Assert.AreEqual(isTosAccepted, playerDataCache.IsTosAccepted);

            var isFirstTimePlaying = !playerDataCache.IsFirstTimePlaying;
            playerDataCache.IsFirstTimePlaying = isFirstTimePlaying;
            Assert.AreEqual(isFirstTimePlaying, playerDataCache.IsFirstTimePlaying);

            var isAccountVerified = !playerDataCache.IsAccountVerified;
            playerDataCache.IsAccountVerified = isAccountVerified;
            Assert.AreEqual(isAccountVerified, playerDataCache.IsAccountVerified);

            // Keep GUID.
            var playerGuid = playerDataCache.PlayerGuid;
            Assert.IsFalse(string.IsNullOrWhiteSpace(playerGuid));
            var tempGuid = Guid.NewGuid().ToString();
            playerDataCache.SetPlayerGuid(tempGuid);
            Assert.AreEqual(tempGuid, playerDataCache.PlayerGuid);
            playerDataCache.SetPlayerGuid(playerGuid);

            Debug.Log($"done {playerDataCache}");
        }
    }
}