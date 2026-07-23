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

    void Update()
    {
        if (!isLocalPlayer) return;
        UpdateTimers();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        HandleKnockbackPhysics();
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
        if (!isLocalPlayer) return;

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
                ApplyLocalKnockback(new Vector2(-1f, 0.5f));
            }
            spikeHitCount = 0;
            spikeResetTimer = 0f;
        }
        else if (spikeHitCount < 3)
        {
            ApplyLocalKnockback(new Vector2(-1f, 0.5f));
        }
    }

    private void ApplyLocalKnockback(Vector2 knockDir)
    {
        controller.rb.linearVelocity = Vector2.zero;
        activeKnockbackX = knockDir.x * knockPowerX;

        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        controller.rb.linearVelocity = new Vector2(activeKnockbackX, knockDir.y * knockPowerY * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.2f;

        knockbackTimeoutTimer = 3.0f;

        CmdPlayHitAnimation();
    }

    private System.Collections.IEnumerator CriticalEscape(float seconds)
    {
        if (criticalUI != null) criticalUI.SetActive(true);

        activeKnockbackX = -30f;
        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        controller.rb.linearVelocity = new Vector2(activeKnockbackX, 40f * mult);

        isKnockedBack = true;
        stunTimer = 0f;
        knockbackGraceTimer = 0.5f;

        knockbackTimeoutTimer = 3.0f;

        CmdPlayHitAnimation();

        yield return new WaitForSeconds(seconds);
        yield return new WaitForSeconds(2f);

        if (criticalUI != null) criticalUI.SetActive(false);
    }

    [Command]
    public void CmdPlayHitAnimation() { RpcPlayHitAnimation(); }

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
        if (!isLocalPlayer) return;
        ApplyLocalKnockback(new Vector2(-1, 0.5f));
    }
}