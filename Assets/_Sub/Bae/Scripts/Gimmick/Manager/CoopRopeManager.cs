using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CoopRopeManager : NetworkBehaviour
{
    [Header("로프 물리 설정")]
    [Tooltip("로프가 늘어날 수 있는 최대 길이 (인스펙터 조절)")]
    public float maxRopeLength = 4f;

    [Header("로프 비주얼 설정")]
    [Tooltip("로프의 두께")]
    public float ropeWidth = 0.3f;
    [Tooltip("로프 스프라이트가 적용된 머티리얼")]
    public Material ropeMaterial;

    [SyncVar(hook = nameof(OnRopeActiveChanged))]
    public bool isRopeActive = false;

    private List<GameObject> connectedPlayers = new List<GameObject>();
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    // 외부 트리거(예: CoopAreaTrigger)나 버튼에서 이 함수를 호출해 기믹을 시작하세요!
    [Server]
    public void StartRopeGimmick()
    {
        if (isRopeActive) return;
        Debug.Log("성공");
        // 씬에 있는 모든 플레이어 찾기 (CoopPlayerIdentity 기준 정렬)
        CoopPlayerIdentity[] players = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);
        if (players.Length < 4)
        {
            Debug.LogWarning("[로프 기믹] 4명이 모이지 않아 로프를 연결할 수 없습니다!");
            return;
        }

        // 1P -> 2P -> 3P -> 4P 순서대로 정렬
        System.Array.Sort(players, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

        isRopeActive = true;

        // 클라이언트들에게 물리 조인트와 비주얼 선을 생성하라고 명령
        RpcLinkPlayers(
            players[0].gameObject,
            players[1].gameObject,
            players[2].gameObject,
            players[3].gameObject,
            maxRopeLength
        );
    }

    [Server]
    public void StopRopeGimmick()
    {
        isRopeActive = false;
        RpcUnlinkPlayers();
    }

    [ClientRpc]
    private void RpcLinkPlayers(GameObject p1, GameObject p2, GameObject p3, GameObject p4, float maxLength)
    {
        connectedPlayers = new List<GameObject> { p1, p2, p3, p4 };

        // 1. 물리 조인트 연결 (P1-P2, P2-P3, P3-P4)
        AttachJoint(p1, p2, maxLength);
        AttachJoint(p2, p3, maxLength);
        AttachJoint(p3, p4, maxLength);

        // 2. 비주얼 라인 렌더러 세팅
        SetupLineRenderers();
    }

    [ClientRpc]
    private void RpcUnlinkPlayers()
    {
        // 조인트 파괴
        foreach (var player in connectedPlayers)
        {
            if (player != null)
            {
                DistanceJoint2D[] joints = player.GetComponents<DistanceJoint2D>();
                foreach (var j in joints) Destroy(j);
            }
        }

        // 라인렌더러 파괴
        foreach (var lr in lineRenderers)
        {
            if (lr != null) Destroy(lr.gameObject);
        }

        lineRenderers.Clear();
        connectedPlayers.Clear();
    }

    private void AttachJoint(GameObject bodyA, GameObject bodyB, float length)
    {
        if (bodyA == null || bodyB == null) return;

        DistanceJoint2D joint = bodyA.AddComponent<DistanceJoint2D>();
        joint.connectedBody = bodyB.GetComponent<Rigidbody2D>();
        joint.autoConfigureDistance = false;
        joint.distance = length;

        // 🌟 핵심: 거리가 가까울 땐 막대기처럼 밀어내지 않고 끈처럼 휘어지게(무시) 만듭니다.
        joint.maxDistanceOnly = true;
    }

    private void SetupLineRenderers()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject lrObj = new GameObject($"RopeLine_{i}");
            lrObj.transform.SetParent(this.transform); // 매니저 하위에 깔끔하게 정리

            LineRenderer lr = lrObj.AddComponent<LineRenderer>();
            lr.startWidth = ropeWidth;
            lr.endWidth = ropeWidth;
            lr.material = ropeMaterial;
            lr.positionCount = 2;

            // 🌟 핵심: 로프 텍스처가 늘어나지 않고 타일처럼 반복되게 설정
            lr.textureMode = LineTextureMode.Tile;
            lr.sortingLayerName = "Foreground"; // 플레이어와 겹칠 때 순서 조정 (필요시 변경)
            lr.sortingOrder = 10;

            lineRenderers.Add(lr);
        }
    }

    void Update()
    {
        // 매 프레임 플레이어 위치를 따라가며 선을 다시 그립니다.
        if (isRopeActive && connectedPlayers.Count == 4 && lineRenderers.Count == 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (connectedPlayers[i] != null && connectedPlayers[i + 1] != null && lineRenderers[i] != null)
                {
                    Vector3 pos1 = connectedPlayers[i].transform.position;
                    Vector3 pos2 = connectedPlayers[i + 1].transform.position;

                    lineRenderers[i].SetPosition(0, pos1);
                    lineRenderers[i].SetPosition(1, pos2);

                    // 밧줄이 길어지면 스프라이트 타일(반복) 횟수를 늘려서 자연스럽게 보이게 함
                    if (ropeMaterial != null)
                    {
                        float distance = Vector2.Distance(pos1, pos2);
                        lineRenderers[i].material.mainTextureScale = new Vector2(distance, 1f);
                    }
                }
            }
        }
    }

    private void OnRopeActiveChanged(bool oldVal, bool newVal)
    {
        // 로프가 끊어지는 시각적 연출이나 효과음을 넣을 수 있습니다.
        if (newVal) Debug.Log("우정 파괴 로프가 연결되었습니다!");
        else Debug.Log("로프가 해제되었습니다.");
    }
}