using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [Header("UI")]
    public GameObject nickNameUI;
    public GameObject connectingUI;

    [Header("Room Settings")]
    public string roomName = "test";
    public string gameSceneName = "Level2";
    public byte maxPlayersPerRoom = 10;

    private string nickName = "unnamed";
    private bool attemptJoinOnConnect = false;

    private void Awake()
    {
        Instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void SetNickname(string playerName)
    {
        nickName = string.IsNullOrWhiteSpace(playerName) ? "unnamed" : playerName.Trim();
    }

    public void OnJoinButtonPressed()
    {
        Debug.Log("Connecting...");

        if (nickNameUI != null)
        {
            nickNameUI.SetActive(false);
        }

        if (connectingUI != null)
        {
            connectingUI.SetActive(true);
        }

        PhotonNetwork.NickName = nickName;

        bool readyForMatchmaking = PhotonNetwork.IsConnectedAndReady &&
                                   (PhotonNetwork.Server == ServerConnection.MasterServer || PhotonNetwork.InLobby);

        if (readyForMatchmaking)
        {
            JoinOrCreateConfiguredRoom();
            return;
        }

        attemptJoinOnConnect = true;
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster()
    {
        if (attemptJoinOnConnect)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        if (attemptJoinOnConnect)
        {
            attemptJoinOnConnect = false;
            JoinOrCreateConfiguredRoom();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room '{PhotonNetwork.CurrentRoom?.Name}'. Loading game scene '{gameSceneName}'.");

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Join room failed: {returnCode} - {message}");
        ShowJoinUI();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Create room failed: {returnCode} - {message}");
        ShowJoinUI();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from Photon: {cause}");
        ShowJoinUI();
    }

    private void JoinOrCreateConfiguredRoom()
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom
        };

        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    private void ShowJoinUI()
    {
        if (nickNameUI != null)
        {
            nickNameUI.SetActive(true);
        }

        if (connectingUI != null)
        {
            connectingUI.SetActive(false);
        }
    }
}
