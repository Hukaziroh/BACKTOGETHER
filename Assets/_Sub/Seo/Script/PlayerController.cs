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
    private float knockbackGraceTimer = 0f; // 🌟 가시에 맞고 바로 바닥에 닿았다고 인식하는 것을 막는 타이머
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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();

        // 🌟 [추가됨] 이중 보간 드리프트 버그(-0.079 회전 버그) 완벽 차단!
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
            horizontalInput = 0f; // 🌟 넉백(공중) 중 조작 완벽 차단

            if (knockbackGraceTimer > 0f)
                knockbackGraceTimer -= Time.deltaTime;

            // 🌟 넉백 후 땅에 닿으면 즉시 기절(Stun) 모드로 돌입!
            if (knockbackGraceTimer <= 0f && isGrounded && rb.linearVelocity.y <= 0.1f)
            {
                isKnockedBack = false;
                stunTimer = stunTime; // 인스펙터에 설정한 시간(0.5초)만큼 기절!
            }
        }
        else if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            horizontalInput = 0f; // 🌟 기절한 동안 조작 차단
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

        if (currentPlatform != null)
        {
            Vector3 currentPlatPos = currentPlatform.position;
            Vector3 platformDelta = currentPlatPos - lastPlatformPos;

            if (platformDelta.magnitude < 1.5f)
            {
                rb.position += (Vector2)platformDelta;

                if (platformDelta.y > 0.001f && rb.linearVelocity.y < 0f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                }
            }

            lastPlatformPos = currentPlatPos;
        }

        if (isKnockedBack)
        {
            rb.mass = 1f;
            // 🌟 넉백 중 X축 속도 강제 고정 삭제! 이제 포물선 그리며 자연스럽게 날아감.
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

            // 🌟 [삭제 완료] 무거워지던 rb.mass = 50f 삭제! 자연스럽게 1로 고정
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
            // 기절 상태에서는 바닥에 미끄러지며 부드럽게 멈춤
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

                bool isRidingPlayer = isOtherPlayer && (transform.position.y > col.transform.position.y + 0.1f);

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
                ApplyLocalKnockback(new Vector2(-1f, 0.5f)); // 무조건 왼쪽(-1f)
            }

            spikeHitCount = 0;
            spikeResetTimer = 0f;
        }
        else if (spikeHitCount < 3)
        {
            ApplyLocalKnockback(new Vector2(-1f, 0.5f)); // 무조건 왼쪽(-1f)
        }
    }

    // 🌟 서버를 기다리지 않고 내 화면에서 즉시 강제로 날려버림! (조작 개입 불가)
    private void ApplyLocalKnockback(Vector2 knockDir)
    {
        activeKnockbackX = knockDir.x * knockPowerX;

        // 딱 한 번만 속도를 부여하고 중력에 곡선을 맡김
        rb.linearVelocity = new Vector2(activeKnockbackX, knockDir.y * knockPowerY);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.1f; // 가시 맞자마자 바닥 닿은 것으로 오해하는 것 방지

        CmdPlayHitAnimation(); // 다른 플레이어들에게 "나 맞았어 애니메이션 틀어줘" 라고 전달
    }

    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        if (criticalUI != null) criticalUI.SetActive(true);

        activeKnockbackX = -30f;
        rb.linearVelocity = new Vector2(activeKnockbackX, 40f);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.5f;

        CmdPlayHitAnimation();

        yield return new WaitForSeconds(seconds);
        yield return new WaitForSeconds(2f);

        if (criticalUI != null) criticalUI.SetActive(false);
    }

    // 🌟 콜라이더를 끄는 코루틴은 모두 지우고, 네트워크로는 가볍게 '애니메이션'만 동기화!
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

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Vector2 checkPosition = (Vector2)transform.position + checkOffset;
        Gizmos.DrawWireSphere(checkPosition, checkRadius);
    }
}