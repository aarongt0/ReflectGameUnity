using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public class PlayerCombat : MonoBehaviour
{
    #region Initial Variables
    [SerializeField] PlayerMovement pms;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Logger logger;
    [SerializeField] Health health;

    public bool IsActioning = false;

    [SerializeField] private MoveSet LightSetGround;
    [SerializeField] private MoveSet LightSetAir;
    [SerializeField] private MoveSet PowerSetGround;
    [SerializeField] private MoveSet PowerSetAir;
    [SerializeField] private MoveSet SpecialSet;
    [SerializeField] public MoveAsset ParryAsset;

    private MoveAsset currentMove;
    private HashSet<Collider2D> hitThisMove = new();

    public string Team;

    private Hurtbox hurtbox;

    public string lastMove;

    private FighterMoves fmoves;

    public LayerMask enemyLayer;

    public Dictionary<string, MoveAsset> moveByHitboxId;

    private Coroutine parryFailCo;
    // Refs
    // readonly outside of script. only settable in this script

    public float ParryCD = 1f;
    
    public float LightCD = 1f;
    public float SideLightCD { get; private set; }
    public float DownLightCD { get; private set; }
    public float NeutralLightCD { get; private set; }
    public float SideAirCD { get; private set; }
    public float NeutralAirCD { get; private set; }
    public float DownAirCD { get; private set; }

    private float parryFailSeconds = 0.3f;

    public bool _showHitboxes = true;

    public bool isParrying = false;

    public float parryWindow = 0.6f;
    #endregion
    private void Awake()
    {
        SideLightCD = LightCD;
        DownLightCD = LightCD;
        NeutralLightCD = LightCD;
        SideAirCD = LightCD;
        DownAirCD = LightCD;
        NeutralAirCD = LightCD;

        if (!pms) pms = GetComponent<PlayerMovement>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!logger) logger = GetComponent<Logger>();
        if (!fmoves) fmoves = GetComponent<FighterMoves>();
        if (!hurtbox) hurtbox = GetComponent<Hurtbox>();
    }

    private void Start()
    {

    }

    public List<MoveSet> GetMoveSets()
    {
        var moveSetsRaw = new List<MoveSet>() { LightSetGround, LightSetAir, PowerSetGround, PowerSetAir, SpecialSet };
        var moveSets = new List<MoveSet>();
        foreach (var moveSet in moveSetsRaw)
        {
            if (moveSet != null)
                { moveSets.Add(moveSet); }
        }

        return moveSets;
    }

    public void Parry()
    {
        pms.movementLocked = true;
        if (!fmoves.PlayMove(ParryAsset))
        {
            pms.movementLocked = false;
            return;
        }
        isParrying = true;
    }


    public void ParryFail()
    {
        logger.log("Parry Failed.", 1);
        isParrying = false;
        parryFailCo = StartCoroutine(ParryFailRoutiune());
    }

    public void ParrySuccess() // Triggered by hitbox if trigger tag is "ParryBox"
    {
        logger.log("Parry Success!", 1);
        isParrying = false;
        fmoves.DisableAllHitboxes();
        fmoves.ClearMove();
        pms.ChangeAnimation("Parry_success");

        // Action State Reset at move end anim
    }
    public void StartActionState()
    {
        IsActioning = true; // Stop Non-Action sfx
        hitThisMove.Clear();
    }
    public void ResetActionState()
    {
        IsActioning = false;
        hitThisMove.Clear();
        pms.ResetAnimation();
        if (parryFailCo != null)
        {
            StopCoroutine(parryFailCo);
            parryFailCo = null;
        }
        pms.movementLocked = false;
    }

    IEnumerator ParryFailRoutiune()
    {
        pms.movementLocked = true;
        pms.ChangeAnimation("Parry_fail");
        logger.log("Playing Parry Fail Animation", 1);

        yield return new WaitForSeconds(parryFailSeconds);

        logger.log("Parry Fail finished", 1);
        pms.movementLocked = false;
        ResetActionState();
        pms.ChangeAnimation("Idle");
    }

    public void OnHitboxEnter(Collider2D other, string id)
    {
        if (hitThisMove.Contains(other)) return;
        if (!other.CompareTag("Hurtbox")) return;
        if (other.GetComponent<Hurtbox>().id == Team) return;

        hitThisMove.Add(other);

        var otherPcs = other.GetComponentInParent<PlayerCombat>();
        if (otherPcs == null) return;
        var move = GetMove(id);

        Transform defender = other.transform.root; // or other.GetComponentInParent<Transform>()
        bool defenderIsToRight = defender.position.x > transform.position.x;

        if (otherPcs.isParrying)
        {
            otherPcs.ParrySuccess();
            defenderIsToRight = !defenderIsToRight;
            health.OnParried(move, defenderIsToRight);
            logger.log("Other Player Parried!");
            return;
        }

        if (move == null)
        {
            logger.log($"No Move Asset associated with hitbox id {id}", 2);
            return;
        }

        otherPcs.OnTakeDamage(move, defenderIsToRight);
    }

    public void OnTakeDamage(MoveAsset move, bool defenderIsToRight)
    {

        health.OnRecieveHitSfx(move);
        health.OnDamage(move.hitData, defenderIsToRight);
    }

    private void StartMove(MoveAsset move)
    {
        currentMove = move;
        hitThisMove.Clear();
    }
    public void SideLight()    => fmoves.PlayMove(LightSetGround.Side);
    public void SideAir()      => fmoves.PlayMove(LightSetAir.Side);
    public void DownLight()    => fmoves.PlayMove(LightSetGround.Down);
    public void DownAir()      => fmoves.PlayMove(LightSetAir.Down);
    public void NeutralLight() { pms.movementLocked = true; fmoves.PlayMove(LightSetGround.Neutral); }
    public void NeutralAir()   { pms.movementLocked = true; fmoves.PlayMove(LightSetAir.Neutral); }
    public MoveAsset GetMove(string id) => moveByHitboxId[id];
}
