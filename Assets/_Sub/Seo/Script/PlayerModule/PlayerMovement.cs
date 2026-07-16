using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerController controller;

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
    private Collider2D[] groundCheckResults = new Collider2D[5];
    private Collider2D[] headCheckResults = new Collider2D[5];

    [Header("외부 환경 속도")]
    public float windVelocity = 0f;
    private Transform currentPlatform;
    private Vector2 platformVelocity;

    private bool isPushingPlayer = false;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");
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

        // 🌟 [유일한 변경점] 밧줄에 묶인 채 공중에 매달려 있다면, X축 속도를 강제로 0으로 만들지 않습니다!
        // 이렇게 해야 닻처럼 버티지 않고 2명이 당기는 힘에 의해 부드럽게 딸려갑니다.
        if (GetComponent<DistanceJoint2D>() != null && !isGrounded)
        {
            return;
        }

        float targetVelocityX = (controller.input.HorizontalInput * moveSpeed) + windVelocity;

        if (isPushingPlayer)
        {
            targetVelocityX = 0f;
        }

        bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
        float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;
        float accel = currentFriction * moveSpeed;
        float currentPlatformVelX = isGrounded ? platformVelocity.x : 0f;

        float currentLocalVelocityX = controller.rb.linearVelocity.x - currentPlatformVelX;

        float smoothedVelocityX = Mathf.MoveTowards(
            currentLocalVelocityX,
            targetVelocityX,
            accel * Time.fixedDeltaTime);

        if (Mathf.Abs(controller.input.HorizontalInput) < 0.01f && Mathf.Abs(windVelocity) < 0.01f)
        {
            if (Mathf.Abs(smoothedVelocityX) < 0.5f) smoothedVelocityX = 0f;
        }

        float finalX = smoothedVelocityX;
        if (isGrounded)
        {
            finalX += platformVelocity.x;
        }

        controller.rb.linearVelocity = new Vector2(finalX, controller.rb.linearVelocity.y);
    }

    private void ClampVelocity()
    {
        float maxSpeedX = 30f;
        Vector2 clampedVelocity = controller.rb.linearVelocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxSpeedX, maxSpeedX);
        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        if (isInverted) clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -maxFallSpeed * 1.5f, maxFallSpeed);
        else clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -maxFallSpeed, maxFallSpeed * 1.5f);
        controller.rb.linearVelocity = clampedVelocity;
    }

    void CheckGroundOrPlayer()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.useTriggers = false;
        filter.layerMask = groundLayer | playerLayerMask;

        int hitCount = Physics2D.OverlapCircle(groundCheck.position, checkRadius, filter, groundCheckResults);

        isGrounded = false;
        bool currentOnIce = false;
        bool foundPlatform = false;
        Transform detectedPlatform = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = groundCheckResults[i];
            if (col.CompareTag("Spike")) continue;
            if (col.gameObject == gameObject || col.isTrigger) continue;

            isGrounded = true;
            if (col.CompareTag("Ice")) currentOnIce = true;
            if (col.CompareTag("MovingPlatform"))
            {
                detectedPlatform = col.transform;
                foundPlatform = true;
            }
        }

        if (foundPlatform)
        {
            if (currentPlatform != detectedPlatform)
            {
                currentPlatform = detectedPlatform;
            }
        }
        else currentPlatform = null;

        isOnIce = currentOnIce;
        if (isGrounded) wasOnIceLastFrame = isOnIce;
    }

    void CheckHeadForPlayer()
    {
        if (headCheck == null) return;

        hasPlayerOnHead = false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.useTriggers = false;
        filter.layerMask = playerLayerMask;

        Vector2 checkPosition = (Vector2)headCheck.position + headCheckOffset;

        int hitCount = Physics2D.OverlapBox(checkPosition, headCheckBoxSize, 0f, filter, headCheckResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = headCheckResults[i];

            if (col.gameObject == gameObject || col.isTrigger) continue;

            Vector2 dir = col.transform.position - transform.position;
            if (dir.y <= 0.2f) continue;

            if (col.attachedRigidbody != null && col.attachedRigidbody.linearVelocity.y > 0.1f)
                continue;

            hasPlayerOnHead = true;
            break;
        }
    }

    private void HandleMovingPlatform()
    {
        platformVelocity = Vector2.zero;

        if (currentPlatform == null)
            return;

        CoopRoundTripPlatform platform = currentPlatform.GetComponentInParent<CoopRoundTripPlatform>();

        if (platform != null)
        {
            platformVelocity = platform.CurrentVelocity;
        }
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isLocalPlayer) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.7f)
                {
                    float normalX = contact.normal.x;
                    float inputX = controller.input.HorizontalInput;

                    if ((inputX > 0.1f && normalX < 0f) || (inputX < -0.1f && normalX > 0f))
                    {
                        isPushingPlayer = true;
                        return;
                    }
                }
            }
        }

        isPushingPlayer = false;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!isLocalPlayer) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isPushingPlayer = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Vector2 gCheckPosition = (Vector2)groundCheck.position + checkOffset;
            Gizmos.DrawWireSphere(gCheckPosition, checkRadius);
        }

        if (headCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector2 hCheckPosition = (Vector2)headCheck.position + headCheckOffset;
            Gizmos.DrawWireCube(hCheckPosition, headCheckBoxSize);
        }
    }
}