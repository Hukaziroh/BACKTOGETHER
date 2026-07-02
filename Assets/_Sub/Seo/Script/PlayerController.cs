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

    [Header("기믹: 좌우반전")]
    private bool isReversedControl = false;
    private float reverseTimer = 0f;

    private bool isKnockedBack = false;

    private Rigidbody2D rb;
    private float horizontalInput;

    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isOnIce = false;
    private bool wasOnIceLastFrame = false; // 공중 관성 유지용
    private Collider2D playerCollider;

    private Animator anim;

    // 💡 눈보라(바람) 속도 및 발판 속도 변수
    public float windVelocity = 0f;
    private Vector2 platformVelocity = Vector2.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (ckTimer > 0f) ckTimer -= Time.deltaTime;
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

        if (stunTimer <= 0f && !isKnockedBack)
        {
            float targetVelocityX = (horizontalInput * moveSpeed) + platformVelocity.x + windVelocity;

            // 🌟 보간(Lerp) 대입 방식: 네트워크 최적화 및 미끄러짐 구현
            bool isSlippery = isOnIce || (!isGrounded && wasOnIceLastFrame);
            float currentFriction = isSlippery ? (isGrounded ? iceSlideFriction : airFriction) : normalFriction;

            float smoothedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, currentFriction * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(smoothedVelocityX, rb.linearVelocity.y);
        }
        else if (stunTimer > 0f && !isKnockedBack)
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLocalPlayer) return;

        if (collision.gameObject.CompareTag("Spike"))
        {
            // 가시에 닿을 때마다 카운트 증가 & 3초 타이머 갱신
            spikeHitCount++;
            spikeResetTimer = 0.5f;

            // 3번 이상 닿았으면 크리티컬 탈출!
            if (spikeHitCount >= 3)
            {
                StartCoroutine(CriticalEscape(0.1f));
                spikeHitCount = 0; // 초기화
                spikeResetTimer = 0f;
            }
            else
            {
                // 평소에는 일반 넉백 (기존 CmdTakeKnockback 그대로 사용)
                CmdTakeKnockback(new Vector2(-1f, 0.5f));
            }
        }
    }
    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        playerCollider.enabled = false;

        float escapeSpeedX = 30f;
        float escapeSpeedY = 40f;

        rb.linearVelocity = new Vector2(-escapeSpeedX, escapeSpeedY);
        isKnockedBack = true;
        anim.SetTrigger("Hit");

        yield return new WaitForSeconds(seconds);

        playerCollider.enabled = true;
    }

    [Command]
    void CmdTakeKnockback(Vector2 knockDir) { RpcApplyKnockback(knockDir); }

    [ClientRpc]
    void RpcApplyKnockback(Vector2 knockDir)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(knockDir.x * knockPowerX, knockDir.y * knockPowerY), ForceMode2D.Impulse);
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
}