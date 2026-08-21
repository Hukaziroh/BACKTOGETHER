using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // 씬 초기화용 네임스페이스 추가
using UnityEngine.UI;

public partial class ClientLobbyManager
{
    public void OnSearchInputChanged(string input) { ApplyFiltersAndRefresh(); }

    // ★ PrivateLobbyManager의 chapterNames 배열을 보호 수준 에러(CS0122) 없이 안전하게 가져오는 헬퍼 메서드 (리플렉션 사용)
    private string[] GetChapterNamesFromPrivateManager()
    {
        if (privateLobbyManager == null) return null;
        try
        {
            var field = privateLobbyManager.GetType().GetField("chapterNames", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(privateLobbyManager) as string[];
            }
            var prop = privateLobbyManager.GetType().GetProperty("chapterNames", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null)
            {
                return prop.GetValue(privateLobbyManager) as string[];
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ClientLobbyManager] chapterNames 취득 실패: {e.Message}");
        }
        return null;
    }

    // PrivateLobbyManager의 챕터 개수를 동적으로 가져옴
    private int GetMaxChapterCount()
    {
        string[] names = GetChapterNamesFromPrivateManager();
        if (names != null && names.Length > 0)
        {
            return names.Length;
        }
        return 6; // 기본값
    }

    private bool IsExChapter(int chapterIndex)
    {
        if (chapterIndex <= 0) return false;
        int arrayIndex = chapterIndex - 1;
        string[] names = GetChapterNamesFromPrivateManager();
        string currentChapterName = (names != null && arrayIndex >= 0 && arrayIndex < names.Length) ? names[arrayIndex] : "";
        return currentChapterName.Contains("EX") || chapterIndex >= 7;
    }

    public void OnClick_PrevFilterChapter()
    {
        int maxCh = GetMaxChapterCount();
        int maxCleared = (GameSaveManager.Instance != null) ? GameSaveManager.Instance.currentData.maxClearedChapter : 0;

        selectedFilterChapter--;

        if (IsExChapter(selectedFilterChapter) && maxCleared < 6)
        {
            selectedFilterChapter--;
        }

        if (selectedFilterChapter < 0) selectedFilterChapter = maxCh;

        if (IsExChapter(selectedFilterChapter) && maxCleared < 6)
        {
            selectedFilterChapter = 0;
        }

        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    public void OnClick_NextFilterChapter()
    {
        int maxCh = GetMaxChapterCount();
        int maxCleared = (GameSaveManager.Instance != null) ? GameSaveManager.Instance.currentData.maxClearedChapter : 0;

        selectedFilterChapter++;

        if (selectedFilterChapter > maxCh) selectedFilterChapter = 0;

        if (IsExChapter(selectedFilterChapter) && maxCleared < 6)
        {
            selectedFilterChapter++;
            if (selectedFilterChapter > maxCh) selectedFilterChapter = 0;
        }

        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    private void UpdateFilterChapterUI()
    {
        if (filterChapterText != null)
        {
            if (selectedFilterChapter == 0)
            {
                filterChapterText.text = "Chapter All";
            }
            else
            {
                int index = selectedFilterChapter - 1;
                string[] names = GetChapterNamesFromPrivateManager();
                if (names != null && index >= 0 && index < names.Length && !string.IsNullOrEmpty(names[index]))
                {
                    filterChapterText.text = names[index];
                }
                else
                {
                    filterChapterText.text = $"Chapter {selectedFilterChapter}";
                }
            }
        }
    }

    private void ApplyFiltersAndRefresh()
    {
        filteredLobbies.Clear();
        string searchKey = (searchInputField != null) ? searchInputField.text.Trim().ToLower() : "";

        string[] chapterNames = GetChapterNamesFromPrivateManager();

        // 플레이어의 최대 클리어 챕터 정보 가져오기
        int maxClearedChapter = 0;
        if (GameSaveManager.Instance != null)
        {
            maxClearedChapter = GameSaveManager.Instance.currentData.maxClearedChapter;
        }

        foreach (var lobby in allFetchedLobbies)
        {
            if (lobby == null) continue;

            string roomName = GetLobbyAttribute(lobby, "ROOM_NAME", "");
            if (!string.IsNullOrEmpty(searchKey) && !roomName.ToLower().Contains(searchKey))
                continue;

            string chapterStr = GetLobbyAttribute(lobby, "CHAPTER", "");

            int chapterNum = 0;
            int.TryParse(chapterStr, out chapterNum);

            // ★ [핵심 요구사항] EX 스테이지 / 6챕 클리어 전까지 검색 결과에서 숨기기 로직
            bool isExStage = chapterStr.Contains("EX") || chapterNum >= 7;
            if (!isExStage && chapterNames != null && chapterNum > 0 && chapterNum <= chapterNames.Length)
            {
                if (chapterNames[chapterNum - 1].Contains("EX"))
                {
                    isExStage = true;
                }
            }

            if (isExStage && maxClearedChapter < 6)
            {
                continue;
            }

            // ★ 기존 챕터 필터링 로직
            if (selectedFilterChapter > 0)
            {
                int target1Based = selectedFilterChapter;
                int target0Based = selectedFilterChapter - 1;
                string targetName = (chapterNames != null && target0Based >= 0 && target0Based < chapterNames.Length) ? chapterNames[target0Based] : null;

                bool isChapterMatch = false;

                if (!string.IsNullOrEmpty(chapterStr))
                {
                    if (int.TryParse(chapterStr, out int chVal))
                    {
                        if (chVal == target1Based || chVal == target0Based)
                            isChapterMatch = true;
                    }

                    if (!isChapterMatch && !string.IsNullOrEmpty(targetName))
                    {
                        if (string.Equals(chapterStr.Trim(), targetName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                            isChapterMatch = true;
                    }

                    if (!isChapterMatch && string.Equals(chapterStr.Trim(), $"Chapter{target1Based}", System.StringComparison.OrdinalIgnoreCase))
                    {
                        isChapterMatch = true;
                    }
                }

                if (!isChapterMatch)
                    continue;
            }

            filteredLobbies.Add(lobby);
        }

        currentPage = 0;
        RefreshUI();
    }

}
