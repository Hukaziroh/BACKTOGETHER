using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerController controller;
    public bool isRestrictedByRope = false;
    private int playerLayerMask;

    [Header("무브")]
    public float moveSpeed = 7f;

    [Header("관성 및 미끄러짐 셋팅")]
    public float normalFriction = 20f;
    public float iceSlideFriction = 3f;
    public float airFriction = 3f;

    [Header("점프셋팅")]
    public float jumpHeight = 4f;
    public float jumpSpeed = 4f;
    public float fallSpeed = 2.5f;
    public float maxFallSpeed = 20f;
    public float coyoteTime = 0.15f;
    public float coyoteTimeCounter { get; private set; }

    [Range(0f, 1f)]
    public float superJump = 0.5f;
    private float jumpCk = 0.1f;
    private float ckTimer;

    [Header("기즈모 및 감지 범위 설정")]
    public bool showGizmo = true;

    [Header("그라운드 체크")]
    [Range(0.01f, 5f)]
    public float checkRadius = 0.5f;
    public Vector2 checkOffset = Vector2.zero;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public bool isGrounded { get; private set; }

    [Header("헤드 체크 (머리 위 플레이어 감지)")]
    public Vector2 headCheckBoxSize = new Vector2(0.8f, 0.2f);
    public Vector2 headCheckOffset = Vector2.zero;
    public Transform headCheck;
    public bool hasPlayerOnHead { get; private set; }

    private bool isOnIce = false;
    private bool wasOnIceLastFrame = false;
    private int touchingPlayerCount = 0;

    // 최적화: 매 프레임 할당 방지
    private Collider2D[] groundCheckResults = new Collider2D[5];
    private Collider2D[] playerCheckResults = new Collider2D[5];
    private Collider2D[] headCheckResults = new Collider2D[5];
    private ContactFilter2D groundFilter;
    private ContactFilter2D playerFilter;

    [Header("외부 환경 속도")]
    public float windVelocity = 0f;
    private Transform currentPlatform;
    private Vector2 platformVelocity;
    private System.Func<Vector2> getPlatformVelocityFunc; // 최적화: 발판 속도 반환 델리게이트 캐싱

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");

        // 필터 초기화 (GC 할당 최적화)
        groundFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = groundLayer };
        playerFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = playerLayerMask };
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (PauseManager.instance != null && PauseManager.instance.isPaused) return;
        if (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()) return;

        UpdateTimers();
        UpdateCoyoteTime();
        HandleJumpInput();
        UpdateGravity();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if ((PauseManager.instance != null && PauseManager.instance.isPaused) ||
            (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()))
        {
            controller.rb.linearVelocity = new Vector2(0f, controller.rb.linearVelocity.y);
            return;
        }

        if (controller.combineHandler != null && controller.combineHandler.isCombined && gameObject != controller.combineHandler.bodyTarget)
            return;

        HandleMovingPlatform();
        CheckGroundOrPlayer();
        CheckHeadForPlayer();
        HandleMovementPhysics();
        ClampVelocity();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) touchingPlayerCount++;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) touchingPlayerCount = Mathf.Max(0, touchingPlayerCount - 1);
    }

    private void UpdateTimers() { if (ckTimer > 0f) ckTimer -= Time.deltaTime; }

    private void UpdateCoyoteTime()
    {
        if (isGrounded && ckTimer <= 0f) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJumpInput()
    {
        if (controller.combineHandler == null || !controller.combineHandler.isCombined)
        {
            if (controller.input.JumpPressedThisFrame && coyoteTimeCounter > 0f && !controller.knockback.IsStunned && !hasPlayerOnHead)
            {
                Jump();
                coyoteTimeCounter = 0f;
            }

            bool inverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
            bool isMovingUp = inverted ? (controller.rb.linearVelocity.y < 0f) : (controller.rb.linearVelocity.y > 0f);

            if (controller.input.JumpReleasedThisFrame && isMovingUp && !controller.knockback.IsStunned)
            {
                if (controller.syncJumpHandler != null && controller.syncJumpHandler.isInSyncZone) controller.syncJumpHandler.CmdCutSyncJump();
                else ApplyShortJump();
            }
        }
    }

    public void Jump()
    {
        if (controller.knockback.isKnockedBack) return;
        float gravity = Mathf.Abs(Physics2D.gravity.y) * jumpSpeed;
        float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);
        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, jumpForce * mult);
        ckTimer = jumpCk;
    }

    public void ApplyShortJump()
    {
        bool inverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        bool isMovingUpCheck = inverted ? (controller.rb.linearVelocity.y < 0f) : (controller.rb.linearVelocity.y > 0f);
        if (isMovingUpCheck) controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, controller.rb.linearVelocity.y * superJump);
    }

    private void UpdateGravity()
    {
        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        bool isFalling = isInverted ? (controller.rb.linearVelocity.y > 0f) : (controller.rb.linearVelocity.y < 0f);
        controller.rb.gravityScale = isFalling ? (jumpSpeed * fallSpeed * mult) : (jumpSpeed * mult);
    }

    private void HandleMovementPhysics()
    {
        if (controller.knockback.isKnockedBack) return;

        if (controller.knockback.IsStunned)
        {
            float slideSpeed = Mathf.Lerp(controller.rb.linearVelocity.x, 0f, 10f * Time.fixedDeltaTime);
            controller.rb.linearVelocity = new Vector2(slideSpeed, controller.rb.linearVelocity.y);
            return;
        }

        float targetVelocityX = (controller.input.HorizontalInput * moveSpeed) + windVelocity;
        float currentPlatformVelX = isGrounded ? platformVelocity.x : 0f;
        bool isTouchingPlayer = touchingPlayerCount > 0;

        float currentFriction = isOnIce ? iceSlideFriction : (isGrounded ? (isTouchingPlayer ? 15f : 9999f) : (wasOnIceLastFrame ? (isRestrictedByRope ? 0f : airFriction) : (isTouchingPlayer ? 15f : 9999f)));

        float newX = Mathf.MoveTowards(
            controller.rb.linearVelocity.x,
            targetVelocityX + (isGrounded ? currentPlatformVelX : 0f),
            (currentFriction > 100f ? currentFriction : currentFriction * moveSpeed) * Time.fixedDeltaTime
        );

        controller.rb.linearVelocity = new Vector2(newX, controller.rb.linearVelocity.y);
    }

    private void ClampVelocity()
    {
        float maxSpeedX = 30f;
        Vector2 clampedVelocity = controller.rb.linearVelocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxSpeedX, maxSpeedX);

        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, isInverted ? -maxFallSpeed * 1.5f : -maxFallSpeed, isInverted ? maxFallSpeed : maxFallSpeed * 1.5f);

        controller.rb.linearVelocity = clampedVelocity;
    }

    void CheckGroundOrPlayer()
    {
        int hitCount = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundFilter, groundCheckResults);

        isGrounded = false;
        bool currentOnIce = false;
        bool foundPlatform = false;
        Transform detectedPlatform = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = groundCheckResults[i];
            if (col.CompareTag("Spike") || col.gameObject == gameObject || col.isTrigger) continue;

            isGrounded = true;
            if (col.CompareTag("Ice")) currentOnIce = true;
            if (col.CompareTag("MovingPlatform"))
            {
                detectedPlatform = col.transform;
                foundPlatform = true;
            }
        }

        if (!isGrounded)
        {
            int playerHitCount = Physics2D.OverlapCircle(groundCheck.position, checkRadius, playerFilter, playerCheckResults);
            for (int i = 0; i < playerHitCount; i++)
            {
                Collider2D col = playerCheckResults[i];
                if (col.gameObject != gameObject && col.CompareTag("Player") && groundCheck.position.y > col.bounds.max.y - 0.05f)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        // 플랫폼 변경 시에만 GetComponent 연산 수행 (핵심 최적화)
        if (foundPlatform)
        {
            if (currentPlatform != detectedPlatform)
            {
                currentPlatform = detectedPlatform;
                CachePlatformVelocityDelegate(currentPlatform);
            }
        }
        else
        {
            currentPlatform = null;
            getPlatformVelocityFunc = null;
        }

        isOnIce = currentOnIce;
        if (isGrounded) wasOnIceLastFrame = isOnIce;
    }

    void CheckHeadForPlayer()
    {
        if (headCheck == null) return;
        hasPlayerOnHead = false;
        Vector2 checkPosition = (Vector2)headCheck.position + headCheckOffset;

        int hitCount = Physics2D.OverlapBox(checkPosition, headCheckBoxSize, 0f, playerFilter, headCheckResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = headCheckResults[i];
            if (col.gameObject == gameObject || col.isTrigger) continue;

            Vector2 dir = col.transform.position - transform.position;
            if (dir.y <= 0.2f) continue;
            if (col.attachedRigidbody != null && col.attachedRigidbody.linearVelocity.y > 0.1f) continue;

            hasPlayerOnHead = true;
            break;
        }
    }

    private void CachePlatformVelocityDelegate(Transform platform)
    {
        if (platform == null) { getPlatformVelocityFunc = null; return; }

        var roundTrip = platform.GetComponentInParent<CoopRoundTripPlatform>();
        if (roundTrip != null) { getPlatformVelocityFunc = () => roundTrip.CurrentVelocity; return; }

        var patrol = platform.GetComponentInParent<CoopPatrolPlatform>();
        if (patrol != null) { getPlatformVelocityFunc = () => patrol.CurrentVelocity; return; }

        var log = platform.GetComponentInParent<CoopMovingLog>();
        if (log != null) { getPlatformVelocityFunc = () => log.CurrentVelocity; return; }

        var parallel = platform.GetComponentInParent<CoopParallelPlatform>();
        if (parallel != null) { getPlatformVelocityFunc = () => parallel.CurrentVelocity; return; }

        var mirror = platform.GetComponentInParent<CoopMirrorPlatforms>();
        if (mirror != null)
        {
            bool isLeft = platform.gameObject == mirror.leftPlatform.gameObject || platform.IsChildOf(mirror.leftPlatform.transform);
            getPlatformVelocityFunc = () => isLeft ? mirror.LeftVelocity : mirror.RightVelocity;
            return;
        }
        getPlatformVelocityFunc = null;
    }

    private void HandleMovingPlatform()
    {
        platformVelocity = getPlatformVelocityFunc != null ? getPlatformVelocityFunc() : Vector2.zero;
    }

    public void CallCombinedJump()
    {
        if ((isGrounded || coyoteTimeCounter > 0f) && !hasPlayerOnHead)
        {
            controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, 0f);
            Jump();
            coyoteTimeCounter = 0f;
        }
    }

    public void CallCombinedAction()
    {
        controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, 0f);
        controller.rb.AddForce(Vector2.down * 18f, ForceMode2D.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)groundCheck.position + checkOffset, checkRadius);
        }
        if (headCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)headCheck.position + headCheckOffset, headCheckBoxSize);
        }
    }
}