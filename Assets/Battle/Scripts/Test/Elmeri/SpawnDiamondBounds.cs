using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
 
public class SpawnDiamondBounds : MonoBehaviour 
{
    [SerializeField] GameObject Diamond;
    [SerializeField] Transform SpawnPoints;
    [SerializeField] Transform SpawnPoint;
    [SerializeField] float SpawnSpace;


    //private GameObject[] SpawnPointsArray;
    public List<float> SpawnPointsArray = new List<float>();

    public Vector3 center;
    public Vector3 size;
    public int SpawnY;

    void Start()
    {
        int i = 1;
        foreach (Transform t in SpawnPoints)
        { 
            if (t != SpawnPoint)
            {
                t.position = new Vector2(t.position.x, SpawnPoint.position.y - SpawnSpace*i); 
                i = i + 1;
            }
            SpawnPointsArray.Add(t.position.y);
            //t.gameObject.SetActive(true);
        }
        if (PhotonNetwork.IsMasterClient)   
        {
            Debug.Log("eefef");
            StartCoroutine(SpawnDiamond());
        }
        //StartCoroutine(SpawnDiamond());
    }

    public IEnumerator SpawnDiamond()
    {
        yield return new WaitForSeconds(Random.Range(5f, 10f));
        SpawnY = Random.Range(0, SpawnPointsArray.Count);
        transform.GetComponent<PhotonView>().RPC("DiamondRPC",  RpcTarget.All, SpawnY);
    }

    [PunRPC]
    private void DiamondRPC(int SpawnY)
    {
        Vector3 pos = new Vector3(SpawnPoint.position.x, SpawnPointsArray[SpawnY], Random.Range(-size.z/2, size.z/2));  //pos = center + new vector3(center.x, )...
        var DiamondParent = GameObject.Instantiate(Diamond, pos, Quaternion.Euler (0f, 0f, 90f));   // transform.TransformPoint(pos)
        DiamondParent.transform.parent = transform;
        DiamondParent.SetActive(true);
        if (PhotonNetwork.IsMasterClient) 
        {
            StartCoroutine(SpawnDiamond());
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(center, size);
    }
}