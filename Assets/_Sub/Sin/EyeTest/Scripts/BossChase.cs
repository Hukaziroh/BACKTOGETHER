using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class BossChase : NetworkBehaviour
{
    [Header("이동 (왼쪽 → 오른쪽 고정)")]
    [Tooltip("화면 안에 보일 때(플레이어가 볼 수 있을 때) 속도")]
    public float normalSpeed = 2f;
    [Tooltip("화면 밖으로 벗어났을 때(안 보일 때) 따라잡는 속도")]
    public float catchUpSpeed = 6f;

    [Tooltip("속도가 바뀔 때 얼마나 부드럽게 가감속할지 (초당 속도 변화량)")]
    public float speedChangeRate = 2f;

    [Header("화면 판정 (제일 뒤처진 플레이어 기준)")]
    [Tooltip("서버는 각 클라이언트의 실제 카메라를 알 수 없어서, 카메라 대신 거리로 근사한다.\n" +
             "제일 뒤처진(X좌표가 가장 작은) 플레이어와 이 거리 이내면 '화면 안'으로 간주.")]
    public float visibleRangeX = 14f;

    [Header("근접 감속 (아무 플레이어든 너무 가까우면 느려짐)")]
    [Tooltip("이 거리 이내에 플레이어가 있으면 위 속도 대신 이 감속 속도를 씀")]
    public float slowdownDistance = 3f;
    [Tooltip("너무 가까울 때의 속도")]
    public float slowSpeed = 1f;

    private Rigidbody2D rb;
    private float currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        currentSpeed = normalSpeed;
    }

    [ServerCallback]
    void FixedUpdate()
    {
        float targetSpeed;

        if (IsAnyPlayerTooClose())
        {
            targetSpeed = slowSpeed;
        }
        else
        {
            targetSpeed = IsOnScreen() ? normalSpeed : catchUpSpeed;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
    }

    private bool IsOnScreen()
    {
        CoopPlayerIdentity rearmost = FindRearmostPlayer();
        if (rearmost == null) return true;

        float xDistance = Mathf.Abs(transform.position.x - rearmost.transform.position.x);
        return xDistance <= visibleRangeX;
    }

    // 아무 플레이어든 이 거리 안에 있으면 감속.
    private bool IsAnyPlayerTooClose()
    {
        foreach (var kvp in CoopPlayerIdentity.players)
        {
            CoopPlayerIdentity p = kvp.Value;
            if (p == null) continue;

            if (Vector2.Distance(transform.position, p.transform.position) <= slowdownDistance)
                return true;
        }

        return false;
    }

    // 보스가 오른쪽으로 이동하며 쫓아오는 구조라, X좌표가 제일 작은(가장 뒤처진) 플레이어가
    // 보스한테 가장 먼저 잡힐 위험이 있는 사람이다. 그 사람 기준으로만 화면 판정을 한다.
    private CoopPlayerIdentity FindRearmostPlayer()
    {
        CoopPlayerIdentity rearmost = null;
        float minX = float.MaxValue;

        foreach (var kvp in CoopPlayerIdentity.players)
        {
            CoopPlayerIdentity p = kvp.Value;
            if (p == null) continue;

            if (p.transform.position.x < minX)
            {
                minX = p.transform.position.x;
                rearmost = p;
            }
        }

        return rearmost;
    }
}
