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
    private Vector2 platformVelocity = Vector2.zero; // 💡 플랫폼 속도 변수 유지

    private Transform currentPlatform;
    private Vector3 lastPlatformPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
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
            if (isGrounded && rb.linearVelocity.y <= 0.1f)
            {
                isKnockedBack = false;
                stunTimer = stunTime;

                if (boxCollider != null) boxCollider.enabled = true;
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
                    if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0f)
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

        if (rb.linearVelocity.y < 0f) rb.gravityScale = jumpSpeed * fallSpeed;
        else rb.gravityScale = jumpSpeed;

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);

        if (horizontalInput != 0 && stunTimer <= 0f)
            transform.localScale = new Vector3(horizontalInput > 0 ? 1 : -1, 1, 1);

        if (transform.position.y < -50f) Respawn();
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

        // 1. 플랫폼 동화 (Position Delta 방식 추적)
        if (currentPlatform != null)
        {
            Vector3 currentPlatPos = currentPlatform.position;
            Vector3 platformDelta = currentPlatPos - lastPlatformPos;

            if (platformDelta.magnitude < 1.5f)
            {
                rb.position += (Vector2)platformDelta;
            }

            lastPlatformPos = currentPlatPos;
        }

        // 2. 순수 플레이어 이동 및 질량 조절 로직 (요청하신 코드 적용)
        if (isKnockedBack)
        {
            rb.mass = 1f; // 넉백 중일 땐 정상 무게로 돌려놔서 잘 날아가게 함
            rb.linearVelocity = new Vector2(activeKnockbackX, rb.linearVelocity.y);
        }
        else if (stunTimer <= 0f)
        {
            float targetVelocityX = (horizontalInput * moveSpeed) + platformVelocity.x + windVelocity;

            // 🌟 [겹침 방지 핵심 로직] 내 진행 방향에 다른 플레이어나 벽이 있다면 강제 전진을 멈춥니다.
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                Vector2 checkDir = horizontalInput > 0 ? Vector2.right : Vector2.left;

                // 캐릭터 크기보다 살짝 작은 박스를 앞쪽으로 0.05만큼 쏴서 부딪히는게 있는지 검사
                Vector2 boxSize = new Vector2(boxCollider.bounds.size.x, boxCollider.bounds.size.y * 0.8f);
                RaycastHit2D[] hits = Physics2D.BoxCastAll(boxCollider.bounds.center, boxSize, 0f, checkDir, 0.05f);

                foreach (var hit in hits)
                {
                    // 나 자신이 아니고 트리거가 아닌 콜라이더를 만났을 때
                    if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
                    {
                        // 그게 바닥(벽)이거나 다른 플레이어라면?
                        if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0 || hit.collider.CompareTag("Player"))
                        {
                            // 억지로 파고들지 않도록 내 키보드 조작 속도만 0으로 날려버립니다!
                            // (발판 이동이나 바람에 의한 밀림은 유지)
                            targetVelocityX = platformVelocity.x + windVelocity;
                            break;
                        }
                    }
                }
            }

            // 부드러운 감속 로직
            bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
            float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;

            float smoothedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, currentFriction * Time.fixedDeltaTime);

            // 미세한 소수점 진동 컷팅 로직 (유지)
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

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    void CheckGroundOrPlayer()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, checkRadius);
        isGrounded = false;
        platformVelocity = Vector2.zero; // 매 프레임 초기화

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

                // 🌟 패트롤 플랫폼 스크립트가 있다면 속도값(Velocity) 가져오기
                if (col.TryGetComponent<CoopPatrolPlatform>(out var platform))
                {
                    platformVelocity = platform.CurrentVelocity;
                }

                // 🌟 [수정됨] Kinematic이더라도 '플레이어'라면 이동 발판(currentPlatform)으로 인식하지 않음!
                if (col.attachedRigidbody != null && col.attachedRigidbody.bodyType == RigidbodyType2D.Kinematic && !col.CompareTag("Player"))
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
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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
                CmdTakeKnockback(new Vector2(-1f, 0.5f));
            }

            spikeHitCount = 0;
            spikeResetTimer = 0f;
        }
        else if (spikeHitCount < 3)
        {
            CmdTakeKnockback(new Vector2(-1f, 0.5f));
        }
    }

    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        if (boxCollider != null) boxCollider.enabled = false;
        if (criticalUI != null) criticalUI.SetActive(true);

        float escapeSpeedX = 30f;
        float escapeSpeedY = 40f;

        activeKnockbackX = -escapeSpeedX;
        rb.linearVelocity = new Vector2(activeKnockbackX, escapeSpeedY);

        isKnockedBack = true;
        anim.SetTrigger("Hit");

        yield return new WaitForSeconds(seconds);

        yield return new WaitForSeconds(2f);
        if (criticalUI != null) criticalUI.SetActive(false);
    }

    [Command]
    void CmdTakeKnockback(Vector2 knockDir) { RpcApplyKnockback(knockDir); }

    [ClientRpc]
    void RpcApplyKnockback(Vector2 knockDir)
    {
        if (boxCollider != null) boxCollider.enabled = false;

        activeKnockbackX = knockDir.x * knockPowerX;
        rb.linearVelocity = new Vector2(activeKnockbackX, knockDir.y * knockPowerY);

        isKnockedBack = true;
        stunTimer = 0f;
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

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Vector2 checkPosition = (Vector2)transform.position + checkOffset;
        Gizmos.DrawWireSphere(checkPosition, checkRadius);
    }
}