using UnityEngine;
using Mirror;

public class CoopRoundTripPlatform : CoopPlatformBase
{
    [Header("왕복 이동 설정")]
    public Transform startPoint;
    public Transform endPoint;
    public float goSpeed = 2f;
    public float returnSpeed = 2f;
    public int requiredPlayers = 4;

    [Header("물리 설정")]
    public Rigidbody2D platformRigidbody;

    private bool isTriggered = false;
    private bool isReturning = false;

    void Start()
    {
        if (platformRigidbody != null && startPoint != null)
        {
            platformRigidbody.position = startPoint.position;
        }
    }

    void FixedUpdate()
    {
        if (isServer && platformRigidbody != null && startPoint != null && endPoint != null)
        {
            CleanUpPlayers();
            if (!isTriggered && playersOnPlatform.Count >= requiredPlayers)
            {
                isTriggered = true;
                isReturning = false;
            }

            if (isTriggered)
            {
                Vector2 currentPos = platformRigidbody.position;
                Vector2 targetPos = isReturning ? (Vector2)startPoint.position : (Vector2)endPoint.position;
                float currentSpeed = isReturning ? returnSpeed : goSpeed;

                Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, currentSpeed * Time.fixedDeltaTime);
                platformRigidbody.MovePosition(nextPos);
                syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;

                if (Vector2.Distance(currentPos, targetPos) < 0.05f)
                {
                    if (!isReturning)
                    {
                        isReturning = true; // 돌아가기 시작
                    }
                    else
                    {
                        isTriggered = false;
                        isReturning = false;
                        syncVelocity = Vector2.zero;
                        Debug.Log($"[{gameObject.name}] 1회 왕복 완료! 대기 상태로 돌아갑니다.");
                    }
                }
            }
            else
            {
                syncVelocity = Vector2.zero;
            }
        }
    }
}