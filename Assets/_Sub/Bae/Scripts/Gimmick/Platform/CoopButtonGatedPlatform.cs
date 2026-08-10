using Mirror;
using System.Collections;
using UnityEngine;

// 발판에 직접 탄 인원수가 아니라, 연결된 버튼 4개(또는 N개)가 전부 눌렸을 때 움직이는 발판.
// CoopParallelPlatform(위/아래 인원수 기반)과 별개로 둔다 - 그쪽은 이미 다른 맵에서 쓰이고 있어서.
public class CoopButtonGatedPlatform : CoopPlatformBase
{
    [Header("연결할 버튼들")]
    public CoopButton[] connectedButtons;

    [Header("이동 포인트")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("속도 및 시간 설정")]
    public float forwardSpeed = 3f;
    public float returnSpeed = 5f;
    [Tooltip("목적지 도착 후 대기 시간 (초)")]
    public float waitTimeAtEnd = 1f;

    [Header("물리 설정")]
    public Rigidbody2D platformRb;

    private enum State { Idle, MovingForward, WaitingAtEnd, Returning }

    [SyncVar]
    private State currentState = State.Idle;
    private Coroutine waitCoroutine;

    [ServerCallback]
    void FixedUpdate()
    {
        if (platformRb == null || startPoint == null || endPoint == null) return;

        bool isReady = CoopButtonUtility.AreAllPressed(connectedButtons);

        switch (currentState)
        {
            case State.Idle:
                if (isReady)
                {
                    currentState = State.MovingForward;
                }
                break;

            case State.MovingForward:
                MoveTowards(endPoint.position, forwardSpeed);

                if (Vector2.Distance(platformRb.position, endPoint.position) < 0.01f)
                {
                    currentState = State.WaitingAtEnd;
                    if (waitCoroutine != null) StopCoroutine(waitCoroutine);
                    waitCoroutine = StartCoroutine(WaitRoutine());
                }
                break;

            case State.WaitingAtEnd:
                break;

            case State.Returning:
                MoveTowards(startPoint.position, returnSpeed);

                if (Vector2.Distance(platformRb.position, startPoint.position) < 0.01f)
                {
                    currentState = State.Idle;
                }
                break;
        }
    }

    [Server]
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector2 currentPos = platformRb.position;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, target, speed * Time.fixedDeltaTime);

        platformRb.MovePosition(nextPos);
        syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;
    }

    private IEnumerator WaitRoutine()
    {
        yield return new WaitForSeconds(waitTimeAtEnd);
        currentState = State.Returning;
    }
}
