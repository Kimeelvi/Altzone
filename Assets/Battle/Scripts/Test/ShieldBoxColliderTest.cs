using System.Collections;
using Altzone.Scripts.Config;
using Battle.Scripts.Battle;
using Battle.Scripts.Battle.Game;
using Battle.Scripts.Battle.Players;
using UnityConstants;
using UnityEngine;

namespace Battle.Scripts.Test
{
    public class ShieldBoxColliderTest : MonoBehaviour
    {
        // Serialized Fields
        [SerializeField] private float _bounceAngle;

        private GridManager _gridManager;
        private PlayerActor _playerActor;
        private Transform _transform;
        private Collider2D _collider;
        private float _attackMultiplier;

        // Debug
        private const string DEBUG_LOG_BALL_COLLISION = "[{0:000000}] [BATTLE] [SHIELD BOX COLLIDER] Ball collision: ";
        private SyncedFixedUpdateClockTest _syncedFixedUpdateClock; // only needed for logging time

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _transform = GetComponent<Transform>();
            _attackMultiplier = GameConfig.Get().Variables._playerAttackMultiplier;
            _playerActor = transform.root.GetComponent<PlayerActor>();
            _gridManager = Context.GetGridManager;

            // debug
            _syncedFixedUpdateClock = Context.GetSyncedFixedUpdateClock;
        }

        private IEnumerator OnTriggerEnter2D(Collider2D collider)
        {
            var otherGameObject = collider.gameObject;
            if (otherGameObject.CompareTag(Tags.Ball))
            {
                var rb = otherGameObject.GetComponentInParent<Rigidbody2D>();
                var gridPos = _gridManager.WorldPointToGridPosition(rb.position);
                rb.position = _gridManager.GridPositionToWorldPoint(gridPos);
                var angle = _transform.rotation.eulerAngles.z + _bounceAngle;
                Debug.Log(string.Format(DEBUG_LOG_BALL_COLLISION + "shield angle {1}", _syncedFixedUpdateClock.UpdateCount, angle));
                var rotation = Quaternion.Euler(0, 0, angle);
                Debug.Log(string.Format(DEBUG_LOG_BALL_COLLISION + "rotation {1}", _syncedFixedUpdateClock.UpdateCount, rotation));
                rb.velocity = rotation * Vector2.up * _attackMultiplier;
                Debug.Log(string.Format(DEBUG_LOG_BALL_COLLISION + "velocity {1}", _syncedFixedUpdateClock.UpdateCount, rb.velocity));
                if (_playerActor != null)
                {
                    _playerActor.ShieldHit(1);
                }
                _collider.enabled = false;
                yield return new WaitForSeconds(.1f);
                _collider.enabled = true;
            }

        }
    }
}
