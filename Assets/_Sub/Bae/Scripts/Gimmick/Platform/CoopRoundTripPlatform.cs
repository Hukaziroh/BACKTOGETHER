using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopRoundTripPlatform : NetworkBehaviour
{
    [Header("왕복 이동 설정")]
    [Tooltip("출발 지점 (씬에 배치된 빈 오브젝트)")]
    public Transform startPoint;
    [Tooltip("도착 지점 (씬에 배치된 빈 오브젝트)")]
    public Transform endPoint;
    [Tooltip("목적지로 가는 속도")]
    public float goSpeed = 2f;
    [Tooltip("원래 자리로 돌아오는 속도")]
    public float returnSpeed = 2f;

    [Tooltip("작동에 필요한 플레이어 수 (인스펙터에서 테스트용으로 변경 가능)")]
    public int requiredPlayers = 4;

    [Header("물리 설정")]
    public Rigidbody2D platformRigidbody;

    // 💡 [수정 1] 서버에서 계산한 진짜 속도를 클라이언트에게 쏴주는 SyncVar 변수 생성!
    [SyncVar]
    private Vector2 syncVelocity;

    // 💡 [수정 2] 플레이어(PlayerMovement)가 발판 속도를 물어보면, 동기화된 syncVelocity를 대답해줌!
    public Vector2 CurrentVelocity
    {
        get { return syncVelocity; }
    }

    private HashSet<GameObject> playersOnPlatform = new HashSet<GameObject>();

    private bool isTriggered = false;
    private bool isReturning = false;

    void Start()
    {
        if (platformRigidbody != null && startPoint != null)
        {
            platformRigidbody.position = startPoint.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isServer)
        {
            playersOnPlatform.Add(other.gameObject);
            CheckTrigger();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isServer)
        {
            playersOnPlatform.Remove(other.gameObject);
        }
    }

    [Server]
    private void CheckTrigger()
    {
        if (!isTriggered)
        {
            playersOnPlatform.RemoveWhere(go => go == null || !go.activeInHierarchy);

            if (playersOnPlatform.Count >= requiredPlayers)
            {
                isTriggered = true;
                isReturning = false;
                Debug.Log($"[{gameObject.name}] 정원 도달! 왕복 이동을 시작합니다.");
            }
        }
    }

    void FixedUpdate()
    {
        if (isServer && platformRigidbody != null && startPoint != null && endPoint != null)
        {
            if (isTriggered)
            {
                // 1. 현재 위치 저장
                Vector2 currentPos = platformRigidbody.position;
                Vector2 targetPos = isReturning ? (Vector2)startPoint.position : (Vector2)endPoint.position;

                float currentSpeed = isReturning ? returnSpeed : goSpeed;

                // 2. 이동 계산
                Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, currentSpeed * Time.fixedDeltaTime);

                // 3. 물리 이동 수행
                platformRigidbody.MovePosition(nextPos);

                // 💡 [수정 3] 계산된 발판 속도를 syncVelocity에 넣어서 클라이언트들에게 전송!
                syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;

                // 5. 도착 체크
                if (Vector2.Distance(currentPos, targetPos) < 0.05f)
                {
                    if (!isReturning)
                    {
                        isReturning = true;
                    }
                    else
                    {
                        isTriggered = false;
                        isReturning = false;
                        syncVelocity = Vector2.zero; // 💡 멈췄을 때 속도 0으로 동기화
                        Debug.Log($"[{gameObject.name}] 1회 왕복 완료! 대기 상태로 돌아갑니다.");
                    }
                }
            }
            else
            {
                // 💡 대기 중일 때 속도 0으로 동기화
                syncVelocity = Vector2.zero;
            }
        }
    }
}