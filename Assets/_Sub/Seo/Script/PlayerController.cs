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

    private Rigidbody2D rb;
    private float horizontalInput;

    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (ckTimer > 0f)
        {
            ckTimer -= Time.deltaTime;
        }

        if (isGrounded && ckTimer <= 0f)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        horizontalInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                horizontalInput = -1f;
            }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                horizontalInput = 1f;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                Jump();
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }

            if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * superJump);
            }
        }

        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = jumpSpeed * fallSpeed;
        }
        else
        {
            rb.gravityScale = jumpSpeed;
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        CheckGroundOrPlayer();

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

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
                bool hitGround = ((1 << col.gameObject.layer) & groundLayer) != 0;
                bool hitPlayer = col.CompareTag("Player");

                if (hitGround || hitPlayer)
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

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            CmdHandleCollision(collision.contacts[0].normal);
        }
    }

    [Command]
    void CmdHandleCollision(Vector2 hitNormal)
    {
        RpcApplyBounceEffect(hitNormal);
    }

    [ClientRpc]
    void RpcApplyBounceEffect(Vector2 hitNormal)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(hitNormal * 20f, ForceMode2D.Impulse);
    }
}