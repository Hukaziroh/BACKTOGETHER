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

        // 🌟 유저 편의성 패치: 인스펙터에서 설정을 깜빡해도 자동으로 "Ground" 레이어를 잡아줍니다!
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        // 🌟 이동하기 전에 앞쪽 장애물(Ground)을 먼저 감지합니다.
        CheckObstacleAhead();

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    [Server]
    private void CheckObstacleAhead()
    {
        // 1. 레이저 시작 지점 (rayCheckPoint가 없다면 몬스터 중심으로 설정)
        Vector2 origin = rayCheckPoint != null ? (Vector2)rayCheckPoint.position : (Vector2)transform.position;

        // 2. 현재 이동 방향에 따라 레이저를 쏠 방향 결정
        Vector2 rayDirection = moveDirection == 1 ? Vector2.right : Vector2.left;

        // 3. 앞을 향해 레이저 발사! (groundLayer에 해당하는 것만 감지)
        RaycastHit2D hit = Physics2D.Raycast(origin, rayDirection, groundCheckDistance, groundLayer);

        // [디버그용] 유니티 씬 뷰에서 빨간색 선으로 레이저를 보여줌 (플레이 중 확인 가능)
        Debug.DrawRay(origin, rayDirection * groundCheckDistance, Color.red);

        // 4. 만약 정해진 거리 안에 Ground 레이어가 감지되었다면 방향 전환
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