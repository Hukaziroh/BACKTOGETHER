using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerController controller;
    public bool isRestrictedByRope = false;
    private int playerLayerMask;

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

    [Header("그라운드 체크")]
    [Range(0.01f, 5f)]
    public float checkRadius = 0.5f;
    public Vector2 checkOffset = Vector2.zero;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public bool isGrounded { get; private set; }

    [Header("헤드 체크 (머리 위 플레이어 감지)")]
    public Vector2 headCheckBoxSize = new Vector2(0.8f, 0.2f);
    public Vector2 headCheckOffset = new Vector2(0f, 0.5f);
    public Transform headCheck;
    public bool hasPlayerOnHead { get; private set; }

    [Header("바람 효과")]
    public float windVelocity = 0f;

    private Transform currentPlatform;
    private Vector2 platformVelocity;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        playerLayerMask = LayerMask.GetMask("Player");
    }

    // 💡 [리팩토링]: PlayerController의 FixedUpdate에서 순서대로 호출
    public void CustomFixedUpdate()
    {
        CheckGrounded();
        CheckPlayerOnHead();
        UpdatePlatformVelocity();
        HandleMovementPhysics();
    }

    private void CheckGrounded()
    {
        if (groundCheck == null) return;

        Vector2 gCheckPosition = (Vector2)groundCheck.position + checkOffset;
        Collider2D hit = Physics2D.OverlapCircle(gCheckPosition, checkRadius, groundLayer);

        isGrounded = (hit != null);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            currentPlatform = hit.transform;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
            currentPlatform = null;
        }
    }

    private void CheckPlayerOnHead()
    {
        if (headCheck == null) return;

        Vector2 hCheckPosition = (Vector2)headCheck.position + headCheckOffset;
        Collider2D hit = Physics2D.OverlapBox(hCheckPosition, headCheckBoxSize, 0f, playerLayerMask);

        hasPlayerOnHead = (hit != null && hit.gameObject != gameObject);
    }

    private void UpdatePlatformVelocity()
    {
        platformVelocity = Vector2.zero;
        if (currentPlatform == null) return;

        if (currentPlatform.TryGetComponent<CoopPlatformBase>(out var platformBase))
        {
            platformVelocity = platformBase.CurrentVelocity;
            return;
        }

        var mirror = currentPlatform.GetComponentInParent<CoopMirrorPlatforms>();
        if (mirror != null)
        {
            if (currentPlatform.gameObject == mirror.leftPlatform.gameObject || currentPlatform.IsChildOf(mirror.leftPlatform.transform))
                platformVelocity = mirror.LeftVelocity;
            else
                platformVelocity = mirror.RightVelocity;
            return;
        }
    }

    private void HandleMovementPhysics()
    {
        if (controller == null || controller.rb == null) return;

        // 넉백 중이거나 스턴 상태일 때는 일반 이동 제어 금지
        if (controller.knockback != null && controller.knockback.IsStunned) return;

        float targetXInput = controller.input != null ? controller.input.HorizontalInput : 0f;
        Vector2 currentVel = controller.rb.linearVelocity;

        float targetVelX = targetXInput * moveSpeed + windVelocity + platformVelocity.x;
        float friction = isGrounded ? normalFriction : airFriction;

        float newVelX = Mathf.Lerp(currentVel.x, targetVelX, Time.fixedDeltaTime * friction);

        // 점프 처리
        float newVelY = currentVel.y;
        if (controller.input != null && controller.input.JumpPressedThisFrame)
        {
            if ((isGrounded || coyoteTimeCounter > 0f) && !hasPlayerOnHead)
            {
                float gravityMult = (controller.gravityModule != null) ? controller.gravityModule.gravityMultiplier : 1f;
                newVelY = jumpSpeed * gravityMult;
                coyoteTimeCounter = 0f;
            }
        }

        // 가변 점프 (짧게 누르면 낮게)
        if (controller.input != null && controller.input.JumpReleasedThisFrame)
        {
            float gravityMult = (controller.gravityModule != null) ? controller.gravityModule.gravityMultiplier : 1f;
            if ((gravityMult > 0 && newVelY > 0) || (gravityMult < 0 && newVelY < 0))
            {
                newVelY *= superJump;
            }
        }

        controller.rb.linearVelocity = new Vector2(newVelX, newVelY);
    }

    public void CallCombinedJump()
    {
        if ((isGrounded || coyoteTimeCounter > 0f) && !hasPlayerOnHead)
        {
            if (controller != null && controller.rb != null)
            {
                controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, 0f);
                float gravityMult = (controller.gravityModule != null) ? controller.gravityModule.gravityMultiplier : 1f;
                controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, jumpSpeed * gravityMult);
            }
            coyoteTimeCounter = 0f;
        }
    }

    public void ApplyShortJump()
    {
        if (controller != null && controller.rb != null)
        {
            float gravityMult = (controller.gravityModule != null) ? controller.gravityModule.gravityMultiplier : 1f;
            Vector2 currentVel = controller.rb.linearVelocity;
            if ((gravityMult > 0 && currentVel.y > 0) || (gravityMult < 0 && currentVel.y < 0))
            {
                controller.rb.linearVelocity = new Vector2(currentVel.x, currentVel.y * superJump);
            }
        }
    }

    public void CallCombinedAction()
    {
        if (controller != null && controller.rb != null)
        {
            controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, 0f);
            controller.rb.AddForce(Vector2.down * 18f, ForceMode2D.Impulse);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Vector2 gCheckPosition = (Vector2)groundCheck.position + checkOffset;
            Gizmos.DrawWireSphere(gCheckPosition, checkRadius);
        }

        if (headCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector2 hCheckPosition = (Vector2)headCheck.position + headCheckOffset;
            Gizmos.DrawWireCube(hCheckPosition, headCheckBoxSize);
        }
    }
}