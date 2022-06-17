using System;
using Altzone.Scripts.Config;
using Battle0.Scripts.Battle.interfaces;
using Photon.Pun;
using Prg.Scripts.Common.Photon;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Battle0.Scripts.Battle.Players2
{
    /// <summary>
    /// Synchronizes player position and speed over network.
    /// </summary>
    /// <remarks>
    /// <c>Stopped</c> state is managed locally and should never send over network as <c>IPlayerActor</c> manages it.
    /// </remarks>
    internal class PlayerMovement2 : IPlayerMovement
    {
        private const byte MsgMoveTo = PhotonEventDispatcher.EventCodeBase + 5;

        private readonly Transform _transform;
        private readonly GameInput _gameInput;
        private readonly Camera _camera;
        private readonly bool _isLocal;
        private readonly bool _isLimitMouseXY;
        private readonly PhotonEventHelper _photonEvent;

        public Rect PlayerArea { get; set; } = Rect.MinMaxRect(-100, -100, 100, 100);
        public float UnReachableDistance { get; set; } = 100;
        public float Speed { get; set; } = 1;

        private bool _isStopped;
        private bool _isMoving;

        private Vector3 _targetPosition;
        private Vector3 _tempPosition;

        private Vector2 _inputClick;
        private Vector3 _inputPosition;

        public string StateString => $"{(_isStopped ? "Stop" : _isMoving ? "Move" : "Idle")} {Speed:0.0}";

        public PlayerMovement2(Transform transform, GameInput gameInput, Camera camera, PhotonView photonView)
        {
            _transform = transform;
            _gameInput = gameInput;
            _camera = camera;
            _isLocal = photonView.IsMine;
            if (_isLocal)
            {
                SetupInput();
            }
            _isLimitMouseXY = !Application.isMobilePlatform;

            // In practice this might happen on runtime when players join and leaves more than 256 times in a room.
            Assert.IsTrue(photonView.OwnerActorNr <= byte.MaxValue, "photonView.OwnerActorNr <= byte.MaxValue");
            var playerId = (byte)photonView.OwnerActorNr;
            _photonEvent = new PhotonEventHelper(PhotonEventDispatcher.Get(), playerId);
            _photonEvent.RegisterEvent(MsgMoveTo, OnMoveToCallback);
        }

        void IPlayerMovement.Update()
        {
            if (_isMoving)
            {
                MoveTo();
            }
        }

        void IPlayerMovement.OnDestroy()
        {
            if (_isLocal)
            {
                ReleaseInput();
            }
        }

        void IPlayerMovement.SetMovementAllowed()
        {
            _isStopped = false;
        }

        void IPlayerMovement.SetStopped()
        {
            _isStopped = true;
            if (_isMoving)
            {
                _isMoving = false;
            }
        }

        private void SetMoveTo(Vector3 position, float speed)
        {
            _isMoving = speed > 0;
            _targetPosition = position;
            Speed = speed;
        }

        private void MoveTo()
        {
            _tempPosition = Vector3.MoveTowards(_transform.position, _targetPosition, Speed * Time.deltaTime);
            _transform.position = _tempPosition;
            _isMoving = !(Mathf.Approximately(_tempPosition.x, _targetPosition.x) && Mathf.Approximately(_tempPosition.y, _targetPosition.y));
        }

        #region UNITY Input System

        private void SetupInput()
        {
            // https://gamedevbeginner.com/input-in-unity-made-easy-complete-guide-to-the-new-system/

            // WASD or GamePad -> performed is called once per key press
            var moveAction = _gameInput._moveInputAction.action;
            moveAction.performed += DoMove;
            moveAction.canceled += StopMove;

            // Pointer movement when pressed down -> move to given point even pointer is released.
            var clickAction = _gameInput._clickInputAction.action;
            clickAction.performed += DoClick;
        }

        private void ReleaseInput()
        {
            var moveAction = _gameInput._moveInputAction.action;
            moveAction.performed -= DoMove;
            moveAction.canceled -= StopMove;

            var clickAction = _gameInput._clickInputAction.action;
            clickAction.performed -= DoClick;
        }

        private void DoMove(InputAction.CallbackContext ctx)
        {
            if (_isStopped)
            {
                return;
            }
            _inputClick = ctx.ReadValue<Vector2>() * UnReachableDistance;
            _inputPosition = _transform.position;
            _inputPosition.x += _inputClick.x;
            _inputPosition.y += _inputClick.y;
            _inputPosition.x = Mathf.Clamp(_inputPosition.x, PlayerArea.xMin, PlayerArea.xMax);
            _inputPosition.y = Mathf.Clamp(_inputPosition.y, PlayerArea.yMin, PlayerArea.yMax);
            SendMoveToRpc(_inputPosition, Speed);
        }

        private void StopMove(InputAction.CallbackContext ctx)
        {
            if (_isStopped)
            {
                return;
            }
            SendMoveToRpc(_inputPosition, 0);
        }

        private void DoClick(InputAction.CallbackContext ctx)
        {
            if (_isStopped)
            {
                return;
            }
            _inputClick = ctx.ReadValue<Vector2>();
#if UNITY_STANDALONE
            if (_isLimitMouseXY)
            {
                if (_inputClick.x < 0 || _inputClick.y < 0 ||
                    _inputClick.x > Screen.width || _inputClick.y > Screen.height)
                {
                    return;
                }
            }
#endif
            if (!_isMoving)
            {
                _isMoving = true;
            }
            _inputPosition.x = _inputClick.x;
            _inputPosition.y = _inputClick.y;
            _inputPosition = _camera.ScreenToWorldPoint(_inputPosition);
            _inputPosition.x = Mathf.Clamp(_inputPosition.x, PlayerArea.xMin, PlayerArea.xMax);
            _inputPosition.y = Mathf.Clamp(_inputPosition.y, PlayerArea.yMin, PlayerArea.yMax);
            SendMoveToRpc(_inputPosition, Speed);
        }

        #endregion

        #region Photon Event (RPC Message) Marshalling

        private readonly byte[] _moveToMsgBuffer = new byte[1 + 4 + 4 + 4];

        private byte[] MoveToToBytes(Vector3 position, float speed)
        {
            var index = 1;
            Array.Copy(BitConverter.GetBytes(position.x), 0, _moveToMsgBuffer, index, 4);
            index += 4;
            Array.Copy(BitConverter.GetBytes(position.y), 0, _moveToMsgBuffer, index, 4);
            index += 4;
            Array.Copy(BitConverter.GetBytes(speed), 0, _moveToMsgBuffer, index, 4);

            return _moveToMsgBuffer;
        }

        /// <summary>
        /// Naming convention to send message over networks is Send-MoveTo-Rpc
        /// </summary>
        private void SendMoveToRpc(Vector3 position, float speed)
        {
            _photonEvent.SendEvent(MsgMoveTo, MoveToToBytes(position, speed));
        }

        /// <summary>
        /// Naming convention to receive message from networks is On-MoveTo-Callback
        /// </summary>
        private void OnMoveToCallback(byte[] payload)
        {
            Vector3 position;
            var index = 1;
            position.x = BitConverter.ToSingle(payload, index);
            index += 4;
            position.y = BitConverter.ToSingle(payload, index);
            position.z = 0;
            index += 4;
            var speed = BitConverter.ToSingle(payload, index);

            SetMoveTo(position, speed);
        }

        #endregion
    }
}