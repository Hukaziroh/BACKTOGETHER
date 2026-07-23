using UnityEngine;
using Mirror;

public class CoopPatrolPlatform : CoopPlatformBase
{
    [Header("패트롤 설정")]
    public Transform startPoint;
    public Transform endPoint;
    public float moveSpeed = 2f;
    public int requiredPlayers = 0;

    [Header("물리 설정")]
    public Rigidbody2D platformRigidbody;

    private Transform currentTarget;

    void Start()
    {
        if (platformRigidbody != null && startPoint != null)
            platformRigidbody.position = startPoint.position;

        currentTarget = endPoint;
    }

    void FixedUpdate()
    {
        if (isServer && platformRigidbody != null && startPoint != null && endPoint != null)
        {
            CleanUpPlayers();
            bool canMove = (requiredPlayers == 0) || (playersOnPlatform.Count >= requiredPlayers);

            if (canMove)
            {
                Vector2 currentPos = platformRigidbody.position;
                Vector2 nextPos = Vector2.MoveTowards(currentPos, currentTarget.position, moveSpeed * Time.fixedDeltaTime);

                platformRigidbody.MovePosition(nextPos);
                syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;

                if (Vector2.Distance(currentPos, currentTarget.position) < 0.05f)
                    currentTarget = (currentTarget == endPoint) ? startPoint : endPoint;
            }
            else
            {
                syncVelocity = Vector2.zero;
            }
        }
    }
}