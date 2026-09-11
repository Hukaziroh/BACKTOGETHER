using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum FaceState
{
    Idle,
    Walk,
    Jump,
    Hit
}

[System.Serializable]
public struct PlayerFaceGroup
{
    public string playerLabel;

    public Sprite idleSprite;
    public Sprite walkSprite;
    public Sprite jumpSprite;
    public Sprite hitSprite;
}

public class CoopPlayerIdentity : NetworkBehaviour
{
    [Header("비주얼 설정")]
    public SpriteRenderer playerSpriteRenderer;

    public Color[] playerColors = new Color[]
    {
        Color.white,
        new Color(1f, 0.5f, 0.5f),
        new Color(0.5f, 0.5f, 1f),
        new Color(0.5f, 1f, 0.5f)
    };



    [SyncVar(hook = nameof(OnPlayerIndexChanged))]
    public int playerIndex = -1;


    [Header("표정(눈) 설정")]
    public SpriteRenderer faceSpriteRenderer;

    public PlayerFaceGroup[] playerFaceGroups =
        new PlayerFaceGroup[4];


    [Header("합체 눈")]
    public PlayerFaceGroup[] combineFaceGroups =
        new PlayerFaceGroup[2];


    private PlayerController controller;
    private Animator anim;
    private PlayerCombineHandler combineHandler;


    public static Dictionary<int, CoopPlayerIdentity> players =
        new Dictionary<int, CoopPlayerIdentity>();

    private static readonly Dictionary<int, int>
        serverConnectionIndexMap =
        new Dictionary<int, int>();


    private void OnDestroy()
    {
        if (players.ContainsKey(playerIndex) && players[playerIndex] == this)
        {
            players.Remove(playerIndex);
        }
    }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
        combineHandler = GetComponent<PlayerCombineHandler>();


