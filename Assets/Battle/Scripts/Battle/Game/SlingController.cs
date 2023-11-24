using System.Collections.Generic;
using Battle.Scripts.Battle;
using UnityEngine;
using Prg.Scripts.Common.PubSub;
using System.Collections;

#region Message Classes

public class SlingControllerReady
{ }

public class BallSlinged
{
    public readonly int SlingingTeamNumber;

    public BallSlinged(int slingingTeamNumber)
    {
        SlingingTeamNumber = slingingTeamNumber;
    }
}

#endregion Message Classes

[RequireComponent(typeof(AudioSource))]
public class SlingController : MonoBehaviour
{
    // Serialized Fields
    [Header("Sling")]
    [SerializeField] private float _minDistance;
    [SerializeField] private float _maxDistance;
    [SerializeField] private float _slingMinSpeed;
    [SerializeField] private float _slingMaxSpeed;
    [SerializeField] private int _slingDefaultgSpeed;
    [SerializeField] private float _ballStartingDistance;
    [Header("Indicator")]
    [SerializeField] private float _wingMinAngleDegrees;
    [SerializeField] private float _wingMaxAngleDegrees;
    [Header("Game Objects")]
    [SerializeField] private BallHandlerTest _ball;
    [SerializeField] private SlingIndicator _slingIndicator;

    // Public Properties
    public bool SlingActive => _slingActive;

    #region Public Methods

    /// <summary>
    /// <para>Activates a teams sling.</para>
    /// <para>
    /// Slings can not be activated when a sling is already active.
    /// The sling stays active until it is launched after which it is deactivated.
    /// Deactivating the sling without launching is not implemented.
    /// </para>
    /// <para>
    /// This method only effects slings on this client.
    /// </para>
    /// </summary>
    /// <param name="teamNumber">The PhotonBattle team number of the team which sling is going to be activated</param>
    public void SlingActivate(int teamNumber)
    {
        if (_slingActive)
        {
            Debug.LogWarning("Sling already active");
            return;
        }

        switch (teamNumber)
        {
            case PhotonBattle.TeamAlphaValue:
                _currentTeam = _teams[TEAM_ALPHA];
                break;

            case PhotonBattle.TeamBetaValue:
                _currentTeam = _teams[TEAM_BETA];
                break;
        }
        _slingActive = true;
        _slingIndicator.SetPusherPosition(0);
    }

    /// <summary>
    /// <para>
    /// Launches the sling that is currently active.
    /// </para>
    /// A sling needs to be already be active.
    /// The sling is deactivated after launching.
    /// Deactivating the sling without launching is not implemented.
    /// <para>
    /// </para>
    /// <para>
    /// This method only effects slings on this client.
    /// </para>
    /// </summary>
    public void SlingLaunch()
    {
        if (!_slingActive)
        {
            Debug.LogWarning("Sling not active");
            return;
        }

        int teamNumber = _currentTeam.TeamNumber;
        Vector3 launchPosition;
        Vector3 launchDirection;
        float launchSpeed;

        if (_currentTeam.Distance >= 0)
        {
            launchSpeed = ClampAndRemap(_currentTeam.Distance, _minDistance, _maxDistance, _slingMinSpeed, _slingMaxSpeed);
            launchDirection = _currentTeam.LaunchDirection;
            launchPosition = _currentTeam.FrontPlayer.position + launchDirection * _ballStartingDistance;

            float launchDuration = _currentTeam.Distance / launchSpeed;

            _syncedFixedUpdateClock.ExecuteOnUpdate(_syncedFixedUpdateClock.UpdateCount + Mathf.Max(_syncedFixedUpdateClock.ToUpdates(launchDuration), 1), -1, () =>
            {
                SlingLaunchPrivate(teamNumber, launchPosition, launchDirection, launchSpeed);
            });

            StartCoroutine(MovePusherCoroutine(launchDuration));
        }
        else
        {
            teamNumber = PhotonBattle.NoTeamValue;
            launchDirection = new Vector3(0.5f, 0.5f);
            launchSpeed = _slingDefaultgSpeed;
            launchPosition = Vector3.zero;

            SlingLaunchPrivate(teamNumber, launchPosition, launchDirection, launchSpeed);
        }
    }

    #endregion Public Methods

    // State
    private bool _slingActive = false;

