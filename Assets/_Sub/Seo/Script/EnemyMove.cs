using UnityEngine;
using Mirror;

public class EnemyMove : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 3f;

    [Header("장애물(Ground) 감지 (Raycast) 설정")]
    [Tooltip("벽/바닥을 감지할 레이저의 길이")]
    public float groundCheckDistance = 0.7f;

    [Tooltip("장애물로 인식할 레이어 (따로 설정 안 하면 'Ground'를 자동 할당함)")]
    public LayerMask groundLayer;

    [Tooltip("(선택) 레이저를 쏠 위치. 비워두면 몬스터 정중앙에서 쏨")]
    public Transform rayCheckPoint;

    [SyncVar(hook = nameof(OnDirectionChanged))]
    private int moveDirection = -1;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        CheckObstacleAhead();

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    [Server]
    private void CheckObstacleAhead()
    {
        Vector2 origin = rayCheckPoint != null ? (Vector2)rayCheckPoint.position : (Vector2)transform.position;

        Vector2 rayDirection = moveDirection == 1 ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(origin, rayDirection, groundCheckDistance, groundLayer);

        Debug.DrawRay(origin, rayDirection * groundCheckDistance, Color.red);

        if (hit.collider != null)
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