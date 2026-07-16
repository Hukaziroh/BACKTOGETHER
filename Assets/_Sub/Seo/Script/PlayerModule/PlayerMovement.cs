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

    [Header("로프 기믹 설정")]
    [Tooltip("밧줄이 당기는 힘 (숫자가 클수록 확 끌려옵니다)")]
    public float ropePullForce = 200f;
    [Tooltip("진자운동 튕김 방지 (브레이크 역할)")]
    public float ropeDamping = 10f;

    [HideInInspector] public Transform ropeLeftNeighbor;
    [HideInInspector] public Transform ropeRightNeighbor;
    [HideInInspector] public float maxRopeLength;

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

        // 🌟 [검증 1] 완벽한 질량 비대칭화
        // 밧줄이 몸에 달려있을 때, 땅에 굳건히 서있으면 질량 2! 공중에 떨어지면 질량 1!
        if (ropeLeftNeighbor != null || ropeRightNeighbor != null)
        {
            controller.rb.mass = isGrounded ? 2.0f : 1.0f;
        }
        else
        {
            controller.rb.mass = 1.0f;
        }

        CheckHeadForPlayer();
        HandleMovementPhysics();
        ClampVelocity();

        // 이동 코드가 다 끝난 후 가장 마지막에 밧줄 텐션을 더해줍니다.
        if (ropeLeftNeighbor != null || ropeRightNeighbor != null)
        {
            ApplyCustomRopeTension();
        }
    }

    // 🌟 [검증 2] 키네메틱 족쇄를 푼 커스텀 텐션
    private void ApplyCustomRopeTension()
    {
        ApplyForceFromNeighbor(ropeLeftNeighbor);
        ApplyForceFromNeighbor(ropeRightNeighbor);
    }

    private void ApplyForceFromNeighbor(Transform neighbor)
    {
        if (neighbor == null) return;

        float distance = Vector2.Distance(transform.position, neighbor.position);
        if (distance > maxRopeLength)
        {
            Vector2 pullDir = (neighbor.position - transform.position).normalized;
            float stretch = distance - maxRopeLength;

            // 1. 강한 고무줄 텐션 추가
            controller.rb.AddForce(pullDir * (ropePullForce * stretch), ForceMode2D.Force);

            // 2. 과거의 치명적이었던 '속도 강제 삭제(족쇄)' 코드를 지우고, 부드러운 소프트 댐핑으로 교체!
            Vector2 velocity = controller.rb.linearVelocity;
            float outwardSpeed = Vector2.Dot(velocity, -pullDir);
            if (outwardSpeed > 0)
            {
                // 완전히 멈추지 않고 스프링처럼 부드럽게 감속시켜 진자운동만 제어합니다.
                controller.rb.AddForce(-pullDir * (outwardSpeed * ropeDamping), ForceMode2D.Force);
            }
        }
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

        if (isPushingPlayer)
        {
            targetVelocityX = 0f;
        }

        bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
        float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;

        // 🌟 [검증 3] 브레이크(마찰) 제거 로직!
        if (ropeLeftNeighbor != null || ropeRightNeighbor != null)
        {
            if (Mathf.Abs(controller.input.HorizontalInput) < 0.01f)
            {
                // 입력이 없을 때: 땅에선 버티라고 마찰력(2f) 부여, 공중에선 완벽한 짐짝이 되도록 마찰력(0f) 완전 제거!
                currentFriction = isGrounded ? 2f : 0f;
            }
        }

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