using System;
using Altzone.Scripts.Battle;
using Altzone.Scripts.Config;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Battle.Scripts.Battle.Players
{
    /// <summary>
    /// Photon <c>PlayerDriver</c> implementation.
    /// </summary>
    internal class PlayerDriverPhoton : PlayerDriver, IPlayerDriver
    {
        [SerializeField] private PlayerActorBase _playerPrefab;

        private IPlayerActor _playerActor;
        private IGridManager _gridManager;
        private IBattlePlayArea _battlePlayArea;
        private IPlayerDriverState _state;
        private PhotonView _photonView;
        private int _playerPos;
        private int _teamNumber;
        private double _movementDelay;

        private bool _isLocal;

        [Header ("Testing")]
        [SerializeField] private bool _isTesting = false;
        [SerializeField] private int _playerPrefabID;

        private void Awake()
        {
            _battlePlayArea = Context.GetBattlePlayArea;
            _photonView = PhotonView.Get(this);
            _gridManager = Context.GetGridManager;
            _playerPos = PhotonBattle.GetPlayerPos(_photonView.Owner);
            _playerActor = InstantiatePlayerPrefab(_photonView.Owner);
            _teamNumber = PhotonBattle.GetTeamNumber(_playerPos);
            _movementDelay = GameConfig.Get().Variables._playerMovementNetworkDelay;
        }

        private IPlayerActor InstantiatePlayerPrefab(Player player)
        {
            var playerTag = $"{_teamNumber}:{_playerPos}:{player.NickName}";
            name = name.Replace("Clone", playerTag);
            if (_playerPrefab != null)
            {
                return PlayerActor.InstantiatePrefabFor(_playerPos, _playerPrefab, playerTag);
            }

            var playerPrefabs = GameConfig.Get().PlayerPrefabs;
            var playerPrefabId = PhotonBattle.GetPlayerPrefabId(player);
            if (_isTesting)
            {
                playerPrefabId = _playerPrefabID;
            }
            var playerPrefab = playerPrefabs.GetPlayerPrefab(playerPrefabId);
            var playerActor = PlayerActor.InstantiatePrefabFor(_playerPos, playerPrefab, playerTag);
            return playerActor;
        }

        private void OnEnable()
        {
            var player = _photonView.Owner;
            _isLocal = player.IsLocal;
            _state = GetPlayerDriverState(this);
            _state.ResetState(_playerActor, _teamNumber);
            if (_teamNumber == PhotonBattle.TeamBetaValue)
            {
                ((IPlayerDriver)this).Rotate(180f);
            }
            if (!_isLocal)
            {
                return;
            }
            var playerInputHandler = Context.GetPlayerInputHandler;
            playerInputHandler.SetPlayerDriver(this);
        }

        #region IPlayerDriver

        string IPlayerDriver.NickName => _photonView.Owner.NickName;

        int IPlayerDriver.TeamNumber => _teamNumber;

        int IPlayerDriver.ActorNumber => _photonView.Owner.ActorNumber;

        bool IPlayerDriver.IsLocal => _photonView.Owner.IsLocal;

        int IPlayerDriver.PlayerPos => _playerPos;

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
            var isSpaceFree = _gridManager.IsMovementGridSpaceFree(gridPos, _teamNumber);
            if (!isSpaceFree)
            {
                return;
            }
            _state.IsWaitingToMove(true);
            var movementStartTime = PhotonNetwork.Time + _movementDelay;
            _photonView.RPC(nameof(MoveDelayedRpc), RpcTarget.All, gridPos.Row, gridPos.Col, movementStartTime);
        }

        #endregion

        #region Photon RPC

        [PunRPC]
        private void MoveDelayedRpc(int row, int col, double movementStartTime)
        {
            var moveExecuteDelay = Math.Max(0, movementStartTime - PhotonNetwork.Time);
            var gridPos = new GridPos(row, col);
            _state.DelayedMove(gridPos, (float)moveExecuteDelay);
        }

        #endregion
    }
}
