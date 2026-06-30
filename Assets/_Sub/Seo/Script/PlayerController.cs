using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;

    [Header("Jump Settings")]
    public float jumpHeight = 3f;
    public float jumpSpeed = 4f;
    public float fallSpeed = 2.5f;
    public float maxFallSpeed = 20f;

    [Header("Jump Feel Settings")]
    public float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    public float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;

    [Range(0f, 1f)]
    public float superJump = 0.5f;
    private float jumpCk = 0.1f;
    private float ckTimer;

    [Header("Trap Settings")]
    public float knockPowerX = 10f;
    public float knockPowerY = 5f;
    public float stunTime = 0.5f;
    private float stunTimer;

    private Rigidbody2D rb;
    private float horizontalInput;

    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private Collider2D playerCollider;

    private Animator anim;

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

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            horizontalInput = 0f;
        }
        else
        {
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
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        CheckGroundOrPlayer();

        if (stunTimer <= 0f)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
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
        foreach (var col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                if (((1 << col.gameObject.layer) & groundLayer) != 0 || col.CompareTag("Player"))
                {
                    isGrounded = true;
                    break;
                }
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

        stunTimer = stunTime;

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
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow cam = mainCam.GetComponent<CameraFollow>() ?? mainCam.gameObject.AddComponent<CameraFollow>();
            cam.target = transform;
        }
    }
}