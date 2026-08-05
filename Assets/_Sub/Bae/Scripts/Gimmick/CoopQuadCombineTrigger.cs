using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoopQuadCombineTrigger : NetworkBehaviour
{
    [Header("4인 1조 합체 기믹 설정")]
    public int requiredPlayers = 4;

    [SyncVar]
    private bool isTriggered = false;

    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.RemoveWhere(p => p == null || !p.activeInHierarchy);

            playersInZone.Add(other.gameObject);

            if (playersInZone.Count >= requiredPlayers)
            {
                isTriggered = true;
                ActivateQuadCombine();
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.Remove(other.gameObject);
        }
    }

    [Server]
    private void ActivateQuadCombine()
    {
        // Linq로 플레이어 섞기 (이건 캡처에 있던 유저님 방식대로 유지)
        List<GameObject> playerList = playersInZone.OrderBy(x => UnityEngine.Random.value).ToList();
        if (playerList.Count < 4) return;

        // 통짜 Move와 잉여 None을 없애고 4명에게 1키씩 공평하게!
        CombineRole[] roles = new CombineRole[]
        {
            CombineRole.Move_Left,
            CombineRole.Move_Right,
            CombineRole.Jump,
            CombineRole.Action
        };

        // 역할 섞기 (Fisher-Yates Shuffle)
        for (int i = roles.Length - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            (roles[i], roles[r]) = (roles[r], roles[i]);
        }

        // 섞인 플레이어에게 섞인 역할 부여
        AssignRole(playerList[0], playerList[1], playerList[2], playerList[3],
                   roles[0], roles[1], roles[2], roles[3]);
    }

    [Server]
    private void AssignRole(GameObject playerA, GameObject playerB, GameObject playerC, GameObject playerD,
                            CombineRole roleA, CombineRole roleB, CombineRole roleC, CombineRole roleD)
    {
        PlayerCombineHandler a = playerA.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler b = playerB.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler c = playerC.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler d = playerD.GetComponent<PlayerCombineHandler>();

        if (a == null || b == null || c == null || d == null)
        {
            Debug.LogError("PlayerCombineHandler가 없는 플레이어가 있습니다.");
            return;
        }
        a.combineColorIndex = 0;
        a.combineFaceIndex = 0;

        b.combineColorIndex = 0;
        b.combineFaceIndex = 0;

        c.combineColorIndex = 0;
        c.combineFaceIndex = 0;

        d.combineColorIndex = 0;
        d.combineFaceIndex = 0;
        // playerA를 본체로 설정
        a.StartCombineMode(roleA, playerA);
        // 나머지는 playerA를 조종
        b.StartCombineMode(roleB, playerA);
        c.StartCombineMode(roleC, playerA);
        d.StartCombineMode(roleD, playerA);

        Debug.Log($"[4인 1조 합체 완료] 본체:{playerA.name}({roleA})");
    }
}