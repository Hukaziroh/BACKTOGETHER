using UnityEngine;
using Mirror;

public class PlayerRespawn : NetworkBehaviour
{
    private PlayerController controller;

    [Header("스폰 시스템")]
    public Vector3 currentSpawnPoint;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        currentSpawnPoint = transform.position;
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        CheckRespawn();
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
        controller.rb.linearVelocity = Vector2.zero;

        if (controller.knockback != null)
        {
            controller.knockback.ResetKnockback();
        }
    }

    private void CheckRespawn()
    {
        if (transform.position.y < -50f || transform.position.y > 50f)
        {
            Respawn();
        }
    }
}