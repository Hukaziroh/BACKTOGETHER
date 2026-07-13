using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerController controller;

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
    [Range(0.01f, 5f)]
    public float checkRadius = 0.5f;
    public Vector2 checkOffset = Vector2.zero;
    public Transform groundCheck;
    public LayerMask groundLayer;

    public bool isGrounded { get; private set; }
    private bool isOnIce = false;
    private bool wasOnIceLastFrame = false;
    private Collider2D[] groundCheckResults = new Collider2D[5];

    [Header("외부 환경 속도")]
    public float windVelocity = 0f;
    private Vector2 platformVelocity = Vector2.zero;
    private Vector2 storedPlatformVelocity = Vector2.zero;
    private Vector2 platformNetworkVelocity = Vector2.zero;
    private Transform currentPlatform;
    private Vector3 lastPlatformPos;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        UpdateTimers();
        UpdateCoyoteTime();
        HandleJumpInput();
        UpdateGravity();
        CalculatePlatformVelocityNetwork();
    }

    private void CalculatePlatformVelocityNetwork()
    {
        if (currentPlatform != null)
        {
            Vector3 delta = currentPlatform.position - lastPlatformPos;

            // 네트워크 보간(Update)된 이동량을 초당 속도로 변환
            if (Time.deltaTime > 0f && delta.magnitude > 0.0001f && delta.magnitude < 1.5f)
            {
                platformNetworkVelocity = delta / Time.deltaTime;
            }
            else if (delta.magnitude == 0)
            {
                // 🔥 핵심 해결책: 클라이언트에서 패킷이 늦게 와서 플랫폼이 1프레임 멈추더라도, 
                // 관성이 증발하지 않도록 이전 속도를 부드럽게 유지(Lerp)해줍니다!
                platformNetworkVelocity = Vector2.Lerp(platformNetworkVelocity, Vector2.zero, Time.deltaTime * 10f);
            }

            lastPlatformPos = currentPlatform.position;
        }
        else
        {
            platformNetworkVelocity = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        // 다른 플레이어에 합체되어 종속된 상태라면 자체 물리 이동 금지
        if (controller.combineHandler != null && controller.combineHandler.isCombined && gameObject != controller.combineHandler.bodyTarget)
            return;

        CheckGroundOrPlayer();
        HandleMovingPlatform();
        HandleMovementPhysics();
        ClampVelocity();
    }

    private void UpdateTimers()
    {
        if (ckTimer > 0f) ckTimer -= Time.deltaTime;
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded && ckTimer <= 0f) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJumpInput()
    {
        if (controller.combineHandler == null || !controller.combineHandler.isCombined)
        {
            if (controller.input.JumpPressedThisFrame && coyoteTimeCounter > 0f && !controller.knockback.IsStunned)
            {
                Jump();
                coyoteTimeCounter = 0f;
            }

            bool inverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
            bool isMovingUp = inverted ? (controller.rb.linearVelocity.y < 0f) : (controller.rb.linearVelocity.y > 0f);

            if (controller.input.JumpReleasedThisFrame && isMovingUp && !controller.knockback.IsStunned)
            {
                if (controller.syncJumpHandler != null && controller.syncJumpHandler.isInSyncZone)
                {
                    controller.syncJumpHandler.CmdCutSyncJump();
                }
                else
                {
                    ApplyShortJump();
                }
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
        if (isMovingUpCheck)
        {
            controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, controller.rb.linearVelocity.y * superJump);
        }
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
        if (controller.knockback.isKnockedBack) return; // 넉백 처리는 PlayerKnockback에서 수행

        if (controller.knockback.IsStunned)
        {
            float slideSpeed = Mathf.Lerp(controller.rb.linearVelocity.x, 0f, 10f * Time.fixedDeltaTime);
            controller.rb.linearVelocity = new Vector2(slideSpeed, controller.rb.linearVelocity.y);
            return;
        }

        float targetVelocityX = (controller.input.HorizontalInput * moveSpeed) + platformVelocity.x + windVelocity;
        bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
        float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;

        float smoothedVelocityX = Mathf.Lerp(controller.rb.linearVelocity.x, targetVelocityX, currentFriction * Time.fixedDeltaTime);

        if (Mathf.Abs(controller.input.HorizontalInput) < 0.01f && Mathf.Abs(platformVelocity.x) < 0.01f && Mathf.Abs(windVelocity) < 0.01f)
        {
            if (Mathf.Abs(smoothedVelocityX) < 0.5f) smoothedVelocityX = 0f;
        }

        controller.rb.linearVelocity = new Vector2(smoothedVelocityX, controller.rb.linearVelocity.y);
    }

    private void ClampVelocity()
    {
        float maxSpeedX = 25f;
        Vector2 clampedVelocity = controller.rb.linearVelocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxSpeedX, maxSpeedX);

        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        if (isInverted)
        {
            clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -maxFallSpeed * 1.5f, maxFallSpeed);
        }
        else
        {
            clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -maxFallSpeed, maxFallSpeed * 1.5f);
        }

        controller.rb.linearVelocity = clampedVelocity;
    }

    void CheckGroundOrPlayer()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.useTriggers = false;
        filter.layerMask = groundLayer | (1 << LayerMask.NameToLayer("Player"));

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
                lastPlatformPos = currentPlatform.position;
                storedPlatformVelocity = Vector2.zero;
            }
            // 🌟 플랫폼 위에서는 위치 강제 이동을 쓰므로, 속도 기반 관성은 0으로 끕니다 (이중 이동 방지)
            platformVelocity = Vector2.zero;
        }
        else
        {
            // 🌟 방금 전까지 플랫폼에 있다가 발이 떨어졌을 때 (점프 or 낙하) -> 플랫폼의 속도를 그대로 물려받음!
            if (currentPlatform != null && !isGrounded)
            {
                platformVelocity = storedPlatformVelocity;
            }
            // 일반 바닥에 착지했을 때 -> 관성 초기화
            else if (isGrounded)
            {
                platformVelocity = Vector2.zero;
            }

            currentPlatform = null;
            storedPlatformVelocity = Vector2.zero;
        }

        isOnIce = currentOnIce;
        if (isGrounded) wasOnIceLastFrame = isOnIce;
    }

    private void HandleMovingPlatform()
    {
        if (currentPlatform == null) return;

        Vector2 platformMoveDelta = platformNetworkVelocity * Time.fixedDeltaTime;

        controller.rb.position += platformMoveDelta;

        storedPlatformVelocity = platformNetworkVelocity;

        bool inverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        bool isFalling = inverted ? (controller.rb.linearVelocity.y > 0f) : (controller.rb.linearVelocity.y < 0f);

        if (Mathf.Abs(platformMoveDelta.y) > 0.001f && isFalling)
        {
            controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, 0f);
        }

    }
    public void CallCombinedJump()
    {
        if (isGrounded || coyoteTimeCounter > 0f)
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
        if (!showGizmo || groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Vector2 checkPosition = (Vector2)groundCheck.position + checkOffset;
        Gizmos.DrawWireSphere(checkPosition, checkRadius);
    }
}