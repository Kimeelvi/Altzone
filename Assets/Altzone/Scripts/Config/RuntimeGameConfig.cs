using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Altzone.Scripts.Config.ScriptableObjects;
using Altzone.Scripts.Model;
using Prg.Scripts.Common.Util;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Altzone.Scripts.Config
{
    #region RuntimeGameConfig "Parts"

    /// <summary>
    /// Game features that can be toggled on and off.
    /// </summary>
    [Serializable]
    public class GameFeatures
    {
        /// <summary>
        /// Rotate game camera for upper team so they see their own game area in lower part of the screen.
        /// </summary>
        [Tooltip("Rotate game camera for upper team so they see their own game area in lower part of the screen")]
        public bool _isRotateGameCamera;

        /// <summary>
        /// Rotate game background for upper team so they see their own game area color in lower part of the screen.
        /// </summary>
        [Tooltip("Rotate game background for upper team so they see their own game area color in lower part of the screen")]
        public bool _isRotateGameBackground;

        /// <summary>
        /// Disable player <c>SetPlayMode</c> calls when ball goes over team's gameplay area.
        /// </summary>
        [Tooltip("Disable player SetPlayMode calls when ball goes over team's gameplay area")]
        public bool _isDisablePlayModeChanges;

        /// <summary>
        /// Disable player shield state changes when ball hits the shield.
        /// </summary>
        [Tooltip("Disable player shield state changes when ball hits the shield")]
        public bool _isDisableShieldStateChanges;

        /// <summary>
        /// Disable ball speed changes when ball collides with shield.
        /// </summary>
        [Tooltip("Disable ball speed changes when ball collides with shield")]
        public bool _isDisableBallSpeedChanges;

        /// <summary>
        /// Disable team forfeit when last team player leaves the room.
        /// </summary>
        [Tooltip("Disable team forfeit when last team player leaves the room")]
        public bool _isDisableTeamForfeit;

        /// <summary>
        /// Is shield always on when team has only one player.
        /// </summary>
        [Tooltip("Is shield always on when team has only one player")]
        public bool _isSinglePlayerShieldOn;

        /// <summary>
        /// Disable RAID gameplay from BATTLE.
        /// </summary>
        [Tooltip("Disable RAID gameplay from BATTLE")]
        public bool _isDisableRaid;

        /// <summary>
        /// Is bricks visible.
        /// </summary>
        [Tooltip("Is bricks visible")]
        public bool _isBricksVisible;

        public void CopyFrom(GameFeatures other)
        {
            PropertyCopier<GameFeatures, GameFeatures>.CopyFields(other, this);
        }
    }

    /// <summary>
    /// Game constraints that that control the workings of the game.
    /// </summary>
    [Serializable]
    public class GameConstraints
    {
        [Header("UI"), Min(2)] public int _minPlayerNameLength = 2;
        [Min(3)] public int _maxPlayerNameLength = 16;

        public void CopyFrom(GameConstraints other)
        {
            PropertyCopier<GameConstraints, GameConstraints>.CopyFields(other, this);
        }
    }

    /// <summary>
    /// Game variables that control game play somehow.
    /// </summary>
    [Serializable]
    public class GameVariables
    {
        [Header("Battle"), Min(1)] public int _roomStartDelay;
        [Min(1)] public int _slingshotDelay;
        [Min(0)] public int _headScoreToWin;
        [Min(0)] public float _playerShieldHitStunDuration;
        [Min(0)] public float _ballSlingshotPower;

        [Header("Ball"), Min(0)] public float _ballMoveSpeedMultiplier;
        [Min(0)] public float _ballMinMoveSpeed;
        [Min(0)] public float _ballMaxMoveSpeed;
        [Min(0)] public float _ballLerpSmoothingFactor;
        [Min(0)] public float _ballTeleportDistance;

        [Header("Player"), Min(0)] public float _playerMoveSpeedMultiplier;
        [Min(0)] public float _playerAttackMultiplier;

        [Header("Shield"), Min(0)] public float _shieldDistanceMultiplier;

        public void CopyFrom(GameVariables other)
        {
            PropertyCopier<GameVariables, GameVariables>.CopyFields(other, this);
        }
    }

    /// <summary>
    /// Well known prefabs for the game.
    /// </summary>
    [Serializable]
    public class GamePrefabs
    {
        [Header("Battle Player Prefabs")] public GameObject _playerForDes;
        public GameObject _playerForDef;
        public GameObject _playerForInt;
        public GameObject _playerForPro;
        public GameObject _playerForRet;
        public GameObject _playerForEgo;
        public GameObject _playerForCon;

        [Header("Battle Shield Prefabs")] public GameObject _shieldForDes;
        public GameObject _shieldForDef;
        public GameObject _shieldForInt;
        public GameObject _shieldForPro;
        public GameObject _shieldForRet;
        public GameObject _shieldForEgo;
        public GameObject _shieldForCon;

        public void CopyFrom(GamePrefabs other)
        {
            PropertyCopier<GamePrefabs, GamePrefabs>.CopyFields(other, this);
        }

        public GameObject GetPlayerPrefab(Defence defence)
        {
            switch (defence)
            {
                case Defence.Desensitisation:
                    return _playerForDes;
                case Defence.Deflection:
                    return _playerForDef;
                case Defence.Introjection:
                    return _playerForInt;
                case Defence.Projection:
                    return _playerForPro;
                case Defence.Retroflection:
                    return _playerForRet;
                case Defence.Egotism:
                    return _playerForEgo;
                case Defence.Confluence:
                    return _playerForCon;
                default:
                    throw new ArgumentOutOfRangeException(nameof(defence), defence, null);
            }
        }

        public GameObject GetShieldPrefab(Defence defence)
        {
            switch (defence)
            {
                case Defence.Desensitisation:
                    return _shieldForDes;
                case Defence.Deflection:
                    return _shieldForDef;
                case Defence.Introjection:
                    return _shieldForInt;
                case Defence.Projection:
                    return _shieldForPro;
                case Defence.Retroflection:
                    return _shieldForRet;
                case Defence.Egotism:
                    return _shieldForEgo;
                case Defence.Confluence:
                    return _shieldForCon;
                default:
                    throw new ArgumentOutOfRangeException(nameof(defence), defence, null);
            }
        }
    }

    /// <summary>
    /// New Input System Package for Player actions.
    /// </summary>
    [Serializable]
    public class GameInput
    {
        [Header("Player Input Actions")] public InputActionReference _clickInputAction;
        public InputActionReference _moveInputAction;
    }

    #endregion

    /// <summary>
    /// Runtime game config variables that can be referenced from anywhere safely and optionally can be changed on the fly.
    /// </summary>
    /// <remarks>
    /// Note that some parts of <c>RuntimeGameConfig</c> can be synchronized over network.
    /// </remarks>
    public class RuntimeGameConfig : MonoBehaviour
    {
        private const string IsFirstTimePlayingKey = "PlayerData.IsFirstTimePlaying";

        public static RuntimeGameConfig Get([CallerFilePath] string callerFilePath = null)
        {
            var instance = FindObjectOfType<RuntimeGameConfig>();
            if (instance == null)
            {
                instance = UnityExtensions.CreateGameObjectAndComponent<RuntimeGameConfig>(nameof(RuntimeGameConfig), true);
                LoadGameConfig(instance);
            }
            //CallerFilePathWarning(callerFilePath);
            return instance;
        }

        [Conditional("UNITY_EDITOR")]
        private static void CallerFilePathWarning(string callerFilePath)
        {
            if (callerFilePath == null || !callerFilePath.Contains("Battle"))
            {
                return;
            }
            var path = callerFilePath.Substring(callerFilePath.IndexOf("Assets", StringComparison.Ordinal));
            Debug.LogWarning($"CHECK THIS CALL {path}");
        }

        public static bool IsFirstTimePlaying => PlayerPrefs.GetInt(IsFirstTimePlayingKey, 1) == 1;

        public static void RemoveIsFirstTimePlayingStatus() => PlayerPrefs.SetInt(IsFirstTimePlayingKey, 0);

#if UNITY_EDITOR
        /// <summary>
        /// Used by Editor classes with <c>MenuItem</c> to pre-load <c>PlayerDataCache</c> as it is not otherwise available.
        /// </summary>
        public static PlayerDataCache GetPlayerDataCacheInEditor() => LoadPlayerDataCache();
#endif

        [SerializeField] private GameFeatures _permanentFeatures;
        [SerializeField] private GameConstraints _permanentConstraints;
        [SerializeField] private GameVariables _permanentVariables;
        [SerializeField] private GamePrefabs _permanentPrefabs;
        [SerializeField] private PlayerDataCache _playerDataCache;
        [SerializeField] private GameInput _gameInput;

        /// <summary>
        /// Game features that can be toggled on and off.
        /// </summary>
        public GameFeatures Features
        {
            get => _permanentFeatures;
            set => _permanentFeatures.CopyFrom(value);
        }

        /// <summary>
        /// Game constraints that that control the workings of the game.
        /// </summary>
        public GameConstraints GameConstraints
        {
            get => _permanentConstraints;
            set => _permanentConstraints.CopyFrom(value);
        }

        /// <summary>
        /// Game variables that control game play somehow.
        /// </summary>
        public GameVariables Variables
        {
            get => _permanentVariables;
            set => _permanentVariables.CopyFrom(value);
        }

        /// <summary>
        /// Well known prefabs for the game.
        /// </summary>
        public GamePrefabs Prefabs
        {
            get => _permanentPrefabs;
            private set => _permanentPrefabs.CopyFrom(value);
        }

        /// <summary>
        /// New Input System Package for Player actions.
        /// </summary>
        public GameInput Input => _gameInput;

        /// <summary>
        /// Player data cache - a common storage for player related data that is persisted somewhere (locally).
        /// </summary>
        public PlayerDataCache PlayerDataCache => _playerDataCache;

        private static void LoadGameConfig(RuntimeGameConfig instance)
        {
            // Create default values
            instance._permanentFeatures = new GameFeatures();
            instance._permanentConstraints = new GameConstraints();
            instance._permanentVariables = new GameVariables();
            instance._permanentPrefabs = new GamePrefabs();
            // Set persistent values
            var gameSettings = Resources.Load<PersistentGameSettings>(nameof(PersistentGameSettings));
            instance.Features = gameSettings._features;
            instance._permanentConstraints = gameSettings._constraints;
            instance.Variables = gameSettings._variables;
            instance.Prefabs = gameSettings._prefabs;
            instance._playerDataCache = LoadPlayerDataCache();
            instance._gameInput = gameSettings._input;
        }

        private static PlayerDataCache LoadPlayerDataCache()
        {
            return new PlayerDataCacheLocal();
        }
    }
}