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
        List<GameObject> validPlayers = playersInZone
            .Where(p =>
                p != null &&
                p.activeInHierarchy &&
                p.GetComponent<PlayerCombineHandler>() != null &&
                !p.GetComponent<PlayerCombineHandler>().isCombined)
            .ToList();

        if (validPlayers.Count < requiredPlayers)
            return;

        validPlayers = validPlayers.Take(4).ToList();

        for (int i = 0; i < validPlayers.Count; i++)
        {
            int randomIndex = Random.Range(i, validPlayers.Count);

            GameObject temp = validPlayers[i];
            validPlayers[i] = validPlayers[randomIndex];
            validPlayers[randomIndex] = temp;
        }

        List<CombineRole> availableRoles = new List<CombineRole>
        {
            CombineRole.Move_Left,
            CombineRole.Move_Right,
            CombineRole.Jump,
            CombineRole.Action
        };

        for (int i = 0; i < availableRoles.Count; i++)
        {
            int randomIndex = Random.Range(i, availableRoles.Count);

            CombineRole temp = availableRoles[i];
            availableRoles[i] = availableRoles[randomIndex];
            availableRoles[randomIndex] = temp;
        }

        GameObject bodyPlayer = validPlayers[0];

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
            handler.myRole = CombineRole.None;
            handler.isCombined = false;
            handler.bodyTarget = null;
            handler.canUseAction = false;
            handler.combineColorIndex = -1;
            handler.combineFaceIndex = -1;
        }

        int combineColor = Random.Range(0, 2);
        int combineFace = Random.Range(0, 2);

        for (int i = 0; i < 4; i++)
        {
            PlayerCombineHandler handler =
                validPlayers[i].GetComponent<PlayerCombineHandler>();

            if (handler == null)
                continue;

            handler.myRole = availableRoles[i];
            handler.combineColorIndex = combineColor;
            handler.combineFaceIndex = combineFace;
        }

        bodyHandler.connectedGhosts.Clear();

        for (int i = 1; i < 4; i++)
        {
            PlayerCombineHandler ghostHandler =
                validPlayers[i].GetComponent<PlayerCombineHandler>();

            if (ghostHandler != null &&
                !bodyHandler.connectedGhosts.Contains(ghostHandler))
            {
                bodyHandler.connectedGhosts.Add(ghostHandler);
            }
        }

        for (int i = 0; i < 4; i++)
        {
            PlayerCombineHandler handler =
                validPlayers[i].GetComponent<PlayerCombineHandler>();

            if (handler == null)
                continue;

            handler.StartCombineMode(
                handler.myRole,
                bodyPlayer
            );

            Debug.Log(
                $"[4인 합체 역할] {validPlayers[i].name} -> {handler.myRole}"
            );
        }

        foreach (GameObject player in validPlayers)
        {
            playersInZone.Remove(player);
        }

        Debug.Log(
            $"[4인 합체 완료] Body = {bodyPlayer.name}"
        );
    }
}