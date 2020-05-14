using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

namespace me.loganfuller.multiplayerfps
{
    public class Launcher : MonoBehaviourPunCallbacks
    {

        public TMP_Text status;
        public Button confirmSensitivity;
        public TMP_InputField sensitivityInput;
        public int sensitivity;

        public void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            Connect();
        }

        public void Connect()
        {
            status.SetText("Connecting...");
            Debug.Log("Connecting to PUN server...");
            PhotonNetwork.GameVersion = "0.0.1";
            PhotonNetwork.ConnectUsingSettings();
        }
        public override void OnConnectedToMaster()
        {
            Join();

            base.OnConnectedToMaster();
        }

        public void Join()
        {
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinedRoom()
        {
            StartGame();

            base.OnJoinedRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("OnJoinRandomFailed: " + message);
            Create();

            base.OnJoinRandomFailed(returnCode, message);
        }

        public void Create()
        {
            PhotonNetwork.CreateRoom("");
        }

        public void StartGame()
        {
            if(PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                PhotonNetwork.LoadLevel(1);
            }
        }

        public void updateSensitivity()
        {
            sensitivity = int.Parse(sensitivityInput.text);
            Debug.Log(sensitivity);

            PlayerPrefs.SetInt("sensitivity", sensitivity);
        }
    }
}
