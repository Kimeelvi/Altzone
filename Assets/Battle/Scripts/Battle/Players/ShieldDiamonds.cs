using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Battle.Scripts.Battle.Game;
using Photon.Pun;

namespace Battle.Scripts.Battle.Players
{
    internal class ShieldDiamonds : MonoBehaviour
    {
        public Pickup Pickup;
        private int TeamNumber;
        public PickupDiamondsBall PickupDiamondsBall;

        void Start()
        {
            if (!PhotonNetwork.InRoom)
            {
                enabled = false;
                return;
            }
            
            TeamNumber = Pickup.TeamNumber;
            PickupDiamondsBall = GameObject.FindGameObjectWithTag("BallRigidBody").GetComponent<PickupDiamondsBall>();
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            var otherGameObject = collider.gameObject;
            if (otherGameObject.CompareTag("Ball") && PhotonNetwork.IsMasterClient)
            {
                if (PickupDiamondsBall.TeamNumber != TeamNumber)
                {
                    //PickupDiamondsBall.TeamNumber = TeamNumber;
                    PickupDiamondsBall.TeamNumberChange(TeamNumber);
                }
            }
        }
    }
}
