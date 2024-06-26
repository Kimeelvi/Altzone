using UnityEngine;

namespace Battle.Scripts.Battle.Game
{
    /// <summary>
    /// Sets the arena, player's starting positions and team play areas.
    /// </summary>
    internal class PlayerPlayArea : MonoBehaviour
    {
        [Tooltip("Arena width in world coordinates"), SerializeField] private float _arenaWidth;
        [Tooltip("Arena height in world coordinates"), SerializeField] private float _arenaHeight;
        [Tooltip("How many squares is the grid height"), SerializeField] private int _gridHeight;
        [Tooltip("How many squares is the grid width"), SerializeField] private int _gridWidth;
        [Tooltip("Middle area height in grid squares"), SerializeField] private int _middleAreaHeight;
        [Tooltip("How many squares is the brick wall height"), SerializeField] private int _brickHeight;

        [Header("Wall Colliders")]
        [SerializeField] private GameObject _topWall;
        [SerializeField] private GameObject _bottomWall;
        [SerializeField] private GameObject _rightWall;
        [SerializeField] private GameObject _leftWall;

        [SerializeField] private GameObject _alphaTeamBrickWall;
        [SerializeField] private GameObject _betaTeamBrickWall;

        public int BrickHealth;

        [Header("Player Start Positions"), SerializeField] private GridPos _startPositionAlpha1;
        [SerializeField] private GridPos _startPositionAlpha2;
        [SerializeField] private GridPos _startPositionBeta1;
        [SerializeField] private GridPos _startPositionBeta2;

        [SerializeField] private GameObject[] _alphaTeamBricks;
        [SerializeField] private GameObject[] _betaTeamBricks;

        private BoxCollider2D _bottomWallCollider;
        private BoxCollider2D _topWallCollider;
        private BoxCollider2D _rightWallCollider;
        private BoxCollider2D _leftWallCollider;


        private const float BrickSpriteWidth = 2.35f;
        private const int BricksPerWall = 5;

        private Rect _playStartAreaAlpha;
        private Rect _playStartAreaBeta;

        private void Awake()
        {
            SetupArenaBorders();
            SetupBottomBrickWalls();

            var middleAreaHeight = _middleAreaHeight * _arenaHeight / _gridHeight;
            var squareHeight = _arenaHeight / _gridHeight;
 
            _playStartAreaAlpha = new Rect(-_arenaWidth / 2, -_arenaHeight / 2 + squareHeight * _brickHeight, _arenaWidth, _arenaHeight / 2 - middleAreaHeight / 2 - squareHeight * _brickHeight);
            _playStartAreaBeta = new Rect(-_arenaWidth / 2, middleAreaHeight / 2, _arenaWidth, _arenaHeight / 2 - middleAreaHeight / 2 - squareHeight * _brickHeight);
        }

        private void SetupArenaBorders()
        {
            _topWallCollider = _topWall.GetComponent<BoxCollider2D>();
            _bottomWallCollider = _bottomWall.GetComponent<BoxCollider2D>();
            _rightWallCollider = _rightWall.GetComponent<BoxCollider2D>();
            _leftWallCollider = _leftWall.GetComponent<BoxCollider2D>();

            _topWallCollider.size = new Vector2(_arenaWidth, _arenaHeight);
            _bottomWallCollider.size = new Vector2(_arenaWidth, _arenaHeight);
            _rightWallCollider.size = new Vector2(_arenaWidth, _arenaHeight * 2);
            _leftWallCollider.size = new Vector2(_arenaWidth, _arenaHeight * 2);

            _topWall.transform.position = new Vector2(0, _arenaHeight);
            _bottomWall.transform.position = new Vector2(0, -_arenaHeight);
            _rightWallCollider.transform.position = new Vector2(_arenaWidth, 0);
            _leftWallCollider.transform.position = new Vector2(-_arenaWidth, 0);
        }

        private void SetupBottomBrickWalls()
        {
            _alphaTeamBricks = new GameObject[BricksPerWall];
            _betaTeamBricks = new GameObject[BricksPerWall];

            for (int i = 0; i < BricksPerWall; i++)
            {
                _alphaTeamBricks[i] = _alphaTeamBrickWall.transform.GetChild(i).gameObject;
                _betaTeamBricks[i] = _betaTeamBrickWall.transform.GetChild(i).gameObject;
                _alphaTeamBricks[i].GetComponent<SpriteRenderer>().size = new Vector2(BrickSpriteWidth, _brickHeight * _arenaHeight / _gridHeight);
                _betaTeamBricks[i].GetComponent<SpriteRenderer>().size = new Vector2(BrickSpriteWidth, _brickHeight * _arenaHeight / _gridHeight);
                _alphaTeamBricks[i].transform.position = new Vector2(_alphaTeamBricks[i].transform.position.x, -_arenaHeight / 2 + (_arenaHeight / _gridHeight));
                _betaTeamBricks[i].transform.position = new Vector2(_betaTeamBricks[i].transform.position.x, _arenaHeight / 2 - (_arenaHeight / _gridHeight));
                _alphaTeamBricks[i].GetComponent<BoxCollider2D>().size = new Vector2(_arenaWidth / BricksPerWall, _brickHeight * _arenaHeight / _gridHeight);
                _betaTeamBricks[i].GetComponent<BoxCollider2D>().size = new Vector2(_arenaWidth / BricksPerWall, _brickHeight * _arenaHeight / _gridHeight);
            }
        }

        internal float ArenaWidth => _arenaWidth;
        internal float ArenaHeight => _arenaHeight;
        internal float ArenaScaleFactor => _arenaHeight / _gridHeight;
        internal int GridWidth => _gridWidth;
        internal int GridHeight => _gridHeight;
        internal int MiddleAreaHeight => _middleAreaHeight;

        internal Rect GetPlayerPlayArea(int teamNumber)
        {
            Rect playArea;
            switch (teamNumber)
            {
                case PhotonBattle.TeamAlphaValue:
                    playArea = _playStartAreaAlpha;
                    break;
                case PhotonBattle.TeamBetaValue:
                    playArea = _playStartAreaBeta;
                    break;
                default:
                    throw new UnityException($"Invalid Team Number {teamNumber}");
            }
            return playArea;
        }

        internal GridPos GetPlayerStartPosition(int playerPos)
        {
            GridPos startPosition;
            switch (playerPos)
            {
                case PhotonBattle.PlayerPosition1:
                    startPosition = _startPositionAlpha1;
                    break;
                case PhotonBattle.PlayerPosition2:
                    startPosition = _startPositionAlpha2;
                    break;
                case PhotonBattle.PlayerPosition3:
                    startPosition = _startPositionBeta1;
                    break;
                case PhotonBattle.PlayerPosition4:
                    startPosition = _startPositionBeta2;
                    break;
                default:
                    throw new UnityException($"Invalid player position {playerPos}");
            }
            return startPosition;
        }
    }
}
