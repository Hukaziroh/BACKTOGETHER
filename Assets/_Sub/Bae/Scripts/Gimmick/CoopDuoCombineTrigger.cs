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

        if (other.CompareTag("Player"))
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

        if (other.CompareTag("Player"))
        {
            playersInZone.Remove(other.gameObject);
        }
    }

    [Server]
    private void ActivateDuoCombine()
    {
        List<GameObject> playerList = new List<GameObject>(playersInZone);
        if (playerList.Count < 2) return;

        Debug.Log("기믹을 발동");

        AssignRole(playerList[0], playerList[1], "Move", "Jump");

        //AssignRole(playerList[2], playerList[3], "Move", "Jump");
    }

    [Server]
    private void AssignRole(GameObject playerA, GameObject playerB,
                        string roleA, string roleB)
    {
        PlayerCombineHandler a =
            playerA.GetComponent<PlayerCombineHandler>();

        PlayerCombineHandler b =
            playerB.GetComponent<PlayerCombineHandler>();

        if (a == null || b == null)
        {
            Debug.LogError("PlayerCombineHandler가 없습니다.");
            return;
        }

        // playerA를 본체로 설정
        a.StartCombineMode(roleA, playerA);

        // playerB는 playerA를 조종
        b.StartCombineMode(roleB, playerA);

        Debug.Log($"[2인 1조] {playerA.name} & {playerB.name} 합체 완료");
    }
}

