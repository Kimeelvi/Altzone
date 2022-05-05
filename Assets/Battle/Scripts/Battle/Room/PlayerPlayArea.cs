using Altzone.Scripts.Battle;
using UnityEngine;

namespace Battle.Scripts.Battle.Room
{
    public class PlayerPlayArea : MonoBehaviour
    {
        [Header("Player Areas"), SerializeField] private Rect _playAreaP1;
        [SerializeField] private Rect _playAreaP2;
        [SerializeField] private Rect _playAreaP3;
        [SerializeField] private Rect _playAreaP4;
        [SerializeField] private Vector2 _deflatePlayArea;

        [Header("Player Start Positions"), SerializeField] private Vector2 _startPositionP1;
        [SerializeField] private Vector2 _startPositionP2;
        [SerializeField] private Vector2 _startPositionP3;
        [SerializeField] private Vector2 _startPositionP4;

        public Rect GetPlayerPlayArea(int playerPos)
        {
            Rect playArea;
            switch (playerPos)
            {
                case PhotonBattle.PlayerPosition1:
                    playArea = _playAreaP1;
                    break;
                case PhotonBattle.PlayerPosition2:
                    playArea = _playAreaP2;
                    break;
                case PhotonBattle.PlayerPosition3:
                    playArea = _playAreaP3;
                    break;
                case PhotonBattle.PlayerPosition4:
                    playArea = _playAreaP4;
                    break;
                default:
                    throw new UnityException($"Invalid player position {playerPos}");
            }
            if (_deflatePlayArea != Vector2.zero)
            {
                playArea = playArea.Inflate(-_deflatePlayArea);
            }
            return playArea;
        }

        public Vector2 GetPlayerStartPosition(int playerPos)
        {
            Vector2 startPosition;
            switch (playerPos)
            {
                case PhotonBattle.PlayerPosition1:
                    startPosition = _startPositionP1;
                    break;
                case PhotonBattle.PlayerPosition2:
                    startPosition = _startPositionP2;
                    break;
                case PhotonBattle.PlayerPosition3:
                    startPosition = _startPositionP3;
                    break;
                case PhotonBattle.PlayerPosition4:
                    startPosition = _startPositionP4;
                    break;
                default:
                    throw new UnityException($"Invalid player position {playerPos}");
            }
            var playArea = GetPlayerPlayArea(playerPos);
            if (!playArea.Contains(startPosition))
            {
                startPosition = playArea.center;
            }
            return startPosition;
        }
    }
}