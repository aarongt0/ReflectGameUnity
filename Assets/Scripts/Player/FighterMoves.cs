using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FighterMoves : MonoBehaviour
{
    AudioManager audioManager => AudioManager.I;

    [Header("Hitbox pool (children)")]
    private Dictionary<string, HitboxMarker> hitboxById;

    private MoveAsset _move;
    private PlayerMovement pms;
    private PlayerCombat pcs;
    [SerializeField] Logger logger;
    [SerializeField] private Transform hitboxRoot;
    private int _frame;
    private float _accum;

    private int hbIndex;
    private int _sfxIndex;

    private readonly HashSet<string> uniqueHitboxIds = new();

    private void Awake()
    {
        if (!pcs) pcs = GetComponent<PlayerCombat>();
        if (!pms) pms = GetComponent<PlayerMovement>();
        if (hitboxRoot == null) logger.log("Hitbox root not assigned!!!", 3);

        hitboxById = new Dictionary<string, HitboxMarker>();
    }
    private void Start()
    {
        var markers = hitboxRoot.GetComponentsInChildren<HitboxMarker>();

        foreach (var marker in markers)
        {
            if (string.IsNullOrEmpty(marker.id))
            {
                logger.log("Hitbox marker missing ID", 2);
                continue;
            }

            hitboxById[marker.id] = marker;
            marker.GetComponent<Collider2D>().enabled = false;
            marker.gameObject.SetActive(false);
        }
    }

    public bool PlayMove(MoveAsset move)
    {
        _move = move;

        if (_move == null)
        {
            logger.log("Tried accessing a non-existent move.", 2);
            return false;
        }

        _frame = 0;
        _accum = 0f;
        hbIndex = 0;
        _sfxIndex = 0;

        // Always clear hitboxes when starting a new move
        DisableAllHitboxes();

        // Allow for correct boolean logic and animations
        pcs.StartActionState();

        // Enables hitbox objects involved in move. Does not enable colliders.
        ReadyMove(move);

        string moveString = move.anim.name;
        logger.log($"Anim named {moveString}", 0);
        pms.ChangeAnimation(moveString);  // works as intended, although not efficiently dynamic

        FireFrameEvents(0); // Starts frame event firing

        return true;
    }

    private void Update()
    {
        if (_move == null) return;
        float dtPerFrame = 1f / Mathf.Max(1, _move.fps);
        _accum += Time.deltaTime;
        var lengthInSeconds = _move.anim.length;
        if (_move.type.ToLower() == "parry")
        {
            lengthInSeconds = pcs.parryWindow;
        }
        while (_accum >= dtPerFrame)
        {
            _accum -= dtPerFrame;
            _frame++;
            FireFrameEvents(_frame);

            if (_frame > lengthInSeconds * _move.fps) // Animation Over
            {
                logger.log($"{_move.moveName} finished animation.", 0);
                if (_move.type.ToLower() == "parry")
                {
                    DisableAllHitboxes();
                    _move = null;
                    pcs.ParryFail();
                    return;
                }

                StopMove(); // safety cap
            }
        }
    }

    private void FireFrameEvents(int frame)
    {
        // Hitboxes (assumes events sorted by frame)
        if (_move.hitboxEvents == null) return;
        var hbEvents = _move.hitboxEvents;
        while (hbIndex < hbEvents.Length && hbEvents[hbIndex].frame == frame) 
        {
            var e = hbEvents[hbIndex];
            if (hitboxById.TryGetValue(e.hitboxId, out var hb))
                hb.Apply(e.localPos, e.size, e.enabled);
            hbIndex++;
        }

        // SFX (assumes events sorted by frame)
        if (_move.sfxEvents == null) return;
        else
        {
            var sfxEvents = _move.sfxEvents;
            while (_sfxIndex < sfxEvents.Length && sfxEvents[_sfxIndex].frame == frame)
            {
                var e = sfxEvents[_sfxIndex];
                if (audioManager != null && e.clip != null)
                    audioManager.PlaySfx(e.clip, Mathf.Clamp01(e.volume <= 0 ? 1f : e.volume));
                _sfxIndex++;
            }
        }
    }

    public void StopMove()
    {
        DisableAllHitboxes();
        pcs.ResetActionState();
        _move = null;
    }

    public void DisableAllHitboxes()
    {
        foreach (var kv in hitboxById) kv.Value.gameObject.SetActive(false);
    }
    public void ClearMove() => _move = null;
    public void ReadyMove(MoveAsset move)
    {
        if (move == null || move.hitboxEvents == null)
            return;

        uniqueHitboxIds.Clear();

        foreach (HitboxEvent hbE in move.hitboxEvents)
        {
            if (string.IsNullOrEmpty(hbE.hitboxId))
            {
                logger.log($"Invalid hitbox ID: {hbE.hitboxId}", 2);
                continue;
            }

            uniqueHitboxIds.Add(hbE.hitboxId);
        }

        logger.log($"Unique hitbox IDS of length {uniqueHitboxIds.Count}: {uniqueHitboxIds}", 0);

        // Enable hitbox GameObjects (not colliders yet)
        foreach (string id in uniqueHitboxIds)
        {
            if (hitboxById.TryGetValue(id, out HitboxMarker hitbox))
            {
                hitbox.gameObject.SetActive(true);
            }
            else
            {
                logger.log($"Hitbox id '{id}' not found for move '{move.name}'", 2);
            }
        }
    }
}
