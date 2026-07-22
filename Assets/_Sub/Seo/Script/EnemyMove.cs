using UnityEngine;
using Mirror;

public class EnemyMove : NetworkBehaviour
{
    public float moveSpeed = 3f;

    [SyncVar(hook = nameof(OnDirectionChanged))]
    private int moveDirection = -1;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    [ServerCallback] 
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            moveDirection *= -1;
        }
    }

    private void OnDirectionChanged(int oldDir, int newDir)
    {
        float sign = newDir == -1 ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sign, transform.localScale.y, transform.localScale.z);
    }
}