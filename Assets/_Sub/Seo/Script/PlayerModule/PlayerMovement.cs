using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerController controller;

    [Header("이동 및 점프 (AddForce 방식)")]
    public float moveForce = 60f; // AddForce를 위한 힘
    public float maxSpeed = 7f;   // 최대 속도 제한
    public float friction = 10f;  // 바닥 마찰력 (감속용)
    public float jumpForce = 15f;

    [Header("동적 질량 (핵심!)")]
    [Tooltip("땅에 있을 때의 무게 (무거워야 매달린 남을 잘 끕니다)")]
    public float groundedMass = 5f;
    [Tooltip("공중에 있을 때의 무게 (가벼워야 잘 끌려옵니다)")]
    public float airborneMass = 1f;

    public bool isGrounded { get; private set; }
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        CheckGrounded();
        HandleJump();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        HandleMovement();
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 🌟 핵심: 땅에 있으면 무겁게(닻 역할), 공중에 있으면 가볍게!
        controller.rb.mass = isGrounded ? groundedMass : airborneMass;
    }

    private void HandleMovement()
    {
        float inputX = controller.input.HorizontalInput;

        // 🌟 핵심: 땅에 있을 때만 좌우 이동 조작 허용 (공중 이동 불가)
        if (isGrounded)
        {
            if (Mathf.Abs(inputX) > 0.1f)
            {
                // 질량에 비례해서 힘을 주어야 무게가 무거워져도 똑같은 가속도를 냅니다.
                controller.rb.AddForce(new Vector2(inputX * moveForce * controller.rb.mass, 0f));
            }
            else
            {
                // 입력이 없으면 마찰력으로 즉시 정지
                controller.rb.AddForce(new Vector2(-controller.rb.linearVelocity.x * friction * controller.rb.mass, 0f));
            }
        }

        // 최대 속도 제한 (AddForce 폭주 방지)
        if (Mathf.Abs(controller.rb.linearVelocity.x) > maxSpeed)
        {
            controller.rb.linearVelocity = new Vector2(Mathf.Sign(controller.rb.linearVelocity.x) * maxSpeed, controller.rb.linearVelocity.y);
        }
    }

    private void HandleJump()
    {
        // 공중 좌우 이동은 막혔지만, 점프 입력은 정상적으로 Impulse(충격량)를 발생시킴
        if (controller.input.JumpPressedThisFrame && isGrounded)
        {
            controller.rb.AddForce(Vector2.up * jumpForce * controller.rb.mass, ForceMode2D.Impulse);
        }
    }
}