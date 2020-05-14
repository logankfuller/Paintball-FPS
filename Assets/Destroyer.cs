using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Destroyer : MonoBehaviourPunCallbacks
{
    [PunRPC]
    public void DestroySplat(float timeToDestroy)
    {
        StartCoroutine(Destroy(timeToDestroy));
    }

    IEnumerator Destroy(float timeToDestroy)
    {
        yield return new WaitForSeconds(timeToDestroy);
        if(photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void Start()
    {
        photonView.RPC("DestroySplat", RpcTarget.All, 10f);
    }
}