        if (faceSpriteRenderer == null)
        {
            Transform faceChild =
                transform.Find("FaceSprite");

            if (faceChild != null)
            {
                faceSpriteRenderer =
                    faceChild.GetComponent<SpriteRenderer>();
            }
        }
    }



    public override void OnStartServer()
    {
        base.OnStartServer();

        if (connectionToClient != null)
        {
            int connId = connectionToClient.connectionId;

            // ⭐ 1. 자가 청소 (Garbage Collection):
            // 현재 방에 실제 연결된 유저 명단(connections)에 없는 
            // 죽은 ID(유령)가 딕셔너리에 남아있다면 싹 다 지워버립니다.
            List<int> deadKeys = new List<int>();
            foreach (int key in serverConnectionIndexMap.Keys)
            {
                if (!NetworkServer.connections.ContainsKey(key))
                {
                    deadKeys.Add(key);
                }
            }
            foreach (int deadKey in deadKeys)
            {
                serverConnectionIndexMap.Remove(deadKey);
            }

            // 2. 인덱스(번호표) 할당
            if (serverConnectionIndexMap.ContainsKey(connId))
            {
                playerIndex = serverConnectionIndexMap[connId];
            }
            else // 처음 온 유저
            {
                int assigned = -1;
                for (int i = 0; i < 4; i++)
                {
                    if (!serverConnectionIndexMap.ContainsValue(i))
                    {
                        assigned = i;
                        break;
                    }
                }

                // ⭐ 3. 핵심 방어 로직: 
                // 타임아웃 지연 등으로 유령이 번호표를 안 놓고 있어서 빈자리가 없을 때 (-1 방지)
                if (assigned == -1)
                {
                    Debug.LogWarning($"[CoopPlayerIdentity] 남는 번호표가 없습니다! 하얀색 플레이어 버그 방지를 위해 임시 번호를 강제 부여합니다.");
                    // -1 대신 현재 접속 ID를 4로 나눈 나머지를 주어 절대 하얀색으로 뻗거나 에러가 나지 않게 만듭니다.
                    assigned = connId % 4;
                }

                serverConnectionIndexMap[connId] = assigned;
                playerIndex = assigned;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        CoopPlayerManager.RegisterPlayer(gameObject);

        if (playerIndex != -1)
        {
            players[playerIndex] = this;

            UpdatePlayerVisual(playerIndex);
        }

        UpdateExSortingOrder();
    }


    public override void OnStopClient()
    {
        base.OnStopClient();

        CoopPlayerManager.UnregisterPlayer(gameObject);

        if (players.ContainsKey(playerIndex))
        {
            players.Remove(playerIndex);
        }
    }


    public override void OnStopServer()
    {
        base.OnStopServer();
        CoopPlayerManager.UnregisterPlayer(gameObject);
        if (connectionToClient != null)
        {
            int connId = connectionToClient.connectionId;
            if (!NetworkServer.connections.ContainsKey(connId))
            {
                if (serverConnectionIndexMap.ContainsKey(connId))
                {
                    serverConnectionIndexMap.Remove(connId);
                    Debug.Log($"[플레이어 퇴장] 접속 ID({connId})가 완전히 퇴장했습니다. 색상을 회수합니다.");
                }
            }
            else
            {
                Debug.Log($"[씬 전환] 접속 ID({connId})는 방에 남아있습니다. 색상 인덱스를 안전하게 보존합니다.");
            }
        }

        if (!NetworkServer.active)
        {
            serverConnectionIndexMap.Clear();
            CoopPlayerManager.Clear();
        }
    }

    private void Update()
    {
        UpdateExSortingOrder();

        if (faceSpriteRenderer == null)
            return;

        if (playerFaceGroups == null ||
            playerFaceGroups.Length == 0)
            return;


        if (combineHandler != null &&
            combineHandler.isCombined &&
            combineHandler.bodyTarget != gameObject)
        {
            faceSpriteRenderer.enabled = false;
            return;
        }
        else
        {
            faceSpriteRenderer.enabled = true;
        }

        int validIndex = playerIndex;

        if (validIndex < 0 ||
            validIndex >= playerFaceGroups.Length)
        {
            validIndex = 0;
        }

        FaceState currentState = FaceState.Idle;

        bool isStunned =
            controller != null &&
            controller.knockback != null &&
            controller.knockback.IsStunned;

        bool isGrounded = true;
        float speed = 0f;


        if (anim != null)
        {
            isGrounded =
                anim.GetBool("isGrounded");

            speed =
                anim.GetFloat("Speed");
        }


        if (isStunned)
        {
            currentState = FaceState.Hit;
        }
        else if (!isGrounded)
        {
            currentState = FaceState.Jump;
        }
        else if (speed > 0.1f)
        {
            currentState = FaceState.Walk;
        }

        PlayerFaceGroup group;


        // 합체 중이면 합체용 눈
        if (combineHandler != null &&
            combineHandler.isCombined &&
            combineHandler.combineFaceIndex >= 0 &&
            combineFaceGroups != null &&
            combineHandler.combineFaceIndex <
                combineFaceGroups.Length)
        {
            group =
                combineFaceGroups[
                    combineHandler.combineFaceIndex];
        }
        else
        {
            // 일반 플레이어 눈
            group =
                playerFaceGroups[validIndex];
        }

        Sprite targetSprite =
            GetFaceSprite(
                group,
                currentState);


        if (targetSprite != null)
        {
            faceSpriteRenderer.sprite =
                targetSprite;
        }
    }

    private Sprite GetFaceSprite(
        PlayerFaceGroup group,
        FaceState state)
    {
        switch (state)
        {
            case FaceState.Idle:
                return group.idleSprite;

            case FaceState.Walk:
                return group.walkSprite;

            case FaceState.Jump:
                return group.jumpSprite;

            case FaceState.Hit:
                return group.hitSprite;

            default:
                return group.idleSprite;
        }
    }

    [Server]
    private void AssignAvailableIndex()
    {
        int connId =
            connectionToClient != null
                ? connectionToClient.connectionId
                : -1;


        if (connId != -1 &&
            serverConnectionIndexMap.TryGetValue(
                connId,
                out int existingIndex))
        {
            playerIndex = existingIndex;

            if (isClient)
            {
                UpdatePlayerVisual(playerIndex);
            }


            Debug.Log(
                $"[플레이어 유지] 접속 ID({connId}) - " +
                $"기존 {playerIndex + 1}P 번호 및 색상을 유지합니다."
            );

            return;
        }


        bool[] isIndexTaken =
            new bool[4];


        foreach (int takenIndex
                 in serverConnectionIndexMap.Values)
        {
            if (takenIndex >= 0 &&
                takenIndex < 4)
            {
                isIndexTaken[takenIndex] = true;
            }
        }


        for (int i = 0; i < 4; i++)
        {
            if (!isIndexTaken[i])
            {
                playerIndex = i;


                if (connId != -1)
                {
                    serverConnectionIndexMap[connId] = i;
                }


                if (isClient)
                {
                    UpdatePlayerVisual(playerIndex);
                }


                Debug.Log(
                    $"[플레이어 최초 생성] 접속 ID({connId}) - " +
                    $"{i + 1}P 번호가 새로 부여되었습니다."
                );

                break;
            }
        }
    }

    private void OnPlayerIndexChanged(
        int oldIndex,
        int newIndex)
    {
        if (players.ContainsKey(oldIndex) &&
            players[oldIndex] == this)
        {
            players.Remove(oldIndex);
        }


        players[newIndex] = this;

        UpdatePlayerVisual(newIndex);
    }


    private void UpdatePlayerVisual(int index)
    {
        if (playerSpriteRenderer == null)
            return;


        if (index >= 0 &&
            index < playerColors.Length)
        {
            playerSpriteRenderer.color =
                playerColors[index];
        }
    }
    private void UpdateExSortingOrder()
    {
        if (SceneManager.GetActiveScene().name != "Ex1" &&
            SceneManager.GetActiveScene().name != "Ex2")
            return;

        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.sortingOrder = isLocalPlayer ? 10 : 0;
        }

        if (faceSpriteRenderer != null)
        {
            faceSpriteRenderer.sortingOrder = isLocalPlayer ? 11 : 1;
        }
    }

    public void ForceUpdateVisual()
    {
        UpdatePlayerVisual(playerIndex);
    }

    public void ResetPlayerColor()
    {
        UpdatePlayerVisual(playerIndex);
    }

    [Server]
    public void ResetCombinedColors()
    {
        RpcResetColor();
    }

    [ClientRpc]
    private void RpcResetColor()
    {
        UpdatePlayerVisual(playerIndex);
    }

    public void ResetFaceVisual()
    {
        if (faceSpriteRenderer == null)
            return;


        faceSpriteRenderer.enabled = true;


        if (playerIndex >= 0 &&
            playerIndex < playerFaceGroups.Length)
        {
            faceSpriteRenderer.sprite =
                playerFaceGroups[playerIndex].idleSprite;
        }
    }
}
