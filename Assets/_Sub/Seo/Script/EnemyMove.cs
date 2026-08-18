using UnityEngine;
using Mirror;

public class EnemyMove : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 3f;

    [Header("장애물(Ground) 감지")]
    [Tooltip("벽/장애물을 감지할 레이저의 길이")]
    public float groundCheckDistance = 0.7f;

    [Tooltip("장애물로 인식할 레이어")]
    public LayerMask groundLayer;

    [Header("레이 시작 위치 오프셋")]
    [Tooltip("몸통 중앙 기준 좌우 위치")]
    public float rayOffsetX = 0f;

    [Tooltip("몸통 중앙 기준 상하 위치")]
    public float rayOffsetY = 0f;

    [Header("레이 기즈모")]
    public bool showRayGizmo = true;

    [Tooltip("Scene에서 보여줄 레이 색상")]
    public Color rayGizmoColor = Color.red;

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

        rb.linearVelocity = new Vector2(
            moveDirection * moveSpeed,
            rb.linearVelocity.y
        );
    }

    [Server]
    private void CheckObstacleAhead()
    {
        // 몬스터 몸통 중앙 + Inspector에서 설정한 X/Y
        Vector2 origin = (Vector2)transform.position +
                         new Vector2(rayOffsetX, rayOffsetY);

        Vector2 rayDirection =
            moveDirection == 1 ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            rayDirection,
            groundCheckDistance,
            groundLayer
        );

        Debug.DrawRay(
            origin,
            rayDirection * groundCheckDistance,
            Color.red
        );

        if (hit.collider != null)
        {
            moveDirection *= -1;
        }
    }

    private void OnDirectionChanged(int oldDir, int newDir)
    {
        float sign = newDir == -1 ? 1f : -1f;

        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * sign,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRayGizmo)
            return;

        // 몸통 중앙 + Inspector 오프셋
        Vector3 origin = transform.position +
                         new Vector3(rayOffsetX, rayOffsetY, 0f);

        // 현재 방향에 맞춰 표시
        Vector3 direction =
            moveDirection == 1 ? Vector3.right : Vector3.left;

        Gizmos.color = rayGizmoColor;

        // 레이 시작점
        Gizmos.DrawSphere(origin, 0.05f);

        // 실제 Raycast와 동일한 위치/길이
        Gizmos.DrawLine(
            origin,
            origin + direction * groundCheckDistance
        );
    }
}