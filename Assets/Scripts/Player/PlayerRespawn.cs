using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    public Transform spawnPoint;
    [SerializeField] private float respawnDelay = 0.0f;

    [Header("Optional safety")]
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private Behaviour[] disableOnDeath;  // e.g., movement script, shooting, etc.

    private Rigidbody2D rb;
    private bool isRespawning;
    private Health health;

    [SerializeField] int STARTING_STOCKS = 3;
    public int stocks;

    public static event Action PlayerLoss;
    public static event Action PlayerDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
        stocks = STARTING_STOCKS;
    }

    public void AttemptRespawn()
    {
        if (isRespawning) return;
        isRespawning = true;

        stocks--;
        Debug.Log($"{gameObject.name} has {stocks} stocks left!");

        PlayerDeath?.Invoke();

        if (stocks == 0)
        {
            Debug.Log($"{gameObject.name} Lost!");
            PlayerLoss?.Invoke();
            return;
        }

        StartCoroutine(RespawnRoutine());
    }
    public void ForceRespawn()
    {
        isRespawning = false;
        stocks = STARTING_STOCKS;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {

        health.ResetHealth();

        // Disable control scripts to prevent input fighting your teleport
        foreach (var b in disableOnDeath)
            if (b != null) b.enabled = false;

        // Disable collider for a moment (prevents immediate re-hit)
        if (playerCollider != null) playerCollider.enabled = false;

        // Wait if a delay is in place
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);
        else
            yield return null;

        // Reset physics BEFORE/AFTER move
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Teleport
        Vector3 targetPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        rb.position = targetPos;              
        rb.rotation = 0f;                   

        // Re-enable
        if (playerCollider != null) playerCollider.enabled = true;
        foreach (var b in disableOnDeath)
            if (b != null) b.enabled = true;

        isRespawning = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bound"))
        {
            AttemptRespawn();
        }
    }

    private void OnEnable()
    {
        GlobalHandler.RestartGame += HandleRestartGame;
    }

    private void OnDisable()
    {
        GlobalHandler.RestartGame -= HandleRestartGame;
    }

    private void HandleRestartGame()
    {
        ForceRespawn();
    }
}
