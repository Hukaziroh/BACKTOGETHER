// CoopDuoCombineTrigger.cs
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class CoopDuoCombineTrigger : NetworkBehaviour
{
    [Header("2인 1조 합체 기믹 설정")]
    [Tooltip("합체에 필요한 최소 인원 수 (항상 2명씩 묶음)")]
    public int requiredPlayers = 4;

    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    // 딕셔너리로 듀오별 색상 및 눈 관리
    private static Dictionary<GameObject, int> activeDuoColors = new();
    private static Dictionary<GameObject, int> activeDuoFaces = new();

    public static void ReleaseColor(int index)
    {
        var keys = activeDuoColors
            .Where(x => x.Value == index)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in keys)
        {
            activeDuoColors.Remove(key);
        }
    }

    public static void ReleaseFace(int index)
    {
        var keys = activeDuoFaces
            .Where(x => x.Value == index)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in keys)
        {
            activeDuoFaces.Remove(key);
        }
    }

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
        List<GameObject> validPlayers = playersInZone.Where(p => !p.GetComponent<PlayerCombineHandler>().isCombined).ToList();

        if (validPlayers.Count < 2) return;

        GameObject p1 = validPlayers[0];
        GameObject p2 = validPlayers[1];

        playersInZone.Remove(p1);
        playersInZone.Remove(p2);

        if (UnityEngine.Random.value > 0.5f)
        {
            GameObject temp = p1;
            p1 = p2;
            p2 = temp;
        }

        Debug.Log($"2인 진입 완료 -> 듀오 기믹 발동! 본체:{p1.name} / 탑승:{p2.name}");
        AssignRole(p1, p2, CombineRole.Move, CombineRole.Jump);
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

        // 색상 중복 방지 (Dictionary values 기반 필터링)
        List<int> colors = new List<int>() { 0, 1 };
        foreach (int used in activeDuoColors.Values)
        {
            colors.Remove(used);
        }
        int color = colors.Count > 0 ? colors[Random.Range(0, colors.Count)] : Random.Range(0, 2);

        // 눈 모양 중복 방지 (Dictionary values 기반 필터링)
        List<int> faces = new List<int>() { 0, 1 };
        foreach (int used in activeDuoFaces.Values)
        {
            faces.Remove(used);
        }
        int face = faces.Count > 0 ? faces[Random.Range(0, faces.Count)] : Random.Range(0, 2);

        // 현재 듀오 정보 등록
        activeDuoColors[playerA] = color;
        activeDuoFaces[playerA] = face;

        a.combineColorIndex = color;
        a.combineFaceIndex = face;

        b.combineColorIndex = color;
        b.combineFaceIndex = face;

        a.connectedGhosts.Clear();
        a.connectedGhosts.Add(b);

        a.StartCombineMode(roleA, playerA);
        b.StartCombineMode(roleB, playerA);
    }
}