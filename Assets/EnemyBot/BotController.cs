using System.Collections;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public Transform bulletSpawn;
    public GameObject bulletPrefab;
    public float SHOOT_COOLDOWN = 3f;
    public StateType state;
    [SerializeField] Logger logger;
    public Transform centerPos;
    public Vector2 centerPosVector;
    Rigidbody2D rb;
    private float resumeLength = 1.5f; 
    private Health health;
    private Coroutine ResetCo;
    private Coroutine PunchCo;
    private PlayerCombat pcs;
    private float punchWaitTime = 3f;

    private void Awake()
    {
        pcs = GetComponent<PlayerCombat>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        if (state == StateType.StandMidAndReset) return;
        else if (state == StateType.Punch) PunchCo = StartCoroutine(PunchRoutine());
    }

    public void ShootBullet() // Animation Event
    {
        return;
    }

    IEnumerator NextShot()
    {
        animator.enabled = false;

        yield return new WaitForSeconds(SHOOT_COOLDOWN);

        animator.enabled = true;
        logger.log("Reset Position.");
    }
    public enum StateType
    {
        Shoot,
        Punch,
        StandMidAndReset
    }

    IEnumerator StartResumeTimer()
    {
        yield return new WaitForSeconds(resumeLength);

        ResetPos();

        ResetTimer();
    }

    void ResetPunch()
    {
        PunchCo = StartCoroutine(PunchRoutine());
    }

    void ResetTimer()
    {
        // Stop the *running instance* (if any)
        if (ResetCo != null)
        {
            StopCoroutine(ResetCo);
            ResetCo = null;
        }

        // Start a fresh timer
        ResetCo = StartCoroutine(StartResumeTimer());
    }


    private void ResetPos()
    {
        logger.log("Postion Reset.", 1);
        transform.position = centerPos.position;
        health.healthPercent = 0.0f;
    }

    IEnumerator PunchRoutine()
    {
        pcs.NeutralLight();

        yield return new WaitForSeconds(punchWaitTime);

        ResetPunch();
    }
}
