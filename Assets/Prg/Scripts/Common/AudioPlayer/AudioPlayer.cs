using UnityEngine;

namespace Prg.Scripts.Common.AudioPlayer
{
    /// <summary>
    /// Plays specific or random audio clips from an array.
    /// </summary>
    ///
    /// @par Unity
    /// Require Components:
    ///   - <code><a href="https://docs.unity3d.com/ScriptReference/AudioSource.html">AudioSource</a></code>
    ///   .
    /// @n
    /// Serialized Fields / Inspector:
    ///   - @c enum StartSetting ( None, PlaySelected, PlayRandom )
    ///   - @c enum LoopSetting ( False, True, LetAudioSourceDecide )
    ///   - <code><a href="https://docs.unity3d.com/ScriptReference/AudioClip.html">AudioClip</a>[]</code> AudioList
    ///   - @c int Selected
    ///   .
    [RequireComponent(requiredComponent: typeof(AudioSource))]
    public class AudioPlayer : MonoBehaviour
    {
        // Serialized Fields
        [SerializeField] private StartSetting _startSetting;
        [SerializeField] private LoopSetting _loopSetting;
        [SerializeField] private AudioClip[] _audioList;
        [SerializeField] private int _selected;

        // Public Properties
        public bool IsPlaying => _audioSource.isPlaying;
        public bool Loop { get => _audioSource.loop; set => _audioSource.loop = value; }
        public int SelectedIndex
        {
            get => _selected;
            set
            {
                _selected = value;
                _audioSource.clip = _audioList[_selected];
            }
        }

        #region Public Methods

        /// <summary>
        /// Plays currently selected audio clip.
        /// </summary>
        public void Play()
        {
            _audioSource.Stop();
            _audioSource.Play();
        }

        /// <summary>
        /// Selects an audio clip and plays it.
        /// </summary>
        /// <param name="index">Index of the audio clip.</param>
        public void Play(int index)
        {
            _audioSource.Stop();
            _selected = index;
            _audioSource.clip = _audioList[_selected];
            _audioSource.Play();
        }

        /// <summary>
        /// Selects random audio clip and plays it.
        /// </summary>
        public void PlayRandom()
        {
            _audioSource.Stop();
            _selected = Random.Range(0, _audioList.Length);
            _audioSource.clip = _audioList[_selected];
            _audioSource.Play();
        }

        /// <summary>
        /// Stops playing audio clip.
        /// </summary>
        public void Stop()
        {
            _audioSource.Stop();
        }

        #endregion Public Methods

        // Private Enums
        private enum StartSetting { None, PlaySelected, PlayRandom }
        private enum LoopSetting { False, True, LetAudioSourceDecide }

        // Components
        AudioSource _audioSource;

        void Start()
        {
            // get components
            _audioSource = GetComponent<AudioSource>();

            switch (_loopSetting)
            {
                case LoopSetting.False:
                    _audioSource.loop = false;
                    break;

                case LoopSetting.True:
                    _audioSource.loop = true;
                    break;

                case LoopSetting.LetAudioSourceDecide:
                    break;
            }

            switch (_startSetting)
            {
                case StartSetting.None:
                    break;

                case StartSetting.PlaySelected:
                    Play(_selected);
                    break;

                case StartSetting.PlayRandom:
                    PlayRandom();
                    break;
            }
        }
    }
}