    // Teams
    private const int TEAM_ALPHA = 0;
    private const int TEAM_BETA  = 1;
    private class Team
    {
        public int TeamNumber;
        public List<Transform> List = new();
        public Transform FrontPlayer;
        public Transform BackPlayer;
        public float Distance;
        public Vector3 LaunchDirection;
    };
    private Team[] _teams;
    private Team _currentTeam;

    // Components
    private AudioSource _audioSource;

    private SyncedFixedUpdateClockTest _syncedFixedUpdateClock;

    void Start()
    {
        // get components
        _audioSource = GetComponent<AudioSource>();

        // subscribe to messages
        this.Subscribe<TeamsAreReadyForGameplay>(OnTeamsReadyForGameplay);

        _syncedFixedUpdateClock = Context.GetSyncedFixedUpdateClock;
    }

    #region Message Listeners
    private void OnTeamsReadyForGameplay(TeamsAreReadyForGameplay data)
    {
        _teams = new Team[2];

        _teams[TEAM_ALPHA] = new();
        _teams[TEAM_ALPHA].TeamNumber = PhotonBattle.TeamAlphaValue;
        foreach (IDriver driver in data.TeamAlpha.GetAllDrivers()) _teams[TEAM_ALPHA].List.Add(driver.ActorTransform);

        _teams[TEAM_BETA] = new();
        _teams[TEAM_BETA].TeamNumber = PhotonBattle.TeamBetaValue;
        foreach (IDriver driver in data.TeamBeta.GetAllDrivers()) _teams[TEAM_BETA].List.Add(driver.ActorTransform);

        this.Publish(new SlingControllerReady());
    }
    #endregion Message Listeners

    #region Update

    void Update()
    {
        if (!_slingActive) return;
        SlingUpdate();
        SlingIndicatorUpdate();
    }

    private void SlingUpdate()
    {
        _currentTeam.Distance = -1;

        if (_currentTeam.List.Count != 2) return;

        float player0YDistance = Mathf.Abs(_currentTeam.List[0].position.y);
        float player1YDistance = Mathf.Abs(_currentTeam.List[1].position.y);

        if (player0YDistance == player1YDistance) return;

        if (player0YDistance < player1YDistance)
        {
            _currentTeam.FrontPlayer = _currentTeam.List[0];
            _currentTeam.BackPlayer = _currentTeam.List[1];
        }
        else
        {
            _currentTeam.FrontPlayer = _currentTeam.List[1];
            _currentTeam.BackPlayer = _currentTeam.List[0];
        }

        Vector3 launchVector = _currentTeam.FrontPlayer.position - _currentTeam.BackPlayer.position;
        _currentTeam.Distance = launchVector.magnitude;
        _currentTeam.LaunchDirection = launchVector / _currentTeam.Distance;
    }

    private void SlingIndicatorUpdate()
    {
        if (_currentTeam.Distance >= 0)
        {
            float length = _currentTeam.Distance;
            _slingIndicator.SetPosition(_currentTeam.BackPlayer.position + (_currentTeam.LaunchDirection * (length * 0.5f)));
            _slingIndicator.SetRotationRadians(Mathf.Atan2(_currentTeam.LaunchDirection.y, _currentTeam.LaunchDirection.x)); 
            _slingIndicator.SetLength(length);
            //_slingIndicator.SetWingAngleDegrees(ClampAndRemap(_currentTeam.Distance, _minDistance, _maxDistance, _wingMinAngleDegrees, _wingMaxAngleDegrees));
            _slingIndicator.SetShow(true);
        }
        else
        {
            _slingIndicator.SetShow(false);
        }
    }

    #endregion Update

    #region Private Methods

    private void SlingLaunchPrivate(int teamNumber, Vector3 position, Vector3 direction, float speed)
    {
        _slingActive = false;
        _slingIndicator.SetShow(false);

        _ball.Launch(position, direction, speed);
        _audioSource.Play();
        this.Publish(new BallSlinged(teamNumber));
    }

    private IEnumerator MovePusherCoroutine(float duration)
    {
        float speed = 1 / duration;
        float pusherPosition = 0;
        while (pusherPosition < 1)
        {
            yield return null;
            pusherPosition += speed * Time.deltaTime;
            _slingIndicator.SetPusherPosition(pusherPosition);
        }
    }

    #region Private Utility Methods
    private float ClampAndRemap(float value, float min, float max, float newMin, float newMax)
    {
        return
            (Mathf.Clamp(value, min, max) - min)
            / (max - min)
            * (newMax - newMin)
            + newMin;
    }
    #endregion Private Utility Methods

    #endregion Private Methods
}