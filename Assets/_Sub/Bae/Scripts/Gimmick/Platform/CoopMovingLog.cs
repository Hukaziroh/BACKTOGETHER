using UnityEngine;
using Mirror;

public class CoopMovingLog : CoopPlatformBase 
{
    [Header("통나무 이동 설정")]
    public Transform endPoint;
    public float moveSpeed = 2f;
    public int requiredPlayers = 4;

    [Header("물리 설정")]
    public Rigidbody2D logRigidbody;

    private Vector2 startPos;

    void Start()
    {
        if (logRigidbody != null)
        {
            startPos = logRigidbody.position;
        }
    }

    void FixedUpdate()
    {
        if (isServer && logRigidbody != null)
        {
            CleanUpPlayers(); 

            Vector2 currentPos = logRigidbody.position;
            Vector2 targetPos = (playersOnPlatform.Count >= requiredPlayers) ? (Vector2)endPoint.position : startPos;
            Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, moveSpeed * Time.fixedDeltaTime);

            logRigidbody.MovePosition(nextPos);
            syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime; 
        }
    }
}