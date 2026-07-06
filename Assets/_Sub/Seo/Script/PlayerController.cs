using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("스폰 시스템")]
    public Vector3 currentSpawnPoint;

    [Header("무브")]
    public float moveSpeed = 8f;

    // 🌟 가짜 마찰력/관성 셋팅
    [Header("관성 및 미끄러짐 셋팅")]
    public float normalFriction = 20f;   // 일반 땅: 즉시 멈춤급
    public float iceSlideFriction = 3f;  // 빙판: 쭈욱 미끄러짐
    public float airFriction = 3f;       // 공중: 미끄러짐 유지

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
    [Tooltip("기즈모 씬 뷰 표시 여부")]
    public bool showGizmo = true;

    [Tooltip("바닥/벽 감지 반경(크기)")]
    [Range(0.01f, 5f)]
    public float checkRadius = 0.5f;

    [Tooltip("감지 위치 조절 (X: 좌우, Y: 상하)")]
    public Vector2 checkOffset = Vector2.zero;
    private bool isKnockedBack = false;
    // 💡 넉백 시 강제로 유지할 X축 속도를 저장할 변수
    private float activeKnockbackX;

    private Rigidbody2D rb;
    private float horizontalInput;

    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isOnIce = false;
    private bool wasOnIceLastFrame = false; // 공중 관성 유지용

    // 🌟 박스 콜라이더 전용 변수 (캡슐 콜라이더와 분리)
    private BoxCollider2D boxCollider;

    private Animator anim;

    // 💡 눈보라(바람) 속도 및 발판 속도 변수
    public float windVelocity = 0f;
    private Vector2 platformVelocity = Vector2.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 🌟 여러 콜라이더 중 BoxCollider2D만 정확히 가져옵니다.
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

                // 🌟 땅에 닿으면(착지) 꺼뒀던 박스 콜라이더를 다시 켭니다!
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

            // 🌟 [추가된 합체 로직 시작] 🌟
            PlayerCombineHandler combine = GetComponent<PlayerCombineHandler>();

            if (combine != null && combine.isCombined)
            {
                // 내가 유령(Ghost)이면 여기서 Update 조작 연산을 아예 중단합니다.
                if (gameObject != combine.bodyTarget) return;

                // 내가 본체(Body)라면 핸들러가 네트워크/로컬에서 취합해 준 통합 입력값을 가져옵니다.
                horizontalInput = combine.GetCombinedHorizontalInput();
            }
            else
            {
                // 기존 일반 상태일 때의 기본 조작 입력 방식
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;
                    else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f;
                }
            }
            // 🌟 [추가된 합체 로직 끝] 🌟

            // 좌우 반전 기믹 (합체 상태에서도 부호 반전 정상 작동)
            if (isReversedControl)
            {
                horizontalInput *= -1f;
            }

            // 일반 상태일 때만 자체 점프 입력을 받음 (합체 점프는 핸들러가 CallCombinedJump를 통해 쏴줍니다)
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
                // 합체 상태일 때도 타이머는 깎아줍니다.
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

    // 🌟 [새로 추가된 함수] 합체 상태에서 외부(핸들러)가 본체에게 점프/액션을 시키기 위한 창구
    public void CallCombinedJump()
    {
        Jump();
    }

    public void CallCombinedAction()
    {
        // 중력 내리찍기 기믹: 순간 Y속도 초기화 후 아래로 강한 충격량 급가속
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

        // 🌟 유령 상태인 플레이어는 물리 연산을 완전히 건너뜁니다.
        PlayerCombineHandler combine = GetComponent<PlayerCombineHandler>();
        if (combine != null && combine.isCombined && gameObject != combine.bodyTarget) return;

        CheckGroundOrPlayer();

        // 💡 넉백 중일 때는 벽에 비벼도 강제 속도를 매 프레임 유지합니다!
        if (isKnockedBack)
        {
            rb.linearVelocity = new Vector2(activeKnockbackX, rb.linearVelocity.y);
        }
        else if (stunTimer <= 0f) // (여기에 있던 !isKnockedBack 조건은 위로 빠졌으므로 삭제)
        {
            float targetVelocityX = (horizontalInput * moveSpeed) + platformVelocity.x + windVelocity;

            // 🌟 보간(Lerp) 대입 방식: 네트워크 최적화 및 미끄러짐 구현
            bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
            float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;

            float smoothedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, currentFriction * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(smoothedVelocityX, rb.linearVelocity.y);
        }
        else if (stunTimer > 0f)
        {
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
        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            if (col.isTrigger) continue;

            if (((1 << col.gameObject.layer) & groundLayer) != 0 || col.CompareTag("Player"))
            {
                isGrounded = true;
                if (col.CompareTag("Ice")) currentOnIce = true;
                if (col.TryGetComponent<CoopPatrolPlatform>(out var platform)) platformVelocity = platform.CurrentVelocity;
                break;
            }
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
            CheckSpikeHit(); // 🌟 따로 빼둔 로직 호출!
        }
    }

    private void CheckSpikeHit()
    {
        // 일반 피격 쿨다운 중이면 무시 (0.5초 대기)
        if (spikeDamageCooldown > 0f) return;

        spikeDamageCooldown = 0.5f;
        spikeHitCount++;
        spikeResetTimer = 1.5f;     // 1.5초 안에 연달아 맞아야 유지됨

        // 3번 이상 맞았고, 크리티컬 쿨다운이 끝났을 때
        if (spikeHitCount >= 3 && criticalCooldownTimer <= 0f)
        {
            // 🌟 50% 확률 계산 (Random.value는 0.0 ~ 1.0 사이의 값을 반환합니다)
            if (Random.value <= 0.5f)
            {
                // 당첨! 크리티컬 탈출 발동
                StartCoroutine(CriticalEscape(0.1f));
                criticalCooldownTimer = 1.0f; // 1초 쿨다운 시작
            }
            else
            {
                // 꽝! 크리티컬 실패 -> 일반 넉백
                CmdTakeKnockback(new Vector2(-1f, 0.5f));
            }

            // 성공하든 실패하든 카운트는 다시 0으로 초기화!
            spikeHitCount = 0;
            spikeResetTimer = 0f;
        }
        else if (spikeHitCount < 3)
        {
            // 아직 3번이 안 찼을 때는 일반 넉백
            CmdTakeKnockback(new Vector2(-1f, 0.5f));
        }
    }
    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        // 🌟 크리티컬 탈출 시 박스 콜라이더만 끕니다! (캡슐은 켜져 있음)
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
        // 🌟 일반 넉백 시에도 박스 콜라이더만 끕니다!
        if (boxCollider != null) boxCollider.enabled = false;

        // 💡 넉백 시 X축 속도를 별도로 기록해둡니다 (FixedUpdate에서 유지하기 위함).
        activeKnockbackX = knockDir.x * knockPowerX;

        // AddForce 대신 확실하게 초기 속도를 덮어씌웁니다.
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
        // 인스펙터에서 껐다면 바로 종료
        if (!showGizmo) return;

        // 눈에 가장 잘 띄는 노란색으로 고정
        Gizmos.color = Color.yellow;

        // 플레이어의 중심점 + 인스펙터에서 설정한 위치(Offset)
        Vector2 checkPosition = (Vector2)transform.position + checkOffset;

        // 테두리 원 그리기
        Gizmos.DrawWireSphere(checkPosition, checkRadius);
    }
}