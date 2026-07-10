using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("입력 설정 (New Input System)")]
    public InputAction moveAction;
    public InputAction jumpAction;

    [Header("스폰 시스템")]
    public Vector3 currentSpawnPoint;

    [Header("무브")]
    public float moveSpeed = 8f;

    [Header("관성 및 미끄러짐 셋팅")]
    public float normalFriction = 20f;
    public float iceSlideFriction = 3f;
    public float airFriction = 3f;

    [Header("점프셋팅")]
    public float jumpHeight = 3f;
    public float jumpSpeed = 4f;
    public float fallSpeed = 2.5f;
    public float maxFallSpeed = 20f;

    [Header("점프세부셋팅")]
    public float coyoteTime = 0.15f;
    private float coyoteTimeCounter;


    [Range(0f, 1f)]
    public float superJump = 0.5f;
    private float jumpCk = 0.1f;
    private float ckTimer;

    [Header("장애물판정")]
    public float knockPowerX = 10f;
    public float knockPowerY = 5f;
    public float stunTime = 0.5f;
    private float stunTimer;

    [Header("크리티컬 ")]
    private int spikeHitCount = 0;
    private float spikeResetTimer = 0f;
    public GameObject criticalUI;

    private float spikeDamageCooldown = 0f;
    private float criticalCooldownTimer = 0f;

    [Header("기믹: 좌우반전")]
    private bool isReversedControl = false;
    private float reverseTimer = 0f;

    [Header("기즈모 및 감지 범위 설정")]
    public bool showGizmo = true;
    [Range(0.01f, 5f)]
    public float checkRadius = 0.5f;
    public Vector2 checkOffset = Vector2.zero;

    private bool isKnockedBack = false;
    private float knockbackGraceTimer = 0f;
    private float activeKnockbackX;

    private Rigidbody2D rb;
    private float horizontalInput;

    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isOnIce = false;
    private bool wasOnIceLastFrame = false;

    private CapsuleCollider2D mainCollider;
    private Animator anim;

    [Header("외부 환경 속도")]
    public float windVelocity = 0f;
    private Vector2 platformVelocity = Vector2.zero;

    private Transform currentPlatform;
    private Vector3 lastPlatformPos;

    // 🌟 중력 모듈 변수 추가
    private PlayerGravityController gravityModule;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<CapsuleCollider2D>();
        anim = GetComponent<Animator>();
        gravityModule = GetComponent<PlayerGravityController>();
        if (moveAction == null || moveAction.bindings.Count == 0)
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
               .With("Negative", "<Keyboard>/a")
               .With("Negative", "<Keyboard>/leftArrow")
               .With("Negative", "<Gamepad>/dpad/left")
               .With("Negative", "<Gamepad>/leftStick/left")
               .With("Positive", "<Keyboard>/d")
               .With("Positive", "<Keyboard>/rightArrow")
               .With("Positive", "<Gamepad>/dpad/right")
               .With("Positive", "<Gamepad>/leftStick/right");
        }
        if (jumpAction == null || jumpAction.bindings.Count == 0)
        {
            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<CapsuleCollider2D>();
        anim = GetComponent<Animator>();

        // 🌟 중력 모듈 연결
        gravityModule = GetComponent<PlayerGravityController>();

        if (!isLocalPlayer)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.None;
            transform.rotation = Quaternion.identity;
        }
    }
    void OnDisable()
    {
        if (isLocalPlayer)
        {
            moveAction.Disable();
            jumpAction.Disable();
        }
    }
    void Update()
    {

        if (!isLocalPlayer) return;

        if (ckTimer > 0f) ckTimer -= Time.deltaTime;

        if (spikeDamageCooldown > 0f) spikeDamageCooldown -= Time.deltaTime;
        if (criticalCooldownTimer > 0f) criticalCooldownTimer -= Time.deltaTime;

        if (spikeResetTimer > 0f)
        {
            spikeResetTimer -= Time.deltaTime;
            if (spikeResetTimer <= 0f) spikeHitCount = 0;
        }

        if (isGrounded && ckTimer <= 0f) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (isKnockedBack)
        {
            horizontalInput = 0f;

            if (knockbackGraceTimer > 0f)
                knockbackGraceTimer -= Time.deltaTime;

            if (knockbackGraceTimer <= 0f && isGrounded && Mathf.Abs(rb.linearVelocity.y) <= 0.1f)
            {
                isKnockedBack = false;
                stunTimer = stunTime;
            }
        }
        else if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            horizontalInput = 0f;
        }
        else
        {
            horizontalInput = 0f;

            PlayerCombineHandler combine = GetComponent<PlayerCombineHandler>();

            if (combine != null && combine.isCombined)
            {
                if (gameObject != combine.bodyTarget) return;
                horizontalInput = combine.GetCombinedHorizontalInput();
            }
            else
            {
                // 🌟 복잡했던 키보드 비교문이 이 한 줄로 끝납니다!
                horizontalInput = moveAction.ReadValue<float>();
            }

            if (isReversedControl)
            {
                horizontalInput *= -1f;
            }

            if (combine == null || !combine.isCombined)
            {
                // 🌟 점프 버퍼 관련 복잡한 계산 싹 다 날림! 
                // 스페이스바를 '누른 그 프레임'에, '코요테 타임(또는 땅)'이 살아있고, '넉백'이 아니면 바로 점프!
                if (jumpAction.WasPressedThisFrame() && coyoteTimeCounter > 0f && !isKnockedBack)
                {
                    Jump();
                    coyoteTimeCounter = 0f;
                }

                // 🌟 수정된 숏점프(슈퍼점프) 로직
                bool inverted = gravityModule != null && gravityModule.isGravityInverted;
                bool isMovingUp = inverted ? (rb.linearVelocity.y < 0f) : (rb.linearVelocity.y > 0f);

                // 추가: 현재 플레이어가 동기화 점프 구역에 있는지 확인
                PlayerSyncJump syncJump = GetComponent<PlayerSyncJump>();
                bool isSyncJumping = (syncJump != null && syncJump.isInSyncZone);
                if (jumpAction.WasReleasedThisFrame() && isMovingUp)
                {
                    ApplyShortJump(); // 1. 나 자신의 점프 끊기 (아래에서 새로 만들 메서드)

                    // 2. 동기화 구역 안이라면, 다른 사람들도 똑같이 점프를 끊으라고 서버에 신호 전송
                    if (isSyncJumping)
                    {
                        syncJump.CmdCutSyncJump();
                    }
                }
            }

            if (isReversedControl)
            {
                reverseTimer -= Time.deltaTime;
                if (reverseTimer <= 0f)
                {
                    StopReverseControl();
                }
            }
        }


        // 🌟 중력 방향에 따른 낙하 가속(fallSpeed) 역방향 패치
        bool isInverted = gravityModule != null && gravityModule.isGravityInverted;
        float mult = gravityModule != null ? gravityModule.gravityMultiplier : 1f;
        bool isFalling = isInverted ? (rb.linearVelocity.y > 0f) : (rb.linearVelocity.y < 0f);

        if (isFalling) rb.gravityScale = jumpSpeed * fallSpeed * mult;
        else rb.gravityScale = jumpSpeed * mult;

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);

        // 🌟 좌우 이동 시 Y축 스케일 고정 버그 수정
        if (horizontalInput != 0 && stunTimer <= 0f)
        {
            float currentY = isInverted ? -1f : 1f;
            transform.localScale = new Vector3(horizontalInput > 0 ? 1 : -1, currentY, 1);
        }

        if (transform.position.y < -50f || transform.position.y > 50f) Respawn();
    }


    public void CallCombinedJump()
    {
        if (isGrounded || coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            Jump();
            coyoteTimeCounter = 0f;
        }
    }

    public void CallCombinedAction()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.down * 18f, ForceMode2D.Impulse);
    }

    public void SetSpawnPoint(Vector3 newPoint)
    {
        if (!isLocalPlayer) return;
        currentSpawnPoint = newPoint;
    }

    public void Respawn()
    {
        if (!isLocalPlayer) return;

        transform.position = currentSpawnPoint;
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
        stunTimer = 0f;
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        PlayerCombineHandler combine = GetComponent<PlayerCombineHandler>();
        if (combine != null && combine.isCombined && gameObject != combine.bodyTarget) return;

        CheckGroundOrPlayer();

        if (currentPlatform != null)
        {
            Vector3 currentPlatPos = currentPlatform.position;
            Vector3 platformDelta = currentPlatPos - lastPlatformPos;

            if (platformDelta.magnitude < 1.5f)
            {
                rb.position += (Vector2)platformDelta;

                bool inverted = gravityModule != null && gravityModule.isGravityInverted;
                bool isFalling = inverted ? (rb.linearVelocity.y > 0f) : (rb.linearVelocity.y < 0f);

                if (Mathf.Abs(platformDelta.y) > 0.001f && isFalling)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                }
            }

            lastPlatformPos = currentPlatPos;
        }

        if (isKnockedBack)
        {
            rb.mass = 1f;

            // 🌟 넉백 초반(0.2초)에는 1칸짜리 벽에 가로막혀 속도가 0이 되는 것을 무시하고 강제로 밀어붙입니다!
            // 이렇게 하면 Y축으로 살짝 떴을 때 벽을 부드럽게 타고 뒤로 넘어가게 됩니다.
            if (knockbackGraceTimer > 0f)
            {
                rb.linearVelocity = new Vector2(activeKnockbackX, rb.linearVelocity.y);
            }
        }
        else if (stunTimer <= 0f)
        {
            float targetVelocityX = (horizontalInput * moveSpeed) + platformVelocity.x + windVelocity;
            bool isBlocked = false; // 🌟 막혔는지 확인하는 스위치!

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                Vector2 checkDir = horizontalInput > 0 ? Vector2.right : Vector2.left;

                // 🌟 boxCollider 대신 mainCollider 사용!
                Vector2 boxSize = new Vector2(mainCollider.bounds.size.x, mainCollider.bounds.size.y * 0.8f);
                RaycastHit2D[] hits = Physics2D.BoxCastAll(mainCollider.bounds.center, boxSize, 0f, checkDir, 0.15f);

                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
                    {
                        if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0 || hit.collider.CompareTag("Player"))
                        {
                            targetVelocityX = platformVelocity.x + windVelocity;
                            isBlocked = true; // 🌟 벽이나 플레이어에 막힘 감지!
                            break;
                        }
                    }
                }
            }

            rb.mass = 1f;

            bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
            float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;

            float smoothedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, currentFriction * Time.fixedDeltaTime);

            // 🌟 막혔을 때는 서서히 멈추지 말고 즉시 멈추기! (벽 파고들기 차단)
            if (isBlocked)
            {
                smoothedVelocityX = targetVelocityX;
            }

            if (Mathf.Abs(horizontalInput) < 0.01f && Mathf.Abs(platformVelocity.x) < 0.01f && Mathf.Abs(windVelocity) < 0.01f)
            {
                if (Mathf.Abs(smoothedVelocityX) < 0.5f) smoothedVelocityX = 0f;
            }

            rb.linearVelocity = new Vector2(smoothedVelocityX, rb.linearVelocity.y);
        }
        else if (stunTimer > 0f)
        {
            rb.mass = 1f;
            float slideSpeed = Mathf.Lerp(rb.linearVelocity.x, 0f, 10f * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(slideSpeed, rb.linearVelocity.y);
        }

        // 🌟 최고 낙하 속도(maxFallSpeed) 천장 패치
        float maxSpeedX = 25f; // X축 폭주 제한 속도 (너무 튕겨나간다 싶으면 줄이세요)
        Vector2 clampedVelocity = rb.linearVelocity;

        // 1. X축 폭주 강제 제한 (벽 뚫고 나가는 현상 방지)
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxSpeedX, maxSpeedX);

        // 2. Y축 낙하 속도 및 폭주 제한 (중력 반전 고려)
        bool isInverted = gravityModule != null && gravityModule.isGravityInverted;
        if (isInverted)
        {
            // 중력 반전: 위로 떨어지는 속도(maxFallSpeed) 제한, 아래로 튕기는 폭주 제한
            clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -maxFallSpeed * 1.5f, maxFallSpeed);
        }
        else
        {
            // 정상 중력: 아래로 떨어지는 속도(-maxFallSpeed) 제한, 위로 튕기는 폭주 제한
            clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -maxFallSpeed, maxFallSpeed * 1.5f);
        }

        // 최종 안전한 속도 적용
        rb.linearVelocity = clampedVelocity;
    }
    void CheckGroundOrPlayer()
    {

        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, checkRadius);
        isGrounded = false;
        platformVelocity = Vector2.zero;

        bool currentOnIce = false;
        bool foundPlatform = false;

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            if (col.isTrigger) continue;

            if (((1 << col.gameObject.layer) & groundLayer) != 0 || col.CompareTag("Player"))
            {
                isGrounded = true;
                if (col.CompareTag("Ice")) currentOnIce = true;

                if (col.TryGetComponent<CoopPatrolPlatform>(out var platform))
                {
                    platformVelocity = platform.CurrentVelocity;
                }

                bool isKinematicPlatform = (col.attachedRigidbody != null && col.attachedRigidbody.bodyType == RigidbodyType2D.Kinematic && !col.CompareTag("Player"));

                bool isOtherPlayer = col.CompareTag("Player");

                // 중력 반전 시 위에 타는 판정 수정
                bool inverted = gravityModule != null && gravityModule.isGravityInverted;
                bool isRidingPlayer;
                if (inverted)
                    isRidingPlayer = isOtherPlayer && (transform.position.y < col.transform.position.y - 0.1f);
                else
                    isRidingPlayer = isOtherPlayer && (transform.position.y > col.transform.position.y + 0.1f);

                if (isKinematicPlatform || isRidingPlayer)
                {
                    foundPlatform = true;
                    if (currentPlatform != col.transform)
                    {
                        currentPlatform = col.transform;
                        lastPlatformPos = currentPlatform.position;
                    }
                }
                break;
            }
        }

        if (!foundPlatform)
        {
            currentPlatform = null;
        }

        isOnIce = currentOnIce;
        if (isGrounded) wasOnIceLastFrame = isOnIce;
    }

    public void Jump()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y) * jumpSpeed;
        float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);

        float mult = gravityModule != null ? gravityModule.gravityMultiplier : 1f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * mult);
        ckTimer = jumpCk;

    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isLocalPlayer) return;

        if (other.CompareTag("Spike"))
        {
            CheckSpikeHit();
        }
    }

    private void CheckSpikeHit()
    {
        if (spikeDamageCooldown > 0f) return;

        spikeDamageCooldown = 0.5f;
        spikeHitCount++;
        spikeResetTimer = 1.5f;

        // 🌟 기획 의도대로 무조건 왼쪽(-1f)으로 튕기도록 롤백
        if (spikeHitCount >= 3 && criticalCooldownTimer <= 0f)
        {
            if (Random.value <= 0.5f)
            {
                StartCoroutine(CriticalEscape(0.1f));
                criticalCooldownTimer = 1.0f;
            }
            else
            {
                ApplyLocalKnockback(new Vector2(-1f, 0.5f));
            }

            spikeHitCount = 0;
            spikeResetTimer = 0f;
        }
        else if (spikeHitCount < 3)
        {
            ApplyLocalKnockback(new Vector2(-1f, 0.5f));
        }
    }

    private void ApplyLocalKnockback(Vector2 knockDir)
    {
        activeKnockbackX = knockDir.x * knockPowerX;

        float mult = gravityModule != null ? gravityModule.gravityMultiplier : 1f;
        rb.linearVelocity = new Vector2(activeKnockbackX, knockDir.y * knockPowerY * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        // 🌟 0.1f에서 0.2f로 늘림 (작은 턱을 타고 넘어갈 충분한 시간)
        knockbackGraceTimer = 0.2f;

        CmdPlayHitAnimation();
    }

    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        if (criticalUI != null) criticalUI.SetActive(true);

        // 🌟 크리티컬 시에도 무조건 왼쪽(-30f)으로 날아가도록 롤백
        activeKnockbackX = -30f;

        float mult = gravityModule != null ? gravityModule.gravityMultiplier : 1f;
        rb.linearVelocity = new Vector2(activeKnockbackX, 40f * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.5f; // (벽을 뚫고 넘어가는 무적 시간은 유지)

        CmdPlayHitAnimation();

        yield return new WaitForSeconds(seconds);
        yield return new WaitForSeconds(2f);

        if (criticalUI != null) criticalUI.SetActive(false);
    }

    [Command]
    void CmdPlayHitAnimation() { RpcPlayHitAnimation(); }

    [ClientRpc]
    void RpcPlayHitAnimation()
    {
        anim.SetTrigger("Hit");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        moveAction.Enable();
        jumpAction.Enable();

        currentSpawnPoint = transform.position;
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow cam = mainCam.GetComponent<CameraFollow>() ?? mainCam.gameObject.AddComponent<CameraFollow>();
            cam.target = transform;
        }
    }

    public void StartReverseControl(float duration)
    {
        if (!isLocalPlayer) return;
        isReversedControl = true;
        reverseTimer = duration;
    }

    public void StopReverseControl()
    {
        if (!isLocalPlayer) return;
        isReversedControl = false;
        reverseTimer = 0f;
    }

    public void ApplyShortJump()
    {
        bool inverted = gravityModule != null && gravityModule.isGravityInverted;
        bool isMovingUpCheck = inverted ? (rb.linearVelocity.y < 0f) : (rb.linearVelocity.y > 0f);

        // 올라가고 있는 중일 때만 속도를 깎아서 숏점프 적용
        if (isMovingUpCheck)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * superJump);
        }
    }

    public void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Vector2 checkPosition = (Vector2)transform.position + checkOffset;
        Gizmos.DrawWireSphere(checkPosition, checkRadius);
    }
}