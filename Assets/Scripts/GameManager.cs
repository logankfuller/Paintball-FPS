using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace me.loganfuller.multiplayerfps
{
    public class GameManager : MonoBehaviour
    {
        public string playerPrefab;
        public Transform[] spawnPoints;

        private void Start()
        {
            Spawn();
        }

        public void Spawn()
        {
            Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            PhotonNetwork.Instantiate(playerPrefab, spawn.position, spawn.rotation);
        }
    }
}
