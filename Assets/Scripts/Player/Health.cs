using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] PlayerCombat pcs;
    [SerializeField] FighterMoves fmoves;
    [SerializeField] Logger logger;
    [SerializeField] PlayerMovement pms;
    public bool isBot = false;
    public bool isStunned = false;
    Rigidbody2D rb;
    public float healthPercent = 0.0f;
    private BotController botController;
    private float launchVelocityThreshold = 10f;
    private float launchChangePerFixedUpdate = 0.1f;
    public bool isLaunched = false;
    private Coroutine LaunchCo;
    private Coroutine StunCo;
    private PlayerController controller;

    private int defaultHitsunFrames = 30;
    private AudioManager audioManager => AudioManager.I;

    public static event Action UpdateUI;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (isBot)
        {
            if (!botController) botController = GetComponent<BotController>();
        }
        else
        {
            if (!pcs) pcs = GetComponent<PlayerCombat>();
            if (!fmoves) fmoves = GetComponent<FighterMoves>();
            if (!pms) pms = GetComponent<PlayerMovement>();
        }
        if (audioManager == null)
        {
            logger.log("Audio manager is null", 3);
        }
        controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if (isLaunched)
        {
            var currentVel = rb.linearVelocity;
            if (currentVel.x > 0) currentVel.x -= launchChangePerFixedUpdate;
            else currentVel.x += launchChangePerFixedUpdate;
            rb.linearVelocity = currentVel;
        }
    }

    private void Update()
    {
        
    }
    public void OnRecieveHitSfx(MoveAsset move)
    {
        if (move == null)
        {
            logger.log("Move is null", 2);
            return;
        }
        if (move.successSfx == null)
        {
            logger.log($"Success SFX null for move {move.moveName}");
            return;
        }
        audioManager.PlaySfx(move.successSfx, move.successVolume);
    }

    public void OnParried(MoveAsset move, bool isRight)
    {
        OnRecieveHitSfx(move);
        OnRecieveHitSfx(pcs.ParryAsset);
        var parryData = move.hitData;
        parryData.causesLaunch = true;
        parryData.power *= 2f;
        parryData.damage *= 0.6f;
        OnDamage(parryData, isRight);
        logger.log("Taking Parry Hit.", 1);

    }
    public void OnDamage(HitData hitData, bool isRight)
    {
        if (isStunned) return;

        fmoves.StopMove();
        pms.StopRoutines();

        ApplyHit(hitData, isRight);
        logger.log("Took Hit.", 1);

        if (isBot) return;
         // Need this for dashing,  
    }

    private Vector2 GetLaunchVector(float theta, float hyp, bool knockRight) // hyp is default knockback. ScaledLaunch is if player % is taken into account.
    {
        // Returns vector2 with x and y components of launch.

        float direction;
        if (knockRight) direction = 1f; else direction = -1f;

        var radians = ToRadians(theta);
        var yComp = Mathf.Sin(radians) * hyp;
        var xComp = Mathf.Cos(radians) * hyp * direction;

        return new Vector2(xComp, yComp);
    }

    private Vector2 ScaleLaunch(Vector2 launch, float health) // For Knockback including health %.
    {
        Vector2 scaledLaunch = launch;

        var scale = (health / 100) + 1;

        scaledLaunch.x *= scale;
        scaledLaunch.y *= scale;
        return scaledLaunch;
    }

    private void ApplyHit(HitData hitData, bool isRight)
    {
        healthPercent += hitData.damage;

        var launch = GetLaunchVector(hitData.angle, hitData.power, isRight);

        if (hitData.causesLaunch)
        {
            launch = ScaleLaunch(launch, healthPercent);
            logger.log($"Scaled Launch Vector: {launch}", 1);
            // Interrupt existing stun
            if (StunCo != null)
            {
                StopCoroutine(StunCo);
                StunCo = null;
            }

            // Interrupt existing launch
            if (LaunchCo != null)
            {
                StopCoroutine(LaunchCo);
            }
            rb.AddForce(launch, ForceMode2D.Impulse);
            LaunchCo = StartCoroutine(LaunchStun());
        }
        else
        {
            // Interrupt existing stun
            if (StunCo != null)
            {
                StopCoroutine(StunCo);
            }
            logger.log($"Unscaled Launch Vector: {launch}", 1);
            rb.AddForce(launch, ForceMode2D.Impulse);
            StunCo = StartCoroutine(StunTimer(hitData.hitstunFrames));
        }

        UpdateUI?.Invoke();
    }

    IEnumerator StunTimer(int frames)
    {
        if (frames <= 0) frames = defaultHitsunFrames;
        isStunned = true;
        logger.log($"Prearing to wait {frames} frames for stun.", 1);
        var idx = frames;
        while (idx > 0)
        {
            idx--;
            yield return null;
        }
        isStunned = false;
        logger.log("Stun Ended.", 1);
    }
    IEnumerator LaunchStun()
    {
        pms.RotateLaunchedSprite();
        while (true)
        {
            isLaunched = true;
            yield return null;

            if (LaunchThresholdMet())
            {
                logger.log("Launch Threshold Met.", 1);
                isLaunched = false;
                pms.ResetLaunchedSprite();
                controller.ClearBuffer();
                break;
            }
        }
    }

    private float ToRadians(float angle) => (float)((Math.PI / 180) * angle);

    bool LaunchThresholdMet() => (Mathf.Abs(rb.linearVelocity.x) <= launchVelocityThreshold);
    public void ResetHealth()
    {
        healthPercent = 0f;
        UpdateUI?.Invoke();
    }
}
