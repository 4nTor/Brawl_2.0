using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Cinemachine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;
    // Start is called before the first frame update
    [Header("Player Object")]
    public GameObject player;

    [Header("Player Spawn Point")]
    public Transform spanPoint;

    [Header("Free Look Camera")]
    public CinemachineFreeLook freeLook;

    [Header("Camera UI")]
    public GameObject roomCam;

    [Header("UI")]
    public GameObject nickNameUI;
    public GameObject connectingUI;

    [Header("Room Name")]
    public string roomName = "test";

    string nickName = "unnamed";
    private void Awake()
    {
        Instance = this;
    }
    public void SetNickname(string _name)
    {
        nickName = _name;
    }
    private bool attemptJoinOnConnect = false;

    public void OnJoinButtonPressed()
    {
        Debug.Log(message: "Connecting. . . ");
        Debug.Log(roomName);

        nickNameUI.SetActive(false);
        connectingUI.SetActive(true);

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.NetworkClientState != ClientState.Disconnecting)
        {
            PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions(), null);
        }
        else
        {
            attemptJoinOnConnect = true;
            Debug.Log("Waiting for connection to Master Server...");
            
            // Should usually be handled by RoomList, but ensure we don't stall
            if (!PhotonNetwork.IsConnected && !PhotonNetwork.IsConnectedAndReady)
            {
                // Optionally trigger connection if not started
                // PhotonNetwork.ConnectUsingSettings(); 
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        if (attemptJoinOnConnect)
        {
            attemptJoinOnConnect = false;
            Debug.Log("Connected to Master, Joining Room...");
            PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions(), null);
        }
    }

    public override void OnJoinedLobby()
    {
        if (attemptJoinOnConnect)
        {
            attemptJoinOnConnect = false;
            Debug.Log("Joined Lobby, Joining Room...");
            PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions(), null);
        }
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("Room Joined");
        roomCam.SetActive(false);
        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        GameObject _player = PhotonNetwork.Instantiate(player.name, spanPoint.position, Quaternion.identity);
        _player.GetComponent<PlayerHealth>().isLocalPlayer = true;
        PhotonView view = _player.GetComponent<PhotonView>();
        view.RPC("SetPlayerName", RpcTarget.AllBuffered, nickName);
        if (view != null && view.IsMine && freeLook != null)
        {
            Transform lookAt = _player.transform.GetChild(1);
            freeLook.Follow = lookAt;
            freeLook.LookAt = lookAt;
        }
    }
}
