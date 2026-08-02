using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoopParallelPlatform : CoopPlatformBase
{
    [Header("이동 포인트")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("속도 및 시간 설정")]
    public float forwardSpeed = 3f;
    public float returnSpeed = 5f;
    [Tooltip("목적지 도착 후 대기 시간 (초)")]
    public float waitTimeAtEnd = 1f; // 1초 대기로 수정

    [Header("필요 인원 설정")]
    public int requiredTop = 1;
    public int requiredBottom = 0;

    [Header("물리 설정")]
    public Rigidbody2D platformRb;
    private Vector2 lastPosition;
    public new Vector2 CurrentVelocity { get; private set; }

    [System.NonSerialized]
    public HashSet<GameObject> topPlayers = new HashSet<GameObject>();
    [System.NonSerialized]
    public HashSet<GameObject> bottomPlayers = new HashSet<GameObject>();

    private enum State { Idle, MovingForward, WaitingAtEnd, Returning }

    [SyncVar]
    private State currentState = State.Idle;
    private Coroutine waitCoroutine;

    void Start()
    {
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        Vector2 currentPos = transform.position;
        CurrentVelocity = (currentPos - lastPosition) / Time.fixedDeltaTime;
        lastPosition = currentPos;

        if (!isServer) return;

        topPlayers.RemoveWhere(p => p == null || !p.activeInHierarchy);
        bottomPlayers.RemoveWhere(p => p == null || !p.activeInHierarchy);

        bool isReady = topPlayers.Count >= requiredTop && bottomPlayers.Count >= requiredBottom;

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

                if (Vector2.Distance(transform.position, endPoint.position) < 0.01f)
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

                if (Vector2.Distance(transform.position, startPoint.position) < 0.01f)
                {
                    currentState = State.Idle;
                }
                break;
        }
    }

    [Server]
    private void MoveTowards(Vector3 target, float speed)
    {
        if (platformRb == null) return;

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

    [Server]
    public void AddPlayer(GameObject player, bool isTop)
    {
        if (isTop) topPlayers.Add(player);
        else bottomPlayers.Add(player);
    }

    [Server]
    public void RemovePlayer(GameObject player, bool isTop)
    {
        if (isTop) topPlayers.Remove(player);
        else bottomPlayers.Remove(player);
    }
}