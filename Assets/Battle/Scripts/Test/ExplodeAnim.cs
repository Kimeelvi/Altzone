using System.Collections;
using System.Collections.Generic;
using Battle.Scripts.Battle;
using Battle.Scripts.Battle.Game;
using UnityEngine;

public class ExplodeAnim : MonoBehaviour
{
    [SerializeField] private GameObject _explotion;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Instantiate(_explotion, transform.position, transform.rotation);
        }
    }
}
