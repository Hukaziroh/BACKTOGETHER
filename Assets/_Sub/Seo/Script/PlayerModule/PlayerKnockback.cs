using UnityEngine;
using Mirror;

public class PlayerKnockback : NetworkBehaviour
{
    private PlayerController controller;

    [Header("장애물판정")]
    public float knockPowerX = 15f;
    public float knockPowerY = 37f;
    public float stunTime = 0.5f;

    [SyncVar] public float stunTimer;

    [Header("크리티컬 ")]
    private int spikeHitCount = 0;
    private float spikeResetTimer = 0f;
    public GameObject criticalUI;
    private float spikeDamageCooldown = 0f;
    private float criticalCooldownTimer = 0f;

    [SyncVar] public bool isKnockedBack;

    private float knockbackGraceTimer = 0f;
    private float knockbackTimeoutTimer = 0f;
    private float activeKnockbackX;

    public bool IsStunned => isKnockedBack || stunTimer > 0f;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!isServer) return;
        UpdateTimers();
    }

    void FixedUpdate()
    {
        if (!isServer) return;
        HandleKnockbackPhysics();

        float maxSafeSpeed = 40f;
        if (controller.rb.linearVelocity.magnitude > maxSafeSpeed)
        {
            controller.rb.linearVelocity = controller.rb.linearVelocity.normalized * maxSafeSpeed;
        }
    }

    private void UpdateTimers()
    {
        if (spikeDamageCooldown > 0f) spikeDamageCooldown -= Time.deltaTime;
        if (criticalCooldownTimer > 0f) criticalCooldownTimer -= Time.deltaTime;

        if (spikeResetTimer > 0f)
        {
            spikeResetTimer -= Time.deltaTime;
            if (spikeResetTimer <= 0f) spikeHitCount = 0;
        }
    }

    private void HandleKnockbackPhysics()
    {
        if (isKnockedBack)
        {
            if (knockbackGraceTimer > 0f) knockbackGraceTimer -= Time.fixedDeltaTime;
            if (knockbackTimeoutTimer > 0f) knockbackTimeoutTimer -= Time.fixedDeltaTime;

            controller.rb.linearVelocity = new Vector2(activeKnockbackX, controller.rb.linearVelocity.y);

            bool hitGround = knockbackGraceTimer <= 0f && controller.movement.isGrounded && Mathf.Abs(controller.rb.linearVelocity.y) <= 0.1f;
            bool airTimeout = knockbackTimeoutTimer <= 0f;

            if (hitGround || airTimeout)
            {
                isKnockedBack = false;
                stunTimer = hitGround ? stunTime : 0f;
            }
        }
        else if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            float slideSpeed = Mathf.Lerp(controller.rb.linearVelocity.x, 0f, 10f * Time.fixedDeltaTime);
            controller.rb.linearVelocity = new Vector2(slideSpeed, controller.rb.linearVelocity.y);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isServer) return;

        if (other.CompareTag("Spike"))
        {
            CheckSpikeHit();
        }
    }

    private void CheckSpikeHit()
    {
        if (spikeDamageCooldown > 0f) return;

        spikeDamageCooldown = 0.3f;
        spikeHitCount++;
        spikeResetTimer = 1.5f;

        if (spikeHitCount >= 3 && criticalCooldownTimer <= 0f)
        {
            if (Random.value <= 0.5f)
            {
                StartCoroutine(CriticalEscape(0.1f));
                criticalCooldownTimer = 1.0f;
            }
            else
            {
                ApplyKnockback(new Vector2(-1f, 0.5f));
            }
            spikeHitCount = 0;
            spikeResetTimer = 0f;
        }
        else if (spikeHitCount < 3)
        {
            ApplyKnockback(new Vector2(-1f, 0.5f));
        }
    }

    private void ApplyKnockback(Vector2 knockDir)
    {
        controller.rb.linearVelocity = Vector2.zero;
        activeKnockbackX = knockDir.x * knockPowerX;

        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        controller.rb.linearVelocity = new Vector2(activeKnockbackX, knockDir.y * knockPowerY * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.2f;
        knockbackTimeoutTimer = 3.0f;

        RpcPlayHitAnimation();

        CoopRopeManager ropeManager = FindAnyObjectByType<CoopRopeManager>();
        if (ropeManager != null && ropeManager.isRopeActive)
        {
            RpcShareKnockbackDrag();
        }
    }

    [ClientRpc]
    private void RpcToggleCriticalUI(bool state)
    {
        if (criticalUI != null) criticalUI.SetActive(state);
    }

    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        RpcToggleCriticalUI(true);

        activeKnockbackX = -30f;
        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        controller.rb.linearVelocity = new Vector2(activeKnockbackX, 40f * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.5f;
        knockbackTimeoutTimer = 3.0f;

        RpcPlayHitAnimation();

        CoopRopeManager ropeManager = FindAnyObjectByType<CoopRopeManager>();
        if (ropeManager != null && ropeManager.isRopeActive)
        {
            RpcShareKnockbackDrag();
        }

        yield return new WaitForSeconds(seconds);
        yield return new WaitForSeconds(2f);

        RpcToggleCriticalUI(false);
    }

    [ClientRpc]
    void RpcPlayHitAnimation()
    {
        if (controller.anim != null) controller.anim.SetTrigger("Hit");
    }

    public void ResetKnockback()
    {
        isKnockedBack = false;
        stunTimer = 0f;
        knockbackTimeoutTimer = 0f;
    }

    public void ApplyKnockbackFromEye(Vector3 eyePosition)
    {
        if (!isServer) return;
        ApplyKnockback(new Vector2(-1, 0.5f));
    }

    [ClientRpc]
    private void RpcShareKnockbackDrag()
    {
        if (!isLocalPlayer) return;
        if (isKnockedBack) return;

        stunTimer = 0.5f;
    }
}