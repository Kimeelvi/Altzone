using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MenuUI.Scripts.SoulHome
{
    [System.Serializable]
    public class MusicObject
    {
        [SerializeField]
        private AudioClip _musicClip;
        [SerializeField]
        private string _name;

        public AudioClip MusicClip { get => _musicClip;}
        public string Name { get => _name;}
    }

    public class MusicList : MonoBehaviour
    {
        [SerializeField]
        private List<MusicObject> _musicList = new();

        public List<MusicObject> Music { get => _musicList;}

        private int _musicTrack = 0;
        [SerializeField]
        private int _defaultMusicTrack = 0;

        // Start is called before the first frame update
        void Start()
        {
            _musicTrack = _defaultMusicTrack;
        }

        public void PlayMusic()
        {
            if (_musicList.Count == 0) return;
            GetComponent<AudioSource>().PlayOneShot(_musicList[_musicTrack].MusicClip);
        }

        public void StopMusic()
        {
            GetComponent<AudioSource>().Stop();
        }

        public string GetTrackName()
        {
            if (_musicList.Count == 0) return null;
            return _musicList[_musicTrack].Name;
        }

        public string NextTrack()
        {
            if (_musicList.Count < 2) return null;
            _musicTrack++;
            if (_musicTrack >= _musicList.Count) _musicTrack = 0;

            GetComponent<AudioSource>().PlayOneShot(_musicList[_musicTrack].MusicClip);
            return _musicList[_musicTrack].Name;
        }

        public string PrevTrack()
        {
            if (_musicList.Count < 2) return null;
            _musicTrack--;
            if (_musicTrack < 0) _musicTrack = _musicList.Count - 1;

            GetComponent<AudioSource>().PlayOneShot(_musicList[_musicTrack].MusicClip);
            return _musicList[_musicTrack].Name;
        }

    }
}
