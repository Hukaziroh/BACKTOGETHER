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
    public float waitTimeAtEnd = 5f;

    [Header("필요 인원 설정")]
    public int requiredTop = 1;
    public int requiredBottom = 0;

    [Header("물리 설정")]
    public Rigidbody2D platformRb;

    [System.NonSerialized]
    public HashSet<GameObject> topPlayers = new HashSet<GameObject>();
    [System.NonSerialized]
    public HashSet<GameObject> bottomPlayers = new HashSet<GameObject>();

    private enum State { Idle, MovingForward, WaitingAtEnd, Returning }

    [SyncVar]
    private State currentState = State.Idle;
    private Coroutine waitCoroutine;

    void FixedUpdate()
    {
        if (!isServer) return;
        topPlayers.RemoveWhere(p => p == null || !p.activeInHierarchy);
        bottomPlayers.RemoveWhere(p => p == null || !p.activeInHierarchy);

        bool isReady = topPlayers.Count >= requiredTop && bottomPlayers.Count >= requiredBottom;

        switch (currentState)
        {
            case State.Idle:
                syncVelocity = Vector2.zero;
                if (isReady) currentState = State.MovingForward;
                break;

            case State.MovingForward:
                if (!isReady)
                {
                    currentState = State.Returning;
                }
                else
                {
                    MoveTowards(endPoint.position, forwardSpeed);
                    if (Vector2.Distance(transform.position, endPoint.position) < 0.01f)
                    {
                        currentState = State.WaitingAtEnd;
                        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
                        waitCoroutine = StartCoroutine(WaitRoutine());
                    }
                }
                break;

            case State.WaitingAtEnd:
                syncVelocity = Vector2.zero;
                if (!isReady)
                {
                    if (waitCoroutine != null) StopCoroutine(waitCoroutine);
                    currentState = State.Returning;
                }
                break;

            case State.Returning:
                if (isReady)
                {
                    currentState = State.MovingForward;
                }
                else
                {
                    MoveTowards(startPoint.position, returnSpeed);
                    if (Vector2.Distance(transform.position, startPoint.position) < 0.01f)
                    {
                        currentState = State.Idle;
                    }
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