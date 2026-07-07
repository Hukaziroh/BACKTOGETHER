using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CoopQuadCombineTrigger : NetworkBehaviour
{
    [Header("4인 1체 합체 기믹 설정")]
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
                ActivateQuadCombine();
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
    private void ActivateQuadCombine()
    {
        List<GameObject> playerList = new List<GameObject>(playersInZone);
        if (playerList.Count < 4) return;

        Debug.Log("4인 기믹 발동!");

        AssignFourRoles(playerList[0], playerList[1], playerList[2], playerList[3]);
    }

    [Server]
    private void AssignFourRoles(GameObject p1, GameObject p2, GameObject p3, GameObject p4)
    {
        PlayerCombineHandler h1 = p1.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler h2 = p2.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler h3 = p3.GetComponent<PlayerCombineHandler>();
        PlayerCombineHandler h4 = p4.GetComponent<PlayerCombineHandler>();

        if (h1 == null || h2 == null || h3 == null || h4 == null)
        {
            Debug.LogError("플레이어 중 PlayerCombineHandler가 없는 오브젝트가 있습니다.");
            return;
        }

        // p1을 본체로 만들고, 각각 역할을 분담시킴
        h1.StartCombineMode("Move_Left", p1);
        h2.StartCombineMode("Move_Right", p1);
        h3.StartCombineMode("Jump", p1);
        h4.StartCombineMode("Action", p1);

        Debug.Log($"[4인 1체] {p1.name}(본체/왼쪽), {p2.name}(오른쪽), {p3.name}(점프), {p4.name}(액션)");
    }
}