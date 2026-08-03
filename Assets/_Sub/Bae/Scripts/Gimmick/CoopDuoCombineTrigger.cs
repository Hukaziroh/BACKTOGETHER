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
            PlayerCombineHandler combine = other.GetComponent<PlayerCombineHandler>();

            // 🌟 버그 7 해결 핵심 1: 이미 합체된 플레이어는 대기열에 넣지 않고 입구컷!
            if (combine != null && combine.isCombined) return;

            // 혹시라도 리스트 안에 죽어있거나 이미 합체된 녀석이 있다면 청소
            playersInZone.RemoveWhere(p => p == null || !p.activeInHierarchy ||
                (p.GetComponent<PlayerCombineHandler>() != null && p.GetComponent<PlayerCombineHandler>().isCombined));

            playersInZone.Add(other.gameObject);

            // 🌟 버그 7 해결 핵심 2: 4명이 동시에 우르르 들어와도 2명씩 계속 짝지어 방출하도록 while문 사용
            while (playersInZone.Count >= requiredPlayers)
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
        // 안전장치: 혹시라도 섞여 들어간 합체 유저를 한 번 더 거르고 순수 솔로 유저만 추출
        List<GameObject> validPlayers = playersInZone.Where(p => !p.GetComponent<PlayerCombineHandler>().isCombined).ToList();

        if (validPlayers.Count < 2) return;

        // 정확히 2명만 빼오기
        GameObject p1 = validPlayers[0];
        GameObject p2 = validPlayers[1];

        playersInZone.Remove(p1);
        playersInZone.Remove(p2);

        // 50% 확률로 본체 랜덤 배정
        if (UnityEngine.Random.value > 0.5f)
        {
            GameObject temp = p1;
            p1 = p2;
            p2 = temp;
        }

        Debug.Log($"2인 진입 완료 -> 듀오 기믹 발동! 본체:{p1.name} / 탑승:{p2.name}");
        AssignRole(p1, p2, CombineRole.Move, CombineRole.Jump);
        int id0 = p1.GetComponent<CoopPlayerIdentity>().playerIndex;
        int id1 = p2.GetComponent<CoopPlayerIdentity>().playerIndex;
        p1.GetComponent<CoopPlayerIdentity>().SetCombinedColors(2, id0, id1);
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