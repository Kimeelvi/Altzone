using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Battle.Scripts.Battle;
using Battle.Scripts.Battle.Game;
using Prg.Scripts.Common.PubSub;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Threading;

namespace Battle.Scripts.Battle.Players
{
    internal class PlayerClassEgotism : MonoBehaviour, IPlayerClass
    {
        // Serialized Fields
        [SerializeField] private GameObject _positionSprite;
        [SerializeField] private LayerMask _collisionLayer;
        [SerializeField] private float _maxDistance;
        [SerializeField] private int _maxReflections;
        [SerializeField] private int _time;
        [SerializeField] private List<Sprite> _spriteList;
        [SerializeField] int _pointStep;

        [Obsolete("SpecialAbilityOverridesBallBounce is deprecated, please use return value of OnBallShieldCollision instead.")]
        public bool SpecialAbilityOverridesBallBounce => false;

        public bool OnBallShieldCollision()
        { return true; }

        public void OnBallShieldBounce()
        {
            if (_isOnLocalTeam)
            {
                _timer = _time;
            }
        }

        [Obsolete("ActivateSpecialAbility is deprecated, please use OnBallShieldCollision and/or OnBallShieldBounce instead.")]
        public void ActivateSpecialAbility()
        {
            Debug.Log(string.Format(DEBUG_LOG_NAME_AND_TIME + "Special ability activated", _syncedFixedUpdateClock.UpdateCount));
        }

        private GridManager _gridManager;
        private Rigidbody2D _rb;
        private int _timer;
        private LineRenderer _lineRenderer;
        private List<GameObject> _positionSprites;
        private List<TrailSprite> _trailSprites;
        private bool _isOnLocalTeam = false;
        private int _trailSpritesAmount;
        private class TrailSprite
        {
            public readonly GameObject GameObject;
            public int Timer;

            public TrailSprite(GameObject gameObject, Vector3 position, Sprite sprite, int timer)
            {
                GameObject = Instantiate(gameObject, position, Quaternion.identity);
                Debug.Log(DEBUG_LOG_NAME + GameObject);
                GameObject.SetActive(true);
                GameObject.GetComponent<SpriteRenderer>().sprite = sprite;
                Timer = timer;
            }
        }

        // Debug
        private const string DEBUG_LOG_NAME = "[BATTLE] [PLAYER CLASS EGOTISM] ";
        private const string DEBUG_LOG_NAME_AND_TIME = "[{0:000000}] " + DEBUG_LOG_NAME;
        private SyncedFixedUpdateClock _syncedFixedUpdateClock; // only needed for logging time
 
        private void Start()
        {
            // Get important objects
            _rb = Context.GetBallHandler.GetComponent<Rigidbody2D>();
            _lineRenderer = GetComponent<LineRenderer>();
            _gridManager = Context.GetGridManager;
            _positionSprites = new();
            _trailSprites = new();

            // Subscribe to messages
            this.Subscribe<TeamsAreReadyForGameplay>(OnTeamsAreReadyForGameplay);
           
            // Debug
            _syncedFixedUpdateClock = Context.GetSyncedFixedUpdateClock;
        }

        private void OnTeamsAreReadyForGameplay(TeamsAreReadyForGameplay data)
        {
            PlayerActor actor = transform.parent.GetComponentInParent<PlayerActor>();

            // Check if the playeractor is on the same team as the local player
            foreach (IDriver driver in data.AllDrivers)
            {
                if(driver.PlayerActor == actor)
                {
                    _isOnLocalTeam = driver.TeamNumber == data.LocalPlayer.TeamNumber;
                    break;
                }
            }
        }

