using UnityEngine;
using Mirror;
using System.Collections.Generic;

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
        List<GameObject> playerList = new List<GameObject>(playersInZone);
        if (playerList.Count < 4) return;

        Debug.Log("4인 기믹 발동!");

        AssignRole(playerList[0], playerList[1], playerList[2], playerList[3],
                   CombineRole.Move_Left, CombineRole.Move_Right, CombineRole.Jump, CombineRole.Action);
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

        // playerA를 본체로 설정
        a.StartCombineMode(roleA, playerA);
        // 나머지는 playerA를 조종
        b.StartCombineMode(roleB, playerA);
        c.StartCombineMode(roleC, playerA);
        d.StartCombineMode(roleD, playerA);

        Debug.Log($"[4인 1조 합체 완료] 본체:{playerA.name}({roleA}) / 파츠:{playerB.name}({roleB}), {playerC.name}({roleC}), {playerD.name}({roleD})");
    }
}