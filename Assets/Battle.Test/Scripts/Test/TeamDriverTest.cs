using System;
using System.Collections;
using Altzone.Scripts.Battle;
using Altzone.Scripts.Config;
using Battle.Test.Scripts.Battle.Players;
using Prg.Scripts.Common.PubSub;
using Prg.Scripts.Common.Unity.ToastMessages;
using UnityEngine;

namespace Battle.Test.Scripts.Test
{
    public class TeamDriverTest : MonoBehaviour
    {
        [Serializable]
        internal class TeamSettings
        {
            public MonoBehaviour _player1;
            public MonoBehaviour _player2;
        }

        [Header("Settings"), SerializeField] private bool _isShowCountdown;
        
        [Header("Live Data"), SerializeField] private TeamSettings _teamBlue;
        [SerializeField] private TeamSettings _teamRed;

        private void OnEnable()
        {
            Debug.Log($"");
            this.Subscribe<TeamCreated>(OnTeamCreated);
            this.Subscribe<TeamBroken>(OnTeamBroken);
            this.Subscribe<TeamsAreReadyForGameplay>(OnTeamsAreReadyForGameplay);
        }

        private void OnDisable()
        {
            Debug.Log($"");
            this.Unsubscribe();
        }

        private void OnTeamCreated(TeamCreated data)
        {
            var team = data.BattleTeam;
            Debug.Log($"team {team.TeamNumber} first {team.FirstPlayer.NickName} second {team.SecondPlayer.NickName}");
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

        private void OnTeamBroken(TeamBroken data)
        {
            Debug.Log($"team {data.PlayerWhoLeft.TeamNumber} player left {data.PlayerWhoLeft.NickName}");
            if (data.PlayerWhoLeft.TeamNumber == PhotonBattle.TeamBlueValue)
            {
                _teamBlue._player1 = null;
                _teamBlue._player2 = null;
            }
            else if (data.PlayerWhoLeft.TeamNumber == PhotonBattle.TeamRedValue)
            {
                _teamRed._player1 = null;
                _teamRed._player2 = null;
            }
        }

        private void OnTeamsAreReadyForGameplay(TeamsAreReadyForGameplay data)
        {
            Debug.Log($"TeamsAreReadyForGameplay {data.TeamBlue} vs {data.TeamRed?.ToString() ?? "null"}");
            if (!_isShowCountdown)
            {
                return;
            }
            var runtimeGameConfig = RuntimeGameConfig.Get();
            var variables = runtimeGameConfig.Variables;
            var countdownDelay = variables._roomStartDelay;
            StartCoroutine(SimulateCountdown(countdownDelay));
        }

        private static IEnumerator SimulateCountdown(int countdownDelay)
        {
            var delay = new WaitForSeconds(1f);
            while (--countdownDelay >= 0)
            {
                yield return delay;
                ScoreFlashNet.Push(countdownDelay.ToString());
            }
        }
    }
}