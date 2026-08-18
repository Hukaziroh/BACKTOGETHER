using UnityEngine;
using Mirror;

public class PlayerGravityController : NetworkBehaviour
{
    [Header("중력 상태")]
    public bool canInvertGravity = false;

    [SyncVar(hook = nameof(OnGravityStateChanged))]
    public bool isGravityInverted = false;

    [SyncVar]
    public float gravityMultiplier = 1f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    [Server]
    public void ServerToggleGravity()
    {
        if (!canInvertGravity)
            return;

        isGravityInverted = !isGravityInverted;
        gravityMultiplier = isGravityInverted ? -1f : 1f;
    }
    private void OnGravityStateChanged(bool oldValue, bool newValue)
    {
        ApplyGravityState(newValue);
    }
    private void ApplyGravityState(bool inverted)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            -rb.linearVelocity.y
        );

        Vector3 currentScale = transform.localScale;
        currentScale.y = inverted ? -Mathf.Abs(currentScale.y) : Mathf.Abs(currentScale.y);
        transform.localScale = currentScale;
    }

    [Server]
    public void ResetGravity()
    {
        if (!isGravityInverted)
            return;

        isGravityInverted = false;
        gravityMultiplier = 1f;
    }
}