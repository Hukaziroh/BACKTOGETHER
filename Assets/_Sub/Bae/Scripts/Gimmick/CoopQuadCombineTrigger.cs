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

        Debug.Log("기믹을 발동!");

        AssignFourRoles(playerList[0], playerList[1], playerList[2], playerList[3]);
    }

    [Server]
    private void AssignFourRoles(GameObject p1, GameObject p2, GameObject p3, GameObject p4)
    {
        string role1 = "Move_Left";  
        string role2 = "Move_Right";
        string role3 = "Jump";      
        string role4 = "Action";    

        // 플레이어 담당자가 구현할 합체 함수 호출 부분 (예시)
        // p1.GetComponent<PlayerCombineHandler>().RpcStartQuadMode(role1, p1);
        // p2.GetComponent<PlayerCombineHandler>().RpcStartQuadMode(role2, p1);
        // p3.GetComponent<PlayerCombineHandler>().RpcStartQuadMode(role3, p1);
        // p4.GetComponent<PlayerCombineHandler>().RpcStartQuadMode(role4, p1);

        Debug.Log($"[4인 1체] {p1.name}(본체/왼쪽), {p2.name}(오른쪽), {p3.name}(점프), {p4.name}(액션)");
    }
}