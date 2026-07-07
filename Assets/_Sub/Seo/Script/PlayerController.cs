using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
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
    public float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;

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

    private BoxCollider2D boxCollider;
    private Animator anim;

    [Header("외부 환경 속도")]
    public float windVelocity = 0f;
    private Vector2 platformVelocity = Vector2.zero;

    private Transform currentPlatform;
    private Vector3 lastPlatformPos;

    // 🌟 중력 모듈 변수 추가
    private PlayerGravityController gravityModule;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
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
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;
                    else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f;
                }
            }

            if (isReversedControl)
            {
                horizontalInput *= -1f;
            }

            if (combine == null || !combine.isCombined)
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
                    else jumpBufferCounter -= Time.deltaTime;

                    if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
                    {
                        Jump();
                        jumpBufferCounter = 0f;
                        coyoteTimeCounter = 0f;
                    }

                    // 🌟 역중력 상태에서의 슈퍼점프 대응
                    bool inverted = gravityModule != null && gravityModule.isGravityInverted;
                    bool isMovingUp = inverted ? (rb.linearVelocity.y < 0f) : (rb.linearVelocity.y > 0f);

                    if (Keyboard.current.spaceKey.wasReleasedThisFrame && isMovingUp)
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * superJump);
                }
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
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
        Jump();
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
        }
        else if (stunTimer <= 0f)
        {
            float targetVelocityX = (horizontalInput * moveSpeed) + platformVelocity.x + windVelocity;

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                Vector2 checkDir = horizontalInput > 0 ? Vector2.right : Vector2.left;

                Vector2 boxSize = new Vector2(boxCollider.bounds.size.x, boxCollider.bounds.size.y * 0.8f);
                RaycastHit2D[] hits = Physics2D.BoxCastAll(boxCollider.bounds.center, boxSize, 0f, checkDir, 0.15f);

                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
                    {
                        if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0 || hit.collider.CompareTag("Player"))
                        {
                            targetVelocityX = platformVelocity.x + windVelocity;
                            break;
                        }
                    }
                }
            }

            rb.mass = 1f;

            bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
            float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;

            float smoothedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, currentFriction * Time.fixedDeltaTime);

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
        bool isInverted = gravityModule != null && gravityModule.isGravityInverted;
        if (isInverted)
        {
            if (rb.linearVelocity.y > maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
        else
        {
            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
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

    void Jump()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y) * jumpSpeed;
        float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);

        // 🌟 모듈의 multiplier 곱하기 (뒤집히면 아래로 점프)
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

        // 🌟 넉백 시에도 중력 방향을 적용
        float mult = gravityModule != null ? gravityModule.gravityMultiplier : 1f;
        rb.linearVelocity = new Vector2(activeKnockbackX, knockDir.y * knockPowerY * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.1f;

        CmdPlayHitAnimation();
    }

    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        if (criticalUI != null) criticalUI.SetActive(true);

        activeKnockbackX = -30f;

        // 🌟 크리티컬 넉백 시에도 중력 방향 적용
        float mult = gravityModule != null ? gravityModule.gravityMultiplier : 1f;
        rb.linearVelocity = new Vector2(activeKnockbackX, 40f * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.5f;

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