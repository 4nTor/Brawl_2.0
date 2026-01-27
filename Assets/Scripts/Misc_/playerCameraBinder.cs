using Photon.Pun;
using UnityEngine;

public class PlayerCameraBinder : MonoBehaviourPun
{
    void Awake()
    {
        if (!photonView.IsMine) return;
ThirdPersonCamera cam = FindObjectOfType<ThirdPersonCamera>(true);
        
        if (cam == null)
        {
            Debug.LogError("❌ ThirdPersonCamera not found (even when disabled)");
            return;
        }

        cam.gameObject.SetActive(true);
        cam.SetTarget(transform);
    }
}

