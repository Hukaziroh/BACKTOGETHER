using UnityEngine;
using Mirror;
using System.Collections; 

public class CoopRoundTripPlatform : CoopPlatformBase
{
    [Header("왕복 이동 설정")]
    public Transform startPoint;
    public Transform endPoint;
    public float goSpeed = 2f;
    public float returnSpeed = 2f;
    public int requiredPlayers = 4;

    [Tooltip("목적지 도착 후 대기 시간 (초)")]
    public float waitTimeAtEnd = 0f;

    [Header("물리 설정")]
    public Rigidbody2D platformRigidbody;

    private enum State { Idle, MovingForward, WaitingAtEnd, Returning }

    [SyncVar]
    private State currentState = State.Idle;
    private Coroutine waitCoroutine;

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

            switch (currentState)
            {
                case State.Idle:
                    if (playersOnPlatform.Count >= requiredPlayers)
                    {
                        currentState = State.MovingForward;
                    }
                    else
                    {
                        syncVelocity = Vector2.zero;
                    }
                    break;

                case State.MovingForward:
                    MoveTowards(endPoint.position, goSpeed);

                    if (Vector2.Distance(platformRigidbody.position, endPoint.position) < 0.05f)
                    {
                        currentState = State.WaitingAtEnd; 
                        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
                        waitCoroutine = StartCoroutine(WaitRoutine()); 
                    }
                    break;

                case State.WaitingAtEnd:
                    syncVelocity = Vector2.zero;
                    break;

                case State.Returning:
                    MoveTowards(startPoint.position, returnSpeed);

                    if (Vector2.Distance(platformRigidbody.position, startPoint.position) < 0.05f)
                    {
                        currentState = State.Idle;
                        syncVelocity = Vector2.zero;
                        // Debug.Log($"[{gameObject.name}] 1회 왕복 완료! 대기 상태로 돌아갑니다.");
                    }
                    break;
            }
        }
    }

    [Server]
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector2 currentPos = platformRigidbody.position;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, target, speed * Time.fixedDeltaTime);
        platformRigidbody.MovePosition(nextPos);

        syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;
    }

    private IEnumerator WaitRoutine()
    {
        yield return new WaitForSeconds(waitTimeAtEnd);
        currentState = State.Returning; 
    }
}