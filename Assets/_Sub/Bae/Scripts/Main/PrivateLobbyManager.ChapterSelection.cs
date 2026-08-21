using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class PrivateLobbyManager
{
    public void OnClick_PrevChapter()
    {
        int maxCleared = (GameSaveManager.Instance != null) ? GameSaveManager.Instance.currentData.maxClearedChapter : 0;
        selectedChapterIndex--;

        // 만약 이전으로 갔는데 그 챕터가 EX 챕터이고 6챕터를 깨지 않았다면 한 번 더 건너뜁니다.
        if (IsExChapter(selectedChapterIndex) && maxCleared < 6)
        {
            selectedChapterIndex--;
        }

        if (selectedChapterIndex < 1) selectedChapterIndex = maxChapterCount;

        // 순회 중 도달한 곳이 여전히 잠겨있는 EX 챕터라면 1번 챕터로 돌립니다.
        if (IsExChapter(selectedChapterIndex) && maxCleared < 6)
        {
            selectedChapterIndex = 1;
        }

        UpdateChapterUI();
    }

    public void OnClick_NextChapter()
    {
        int maxCleared = (GameSaveManager.Instance != null) ? GameSaveManager.Instance.currentData.maxClearedChapter : 0;
        int nextIndex = selectedChapterIndex + 1;

        // 다음 챕터가 EX 스테이지이고 6챕터를 클리어하지 않았다면 목록에서 아예 숨기기 위해 다음으로 넘어가지 않고 1번(또는 처음)으로 순환시킵니다.
        if (IsExChapter(nextIndex) && maxCleared < 6)
        {
            nextIndex = 1; // 혹은 return을 통해 진입을 막을 수 있습니다. 여기서는 순환 구조상 1번으로 리셋
        }

        selectedChapterIndex = nextIndex;
        if (selectedChapterIndex > maxChapterCount) selectedChapterIndex = 1;

        // 한 번 더블 체크
        if (IsExChapter(selectedChapterIndex) && maxCleared < 6)
        {
            selectedChapterIndex = 1;
        }

        UpdateChapterUI();
    }

    private bool IsExChapter(int chapterIndex)
    {
        int arrayIndex = chapterIndex - 1;
        string currentChapterName = (chapterNames != null && arrayIndex >= 0 && arrayIndex < chapterNames.Length) ? chapterNames[arrayIndex] : "";
        return currentChapterName.Contains("EX") || chapterIndex >= 7;
    }

    // PrivateLobbyManager.cs의 UpdateChapterUI 메서드 부분을 아래와 같이 보완합니다.

    private void UpdateChapterUI()
    {
        int displayChapter = selectedChapterIndex;
        int arrayIndex = selectedChapterIndex - 1;

        if (chapterDisplayText != null)
        {
            if (chapterNames != null && arrayIndex >= 0 && arrayIndex < chapterNames.Length && !string.IsNullOrEmpty(chapterNames[arrayIndex]))
            {
                chapterDisplayText.text = chapterNames[arrayIndex];
            }
            else
            {
                chapterDisplayText.text = $"Chapter {displayChapter}";
            }
        }

        if (chapterPreviewImage != null && chapterSprites != null && chapterSprites.Length > arrayIndex)
            chapterPreviewImage.sprite = chapterSprites[arrayIndex];

        bool isUnlocked = true;

        // ★ EX 스테이지 또는 특정 고난도 챕터(예: EX 6챕 등) 클리어 체크 조건 예시
        // 만약 챕터 이름에 "EX"가 포함되어 있거나 특정 인덱스 이상일 때의 조건 처리
        string currentChapterName = (chapterNames != null && arrayIndex >= 0 && arrayIndex < chapterNames.Length) ? chapterNames[arrayIndex] : "";
        bool isExStage = currentChapterName.Contains("EX") || displayChapter >= 7; // EX 스테이지 판별 조건 (프로젝트에 맞게 조절)

        if (GameSaveManager.Instance != null)
        {
            int maxCleared = GameSaveManager.Instance.currentData.maxClearedChapter;

            // 예시: EX 스테이지이거나 특정 챕터인 경우, 이전 6챕터 이상을 클리어해야만 열리도록 설정
            if (isExStage)
            {
                // 예: EX 6챕 클리어 전까지 숨기거나 잠그기 (여기서는 maxCleared가 특정 값 미만이면 잠금)
                // 프로젝트의 maxClearedChapter 체계에 맞춰서 비교 (예: 6챕 이상 클리어 필요)
                if (maxCleared < 6) // 6챕 클리어 전 조건
                {
                    isUnlocked = false;
                }
            }
            else
            {
                if (maxCleared < arrayIndex) isUnlocked = false;
            }
        }

        if (chapterLockObject != null) chapterLockObject.SetActive(!isUnlocked);
        if (makeRoomButton != null) makeRoomButton.interactable = isUnlocked;
        if (chapterPreviewImage != null) chapterPreviewImage.color = isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
    }

}
