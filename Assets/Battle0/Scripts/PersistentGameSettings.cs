using UnityEngine;

namespace Battle0.Scripts
{
    /// <summary>
    /// Editable persistent settings for the game.
    /// </summary>
    /// <remarks>
    /// Create these in <c>Resources</c> folder with name "PersistentGameSettings" so they can be loaded when needed first time.
    /// </remarks>
    //[CreateAssetMenu(menuName = "ALT-Zone/PersistentGameSettings", fileName = "PersistentGameSettings")]
    public class PersistentGameSettings : ScriptableObject
    {
        [Header("Game Features")] public GameFeatures _features;
        [Header("Game Constraints")] public GameConstraints _constraints;
        [Header("Game Variables")] public GameVariables _variables;
        [Header("Battle UI")] public BattleUiConfig _battleUiConfig;
        [Header("Game Prefabs")] public GamePrefabs _prefabs;
        [Header("Game Input")] public GameInput _input;
    }
}