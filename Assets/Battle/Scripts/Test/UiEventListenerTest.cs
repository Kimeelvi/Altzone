using System.Collections;
using Altzone.Scripts.Battle;
using Altzone.Scripts.Config;
using Battle.Scripts.Battle;
using Battle.Scripts.Ui;
using Photon.Pun;
using Prg.Scripts.Common.PubSub;
using Prg.Scripts.Common.Unity.ToastMessages;
using UnityEngine;

namespace Battle.Scripts.Test
{
    internal class UiEventListenerTest : MonoBehaviour
    {
        private int _roomStartDelay;
        private int _slingshotDelay;
        private bool _isDisableRaid;

        private void Awake()
        {
            ScoreFlashNet.RegisterEventListener();

            var runtimeGameConfig = RuntimeGameConfig.Get();
            var variables = runtimeGameConfig.Variables;
            _roomStartDelay = variables._roomStartDelay;
            _slingshotDelay = variables._slingshotDelay;
            var features = runtimeGameConfig.Features;
            _isDisableRaid = features._isDisableRaid;
        }

        private void OnEnable()
        {
            this.Subscribe<UiEvents.StartBattle>(OnStartBattle);
            this.Subscribe<UiEvents.RestartBattle>(OnRestartBattle);
            this.Subscribe<UiEvents.StartRaid>(OnStartRaid);
            this.Subscribe<UiEvents.ExitRaid>(OnExitRaid);

            this.Subscribe<UiEvents.HeadCollision>(OnHeadCollision);
            this.Subscribe<UiEvents.ShieldCollision>(OnShieldCollision);
            this.Subscribe<UiEvents.WallCollision>(OnWallCollision);
            this.Subscribe<UiEvents.TeamActivation>(OnTeamActivation);
        }

        private void OnDisable()
        {
            this.Unsubscribe();
        }

        private void OnStartBattle(UiEvents.StartBattle data)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Debug.Log($"{data}");
            var startTheBallTest = FindObjectOfType<StartTheBallTest>();
            if (startTheBallTest == null)
            {
                ScoreFlashNet.Push("NO START COMPONENT");
                return;
            }
            ScoreFlashNet.Push("START THE GAME");
            startTheBallTest.StartBallFirstTime();
            StartCoroutine(SimulateCountdown(_roomStartDelay));
        }

        private void OnRestartBattle(UiEvents.RestartBattle data)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Debug.Log($"{data}");
            var startTheBallTest = FindObjectOfType<StartTheBallTest>();
            if (startTheBallTest == null)
            {
                ScoreFlashNet.Push("NO RESTART COMPONENT");
                return;
            }
            ScoreFlashNet.Push("RESTART");
            startTheBallTest.RestartBallInGame(data.PlayerToStart);
            StartCoroutine(SimulateCountdown(_slingshotDelay));
        }

        private void OnStartRaid(UiEvents.StartRaid data)
        {
            if (_isDisableRaid)
            {
                return;
            }
            var startTheRaidTest = FindObjectOfType<StartTheRaidTest>();
            if (startTheRaidTest == null)
            {
                ScoreFlashNet.Push("NO RAID COMPONENT");
                return;
            }
            if (!startTheRaidTest.CanRaid)
            {
                ScoreFlashNet.Push("CAN NOT RAID");
                return;
            }
            var player = data.PlayerToStart;
            var info = player.TeamNumber == PhotonBattle.TeamBlueValue ? "RED" : "BLUE";
            ScoreFlashNet.Push($"RAID {info}", player.Position);
            startTheRaidTest.StartTheRaid(data.PlayerToStart);
        }
        
        private void OnExitRaid(UiEvents.ExitRaid data)
        {
            if (_isDisableRaid)
            {
                return;
            }
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Debug.Log($"{data}");
            ScoreFlashNet.Push("EXIT RAID", data.PlayerToExit.Position);
        }
        
        private static void OnHeadCollision(UiEvents.HeadCollision data)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Debug.Log($"{data}");
            var collision = data.Collision;
            var contactPoint = collision.GetFirstContactPoint();
            ScoreFlashNet.Push("HEAD", contactPoint.point);
        }

        private static void OnShieldCollision(UiEvents.ShieldCollision data)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Debug.Log($"{data}");
            var collision = data.Collision;
            var contactPoint = collision.GetFirstContactPoint();
            var info = data.HitType;
            ScoreFlashNet.Push($"SHIELD {info}", contactPoint.point);
        }

        private void OnWallCollision(UiEvents.WallCollision data)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Debug.Log($"{data}");
            if (_isDisableRaid)
            {
                return;
            }
            var player = Context.PlayerManager.GetPlayerByLastBallHitTime(data.RaidTeam);
            if (player == null)
            {
                return;
            }
            this.Publish(new UiEvents.StartRaid(player));
        }

        private static void OnTeamActivation(UiEvents.TeamActivation data)
        {
            Debug.Log($"{data}");
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