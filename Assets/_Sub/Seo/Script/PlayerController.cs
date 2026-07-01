using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("스폰 시스템")]
    public Vector3 currentSpawnPoint;

    [Header("무브")]
    public float moveSpeed = 8f;

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

    private bool isKnockedBack = false;

    private Rigidbody2D rb;
    private float horizontalInput;

    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
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

        if (isGrounded && ckTimer <= 0f) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (isKnockedBack)
        {
            // 1. 공중으로 날아가는 동안 조작 불가
            horizontalInput = 0f;

            // 2. 바닥에 닿았고, 위로 솟구치는 중이 아니라면 (착지 판정)
            if (isGrounded && rb.linearVelocity.y <= 0.1f)
            {
                isKnockedBack = false; // 비행 상태 종료
                stunTimer = stunTime;  // 땅에 닿은 이 순간부터 스턴 시간(0.5초) 시작
            }
        }
        else if (stunTimer > 0f)
        {
            // 3. 착지 후 땅에서 기절해 있는 동안
            stunTimer -= Time.deltaTime;
            horizontalInput = 0f;
        }
        else
        {
            // 4. 기절도 끝났고 정상 상태일 때 (기존 조작 로직 그대로)
            horizontalInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f;

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
        if (rb.linearVelocity.y < 0f) rb.gravityScale = jumpSpeed * fallSpeed;
        else rb.gravityScale = jumpSpeed;

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);

        // 스턴 상태가 아닐 때만 좌우 방향 뒤집기
        if (horizontalInput != 0 && stunTimer <= 0f)
        {
            transform.localScale = new Vector3(horizontalInput > 0 ? 1 : -1, 1, 1);
        }

        if (transform.position.y < -30f)
        {
            Respawn();
        }
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
        rb.linearVelocity = Vector2.zero; // 날아가던 관성 초기화
        isKnockedBack = false;            // 넉백 상태 강제 해제
        stunTimer = 0f;                   // 스턴 상태 강제 해제
    }
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        CheckGroundOrPlayer();

        if (stunTimer <= 0f && !isKnockedBack)
        {
            // 💡 핵심: 키보드 이동 속도 + 움직이는 발판 속도 + 눈보라 바람 속도 
            float targetVelocityX = (horizontalInput * moveSpeed) + platformVelocity.x + windVelocity;
            rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        }
        else if (stunTimer > 0f && !isKnockedBack)
        {
            // 🌟 땅에 떨어져서 기절한 상태일 때 서서히 미끄러짐 (스마일모 방식)
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

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            if (col.isTrigger) continue;

            if (((1 << col.gameObject.layer) & groundLayer) != 0 || col.CompareTag("Player"))
            {
                isGrounded = true;

                if (col.TryGetComponent<CoopPatrolPlatform>(out var platform))
                {
                    platformVelocity = platform.CurrentVelocity;
                }
                break;
            }
        }
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
            Vector2 knockDir = new Vector2(-1f, 0.5f);
            CmdTakeKnockback(knockDir);
        }
    }

    [Command]
    void CmdTakeKnockback(Vector2 knockDir)
    {
        RpcApplyKnockback(knockDir);
    }

    [ClientRpc]
    void RpcApplyKnockback(Vector2 knockDir)
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 force = new Vector2(knockDir.x * knockPowerX, knockDir.y * knockPowerY);
        rb.AddForce(force, ForceMode2D.Impulse);

        isKnockedBack = true;
        stunTimer = 0f;

        anim.SetTrigger("Hit");

        StartCoroutine(DisableColliderForSeconds(0.1f));
    }

    private System.Collections.IEnumerator DisableColliderForSeconds(float seconds)
    {
        playerCollider.enabled = false;
        yield return new WaitForSeconds(seconds);
        playerCollider.enabled = true;
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
}