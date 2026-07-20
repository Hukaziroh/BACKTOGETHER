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

        SetupLineRenderers(connectedPlayers.Count - 1);
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
        if (!isRopeActive || connectedPlayers.Count < 2) return;

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
                rb.mass = movement.isGrounded ? groundedMass : airborneMass;

                bool isTensionActive = false;

                if (localIndex > 0 && connectedPlayers[localIndex - 1] != null)
                {
                    Rigidbody2D prevRb = connectedPlayers[localIndex - 1].GetComponent<Rigidbody2D>();
                    if (prevRb != null && ApplyRopeConstraint(rb, prevRb))
                        isTensionActive = true;
                }

                if (localIndex < connectedPlayers.Count - 1 && connectedPlayers[localIndex + 1] != null)
                {
                    Rigidbody2D nextRb = connectedPlayers[localIndex + 1].GetComponent<Rigidbody2D>();
                    if (nextRb != null && ApplyRopeConstraint(rb, nextRb))
                        isTensionActive = true;
                }

                movement.isRestrictedByRope = isTensionActive;
            }
        }
    }

    private bool ApplyRopeConstraint(Rigidbody2D rb, Rigidbody2D targetRb)
    {
        Vector2 targetPos = targetRb.position;
        Vector2 direction = targetPos - rb.position;
        float distance = direction.magnitude;

        if (distance > maxRopeLength)
        {
            Vector2 dirNorm = direction.normalized;
            float stretch = distance - maxRopeLength;
            float totalMass = rb.mass + targetRb.mass;
            float myRatio = targetRb.mass / totalMass;
            rb.position += dirNorm * (stretch * myRatio);

            Vector2 relativeVelocity = rb.linearVelocity - targetRb.linearVelocity;
            float relVelAlongRope = Vector2.Dot(relativeVelocity, dirNorm);

            if (relVelAlongRope < 0)
            {
                rb.linearVelocity -= dirNorm * (relVelAlongRope * myRatio);
            }

            Vector2 tangentialVelocity = rb.linearVelocity - (dirNorm * Vector2.Dot(rb.linearVelocity, dirNorm));
            rb.linearVelocity -= tangentialVelocity * (Time.fixedDeltaTime * 4f);

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