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
    public float iceSlideFriction = 2.5f;
    public float airFriction = 3f;

    [Header("얼음 셋팅")]
    public float iceAcceleration = 150f;

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

    private Collider2D[] groundCheckResults = new Collider2D[5];
    private Collider2D[] playerCheckResults = new Collider2D[5];
    private Collider2D[] headCheckResults = new Collider2D[5];
    private ContactFilter2D groundFilter;
    private ContactFilter2D playerFilter;

    [Header("외부 환경 속도")]
    public float windVelocity = 0f;
    private Transform currentPlatform;
    private Vector2 platformVelocity;
    private System.Func<Vector2> getPlatformVelocityFunc;

    private bool justJumped = false;
    private bool wasGroundedLastFrame = false;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");

        groundFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = groundLayer };
        playerFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = playerLayerMask };
    }

    void Update()
    {
        if (!isLocalPlayer) return;


        UpdateTimers();
        UpdateCoyoteTime();
        HandleJumpInput();
        UpdateGravity();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (controller.combineHandler != null && controller.combineHandler.isCombined && gameObject != controller.combineHandler.bodyTarget)
            return;

        HandleMovingPlatform();
        CheckGroundOrPlayer();
        CheckHeadForPlayer();
        HandleMovementPhysics();
        ClampVelocity();

        wasGroundedLastFrame = isGrounded;
    }

    private void UpdateTimers() { if (ckTimer > 0f) ckTimer -= Time.deltaTime; }

    private void UpdateCoyoteTime()
    {
        if (isGrounded && ckTimer <= 0f) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJumpInput()
    {
        if (PauseManager.instance != null && PauseManager.instance.isPaused)
            return;

        if (EmojiRadialMenu.Instance != null &&
            EmojiRadialMenu.Instance.IsOpen())
            return;
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

        justJumped = true;
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
        float rawInput = controller.input.HorizontalInput;

        if ((PauseManager.instance != null && PauseManager.instance.isPaused) ||
            (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()))
        {
            rawInput = 0f;
        }
        if (controller.knockback.isKnockedBack) return;

        if (controller.knockback.IsStunned)
        {
            float slideSpeed = Mathf.Lerp(controller.rb.linearVelocity.x, 0f, 10f * Time.fixedDeltaTime);
            controller.rb.linearVelocity = new Vector2(slideSpeed, controller.rb.linearVelocity.y);
            return;
        }

        if (controller.currentReverseZone != null && !controller.currentReverseZone.isForward)
        {
            float originalInput = 0f;
            if (controller.combineHandler != null && controller.combineHandler.isCombined)
                originalInput = controller.combineHandler.GetCombinedHorizontalInput();
            else
                originalInput = controller.input.moveAction.ReadValue<float>();

            if ((originalInput > 0 && rawInput > 0) || (originalInput < 0 && rawInput < 0))
            {
                rawInput *= -1f;
            }
        }

        bool isTouchingPlayer = false;
        if (Mathf.Abs(rawInput) > 0.1f)
        {
            float moveDir = Mathf.Sign(rawInput);
            Vector2 boxCenter = controller.bodyCollider.bounds.center;
            Vector2 boxSize = controller.bodyCollider.bounds.size;
            boxSize.y -= 0.3f;

            RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, new Vector2(moveDir, 0f), 0.05f, playerLayerMask);
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                rawInput = 0f;
                isTouchingPlayer = true;
            }
        }

        float targetVelocityX = (rawInput * moveSpeed) + windVelocity;
        float currentPlatformVelX = isGrounded ? platformVelocity.x : 0f;

        float currentFriction = isGrounded
            ? (isTouchingPlayer ? 15f : normalFriction)
            : (isTouchingPlayer ? 15f : (isRestrictedByRope ? 0f : 9999f));

        float newX;

        if (isOnIce || (!isGrounded && wasOnIceLastFrame))
        {
            if (Mathf.Abs(rawInput) > 0.01f)
            {
                newX = Mathf.MoveTowards(
                    controller.rb.linearVelocity.x,
                    targetVelocityX + currentPlatformVelX,
                    iceAcceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                newX = Mathf.MoveTowards(
                    controller.rb.linearVelocity.x,
                    windVelocity + currentPlatformVelX,
                    iceSlideFriction * Time.fixedDeltaTime
                );
            }
        }
        else
        {
            newX = Mathf.MoveTowards(
                controller.rb.linearVelocity.x,
                targetVelocityX + currentPlatformVelX,
                (currentFriction > 100f ? currentFriction : currentFriction * moveSpeed) * Time.fixedDeltaTime
            );
        }

        controller.rb.linearVelocity = new Vector2(newX, controller.rb.linearVelocity.y);
    }
    private void ClampVelocity()
    {
        float maxSpeedX = 30f;
        Vector2 clampedVelocity = controller.rb.linearVelocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxSpeedX, maxSpeedX);

        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, isInverted ? -maxFallSpeed * 1.5f : -maxFallSpeed, isInverted ? maxFallSpeed : maxFallSpeed * 1.5f);

        // 🚀 [버그 수정]: 넉백 맞고 날아가는 상태(isKnocked)일 때는 방어막을 끕니다!
        bool isKnocked = controller.knockback != null && controller.knockback.isKnockedBack;

        // 방어막 작동 조건에 !isKnocked 추가
        if (wasGroundedLastFrame && !justJumped && !isKnocked)
        {
            float allowedSpeed = currentPlatform != null ? platformVelocity.y : 0f;
            if (isInverted)
            {
                if (clampedVelocity.y < allowedSpeed - 0.1f) clampedVelocity.y = allowedSpeed;
            }
            else
            {
                if (clampedVelocity.y > allowedSpeed + 0.1f) clampedVelocity.y = allowedSpeed;
            }
        }

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

        if (isGrounded && Mathf.Abs(controller.rb.linearVelocity.y) <= 0.1f) justJumped = false;

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