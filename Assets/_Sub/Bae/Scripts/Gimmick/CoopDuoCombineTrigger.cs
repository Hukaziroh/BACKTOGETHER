using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq; 

public class CoopDuoCombineTrigger : NetworkBehaviour
{
    [Header("2인 1조 합체 기믹 설정")]
    [Tooltip("합체에 필요한 최소 인원 수 (항상 2명씩 묶음)")]
    public int requiredPlayers = 2;

    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.RemoveWhere(p => p == null || !p.activeInHierarchy);
            playersInZone.Add(other.gameObject);
            if (playersInZone.Count >= requiredPlayers)
            {
                ActivateDuoPair();
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.Remove(other.gameObject);
        }
    }

    [Server]
    private void ActivateDuoPair()
    {
        List<GameObject> pair = playersInZone.Take(2).ToList();

        if (pair.Count < 2) return;
        playersInZone.Remove(pair[0]);
        playersInZone.Remove(pair[1]);

        if (UnityEngine.Random.value > 0.5f)
        {
            GameObject temp = pair[0];
            pair[0] = pair[1];
            pair[1] = temp;
        }

        Debug.Log($"2인 진입 완료 -> 듀오 기믹 발동! 본체:{pair[0].name} / 탑승:{pair[1].name}");
        AssignRole(pair[0], pair[1], CombineRole.Move, CombineRole.Jump);
        int id0 = pair[0].GetComponent<CoopPlayerIdentity>().playerIndex;
        int id1 = pair[1].GetComponent<CoopPlayerIdentity>().playerIndex;
        pair[0].GetComponent<CoopPlayerIdentity>().SetCombinedColors(2, id0, id1);
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