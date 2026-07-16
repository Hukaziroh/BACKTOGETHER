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

    [Server]
    public void StartRopeGimmick()
    {
        if (isRopeActive) return;
        CoopPlayerIdentity[] players = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);

        // 🌟 [변경됨] 수동 인원 설정 없이, 최소 2명 이상만 있으면 무조건 작동!
        if (players.Length < 2)
        {
            Debug.LogWarning("[로프 기믹] 최소 2명의 플레이어가 필요합니다!");
            return;
        }

        System.Array.Sort(players, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

        isRopeActive = true;

        // 🌟 [변경됨] 현재 맵에 들어와 있는 실제 인원수(최대 4명)를 자동으로 계산해서 묶음
        int actualPlayerCount = Mathf.Min(players.Length, 4);
        GameObject[] playersToLink = new GameObject[actualPlayerCount];
        for (int i = 0; i < actualPlayerCount; i++)
        {
            playersToLink[i] = players[i].gameObject;
        }

        RpcLinkPlayers(playersToLink, maxRopeLength);
    }

    [Server]
    public void StopRopeGimmick()
    {
        isRopeActive = false;
        RpcUnlinkPlayers();
    }

    [ClientRpc]
    private void RpcLinkPlayers(GameObject[] playersToLink, float maxLength)
    {
        connectedPlayers = new List<GameObject>(playersToLink);
        for (int i = 0; i < connectedPlayers.Count - 1; i++)
        {
            AttachJoint(connectedPlayers[i], connectedPlayers[i + 1], maxLength);
        }
        SetupLineRenderers(connectedPlayers.Count - 1);
    }

    [ClientRpc]
    private void RpcUnlinkPlayers()
    {
        foreach (var player in connectedPlayers)
        {
            if (player != null)
            {
                DistanceJoint2D[] joints = player.GetComponents<DistanceJoint2D>();
                foreach (var j in joints) Destroy(j);
            }
        }
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
        joint.maxDistanceOnly = true;
        joint.enableCollision = true;
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

    private void OnRopeActiveChanged(bool oldVal, bool newVal)
    {
        if (newVal) Debug.Log($"우정 파괴 로프가 {connectedPlayers.Count}명에게 연결되었습니다!");
        else Debug.Log("로프가 해제되었습니다.");
    }
}