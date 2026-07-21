using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region Initial Variables

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GameObject spriteObj;
    [SerializeField] public Animator animator;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private PlayerCombat pcs;
    [SerializeField] private Logger logger;
    private Health health;
    [SerializeField] LayerMask groundLayer;

    public bool isDashing = false;

    // Set in PlayerActionController
    public bool holdingSink = false;
    public bool jumpEnabled = true;
    public float directionSign = 1f;
    public float horizontalMove;


    private const float MOVE_SPEED = 7f;
    private const float DASH_SPEED = 20f;
    private const float JUMP_POWER = 15f;
    private const float _midJumpAnimWindow = 0.8f;

    private float _dashDuration = 0.2f;

    private Coroutine dashCo;

    private float _sinkGravity = 9f;
    private float _normalGravity = 5f;
    private float _fallGravity = 6f;

    public float DashCD = 0.4f;
    public float JumpCD = 0.1f;

    public bool isGrounded { get; private set; }
    private float setGravity;
    private float setVelocityX;
    private bool facingRight = true;

    public string currentAnimation = "";
    
    private bool moveAnimsEnabled => !isDashing && !pcs.IsActioning;
    public bool movementEnabled => !(health.isStunned || health.isLaunched); // Stunned or Launched
    public bool movementLocked = false; // Parrying or in Move that locks movement

    // Control Variables
    private bool _animsEnabled = true;
    public bool spriteRotating = false;

    #endregion

    private void Awake()
    {
        setGravity = _normalGravity;
        health = GetComponent<Health>();
    }

    private void Start()
    {

    }

    private void FixedUpdate()
    {
        if (movementEnabled) ChangeGrav();

        Move();
    }

    private void OnEnable()
    {
        GlobalHandler.GameStart += HandleGameStart;
    }
    private void OnDisable()
    {
        GlobalHandler.GameStart -= HandleGameStart;
    }

    private void HandleGameStart()
    {
        rb.linearVelocityX = 0f; 
        rb.linearVelocityY = 0f;
        StopRoutines();
        horizontalMove = 0f;
    }

    private void Update()
    {
        CheckGround();
        CheckState();
    }

    private void CheckGround()
    {
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, 0.1f, LayerMask.GetMask("platform"));
        if (hit != null)
        {
            isGrounded = true;
            jumpEnabled = true;
        }
        else
            isGrounded = false;
    }

    public float ChangeDirection()
    {
        if (horizontalMove == 0) return directionSign;
        else return horizontalMove;
    }
    private void Move() // Disabled From being called if dashing, actioning, or stunned
    {
        if (!movementEnabled)
        {
            return;
        }

        if (movementLocked)
        {
            rb.linearVelocityX = 0f;
            return;
        }

        if (isDashing) rb.linearVelocity = new Vector2(DASH_SPEED * directionSign, 0f);
        else rb.linearVelocity = new Vector2(MOVE_SPEED * horizontalMove, rb.linearVelocity.y);
    }

    public void Jump()
    {
        logger.log("Jumped", 0);
        jumpEnabled = false;
        rb.linearVelocityY = 0f;
        rb.AddForce(Vector2.up * JUMP_POWER, ForceMode2D.Impulse);
    }

    public void Dash()
    {
        if (dashCo != null) StopCoroutine(dashCo);

        dashCo = StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        logger.log("Starting Dash", 0);

        isDashing = true;

        ChangeAnimation("Dash_start"); // Dash_start transitions to Dash_loop when finished

        yield return new WaitForSeconds(_dashDuration);

        logger.log("Dash Complete", 0);
        isDashing = false;

        dashCo = null;
    }


    #region State Observation Methods
    private void CheckState()
    {
        if (health.isLaunched)
        {
            spriteRotating = true;
            ChangeAnimation("Launched");
            return;
        }

        if (health.isStunned)
        {
            ChangeAnimation("Stunned");
            return;
        }

        spriteRotating = false;

        float verticalVel = rb.linearVelocity.y;
        float absVerticalVel = Mathf.Abs(verticalVel);

        // Gravity Order of Precedence: Dashing -> Grounded -> Sinking -> Falling
        if (isGrounded) setGravity = _normalGravity;
        else if (holdingSink) setGravity = _sinkGravity;
        else
        {
            if (verticalVel < 0f) setGravity = _fallGravity;
            else setGravity = _normalGravity;
        }

        // Sprite Flipping
        if (horizontalMove != 0)
        {
            var hVel = rb.linearVelocity.x;
            if (hVel > 0.1f && !facingRight || hVel < -0.1f && facingRight) Flip();
        }

        // Jump and Move Anims
        if (moveAnimsEnabled)
        {
            if (isGrounded)
            {
                if (horizontalMove != 0) ChangeAnimation("Run");
                else ChangeAnimation("Idle");
            }
            else
            {
                if (absVerticalVel < _midJumpAnimWindow) ChangeAnimation("Jump_mid");
                else if (verticalVel > 0) ChangeAnimation("Jump_rise");
                else ChangeAnimation("Jump_fall");
            }
        }
    }


    #endregion

    public void ChangeAnimation(string animation)
    {
        if (_animsEnabled)
        {
            if (currentAnimation != animation)
            {
                currentAnimation = animation;
                animator.CrossFade(animation, 0f);
            }
        }
    }

    public void CrossFadeAnimation(string animation, float crossfade = 0f)
    {
        if (_animsEnabled)
        {
            if (currentAnimation != animation)
            {
                currentAnimation = animation;
                animator.CrossFade(animation, crossfade);
            }
        }
    }


    public void ResetAnimation() => animator.ResetControllerState();
    private void ChangeGrav() 
    {
        if (rb.gravityScale != setGravity)
        {
            rb.gravityScale = setGravity;
            logger.log($"Gravity Set to {setGravity}.", 0);
        }
    }

    public void Flip()
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    public void StopRoutines()
    {
        if (dashCo != null)
        {
            StopCoroutine(dashCo);
            dashCo = null;
        }

        isDashing = false;
        if (spriteRotating)
        {
            ResetLaunchedSprite();
        }
    }

    public void RotateLaunchedSprite()
    {
        var angle = -90f * Mathf.Sign(rb.linearVelocityX);
        spriteObj.transform.Rotate(0f, 0f, angle);
        spriteRotating = true;
    }

    public void ResetLaunchedSprite()
    {
        spriteObj.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        spriteRotating = false;
    }
}