        private void ProjectilePredictionUpdate()
        {
            GridPos gridPosition = null;
            Vector2 currentVelocity = GetCurrentVelocity();
            Vector2 currentPosition = GetCurrentPosition();
            gridPosition = _gridManager.WorldPointToGridPosition(_rb.position);

            Vector3 worldPosition = _gridManager.GridPositionToWorldPoint(gridPosition);
            float distance = _maxDistance;
            int reflections = 0;
            List<Vector3> positions = new();
            Vector3 pointPosition;
            Vector3 pointVelocity;

            while (distance > 0 && reflections < _maxReflections)
            {
                RaycastHit2D hit = Physics2D.Raycast(currentPosition, currentVelocity.normalized, distance, _collisionLayer);
                Debug.Log(DEBUG_LOG_NAME + "reflections " + reflections);
                Debug.Log(DEBUG_LOG_NAME + "distance " + distance);

                if (hit.collider != null)
                {
                    Debug.DrawLine(hit.point, hit.point + hit.normal, Color.green);

                    // Calculate the reflection
                    Vector2 hitPosition = hit.point;
                    Vector2 reflectionDirection = Vector2.Reflect(currentVelocity.normalized, hit.normal);
                    Debug.DrawLine(hit.point, hit.point + reflectionDirection, Color.blue);
                    gridPosition = _gridManager.WorldPointToGridPosition(hitPosition);
                    worldPosition = _gridManager.GridPositionToWorldPoint(gridPosition);

                    pointPosition = currentPosition;
                    pointVelocity = currentVelocity;

                    // Update currentPosition for next raycast
                    currentPosition = (Vector2)worldPosition + reflectionDirection.normalized * 0.1f;
                    currentVelocity = reflectionDirection * currentVelocity.magnitude;

                    // Reduce distance by the distance traveled
                    distance -= hit.distance;
                    reflections++;
                }
                else
                {
                    worldPosition = currentPosition + currentVelocity.normalized * distance;
                    pointPosition = currentPosition;
                    pointVelocity = currentVelocity;
                    distance = 0;
                }

                Debug.DrawLine(worldPosition, pointPosition, Color.red);

                float pointDistance = (worldPosition - pointPosition).magnitude;

                pointVelocity /= 50;

                int positionCount = (int)Mathf.Floor(pointDistance / pointVelocity.magnitude / _pointStep);

                Debug.Log(DEBUG_LOG_NAME + "positionCount " + positionCount);

                for (int i = 0; i < positionCount; i++)
                {
                    // Calculate the next point position based on velocity and step and add to the list of positions
                    pointPosition += pointVelocity * _pointStep;
                    positions.Add(pointPosition);
                }

                positions.Add(worldPosition);
            }

            Debug.Log(DEBUG_LOG_NAME + positions.Count);

            UpdatePredictionSprites(positions);

            UpdateTrailSprites();
        }

        private void UpdatePredictionSprites(List<Vector3> positions)
        {
            // Check if there are fewer sprite objects than position points
            if (_positionSprites.Count < positions.Count)
            {
                int difference = positions.Count - _positionSprites.Count;

                // Create new sprite objects to match the number of position
                for (int i = 0; i < difference; i++)
                {
                    _positionSprites.Add(Instantiate(_positionSprite, Vector3.zero, Quaternion.identity));
                }

                Debug.Log(DEBUG_LOG_NAME + "_positionSprite " + _positionSprites.Count);
                Debug.Log(DEBUG_LOG_NAME + "positions.Count " + positions.Count);
            }

            // Update the position and activation state of each sprite
            for (int i = 0; i < positions.Count; i++)
            {
                _positionSprites[i].transform.position = positions[i];
                _positionSprites[i].SetActive(true);
                SpriteRenderer spriteRenderer = _positionSprites[i].GetComponent<SpriteRenderer>();

                // Assign a random sprite from the spritelist
                spriteRenderer.sprite = _spriteList[UnityEngine.Random.Range(0, _spriteList.Count)];
            }

            // Check if there are more sprites than positions
            if (_positionSprites.Count > positions.Count)
            {
                int difference = _positionSprites.Count - positions.Count;

                // Deactivate excess sprite objects
                for (int i = 0; i < difference; i++)
                {
                    _positionSprites[positions.Count + i].SetActive(false);
                }
            }
        }

        private void UpdateTrailSprites()
        {
            // Check if the current timer value is a multiple of pointStep
            if (_timer % _pointStep == 0)
            {
                TrailSprite newTrailSprite = new TrailSprite(_positionSprite, GetCurrentPosition(), _spriteList[UnityEngine.Random.Range(0, _spriteList.Count)], 50);

                // Check if the trail sprite list is already full
                if (_trailSprites.Count == _trailSpritesAmount)
                {
                    _trailSprites.Add(newTrailSprite);
                }
                else
                {
                    // If the list is full, replace the oldest sprite
                    _trailSprites[_trailSpritesAmount] = newTrailSprite;
                }

                _trailSpritesAmount++;
            }

            int offset = 0;
            bool delete;

            for(int i= 0; i < _trailSpritesAmount; i++)
            {
                // Check if the sprite's timer has expired
                delete = _trailSprites[i].Timer <= 0;

                if (delete)
                {
                    // If expired, destroy the sprite's game object
                    Destroy(_trailSprites[i].GameObject);
                }
                else
                {
                    // If not expired, decrement the timer and shift the sprites position in the list if some were deleted earlier
                    _trailSprites[i].Timer--;
                    _trailSprites[i - offset] = _trailSprites[i];
                }

                // Increment offset if a sprite was deleted
                if (delete)
                {
                    offset++;
                }
            }

            // Adjust the count of trail sprites by the number of deleted sprites
            _trailSpritesAmount -= offset;
        }

        private void FixedUpdate()
        {
            if(_timer > 0)
            {
                if (GetCurrentVelocity() != Vector2.zero)
                {
                    _timer--;
                    ProjectilePredictionUpdate();
                    return;
                }
                else
                {
                    _timer = 0;
                }
            }

            for (int i = 0; i < _positionSprites.Count; i++)
            {
                _positionSprites[i].SetActive(false);
            }         
        }

        private Vector2 GetCurrentVelocity()
        {
            return _rb.velocity;
        }

        private Vector2 GetCurrentPosition()
        {
            return _rb.position;
        }
    }
}
