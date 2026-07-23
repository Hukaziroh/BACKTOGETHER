using UnityEngine;
using Mirror;

public class PlayerKnockback : NetworkBehaviour
{
    private PlayerController controller;

    [Header("장애물판정")]
    public float knockPowerX = 15f;
    public float knockPowerY = 37f;
    public float stunTime = 0.7f;
    public float stunTimer { get; private set; }

    [Header("크리티컬 ")]
    private int spikeHitCount = 0;
    private float spikeResetTimer = 0f;
    public GameObject criticalUI;
    private float spikeDamageCooldown = 0f;
    private float criticalCooldownTimer = 0f;

    public bool isKnockedBack { get; private set; }
    private float knockbackGraceTimer = 0f;

    private float knockbackTimeoutTimer = 0f;

    private float activeKnockbackX;

    public bool IsStunned => isKnockedBack || stunTimer > 0f;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    // 💡 [리팩토링]: PlayerController에서 순서대로 호출
    public void CustomUpdate()
    {
        UpdateTimers();
    }

    // 💡 [리팩토링]: PlayerController의 FixedUpdate에서 순서대로 호출
    public void CustomFixedUpdate()
    {
        HandleKnockbackPhysics();
    }

    private void UpdateTimers()
    {
        if (spikeDamageCooldown > 0f) spikeDamageCooldown -= Time.deltaTime;
        if (criticalCooldownTimer > 0f) criticalCooldownTimer -= Time.deltaTime;

        if (spikeResetTimer > 0f)
        {
            spikeResetTimer -= Time.deltaTime;
            if (spikeResetTimer <= 0f)
            {
                spikeHitCount = 0;
            }
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }
    }

    private void HandleKnockbackPhysics()
    {
        if (!isKnockedBack) return;

        if (knockbackGraceTimer > 0f)
        {
            knockbackGraceTimer -= Time.fixedDeltaTime;
        }

        if (knockbackTimeoutTimer > 0f)
        {
            knockbackTimeoutTimer -= Time.fixedDeltaTime;
            if (knockbackTimeoutTimer <= 0f)
            {
                ResetKnockback();
                return;
            }
        }

        if (knockbackGraceTimer <= 0f && controller != null && controller.movement != null && controller.movement.isGrounded)
        {
            ResetKnockback();
        }
    }

    public void ApplyKnockbackFromEye(Vector3 eyePosition)
    {
        if (!isLocalPlayer) return;

        Vector2 knockDir = (transform.position - eyePosition).normalized;
        activeKnockbackX = (knockDir.x >= 0 ? 1f : -1f) * knockPowerX;

        float mult = (controller != null && controller.gravityModule != null) ? controller.gravityModule.gravityMultiplier : 1f;
        if (controller != null && controller.rb != null)
        {
            controller.rb.linearVelocity = new Vector2(activeKnockbackX, knockDir.y * knockPowerY * mult);
        }

        isKnockedBack = true;
        stunTimer = stunTime;
        knockbackGraceTimer = 0.2f;
        knockbackTimeoutTimer = 3.0f;

        CmdPlayHitAnimation();
    }

    public void ResetKnockback()
    {
        isKnockedBack = false;
        stunTimer = 0f;
        knockbackTimeoutTimer = 0f;
        knockbackGraceTimer = 0f;
    }

    [Command]
    public void CmdPlayHitAnimation() { RpcPlayHitAnimation(); }

    [ClientRpc]
    void RpcPlayHitAnimation()
    {
        if (controller != null && controller.anim != null) controller.anim.SetTrigger("Hit");
    }
}