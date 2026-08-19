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

    // PlayerKnockback에서 읽을 수 있게 public으로 변경
    public List<GameObject> connectedPlayers = new List<GameObject>();
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    [Server]
    public void StartRopeGimmick()
    {
        if (isRopeActive) return;
        CoopPlayerIdentity[] players = FindObjectsByType<CoopPlayerIdentity>(FindObjectsInactive.Exclude);

        if (players.Length < requiredPlayers)
        {
            Debug.LogWarning($"[로프 기믹] {requiredPlayers}명이 모이지 않아 로프를 연결할 수 없습니다!");
            return;
        }

        System.Array.Sort(players, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

        isRopeActive = true;

        // ⭐ 수정됨: GameObject 대신 네트워크 고유 ID(netId) 배열을 생성하여 안전하게 전송합니다.
        uint[] playerNetIds = new uint[requiredPlayers];
        for (int i = 0; i < requiredPlayers; i++)
        {
            playerNetIds[i] = players[i].netId;
        }

        RpcLinkPlayers(playerNetIds);
    }

    [Server]
    public void StopRopeGimmick()
    {
        isRopeActive = false;
        RpcUnlinkPlayers();
    }

    [ClientRpc]
    // ⭐ 수정됨: 클라이언트는 GameObject 배열 대신 숫자(uint) 배열을 받습니다.
    private void RpcLinkPlayers(uint[] playerNetIds)
    {
        connectedPlayers.Clear();

        // ⭐ 회원님이 작성하신 철벽 방어 로직 적용 완료
        foreach (uint netId in playerNetIds)
        {
            if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                if (identity != null && identity.gameObject != null)
                {
                    connectedPlayers.Add(identity.gameObject);
                }
                else
                {
                    Debug.LogWarning($"[로프 기믹] netId {netId}의 NetworkIdentity가 null입니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[로프 기믹] netId {netId}를 현재 클라이언트에서 찾을 수 없습니다.");
            }
        }

        if (connectedPlayers.Count >= 2)
        {
            SetupLineRenderers(connectedPlayers.Count - 1);
        }
        else
        {
            Debug.LogWarning(
                $"[로프 기믹] 플레이어를 충분히 찾지 못했습니다. " +
                $"찾음: {connectedPlayers.Count}/{playerNetIds.Length}"
            );
        }
    }

    [ClientRpc]
    private void RpcUnlinkPlayers()
    {
        foreach (var player in connectedPlayers)
        {
            if (player != null)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.mass = 1f;

                PlayerMovement mov = player.GetComponent<PlayerMovement>();
                if (mov != null) mov.isRestrictedByRope = false;
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
        if (!isServer) return;

        if (!isRopeActive || connectedPlayers.Count < 2) return;

        foreach (var player in connectedPlayers)
        {
            if (player != null)
            {
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null) movement.isRestrictedByRope = false;
            }
        }

        for (int i = 0; i < connectedPlayers.Count - 1; i++)
        {
            GameObject p1 = connectedPlayers[i];
            GameObject p2 = connectedPlayers[i + 1];

            if (p1 != null && p2 != null)
            {
                Rigidbody2D rb1 = p1.GetComponent<Rigidbody2D>();
                Rigidbody2D rb2 = p2.GetComponent<Rigidbody2D>();
                PlayerMovement mov1 = p1.GetComponent<PlayerMovement>();
                PlayerMovement mov2 = p2.GetComponent<PlayerMovement>();

                if (rb1 != null && rb2 != null && mov1 != null && mov2 != null)
                {
                    rb1.mass = mov1.isGrounded ? groundedMass : airborneMass;
                    rb2.mass = mov2.isGrounded ? groundedMass : airborneMass;

                    bool isTensionActive = ApplySymmetricRopeConstraint(rb1, rb2, mov1, mov2);
                    if (isTensionActive)
                    {
                        mov1.isRestrictedByRope = true;
                        mov2.isRestrictedByRope = true;
                    }
                }
            }
        }
    }

    private bool ApplySymmetricRopeConstraint(Rigidbody2D rb1, Rigidbody2D rb2, PlayerMovement mov1, PlayerMovement mov2)
    {
        Vector2 direction = rb2.position - rb1.position;
        float distance = direction.magnitude;

        if (distance > maxRopeLength)
        {
            Vector2 dirNorm = direction.normalized;
            float stretch = distance - maxRopeLength;

            float totalMass = rb1.mass + rb2.mass;
            if (totalMass <= 0f) totalMass = 1f;

            float ratio1 = rb2.mass / totalMass;
            float ratio2 = rb1.mass / totalMass;

            Vector2 posCorrection1 = dirNorm * (stretch * ratio1);
            Vector2 posCorrection2 = -dirNorm * (stretch * ratio2);

            if (mov1.isGrounded && posCorrection1.y < 0) posCorrection1.y = 0;
            if (mov2.isGrounded && posCorrection2.y < 0) posCorrection2.y = 0;

            rb1.position += posCorrection1;
            rb2.position += posCorrection2;

            Vector2 relativeVelocity = rb1.linearVelocity - rb2.linearVelocity;
            float relVelAlongRope = Vector2.Dot(relativeVelocity, dirNorm);

            if (relVelAlongRope < 0)
            {
                Vector2 velCorrection = dirNorm * relVelAlongRope;
                Vector2 velCorr1 = velCorrection * ratio1;
                Vector2 velCorr2 = -velCorrection * ratio2;

                if (mov1.isGrounded && velCorr1.y < 0) velCorr1.y = 0;
                if (mov2.isGrounded && velCorr2.y < 0) velCorr2.y = 0;

                rb1.linearVelocity -= velCorr1;
                rb2.linearVelocity -= velCorr2;
            }

            float maxAllowedSpeed = 20f;
            if (rb1.linearVelocity.sqrMagnitude > maxAllowedSpeed * maxAllowedSpeed)
                rb1.linearVelocity = rb1.linearVelocity.normalized * maxAllowedSpeed;
            if (rb2.linearVelocity.sqrMagnitude > maxAllowedSpeed * maxAllowedSpeed)
                rb2.linearVelocity = rb2.linearVelocity.normalized * maxAllowedSpeed;

            if (mov1.isGrounded)
                rb1.linearVelocity = new Vector2(Mathf.Lerp(rb1.linearVelocity.x, 0f, Time.fixedDeltaTime * 10f), rb1.linearVelocity.y);
            if (mov2.isGrounded)
                rb2.linearVelocity = new Vector2(Mathf.Lerp(rb2.linearVelocity.x, 0f, Time.fixedDeltaTime * 10f), rb2.linearVelocity.y);

            return true;
        }

        return false;
    }

    private void OnRopeActiveChanged(bool oldVal, bool newVal)
    {
        if (newVal) Debug.Log("우정 파괴 로프가 연결되었습니다!");
        else Debug.Log("로프가 해제되었습니다.");
    }
}