using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerController controller;
    public bool isRestrictedByRope = false;
    private int playerLayerMask;
    public bool isTouchingPlayer { get; private set; }

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

    // 🌟 [추가됨] 숏점프 커팅 권한 상태 변수
    private bool canCutJump = false;

    [Header("코너 충돌 보정")]
    public float cornerPushEpsilon = 0.02f;
    public float overlapPushTolerance = 0.01f;
    public bool debugLogOverlap = false;
    private float lastCommandedVelocityX;
    private bool hasCommandedVelocity;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");

        groundFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = groundLayer };
        playerFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = playerLayerMask };
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        // 고스트(팀원)는 스스로 물리 연산을 하지 않고 본체에 합쳐짐
        if (controller.combineHandler != null && controller.combineHandler.isCombined && gameObject != controller.combineHandler.bodyTarget)
            return;

        UpdateTimers();
        UpdateCoyoteTime();
        UpdateGravity();

        CancelUnexpectedPlayerCollisionPush();

        HandleMovingPlatform();
        CheckGroundOrPlayer();
        CheckHeadForPlayer();
        CheckTouchingPlayer();

        HandleJumpInput();
        HandleActionInput();
        HandleMovementPhysics();
        ClampVelocity();

        wasGroundedLastFrame = isGrounded;

        float maxSpeed = 40f;
        if (controller.rb.linearVelocity.magnitude > maxSpeed)
        {
            controller.rb.linearVelocity = controller.rb.linearVelocity.normalized * maxSpeed;
        }

        lastCommandedVelocityX = controller.rb.linearVelocity.x;
        hasCommandedVelocity = true;

        if (controller.combineHandler != null && controller.combineHandler.isCombined && controller.combineHandler.bodyTarget == gameObject)
        {
            controller.combineHandler.ClearAllCombinedInputBuffers();
        }
        else
        {
            controller.input.ClearInputBuffers();
        }
    }

    // ============================================================
    // 입력 처리 파트
    // ============================================================

    // 🌟 [수정됨] 완벽한 숏점프 방어 로직이 결합된 HandleJumpInput
    private void HandleJumpInput()
    {
        if (controller.combineHandler != null && controller.combineHandler.isCombined && gameObject != controller.combineHandler.bodyTarget)
            return;

        bool isJumpPressed = controller.input.JumpPressedThisFrame;
        bool isJumpReleased = controller.input.JumpReleasedThisFrame;
        bool isJumpHolding = controller.input.JumpHolding;

        if (controller.combineHandler != null && controller.combineHandler.isCombined && controller.combineHandler.bodyTarget == gameObject)
        {
            isJumpPressed = controller.combineHandler.GetServerCombinedJumpPressed();
            isJumpReleased = controller.combineHandler.GetServerCombinedJumpReleased();
            isJumpHolding = controller.combineHandler.GetServerCombinedJumpHolding();
        }

        if (isJumpPressed && coyoteTimeCounter > 0f && !controller.knockback.IsStunned && !hasPlayerOnHead)
        {
            Jump();
            coyoteTimeCounter = 0f;
        }

        bool inverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        bool isMovingUp = inverted ? (controller.rb.linearVelocity.y < 0f) : (controller.rb.linearVelocity.y > 0f);

        // 🌟 숏점프 강제 커팅 로직
        if (canCutJump && isMovingUp && !controller.knockback.IsStunned)
        {
            // 네트워크 핑 때문에 Released 버퍼가 증발해도 !isJumpHolding을 통해 칼같이 잡아냄
            if (isJumpReleased || !isJumpHolding)
            {
                if (controller.syncJumpHandler != null && controller.syncJumpHandler.isInSyncZone)
                {
                    controller.syncJumpHandler.CmdCutSyncJump();
                }
                else
                {
                    ApplyShortJump();
                }
                // 🌟 한 번 깎았으면 무한 감속을 막기 위해 숏점프 권한 차단
                canCutJump = false;
            }
        }
        else if (!isMovingUp)
        {
            // 점프가 정점을 찍고 떨어지기 시작하면 숏점프 권한 해제
            canCutJump = false;
        }
    }

    private void HandleActionInput()
    {
        if (controller.combineHandler != null && controller.combineHandler.isCombined && gameObject != controller.combineHandler.bodyTarget)
            return;

        bool isActionPressed = controller.input.ActionPressedThisFrame;

        if (controller.combineHandler != null && controller.combineHandler.isCombined && controller.combineHandler.bodyTarget == gameObject)
        {
            isActionPressed = controller.combineHandler.GetServerCombinedActionPressed();
        }

        if (isActionPressed)
        {
            if (controller.combineHandler != null && controller.combineHandler.canUseAction)
            {
                CallCombinedAction();
            }
        }
    }

    private void HandleMovementPhysics()
    {
        float rawInput = controller.input.HorizontalInput;

        if (controller.combineHandler != null && controller.combineHandler.isCombined && controller.combineHandler.bodyTarget == gameObject)
        {
            rawInput = controller.combineHandler.GetServerCombinedHorizontalInput();
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
            float originalInput = rawInput;
            if ((originalInput > 0 && rawInput > 0) || (originalInput < 0 && rawInput < 0))
            {
                rawInput *= -1f;
            }
        }

        isTouchingPlayer = false;
        if (Mathf.Abs(rawInput) > 0.1f)
        {
            float moveDir = Mathf.Sign(rawInput);
            Vector2 boxCenter = controller.bodyCollider.bounds.center;
            Vector2 boxSize = controller.bodyCollider.bounds.size;
            boxSize.y -= 0.3f;

            float currentSpeedX = Mathf.Abs(controller.rb.linearVelocity.x);
            float castDistance = Mathf.Max(0.05f, currentSpeedX * Time.fixedDeltaTime + 0.05f);

            RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, new Vector2(moveDir, 0f), castDistance, playerLayerMask);
            if (debugLogOverlap && hit.collider != null && hit.collider.gameObject != gameObject)
            {
                LogDebug($"BoxCast 차단됨 - castDistance={castDistance:F3} 상대={hit.collider.gameObject.name}");
            }
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                rawInput = 0f;
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
            // 🌟 [핵심 수정 구간] 클라이언트 무조건 승리 버그 원천 차단!
            if (isRestrictedByRope)
            {
                // 로프가 팽팽할 때는 속도를 덮어쓰지 않고, 땀 흘리며 힘(AddForce)만 주도록 만듦
                float pullForce = targetVelocityX * 80f; // 줄다리기 저항력 (적당히 묵직하게 세팅)
                controller.rb.AddForce(new Vector2(pullForce, 0f));

                // 속도를 덮어쓰지 않고 로프 매니저가 계산한 현재 속도를 그대로 보존!
                newX = controller.rb.linearVelocity.x;
            }
            else
            {
                // 평소에는 기존처럼 빠릿빠릿한 플랫포머 조작감 유지
                newX = Mathf.MoveTowards(
                    controller.rb.linearVelocity.x,
                    targetVelocityX + currentPlatformVelX,
                    (currentFriction > 100f ? currentFriction : currentFriction * moveSpeed) * Time.fixedDeltaTime
                );
            }
        }

        controller.rb.linearVelocity = new Vector2(newX, controller.rb.linearVelocity.y);
    }
    // ============================================================
    // 기타 물리 및 보조 파트
    // ============================================================

    public void Jump()
    {
        if (controller.knockback.isKnockedBack) return;
        float gravity = Mathf.Abs(Physics2D.gravity.y) * jumpSpeed;
        float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);
        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, jumpForce * mult);
        ckTimer = jumpCk;
        justJumped = true;

        // 🌟 [추가됨] 점프를 뛰는 그 순간 숏점프 커팅용 총알 장전!
        canCutJump = true;
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

        // 땅에서는 항상 기본 중력
        if (isGrounded)
        {
            controller.rb.gravityScale = jumpSpeed * mult;
            return;
        }

        // 공중에서만 낙하 여부 판단
        bool isFalling = isInverted
            ? (controller.rb.linearVelocity.y > 0f)
            : (controller.rb.linearVelocity.y < 0f);

        controller.rb.gravityScale = isFalling
            ? (jumpSpeed * fallSpeed * mult)
            : (jumpSpeed * mult);
    }

    void CheckTouchingPlayer()
    {
        Vector2 boxCenter = controller.bodyCollider.bounds.center;
        Vector2 boxSize = controller.bodyCollider.bounds.size;
        boxSize.x += 0.1f;
        boxSize.y -= 0.2f;

        int hitCount = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerFilter, playerCheckResults);
        isTouchingPlayer = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = playerCheckResults[i];
            if (col.gameObject != gameObject && !col.isTrigger)
            {
                isTouchingPlayer = true;
                break;
            }
        }
    }

    private void ClampVelocity()
    {
        float maxSpeedX = 30f;
        Vector2 clampedVelocity = controller.rb.linearVelocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxSpeedX, maxSpeedX);

        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, isInverted ? -maxFallSpeed * 1.5f : -maxFallSpeed, isInverted ? maxFallSpeed : maxFallSpeed * 1.5f);

        bool isKnocked = controller.knockback != null && controller.knockback.isKnockedBack;

        if (wasGroundedLastFrame && !justJumped && !isKnocked && !hasPlayerOnHead && !isTouchingPlayer)
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

    private void LogDebug(string message)
    {
        if (debugLogOverlap) Debug.Log($"[겹침디버그:{gameObject.name}] {message}", this);
    }

    private void CancelUnexpectedPlayerCollisionPush()
    {
        if (!hasCommandedVelocity) return;

        if (controller.knockback != null && (controller.knockback.isKnockedBack || controller.knockback.IsStunned))
            return;

        if (!IsOverlappingAnyPlayer()) return;

        float actualX = controller.rb.linearVelocity.x;
        float diff = actualX - lastCommandedVelocityX;

        if (Mathf.Abs(diff) > cornerPushEpsilon)
        {
            controller.rb.linearVelocity = new Vector2(lastCommandedVelocityX, controller.rb.linearVelocity.y);
        }
    }

    private bool IsOverlappingAnyPlayer()
    {
        Vector2 boxCenter = controller.bodyCollider.bounds.center;
        Vector2 boxSize = (Vector2)controller.bodyCollider.bounds.size + new Vector2(0.02f, 0.02f);

        int hitCount = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerFilter, playerCheckResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = playerCheckResults[i];
            if (col.gameObject != gameObject && !col.isTrigger) return true;
        }
        return false;
    }

    private void UpdateTimers() { if (ckTimer > 0f) ckTimer -= Time.deltaTime; }

    private void UpdateCoyoteTime()
    {
        if (isGrounded && ckTimer <= 0f) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
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