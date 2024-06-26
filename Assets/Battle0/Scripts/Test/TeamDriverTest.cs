using System;
using System.Collections;
using Battle0.Scripts.Battle.Game;
using Battle0.Scripts.Ui;
using Photon.Pun;
using Prg.Scripts.Common.PubSub;
using Prg.Scripts.Common.Unity.ToastMessages;
using UnityEngine;

namespace Battle0.Scripts.Test
{
    internal class TeamDriverTest : MonoBehaviour
    {
        private const string Tooltip1 = "Set ball moving using StartBattle event when all players are registered";

        [Serializable]
        internal class TeamSettings
        {
            public MonoBehaviour _player1;
            public MonoBehaviour _player2;
        }

        [Header("Autostart"), Tooltip(Tooltip1), SerializeField] private bool _isAutoStartBall;
        [Range(0, 4), SerializeField] private int _roomPlayersOverride;

        [Header("Live Data"), SerializeField] private TeamSettings _teamBlue;
        [SerializeField] private TeamSettings _teamRed;

        private void OnEnable()
        {
            Debug.Log($"");
            this.Subscribe<TeamCreated>(OnTeamCreated);
            this.Subscribe<TeamUpdated>(OnTeamUpdated);
            this.Subscribe<TeamBroken>(OnTeamBroken);
            this.Subscribe<TeamsAreReadyForGameplay>(OnTeamsAreReadyForGameplay);
            StartCoroutine(ConfigureRoom(_roomPlayersOverride));
        }

        private IEnumerator ConfigureRoom(int roomPlayersCount)
        {
            Debug.Log($"roomPlayersCount {roomPlayersCount}");
            for (;;)
            {
                // Safety measure if app exists before we enter a room!
                if (!enabled)
                {
                    yield break;
                }
                if (PhotonNetwork.InRoom)
                {
                    break;
                }
                yield return null;
            }
            if (!PhotonNetwork.IsMasterClient)
            {
                yield break;
            }
            var room = PhotonNetwork.CurrentRoom;
            if (roomPlayersCount > 0)
            {
                PhotonBattle.SetDebugRoomProperties(room, roomPlayersCount);
                if (roomPlayersCount > 1)
                {
                    ScoreFlash.Push($"PLAYERS TO WAIT:  {roomPlayersCount}");
                }
            }
        }

        private void OnDisable()
        {
            Debug.Log($"");
            this.Unsubscribe();
        }

        private void OnTeamCreated(TeamCreated data)
        {
            var team = data.BattleTeam;
            Debug.Log($"team {team} first {team.FirstPlayer.NickName} second {team.SecondPlayer?.NickName}");
            UpdateTeam(team);
        }

        private void OnTeamUpdated(TeamUpdated data)
        {
            var team = data.BattleTeam;
            Debug.Log($"team {team} first {team.FirstPlayer.NickName} second {team.SecondPlayer.NickName}");
            UpdateTeam(team);
        }

        private void OnTeamBroken(TeamBroken data)
        {
            var team = data.BattleTeam;
            Debug.Log($"team {team} first {team.FirstPlayer.NickName} LEFT {data.PlayerWhoLeft.NickName}");
            UpdateTeam(team);
        }

        private void UpdateTeam(BattleTeam team)
        {
            if (team.FirstPlayer.TeamNumber == PhotonBattle.TeamBlueValue)
            {
                _teamBlue._player1 = team.FirstPlayer as MonoBehaviour;
                _teamBlue._player2 = team.SecondPlayer as MonoBehaviour;
            }
            else if (team.FirstPlayer.TeamNumber == PhotonBattle.TeamRedValue)
            {
                _teamRed._player1 = team.FirstPlayer as MonoBehaviour;
                _teamRed._player2 = team.SecondPlayer as MonoBehaviour;
            }
        }

        private void OnTeamsAreReadyForGameplay(TeamsAreReadyForGameplay data)
        {
            Debug.Log($"TeamsAreReadyForGameplay {data.TeamBlue} vs {data.TeamRed?.ToString() ?? "null"} " +
                      $"IsMasterClient {PhotonNetwork.IsMasterClient}");
            this.Publish(new UiEvents.StartAnimation());
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            if (_isAutoStartBall)
            {
                this.Publish(new UiEvents.StartBattle());
            }
        }
    }
}
