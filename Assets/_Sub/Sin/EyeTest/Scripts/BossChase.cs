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

    [Header("화면 판정용 카메라 (비워두면 Camera.main 사용)")]
    public Camera trackingCamera;

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
        if (trackingCamera == null)
            trackingCamera = Camera.main;

        float targetSpeed = IsOnScreen() ? normalSpeed : catchUpSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
    }

    private bool IsOnScreen()
    {
        if (trackingCamera == null) return true;

        Vector3 viewportPos = trackingCamera.WorldToViewportPoint(transform.position);
        return viewportPos.z > 0f &&
               viewportPos.x >= 0f && viewportPos.x <= 1f &&
               viewportPos.y >= 0f && viewportPos.y <= 1f;
    }
}
