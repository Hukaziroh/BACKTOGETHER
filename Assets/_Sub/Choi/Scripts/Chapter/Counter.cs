using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class Counter : NetworkBehaviour
{
    [Header("판정 설정")]
    [Tooltip("충족해야 하는 플레이어 수 (기본 4명)")]
    public int requiredPlayerCount = 4;

    [Header("UI 설정 (TextMeshPro)")]
    [Tooltip("챕터 정보와 인원수를 표시할 TextMeshProUGUI 컴포넌트")]
    public TextMeshProUGUI counterAndChapterText;

    [Header("문구 설정")]
    [Tooltip("인원이 모두 찼을 때 표시할 글자")]
    public string completeText = "Go!"; // 유니티 인스펙터에서 원하는 글자로 수정 가능

    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    private int currentCount = 0;

    // 외부에서 4명이 다 찼는지 판정(true/false)으로 가져다 쓰는 속성
    public bool IsConditionMet => currentCount >= requiredPlayerCount;

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateUI(currentCount);
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersInZone.Add(other.gameObject);
            EvaluateCount();
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersInZone.Remove(other.gameObject);
            EvaluateCount();
        }
    }

    [Server]
    private void EvaluateCount()
    {
        playersInZone.RemoveWhere(go => go == null || !go.activeInHierarchy);
        currentCount = playersInZone.Count;
    }

    private void OnPlayerCountChanged(int oldVal, int newVal)
    {
        UpdateUI(newVal);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        // 이 프리팹은 NetworkIdentity가 있는 상위 네트워크 오브젝트의 자식으로 사용된다.
        // 프리팹 원본을 단독 검사할 때는 바깥 부모를 볼 수 없으므로 거짓 경고만 건너뛴다.
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject) &&
            GetComponentInParent<NetworkIdentity>(true) == null)
        {
            return;
        }

        // 씬에 배치된 인스턴스에서는 실제 부모 NetworkIdentity가 있는지 계속 검증한다.
        base.OnValidate();
    }
#endif

    private void UpdateUI(int count)
    {
        if (counterAndChapterText != null)
        {
            // 4명이 다 찼을 때와 아닐 때를 분기 처리
            if (count >= requiredPlayerCount)
            {
                counterAndChapterText.text = completeText;
            }
            else
            {
                counterAndChapterText.text = $"{count} / {requiredPlayerCount}";
            }
        }
    }
}
