using System;
using Altzone.Scripts.Battle;
using Battle.Scripts.Battle;
using Battle.Scripts.Battle.Players;
using Unity.Collections;
using UnityEngine;

namespace Battle.Scripts.Test
{
    /// <summary>
    /// Static <c>PlayerDriver</c> implementation.
    /// </summary>
    /// <remarks>
    /// Set our ExecutionOrder a bit lower to let other components initialize properly before us.<br />
    /// Note that this (class) is strictly for testing purposes!
    /// </remarks>
    [DefaultExecutionOrder(100)]
    internal class PlayerDriverStatic : PlayerDriver, IPlayerDriver
    {
        [Serializable]
        internal class Settings
        {
            public string _nickName;
            public int _playerPos = PhotonBattle.PlayerPosition1;
            public int _teamNumber = PhotonBattle.TeamBlueValue;
            public bool _isLocal;
        }

        [Header("Settings"), SerializeField] private Settings _settings;

        [SerializeField] private PlayerActorBase _playerPrefab;
        [SerializeField] private double _movementDelay;

        private float _defaultRotation;
        private IPlayerActor _playerActor;
        private IGridManager _gridManager;
        private IPlayerDriverState _state;

        [Header("Live Data"), SerializeField, ReadOnly] private int _actorNumber;

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(_settings._nickName))
            {
                _settings._nickName = name;
            }
            _gridManager = Context.GetGridManager;
            _playerActor = PlayerActor.InstantiatePrefabFor(_settings._playerPos, _playerPrefab);
            _state = GetPlayerDriverState(this);
            if (_settings._teamNumber == PhotonBattle.TeamBlueValue)
            {
                _defaultRotation = 180f;
            }
            if (_settings._teamNumber == PhotonBattle.TeamRedValue)
            {
                _defaultRotation = 0f;
            }
            _state.ResetState(_playerActor, _settings._teamNumber);
            ((IPlayerDriver)this).Rotate(_defaultRotation);
            if (!_settings._isLocal)
            {
                return;
            }
            var playerInputHandler = Context.GetPlayerInputHandler;
            playerInputHandler.SetPlayerDriver(this);
        }

        #region IPlayerDriver

        string IPlayerDriver.NickName => _settings._nickName;

        int IPlayerDriver.TeamNumber => _settings._teamNumber;

        int IPlayerDriver.ActorNumber => _actorNumber;

        bool IPlayerDriver.IsLocal => _settings._isLocal;

        int IPlayerDriver.PlayerPos => _settings._playerPos;

        void IPlayerDriver.Rotate(float angle)
        {
            _playerActor.SetRotation(angle);
        }

        void IPlayerInputTarget.MoveTo(Vector2 targetPosition)
        {
            if (!_state.CanRequestMove)
            {
                return;
            }
            var gridPos = _gridManager.WorldPointToGridPosition(targetPosition);
            var isSpaceFree = _gridManager.IsMovementGridSpaceFree(gridPos, _settings._teamNumber);
            if (!isSpaceFree)
            {
                return;
            }
            _state.IsWaitingToMove(true);
            _state.DelayedMove(gridPos, (float)_movementDelay);
        }

        #endregion
    }
}
