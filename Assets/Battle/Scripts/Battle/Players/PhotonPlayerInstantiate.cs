using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Battle.Scripts.Battle.Players
{
    public class PhotonPlayerInstantiate : MonoBehaviour
    {
        [Header("Prefab Settings"), SerializeField] private PlayerDriverPhoton _photonPrefab;

        private void OnEnable()
        {
            if (PhotonNetwork.InRoom)
            {
                OnLocalPlayerReady();
            }
        }

        private void OnLocalPlayerReady()
        {
            // Not important but give one frame slack for local player instantiation
            StartCoroutine(OnLocalPlayerReadyForPlay());
        }

        private IEnumerator OnLocalPlayerReadyForPlay()
        {
            yield return null;
            PhotonNetwork.Instantiate(_photonPrefab.name, Vector3.zero, Quaternion.identity);
        }
    }
}
