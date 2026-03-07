using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PlayerHealth : MonoBehaviourPun
{
    public float maxHealth = 100f;
    public float health;
    public PlayerStateMachine _playerStateMachine;
    public bool isLocalPlayer;

    [Header("UI")]
    public HealthBarWorld healthBar;

    void Start()
    {
        health = maxHealth;

        if (_playerStateMachine == null)
        {
            _playerStateMachine = GetComponent<PlayerStateMachine>();
        }

        if (healthBar != null)
        {
            healthBar.SetHealth(health, maxHealth);
        }
    }

    [PunRPC]
    public void TakeDamage(int damageAmount)
    {
        if (_playerStateMachine != null && _playerStateMachine.isShieldActive)
        {
            Debug.Log("Shield is active! No damage taken.");
            return;
        }

        health -= damageAmount;
        health = Mathf.Max(health, 0f);
        Debug.Log("Player health: " + health);

        if (healthBar != null)
        {
            healthBar.SetHealth(health, maxHealth);
        }

        if (health <= 0f)
        {
            Debug.Log("Player died!");
            Die();
        }
    }

    void Die()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        StartCoroutine(RespawnAfterDelay(5f));
        if (gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameSceneSpawner spawner = FindObjectOfType<GameSceneSpawner>();
        if (spawner != null)
        {
            spawner.ForceRespawnLocalPlayer();
        }
    }
}
