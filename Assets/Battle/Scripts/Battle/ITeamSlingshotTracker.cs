using UnityEngine;

namespace Battle.Scripts.Battle
{
    internal interface ITeamSlingshotTracker
    {
        int TeamNumber { get; }
        int PlayerCount { get; }
        float SqrDistance { get; }

        Transform Player1Transform { get; }
        Transform Player2Transform { get; }

        public float StopTracking();
    }
}