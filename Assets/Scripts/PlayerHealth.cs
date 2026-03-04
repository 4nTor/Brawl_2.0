using System.Collections;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using UnityEngine;

public class PlayerHealth : MonoBehaviourPun
{
    public float maxHealth = 100f;
    public float health;
    public PlayerStateMachine _playerStateMachine;  // Reference to your existing shield script
    public bool isLocalPlayer;

    [Header("UI")]
    public HealthBarWorld healthBar;  // Assign the World Space Canvas's HealthBarWorld component
    void Start()
    {
        health = maxHealth;

        if (_playerStateMachine == null)
            _playerStateMachine = GetComponent<PlayerStateMachine>();

        // Initialise bar at full health
        if (healthBar != null)
            healthBar.SetHealth(health, maxHealth);
    }

    [PunRPC]
    public void TakeDamage(int damageAmount)
    {
        // If shield active, ignore damage
        if (_playerStateMachine != null && _playerStateMachine.isShieldActive)
        {
            Debug.Log("Shield is active! No damage taken.");
            return;
        }

        health -= damageAmount;
        health = Mathf.Max(health, 0f);
        Debug.Log("Player health: " + health);

        // Update world-space bar
        if (healthBar != null)
            healthBar.SetHealth(health, maxHealth);

        if (health <= 0)
        {
            Debug.Log("Player died!");
            Die();
            if (isLocalPlayer) RoomManager.Instance.SpawnPlayer();
            Destroy(gameObject);
        }
    }

    void Die()
    {
        if (photonView.IsMine)
        {
            StartCoroutine(RespawnAfterDelay(5f));
            if(gameObject != null ) PhotonNetwork.Destroy(gameObject);
            
        }
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FindObjectOfType<RoomManager>().SpawnPlayer();
    }
}
