using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CoopDuoCombineTrigger : NetworkBehaviour
{
    [Header("2인 1조 합체 기믹 설정")]
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
            playersInZone.Add(other.gameObject);

            if (playersInZone.Count >= requiredPlayers)
            {
                isTriggered = true;
                ActivateDuoCombine();
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
    private void ActivateDuoCombine()
    {
        List<GameObject> playerList = new List<GameObject>(playersInZone);

        if (playerList.Count < 4) return;

        Debug.Log("4인 진입 완료 -> 2:2 듀오 기믹 발동!");

        AssignRole(playerList[0], playerList[1], CombineRole.Move, CombineRole.Jump);
        AssignRole(playerList[2], playerList[3], CombineRole.Move, CombineRole.Jump);
    }

    [Server]
    private void AssignRole(GameObject playerA, GameObject playerB, CombineRole roleA, CombineRole roleB) // 🌟 매개변수 타입 변경
    {
        PlayerCombineHandler a = playerA.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler b = playerB.GetComponent<PlayerCombineHandler>();

        if (a == null || b == null)
        {
            Debug.LogError("PlayerCombineHandler가 없습니다.");
            return;
        }

        // playerA를 본체로 설정
        a.StartCombineMode(roleA, playerA);
        // playerB는 playerA를 조종
        b.StartCombineMode(roleB, playerA);

        Debug.Log($"[2인 1조] {playerA.name}({roleA}) & {playerB.name}({roleB}) 합체 완료");
    }
}