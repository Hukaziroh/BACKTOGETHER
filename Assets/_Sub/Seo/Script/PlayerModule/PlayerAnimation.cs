using UnityEngine;
using Mirror;

public class PlayerAnimation : NetworkBehaviour
{
    private PlayerController controller;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (controller.anim == null) return;

        controller.anim.SetFloat("Speed", Mathf.Abs(controller.input.HorizontalInput));
        controller.anim.SetBool("isGrounded", controller.movement.isGrounded);

        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;

        // 스턴 상태가 아니고 입력이 있을 때만 방향 전환
        if (controller.input.HorizontalInput != 0 && !controller.knockback.IsStunned)
        {
            transform.localScale = new Vector3(
                controller.input.HorizontalInput > 0 ? 1 : -1,
                isInverted ? -1f : 1f,
                1);
        }
    }
}