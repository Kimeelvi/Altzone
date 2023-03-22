using System.Collections;
using Altzone.Scripts.Battle;
using Altzone.Scripts.Config;
using Battle.Scripts.Test;
using UnityConstants;
using UnityEngine;

namespace Battle.Scripts.Battle.Players
{
    /// <summary>
    /// <c>PlayerActor</c> for local and remote instances.
    /// </summary>
    internal class PlayerActor : PlayerActorBase, IPlayerActor
    {
        [SerializeField] private Transform _geometryRoot;
        [SerializeField] private float _movementSpeed;

        private IPlayerDriver _playerDriver;
        private IShieldPoseManager _shieldPoseManager;
        private float _playerMoveSpeedMultiplier;
        private Transform _transform;
        private Vector3 _tempPosition;
        private bool _hasTarget;
        private int _shieldResistance;
        private int _shieldHitPoints;
        private float _shieldDeformDelay;
        private float _angleLimit;
        private int _maxPoseIndex;
        private int _currentPoseIndex;

        public static string PlayerName;

        private void Awake()
        {
            _transform = GetComponent<Transform>();
            var variables = GameConfig.Get().Variables;
            _playerMoveSpeedMultiplier = variables._playerMoveSpeedMultiplier;
            _shieldResistance = variables._shieldResistance;
            _shieldHitPoints = _shieldResistance;
            _shieldDeformDelay = variables._shieldDeformDelay;
            _angleLimit = variables._angleLimit;
            _shieldPoseManager = GetComponentInChildren<ShieldPoseManager>();
            if (_shieldPoseManager != null)
            {
                StartCoroutine(ResetPose());
            }
        }

        private IEnumerator MoveCoroutine(Vector2 position)
        {
            Vector3 targetPosition = position;
            _hasTarget = true;
            while (_hasTarget)
            {
                yield return null;
                var maxDistanceDelta = _movementSpeed * _playerMoveSpeedMultiplier * Time.deltaTime;
                _tempPosition = Vector3.MoveTowards(_transform.position, targetPosition, maxDistanceDelta);
                _transform.position = _tempPosition;
                _hasTarget = !(Mathf.Approximately(_tempPosition.x, targetPosition.x) && Mathf.Approximately(_tempPosition.y, targetPosition.y));
            }
        }

        private IEnumerator ResetPose()
        {
            yield return new WaitUntil(() => _shieldPoseManager.MaxPoseIndex > 0);
            _currentPoseIndex = 0;
            _shieldPoseManager.SetPose(_currentPoseIndex);
            _maxPoseIndex = _shieldPoseManager.MaxPoseIndex;
        }

        private IEnumerator ShieldHitDelay(int poseIndex)
        {
            yield return new WaitForSeconds(_shieldDeformDelay);
            _shieldPoseManager.SetPose(poseIndex);
        }

        #region IPlayerActor

        bool IPlayerActor.IsBusy => _hasTarget;

        void IPlayerActor.MoveTo(Vector2 targetPosition)
        {
            StartCoroutine(MoveCoroutine(targetPosition));
        }

        void IPlayerActor.SetPlayerDriver(IPlayerDriver playerDriver)
        {
            _playerDriver = playerDriver;
        }

        void IPlayerActor.SetRotation(float angle)
        {
            var multiplier = Mathf.Round (angle / _angleLimit);
            var newAngle = _angleLimit * multiplier;
            _geometryRoot.eulerAngles = new Vector3(0, 0, newAngle);
        }

        void IPlayerActor.ShieldHit(int damage)
        {
            if (_shieldPoseManager == null)
            {
                return;
            }
            if (_currentPoseIndex < _maxPoseIndex)
            {
                _shieldHitPoints -= damage;
                if (_shieldHitPoints <= 0)
                {
                    _currentPoseIndex++;
                    _playerDriver.SetCharacterPose(_currentPoseIndex);
                    _shieldHitPoints = _shieldResistance;
                }
            }
        }

        void IPlayerActor.SetCharacterPose(int poseIndex)
        {
            StartCoroutine(ShieldHitDelay(poseIndex));
        }

        #endregion

        public static IPlayerActor InstantiatePrefabFor(IPlayerDriver playerDriver, int playerPos, PlayerActorBase playerPrefab, string gameObjectName, float scale)
        {
            PlayerName = gameObjectName;
            Debug.Log($"heoooo{gameObjectName}");            
            var instantiationGridPosition = Context.GetBattlePlayArea.GetPlayerStartPosition(playerPos);
            var instantiationPosition = Context.GetGridManager.GridPositionToWorldPoint(instantiationGridPosition);
            var playerActorBase = Instantiate(playerPrefab, instantiationPosition, Quaternion.identity);
            if (playerActorBase != null)
            {
                playerActorBase.name = playerActorBase.name.Replace("Clone", gameObjectName);
                switch (playerPos)
                {
                    case PhotonBattle.PlayerPosition1:
                        playerActorBase.gameObject.layer = Layers.Player1;
                        break;
                    case PhotonBattle.PlayerPosition2:
                        playerActorBase.gameObject.layer = Layers.Player2;
                        break;
                    case PhotonBattle.PlayerPosition3:
                        playerActorBase.gameObject.layer = Layers.Player3;
                        break;
                    case PhotonBattle.PlayerPosition4:
                        playerActorBase.gameObject.layer = Layers.Player4;
                        break;
                    default:
                        throw new UnityException($"Invalid player position {playerPos}");
                }
            }            
            playerActorBase.transform.localScale = Vector3.one * scale;
            var playerActor = (IPlayerActor)playerActorBase;
            playerActor.SetPlayerDriver(playerDriver);
            return playerActor;
        }
    }
}
