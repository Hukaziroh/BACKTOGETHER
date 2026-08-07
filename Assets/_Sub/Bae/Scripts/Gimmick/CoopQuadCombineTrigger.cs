using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class CoopQuadCombineTrigger : NetworkBehaviour
{
    [Header("4인 합체 기믹 설정")]
    public int requiredPlayers = 4;

    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            PlayerCombineHandler combine = other.GetComponent<PlayerCombineHandler>();

            if (combine != null && combine.isCombined) return;

            playersInZone.RemoveWhere(p => p == null || !p.activeInHierarchy ||
                (p.GetComponent<PlayerCombineHandler>() != null && p.GetComponent<PlayerCombineHandler>().isCombined));

            playersInZone.Add(other.gameObject);

            while (playersInZone.Count >= requiredPlayers)
            {
                ActivateQuadGroup();
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
    private void ActivateQuadGroup()
    {
        List<GameObject> validPlayers = playersInZone.Where(p => p != null && !p.GetComponent<PlayerCombineHandler>().isCombined).ToList();

        if (validPlayers.Count < 4) return;

        GameObject playerA = validPlayers[0];
        GameObject playerB = validPlayers[1];
        GameObject playerC = validPlayers[2];
        GameObject playerD = validPlayers[3];

        PlayerCombineHandler a = playerA.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler b = playerB.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler c = playerC.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler d = playerD.GetComponent<PlayerCombineHandler>();

        a.connectedGhosts.Clear();
        b.connectedGhosts.Clear();
        c.connectedGhosts.Clear();
        d.connectedGhosts.Clear();

        int color = Random.Range(0, 2);
        int face = Random.Range(0, 2);

        a.combineColorIndex = color; a.combineFaceIndex = face;
        b.combineColorIndex = color; b.combineFaceIndex = face;
        c.combineColorIndex = color; c.combineFaceIndex = face;
        d.combineColorIndex = color; d.combineFaceIndex = face;

        List<CombineRole> roles = new List<CombineRole> {
            CombineRole.Move_Left,
            CombineRole.Move_Right,
            CombineRole.Jump,
            CombineRole.Action
        };

        for (int i = 0; i < roles.Count; i++)
        {
            int rand = Random.Range(i, roles.Count);
            CombineRole temp = roles[i];
            roles[i] = roles[rand];
            roles[rand] = temp;
        }

        a.StartCombineMode(roles[0], playerA);
        b.StartCombineMode(roles[1], playerA);
        c.StartCombineMode(roles[2], playerA);
        d.StartCombineMode(roles[3], playerA);

        Debug.Log($"[4인 합체 배정 완료] 본체({playerA.name}) -> {roles[0]}");
        Debug.Log($"[4인 합체 배정 완료] 탑승({playerB.name}) -> {roles[1]}");
        Debug.Log($"[4인 합체 배정 완료] 탑승({playerC.name}) -> {roles[2]}");
        Debug.Log($"[4인 합체 배정 완료] 탑승({playerD.name}) -> {roles[3]}");

        playersInZone.Remove(playerA);
        playersInZone.Remove(playerB);
        playersInZone.Remove(playerC);
        playersInZone.Remove(playerD);
    }
}
   