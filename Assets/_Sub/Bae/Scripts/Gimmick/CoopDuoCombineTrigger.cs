using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CoopDuoCombineTrigger : NetworkBehaviour
{
    [Header("2인 1조 합체 기믹 설정")]
    [Tooltip("합체에 필요한 인원 수 (듀오이므로 2 권장)")]
    public int requiredPlayers = 2;

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
        if (playerList.Count < 2) return;

        Debug.Log($"{playerList.Count}인 진입 완료 -> 2인 듀오 기믹 발동!");
        AssignRole(playerList[0], playerList[1], CombineRole.Move, CombineRole.Jump);
        if (playerList.Count >= 4)
        {
            AssignRole(playerList[2], playerList[3], CombineRole.Move, CombineRole.Jump);
        }
    }

    [Server]
    private void AssignRole(GameObject playerA, GameObject playerB, CombineRole roleA, CombineRole roleB)
    {
        PlayerCombineHandler a = playerA.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler b = playerB.GetComponent<PlayerCombineHandler>();

        if (a == null || b == null)
        {
            Debug.LogError("PlayerCombineHandler가 없는 플레이어가 있습니다.");
            return;
        }
        a.StartCombineMode(roleA, playerA);
        b.StartCombineMode(roleB, playerA);

        Debug.Log($"[2인 1조 합체 완료] 본체:{playerA.name}({roleA}) / 탑승:{playerB.name}({roleB})");
    }
}