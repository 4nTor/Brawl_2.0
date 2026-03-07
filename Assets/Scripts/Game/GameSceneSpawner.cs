using Cinemachine;
using Photon.Pun;
using UnityEngine;

public class GameSceneSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn")]
    public string playerPrefabName = "ThirdPersonController";
    public Transform spawnPoint;

    [Header("Camera")]
    public CinemachineFreeLook freeLook;

    private bool hasSpawned;

    private void Start()
    {
        TrySpawnLocalPlayer();
    }

    public override void OnJoinedRoom()
    {
        TrySpawnLocalPlayer();
    }

    public void ForceRespawnLocalPlayer()
    {
        hasSpawned = false;
        TrySpawnLocalPlayer();
    }

    private void TrySpawnLocalPlayer()
    {
        if (hasSpawned || !PhotonNetwork.InRoom)
        {
            return;
        }

        if (HasOwnedPlayerInstance())
        {
            hasSpawned = true;
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject spawnedPlayer = PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);
        hasSpawned = true;

        PlayerHealth health = spawnedPlayer.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.isLocalPlayer = true;
        }

        PhotonView view = spawnedPlayer.GetComponent<PhotonView>();
        if (view != null)
        {
            view.RPC("SetPlayerName", RpcTarget.AllBuffered, PhotonNetwork.NickName);
        }

        if (freeLook != null)
        {
            Transform lookTarget = spawnedPlayer.transform.childCount > 1 ? spawnedPlayer.transform.GetChild(1) : spawnedPlayer.transform;
            freeLook.Follow = lookTarget;
            freeLook.LookAt = lookTarget;
        }
    }

    private bool HasOwnedPlayerInstance()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in views)
        {
            if (!view.IsMine)
            {
                continue;
            }

            if (view.gameObject.name.StartsWith(playerPrefabName))
            {
                return true;
            }
        }

        return false;
    }
}
