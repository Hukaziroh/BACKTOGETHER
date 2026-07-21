using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(Collider2D))]
public class TriggerPlayerCounter : NetworkBehaviour
{
    [Header("챕터 및 판정 설정")]
    [Tooltip("현재 챕터 번호")]
    public int chapterNumber = 1;
    [Tooltip("충족해야 하는 플레이어 수 (기본 4명)")]
    public int requiredPlayerCount = 4;

    [Header("UI 설정 (TextMeshPro)")]
    [Tooltip("챕터 정보와 인원수를 표시할 TextMeshProUGUI 컴포넌트")]
    public TextMeshProUGUI counterAndChapterText;

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

    private void UpdateUI(int count)
    {
        if (counterAndChapterText != null)
        {
            // 허공 캔버스에 챕터 번호와 인원수가 실시간으로 표시됩니다. (예: Chapter 1 \n 2 / 4)
            counterAndChapterText.text = $"Chapter {chapterNumber}\n{count} / {requiredPlayerCount}";
        }
    }
}