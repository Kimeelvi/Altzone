using Battle0.Scripts.Battle.interfaces;
using UnityEngine;
using UnityEngine.Assertions;

namespace Battle0.Scripts.Battle.Players2
{
    /// <summary>
    /// Manager for local shield state.
    /// </summary>
    internal class PlayerShield : IPlayerShield
    {
        private readonly ShieldConfig _config;
        private readonly ParticleSystem _shieldHitEffect;
        private readonly int _maxRotationIndex;

        private string _shieldName;
        private BattlePlayMode _playMode;

        private GameObject _shield;
        private Collider2D _collider;

        public bool IsVisible { get; private set; }
        public bool CanRotate => RotationIndex < _maxRotationIndex;
        public int RotationIndex { get; private set; }

        public string StateString => $"{(IsVisible ? "V" : "H")} R{RotationIndex} {(_collider.enabled ? "col" : "~~~")}";

        public PlayerShield(ShieldConfig config)
        {
            _config = config;
            _shieldHitEffect = _config._shieldHitEffect;
            _maxRotationIndex = _config.Shields.Length - 1;
        }

        private void SetupShield(bool isShieldRotated, bool isVisible, int rotationIndex)
        {
            Debug.Log($"{_shieldName} isShieldRotated {isShieldRotated} isVisible {isVisible} rotationIndex {rotationIndex}");
            var shields = _config.Shields;
            var pivot = _config._particlePivot;
            for (var i = 0; i < shields.Length; ++i)
            {
                var shield = shields[i];
                shield.Rotate(isShieldRotated);
                pivot.Rotate(isShieldRotated);
                if (i == RotationIndex)
                {
                    _shield = shield.gameObject;
                    _shield.SetActive(true);
                    _collider = shield.GetComponent<Collider2D>();
                }
                else
                {
                    shield.gameObject.SetActive(false);
                    shield.GetComponent<Collider2D>().enabled = false;
                }
            }
            IsVisible = isVisible;
            RotationIndex = rotationIndex;
        }

        void IPlayerShield.Setup(string shieldName, BattlePlayMode playMode, bool isShieldRotated, bool isVisible, int rotationIndex)
        {
            _shieldName = shieldName;
            SetupShield(isShieldRotated, isVisible, rotationIndex);
            ((IPlayerShield)this).SetVisibility(isVisible);
            ((IPlayerShield)this).SetPlayMode(playMode);
            ((IPlayerShield)this).SetRotation(rotationIndex);
        }

        void IPlayerShield.SetVisibility(bool isVisible)
        {
            Debug.Log($"{_shieldName} mode {_playMode} isVisible {IsVisible} <- {isVisible} collider {_collider.enabled}");
            IsVisible = isVisible;
            _shield.SetActive(IsVisible);
        }

        void IPlayerShield.SetPlayMode(BattlePlayMode playMode)
        {
            Debug.Log(
                $"{_shieldName} isVisible {IsVisible} mode {_playMode} <- {playMode} rotation {RotationIndex} collider {_collider.enabled}");
            _playMode = playMode;
            switch (_playMode)
            {
                case BattlePlayMode.Normal:
                case BattlePlayMode.Frozen:
                    _collider.enabled = true;
                    break;
                case BattlePlayMode.Ghosted:
                    _collider.enabled = false;
                    break;
                default:
                    throw new UnityException($"invalid playmode {_playMode}");
            }
            _shield.SetActive(IsVisible);
        }

        void IPlayerShield.SetRotation(int rotationIndex)
        {
            Debug.Log($"{_shieldName} mode {_playMode} rotation {RotationIndex} <- {rotationIndex} collider {_collider.enabled}");
            Assert.IsTrue(rotationIndex >= 0 && rotationIndex <= _maxRotationIndex,
                "rotationIndex >= 0 && rotationIndex <= _maxRotationIndex");
            if (RotationIndex == rotationIndex)
            {
                return;
            }
            RotationIndex = rotationIndex;
            _shield.SetActive(false);
            _shield = _config.Shields[RotationIndex].gameObject;
            _collider = _shield.GetComponent<Collider2D>();
            _shield.SetActive(IsVisible);
        }

        void IPlayerShield.PlayHitEffects()
        {
            _shieldHitEffect.Play();
        }
    }
}