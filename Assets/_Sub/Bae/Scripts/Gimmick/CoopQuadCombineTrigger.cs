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

            if (playersInZone.Count >= requiredPlayers)
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
        List<GameObject> validPlayers = playersInZone
            .Where(p =>
                p != null &&
                p.activeInHierarchy &&
                p.GetComponent<PlayerCombineHandler>() != null &&
                !p.GetComponent<PlayerCombineHandler>().isCombined)
            .Distinct()
            .ToList();

        if (validPlayers.Count < 4)
            return;

        // 정확히 4명만 확정
        validPlayers = validPlayers.Take(4).ToList();

        List<CombineRole> roles = new List<CombineRole>
    {
        CombineRole.Move_Left,
        CombineRole.Move_Right,
        CombineRole.Jump,
        CombineRole.Action
    };

        // 역할 랜덤 섞기
        for (int i = 0; i < roles.Count; i++)
        {
            int randomIndex = Random.Range(i, roles.Count);

            CombineRole temp = roles[i];
            roles[i] = roles[randomIndex];
            roles[randomIndex] = temp;
        }

        int bodyIndex = Random.Range(0, validPlayers.Count);
        GameObject bodyPlayer = validPlayers[bodyIndex];

        PlayerCombineHandler bodyHandler =
            bodyPlayer.GetComponent<PlayerCombineHandler>();

        if (bodyHandler == null)
            return;

        foreach (GameObject player in validPlayers)
        {
            PlayerCombineHandler handler =
                player.GetComponent<PlayerCombineHandler>();

            if (handler == null)
                continue;

            handler.connectedGhosts.Clear();

            handler.isCombined = false;
            handler.myRole = CombineRole.None;
            handler.bodyTarget = null;
            handler.canUseAction = false;
            handler.combineColorIndex = -1;
            handler.combineFaceIndex = -1;
        }
        int combineColor = Random.Range(0, 2);
        int combineFace = Random.Range(0, 2);

        for (int i = 0; i < 4; i++)
        {
            GameObject player = validPlayers[i];

            PlayerCombineHandler handler =
                player.GetComponent<PlayerCombineHandler>();

            if (handler == null)
                continue;

            CombineRole assignedRole = roles[i];

            handler.myRole = assignedRole;

            handler.combineColorIndex = combineColor;
            handler.combineFaceIndex = combineFace;

            handler.canUseAction =
                assignedRole == CombineRole.Action;

            Debug.Log(
                $"[4인 합체 역할 확정] " +
                $"{player.name} -> {assignedRole}"
            );
        }
        HashSet<CombineRole> assignedRoles = new HashSet<CombineRole>();

        for (int i = 0; i < 4; i++)
        {
            PlayerCombineHandler handler =
                validPlayers[i].GetComponent<PlayerCombineHandler>();

            if (handler == null)
            {
                Debug.LogError(
                    $"[4인 합체 오류] PlayerCombineHandler 없음: {validPlayers[i].name}"
                );
                return;
            }

            if (!assignedRoles.Add(handler.myRole))
            {
                Debug.LogError(
                    $"[4인 합체 오류] 역할 중복 발생! " +
                    $"{validPlayers[i].name} -> {handler.myRole}"
                );
                return;
            }
        }
        bodyHandler.connectedGhosts.Clear();

        foreach (GameObject player in validPlayers)
        {
            if (player == bodyPlayer)
                continue;

            PlayerCombineHandler ghostHandler =
                player.GetComponent<PlayerCombineHandler>();

            if (ghostHandler != null &&
                !bodyHandler.connectedGhosts.Contains(ghostHandler))
            {
                bodyHandler.connectedGhosts.Add(ghostHandler);
            }
        }
        foreach (GameObject player in validPlayers)
        {
            PlayerCombineHandler handler =
                player.GetComponent<PlayerCombineHandler>();

            if (handler == null)
                continue;

            handler.StartCombineMode(
                handler.myRole,
                bodyPlayer
            );
        }
        foreach (GameObject player in validPlayers)
        {
            playersInZone.Remove(player);
        }
        Debug.Log(
            $"[4인 합체 완료] " +
            $"Body={bodyPlayer.name} / " +
            $"{validPlayers[0].name}={validPlayers[0].GetComponent<PlayerCombineHandler>().myRole}, " +
            $"{validPlayers[1].name}={validPlayers[1].GetComponent<PlayerCombineHandler>().myRole}, " +
            $"{validPlayers[2].name}={validPlayers[2].GetComponent<PlayerCombineHandler>().myRole}, " +
            $"{validPlayers[3].name}={validPlayers[3].GetComponent<PlayerCombineHandler>().myRole}"
        );

    }
}