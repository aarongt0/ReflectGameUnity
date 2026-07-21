using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    private PlayerInput playerInput;
    [SerializeField] private PlayerMovement pms;
    [SerializeField] private PlayerCombat pcs;
    [SerializeField] private Logger logger;
    [SerializeField] private Health health;

    [Header("Buffering")]
    private float BUFFER_WINDOW = 0.4f;

    private readonly Dictionary<string, GameAction> actionsByName = new();
    private readonly List<BufferedInput> buffer = new();

    [Header("Bot")]
    public bool isBot;

    // canUse boolean logic per type of Action
    public bool canJump => pms.jumpEnabled && !pcs.IsActioning && !pms.isDashing && !health.isStunned && !health.isLaunched;
    public bool canDash => !pms.isDashing && !pcs.IsActioning && (!health.isStunned && !health.isLaunched);
    private bool canAction => !pcs.IsActioning && (!health.isStunned && !health.isLaunched);

    public bool movementLocked = true; // locked until StartGame unlocks

    private void Awake()
    {
        // If bot, do not set up PlayerInput at all.
        if (isBot)
        {
            // Optional: if a PlayerInput component exists, disable it so it won't do anything.
            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            buffer.Clear();
            return;
        }

        playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            if (logger != null) logger.log("PlayerInput missing on PlayerController", 2);
            else Debug.LogError("PlayerInput missing on PlayerController");

            enabled = false;
            return;
        }

        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

        playerInput.actions.FindActionMap("Fight").Enable();
        playerInput.SwitchCurrentActionMap("Fight");
        playerInput.onActionTriggered += HandleActionTriggered;

        logger.log("PlayerController Awake complete", 1);
    }

    private void Start()
    {
        // Actions can exist for both bot and player (bots may want to call TryInvoke on them too).
        actionsByName["Jump"] = new GameAction("Jump", pms.JumpCD, () => pms.Jump(), () => canJump);
        actionsByName["Dash"] = new GameAction("Dash", pms.DashCD, () => pms.Dash(), () => canDash);

        actionsByName["Parry"] = new GameAction("Parry", pcs.ParryCD, () => pcs.Parry(), () => canAction);

        // Light Ground Variants
        actionsByName["NeutralLight"] = new GameAction(
            "NeutralLight",
            pcs.NeutralLightCD,
            () => pcs.NeutralLight(),
            () => canAction);

        actionsByName["SideLight"] = new GameAction(
            "SideLight",
            pcs.SideLightCD,
            () => pcs.SideLight(),
            () => canAction);

        actionsByName["DownLight"] = new GameAction(
            "DownLight",
            pcs.DownLightCD,
            () => pcs.DownLight(),
            () => canAction);

        // Light Air Variants (separate cooldown timers from ground)
        actionsByName["NeutralAir"] = new GameAction(
            "NeutralAir",
            pcs.NeutralAirCD,
            () => pcs.NeutralAir(),
            () => canAction);

        actionsByName["SideAir"] = new GameAction(
            "SideAir",
            pcs.SideAirCD,
            () => pcs.SideAir(),
            () => canAction);

        actionsByName["DownAir"] = new GameAction(
            "DownAir",
            pcs.DownAirCD,
            () => pcs.DownAir(),
            () => canAction);

        buffer.Clear(); // Clear Inputs pressed before start
    }

    private void OnDestroy()
    {
        if (playerInput != null)
            playerInput.onActionTriggered -= HandleActionTriggered;
    }

    public void ClearBuffer()
    {
        buffer.Clear();
    }

    private void HandleActionTriggered(InputAction.CallbackContext ctx)
    {
        if (isBot) return;

        if (movementLocked)
        {
            pms.horizontalMove = 0f;
            return;
        }

        if (ctx.action.name == "Move")
        {
            float value = ctx.ReadValue<Vector2>().x;

            if (value != 0) pms.directionSign = value;

            pms.horizontalMove = value;
        }

        if (ctx.action.name == "Sink")
        {
            if (ctx.performed)
                pms.holdingSink = true;
            else if (ctx.canceled)
                pms.holdingSink = false;
        }

        if (ctx.performed)
        {
            if (ctx.action.name == "LightAttack")
            {
                buffer.Add(new BufferedInput("LightAttack", Time.time, GetAttackDirSnapshot()));
                return;
            }

            if (actionsByName.ContainsKey(ctx.action.name))
            {
                buffer.Add(new BufferedInput(ctx.action.name, Time.time, null));
            }
        }
    }

    private void Update()
    {
        if (isBot)
            return;

        // Remove expired buffered inputs
        for (int i = buffer.Count - 1; i >= 0; i--)
        {
            if (Time.time - buffer[i].timePressed > BUFFER_WINDOW)
            {
                if (logger != null) logger.log($"{buffer[i].actionName} timed out. Removed from buffer.", 1);
                buffer.RemoveAt(i);
            }
        }

        // Execute buffered inputs in order
        for (int i = 0; i < buffer.Count; i++)
        {
            var input = buffer[i];

            string resolvedName = input.actionName;

            if (input.actionName == "LightAttack")
            {
                bool air = !pms.isGrounded;
                resolvedName = input.attackDir switch
                {
                    AttackDir.Side => air ? "SideAir"    : "SideLight",
                    AttackDir.Down => air ? "DownAir"    : "DownLight",
                    _              => air ? "NeutralAir" : "NeutralLight"
                };
            }

            if (actionsByName.TryGetValue(resolvedName, out var action))
            {
                if (action.TryInvoke())
                {
                    if (logger != null) logger.log($"{resolvedName} successfully consumed by buffer!", 1);
                    buffer.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private readonly struct BufferedInput
    {
        public readonly string actionName;
        public readonly float timePressed;
        public readonly AttackDir? attackDir;

        public BufferedInput(string actionName, float timePressed, AttackDir? attackDir)
        {
            this.actionName = actionName;
            this.timePressed = timePressed;
            this.attackDir = attackDir;
        }
    }

    private enum AttackDir
    {
        Neutral,
        Side,
        Down
    }

    private AttackDir GetAttackDirSnapshot()
    {
        if (pms.holdingSink) return AttackDir.Down;
        if (pms.horizontalMove != 0) return AttackDir.Side;
        return AttackDir.Neutral;
    }

    // Helper for bot AI to trigger actions safely from another script:
    public bool TryBotAction(string actionName)
    {
        if (!isBot) return false;
        return actionsByName.TryGetValue(actionName, out var action) && action.TryInvoke();
    }

    void OnEnable()
    {
        GlobalHandler.GameStart += HandleGameStart;
    }

    void OnDisable()
    {
        GlobalHandler.GameStart -= HandleGameStart;
    }

    private void HandleGameStart()
    {
        buffer.Clear();
    }
}
