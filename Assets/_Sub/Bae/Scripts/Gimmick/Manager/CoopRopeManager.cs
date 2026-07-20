using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CoopRopeManager : NetworkBehaviour
{
    [Header("로프 인원 설정")]
    [Tooltip("기믹 작동에 필요한 인원수입니다. (테스트 시 2로 낮추세요)")]
    [Range(2, 4)]
    public int requiredPlayers = 4;

    [Header("로프 물리 설정 (스프링 장력)")]
    [Tooltip("로프가 팽팽해지기 시작하는 최대 길이")]
    public float maxRopeLength = 3f;
    [Tooltip("로프가 서로를 강하게 당기는 탄성력 (Hooke's Law)")]
    public float springForce = 800f;
    [Tooltip("고무줄처럼 무한정 튕기는 것을 막는 감쇠력")]
    public float damper = 50f;

    [Header("동적 질량 설정 (줄다리기 밸런스)")]
    [Tooltip("땅에 닿아 버틸 때의 질량 (무거움)")]
    public float groundedMass = 5f;
    [Tooltip("공중에 매달렸을 때의 질량 (가벼움)")]
    public float airborneMass = 1f;

    [Header("로프 비주얼 설정")]
    [Tooltip("로프 렌더러 두께")]
    public float ropeWidth = 0.3f;
    [Tooltip("로프 스프라이트가 적용된 머티리얼")]
    public Material ropeMaterial;

    [SyncVar(hook = nameof(OnRopeActiveChanged))]
    public bool isRopeActive = false;

    private List<GameObject> connectedPlayers = new List<GameObject>();
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    [Server]
    public void StartRopeGimmick()
    {
        if (isRopeActive) return;
        CoopPlayerIdentity[] players = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);

        if (players.Length < requiredPlayers)
        {
            Debug.LogWarning($"[로프 기믹] {requiredPlayers}명이 모이지 않아 로프를 연결할 수 없습니다!");
            return;
        }

        // 플레이어 번호 순으로 정렬하여 로프를 1-2-3-4 순서로 연결
        System.Array.Sort(players, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

        isRopeActive = true;
        GameObject[] playersToLink = new GameObject[requiredPlayers];
        for (int i = 0; i < requiredPlayers; i++)
        {
            playersToLink[i] = players[i].gameObject;
        }

        RpcLinkPlayers(playersToLink);
    }

    [Server]
    public void StopRopeGimmick()
    {
        isRopeActive = false;
        RpcUnlinkPlayers();
    }

    [ClientRpc]
    private void RpcLinkPlayers(GameObject[] playersToLink)
    {
        connectedPlayers = new List<GameObject>(playersToLink);

        // 🚨 기존의 DistanceJoint2D 생성 코드를 완전히 제거했습니다!
        // 물리 연산은 FixedUpdate에서 스크립트로 직접 처리합니다.

        SetupLineRenderers(connectedPlayers.Count - 1);
    }

    [ClientRpc]
    private void RpcUnlinkPlayers()
    {
        foreach (var player in connectedPlayers)
        {
            if (player != null)
            {
                // 로프가 끊어지면 질량을 원래대로 1로 복구
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.mass = 1f;
            }
        }

        foreach (var lr in lineRenderers)
        {
            if (lr != null) Destroy(lr.gameObject);
        }

        lineRenderers.Clear();
        connectedPlayers.Clear();
    }

    private void SetupLineRenderers(int lineCount)
    {
        for (int i = 0; i < lineCount; i++)
        {
            GameObject lrObj = new GameObject($"RopeLine_{i}");
            lrObj.transform.SetParent(this.transform);

            LineRenderer lr = lrObj.AddComponent<LineRenderer>();
            lr.startWidth = ropeWidth;
            lr.endWidth = ropeWidth;
            lr.material = ropeMaterial;
            lr.positionCount = 2;

            lr.textureMode = LineTextureMode.Tile;
            lr.sortingLayerName = "Foreground";
            lr.sortingOrder = 10;

            lineRenderers.Add(lr);
        }
    }

    void Update()
    {
        // 로프 선(Line Renderer) 시각적 업데이트
        if (isRopeActive && connectedPlayers.Count >= 2 && lineRenderers.Count == connectedPlayers.Count - 1)
        {
            for (int i = 0; i < lineRenderers.Count; i++)
            {
                if (connectedPlayers[i] != null && connectedPlayers[i + 1] != null && lineRenderers[i] != null)
                {
                    Vector3 pos1 = connectedPlayers[i].transform.position;
                    Vector3 pos2 = connectedPlayers[i + 1].transform.position;

                    lineRenderers[i].SetPosition(0, pos1);
                    lineRenderers[i].SetPosition(1, pos2);

                    if (ropeMaterial != null)
                    {
                        float distance = Vector2.Distance(pos1, pos2);
                        lineRenderers[i].material.mainTextureScale = new Vector2(distance, 1f);
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!isRopeActive || connectedPlayers.Count < 2) return;

        // 🚨 클라이언트 주도(Client-Auth) 물리 연산
        // 멀티플레이어 환경이므로 오직 '내 캐릭터'의 물리 힘만 직접 계산하여 적용합니다.
        GameObject localPlayer = null;
        int localIndex = -1;

        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            if (connectedPlayers[i] != null)
            {
                NetworkIdentity identity = connectedPlayers[i].GetComponent<NetworkIdentity>();
                if (identity != null && identity.isLocalPlayer)
                {
                    localPlayer = connectedPlayers[i];
                    localIndex = i;
                    break;
                }
            }
        }

        if (localPlayer != null)
        {
            Rigidbody2D rb = localPlayer.GetComponent<Rigidbody2D>();
            PlayerMovement movement = localPlayer.GetComponent<PlayerMovement>();

            if (rb != null && movement != null)
            {
                // [핵심 1] 동적 질량 처리: 바닥에 있으면 무겁게, 공중에 뜨면 가볍게 설정
                rb.mass = movement.isGrounded ? groundedMass : airborneMass;

                // [핵심 2] 훅의 법칙 장력 계산
                // 내 앞사람(좌측)을 향해 당기는 힘 가하기
                if (localIndex > 0 && connectedPlayers[localIndex - 1] != null)
                {
                    ApplySpringForce(rb, connectedPlayers[localIndex - 1].transform.position);
                }

                // 내 뒷사람(우측)을 향해 당기는 힘 가하기
                if (localIndex < connectedPlayers.Count - 1 && connectedPlayers[localIndex + 1] != null)
                {
                    ApplySpringForce(rb, connectedPlayers[localIndex + 1].transform.position);
                }
            }
        }
    }

    private void ApplySpringForce(Rigidbody2D rb, Vector3 targetPos)
    {
        Vector2 direction = targetPos - rb.transform.position;
        float distance = direction.magnitude;

        // 거리가 최대 길이를 벗어났을 때만 고무줄처럼 팽팽해지며 당기는 힘(Tension) 발생
        if (distance > maxRopeLength)
        {
            Vector2 dirNorm = direction.normalized;
            float stretch = distance - maxRopeLength;

            // F = kx - cv (스프링 장력 - 감쇠력)
            // 타겟 방향으로 향하는 현재 내 속도를 구해서 너무 빠르게 당겨지는 것을 억제
            float currentVelocityAlongSpring = Vector2.Dot(rb.linearVelocity, dirNorm);
            float force = (stretch * springForce) - (currentVelocityAlongSpring * damper);

            rb.AddForce(dirNorm * force);
        }
    }

    private void OnRopeActiveChanged(bool oldVal, bool newVal)
    {
        if (newVal) Debug.Log("우정 파괴 로프가 연결되었습니다!");
        else Debug.Log("로프가 해제되었습니다.");
    }
}