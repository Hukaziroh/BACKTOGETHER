using UnityEngine;
using Mirror;

public class PlayerAnimation : NetworkBehaviour
{
    private PlayerController controller;

    [SyncVar(hook = nameof(OnDirectionChanged))]
    public float syncDirectionX = 1f;

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

        if (controller.input.HorizontalInput != 0 && !controller.knockback.IsStunned)
        {
            float targetDirection = controller.input.HorizontalInput > 0 ? 1f : -1f;

            if (syncDirectionX != targetDirection)
            {
                CmdSetDirection(targetDirection);
            }
        }

        ApplyScale(syncDirectionX);
    }

    [Command]
    private void CmdSetDirection(float dir)
    {
        syncDirectionX = dir;
    }

    private void OnDirectionChanged(float oldDir, float newDir)
    {
        ApplyScale(newDir);
    }

    private void ApplyScale(float dirX)
    {
        bool isInverted = false;
        if (controller != null && controller.gravityModule != null)
        {
            isInverted = controller.gravityModule.isGravityInverted;
        }

        transform.localScale = new Vector3(
            dirX,
            isInverted ? -1f : 1f,
            1f
        );
    }
}