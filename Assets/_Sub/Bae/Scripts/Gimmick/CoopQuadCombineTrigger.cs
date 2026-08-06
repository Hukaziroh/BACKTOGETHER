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
        List<GameObject> validPlayers = playersInZone.Where(p => !p.GetComponent<PlayerCombineHandler>().isCombined).ToList();

        if (validPlayers.Count < 4) return;

        GameObject p1 = validPlayers[0];
        GameObject p2 = validPlayers[1];
        GameObject p3 = validPlayers[2];
        GameObject p4 = validPlayers[3];

        for (int i = 0; i < 4; i++)
        {
            playersInZone.Remove(validPlayers[i]);
        }

        Debug.Log("4인 진입 완료 -> 쿼드 기믹 발동!");
        AssignRole(p1, p2, p3, p4);
    }

    [Server]
    private void AssignRole(GameObject playerA, GameObject playerB, GameObject playerC, GameObject playerD)
    {
        PlayerCombineHandler a = playerA.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler b = playerB.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler c = playerC.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler d = playerD.GetComponent<PlayerCombineHandler>();

        a.connectedGhosts.Clear();
        b.connectedGhosts.Clear();
        c.connectedGhosts.Clear();
        d.connectedGhosts.Clear();

        if (a == null || b == null || c == null || d == null)
        {
            Debug.LogError("PlayerCombineHandler가 없는 플레이어가 있습니다.");
            return;
        }

        int color = Random.Range(0, 2);
        int face = Random.Range(0, 2);

        a.combineColorIndex = color;
        a.combineFaceIndex = face;
        b.combineColorIndex = color;
        b.combineFaceIndex = face;
        c.combineColorIndex = color;
        c.combineFaceIndex = face;
        d.combineColorIndex = color;
        d.combineFaceIndex = face;

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
    }
}