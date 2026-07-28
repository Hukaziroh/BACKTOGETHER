using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using System.Text.RegularExpressions;

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
            string rawText = counterAndChapterText.text;
            string baseChapterWord = "Chapter";

            if (!string.IsNullOrEmpty(rawText))
            {
                // 기존 텍스트에서 개행 문자('\n')나 숫자 등을 제외하고 순수 다국어 챕터 명칭만 추출
                int newlineIdx = rawText.IndexOf('\n');
                if (newlineIdx != -1)
                {
                    rawText = rawText.Substring(0, newlineIdx);
                }

                int spaceIdx = rawText.IndexOf(' ');
                if (spaceIdx != -1)
                {
                    baseChapterWord = rawText.Substring(0, spaceIdx);
                }
                else
                {
                    baseChapterWord = Regex.Replace(rawText, @"[\d]", "").Trim();
                    if (string.IsNullOrEmpty(baseChapterWord))
                    {
                        baseChapterWord = rawText;
                    }
                }
            }

            // [다국어 챕터 명칭] [챕터 번호] \n [현재 인원] / [필요 인원] 형태로 조합
            counterAndChapterText.text = $"{baseChapterWord} {chapterNumber}\n{count} / {requiredPlayerCount}";
        }
    }
}