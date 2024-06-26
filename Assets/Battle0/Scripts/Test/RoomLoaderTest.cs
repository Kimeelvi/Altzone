using System;
using System.Collections;
using Photon.Pun;
using Prg.Scripts.Common.Photon;
using Prg.Scripts.Common.Unity.ToastMessages;
using UnityEngine;

namespace Battle0.Scripts.Test
{
    /// <summary>
    /// <c>TestRoomLoader</c> creates a new test room unless it is in a room already.
    /// </summary>
    internal class RoomLoaderTest : MonoBehaviourPunCallbacks
    {
        private const string Tooltip1 = "If 'Is Offline Mode' only one player can play";

        [Header("Debug Settings"), Tooltip(Tooltip1), SerializeField] private bool _isOfflineMode;
        [SerializeField] private string _roomName;

        private IEnumerator Start()
        {
            PhotonNetwork.NickName = PhotonBattle.GetLocalPlayerName();
            Debug.Log($"{PhotonNetwork.NetworkClientState} {PhotonNetwork.LocalPlayer.GetDebugLabel()}");
            if (_isOfflineMode && !AppPlatform.IsEditor)
            {
                ScoreFlash.Push("NOTE\r\nPHOTON NETWORK\r\nOFFLINE MODE");
            }
            PhotonNetwork.OfflineMode = _isOfflineMode;
            for (; enabled;)
            {
                if (PhotonNetwork.InRoom)
                {
                    // We are in a room.
                    ShowRoomJoinedMessage();
                    enabled = false;
                    yield break;
                }
                if (_isOfflineMode)
                {
                    // JoinRandomRoom -> OnJoinedRoom
                    PhotonNetwork.JoinRandomRoom();
                }
                else if (PhotonWrapper.CanConnect)
                {
                    // Connect -> ConnectedToMasterServer
                    PhotonLobby.Connect(PhotonNetwork.NickName);
                }
                else if (PhotonWrapper.CanJoinLobby)
                {
                    // OnJoinedLobby -> JoinOrCreateRoom -> OnJoinedRoom
                    PhotonLobby.JoinLobby();
                }
                yield return null;
            }
        }

        public override void OnJoinedLobby()
        {
            if (string.IsNullOrWhiteSpace(_roomName))
            {
                _roomName = Environment.MachineName;
            }
            Debug.Log($"JoinOrCreateRoom {PhotonNetwork.NetworkClientState} room {_roomName}");
            PhotonLobby.JoinOrCreateRoom(_roomName);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"{PhotonNetwork.NetworkClientState} Error {returnCode} {message}");
            enabled = false;
        }

        private static void ShowRoomJoinedMessage()
        {
            Debug.Log($"room {PhotonNetwork.CurrentRoom.Name}");
            if (AppPlatform.IsEditor || AppPlatform.IsDevelopmentBuild)
            {
                return;
            }
            ScoreFlash.Push("DEVELOPMENT\r\nBUILD\r\nREQUIRED\r\nFOR TESTING");
        }
    }
}