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
    public void OnClick_Research()
    {
        if (isLocalSearchRequest) return;

        SubscribeEvents();

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowError("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        // ★ 리로드 버튼을 누른 순간 현재 포커스(리로드 버튼)를 기억합니다.
        if (EventSystem.current != null)
        {
            lastSelectedBeforeSearch = EventSystem.current.currentSelectedGameObject;
        }
        BeginPublicListRefreshFocusLock();

        SetInteractableAll(false);

        List<LobbySearchSetParameterOptions> searchOptionsList = new List<LobbySearchSetParameterOptions>();

        bool isPrivateSearch = clientPrivatePanel != null && clientPrivatePanel.activeInHierarchy && privateRoomInputField != null && !string.IsNullOrEmpty(privateRoomInputField.text);

        if (isPrivateSearch)
        {
            searchOptionsList.Add(new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "IS_PUBLIC", Value = "0" }
            });
            searchOptionsList.Add(new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "SHORTCODE", Value = privateRoomInputField.text.Trim() }
            });
        }
        else
        {
            searchOptionsList.Add(new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "IS_PUBLIC", Value = "1" }
            });
        }

        isLocalSearchRequest = true;
        lobby.FindLobbies(50, searchOptionsList.ToArray());
    }

    private void OnFindLobbiesSucceeded(List<LobbyDetails> lobbies)
    {
        if (!isLocalSearchRequest) return;
        isLocalSearchRequest = false;

        bool isPrivateSearch = clientPrivatePanel != null && clientPrivatePanel.activeInHierarchy;

        if (!isQuickJoining && !isPrivateSearch && (clientPublicPanel == null || !clientPublicPanel.activeInHierarchy))
        {
            isQuickJoining = false;
            EndPublicListRefreshFocusLock();
            return;
        }

        SetInteractableAll(true);

        // 프라이빗 방 검색인 경우, 결과를 찾았으면 즉시 입장 시도
        if (isPrivateSearch)
        {
            if (lobbies != null && lobbies.Count > 0)
            {
                // Debug.Log($"[PrivateJoin] 프라이빗 방 발견! 코드: {privateRoomInputField.text}");
                JoinRoom(lobbies[0]);
            }
            else
            {
                ShowError("존재하지 않거나 이미 꽉 찬 방입니다.");
            }
            return;
        }

        // ★ 저장해 둔 포커스 복구 (리로드 버튼으로 다시 포커스 고정)
        if (EventSystem.current != null && lastSelectedBeforeSearch != null && lastSelectedBeforeSearch.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedBeforeSearch);
        }
        else if (researchButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(researchButton.gameObject);
        }

        allFetchedLobbies = lobbies ?? new List<LobbyDetails>();

        if (isQuickJoining)
        {
            isQuickJoining = false;

            foreach (var lobby in allFetchedLobbies)
            {
                if (EOSLobby.IsLobbyJoinable(lobby, out uint currentMembers, out uint maxMembers))
                {
                    // ★ 유령방(0명)에는 퀵 조인으로 들어가지 않도록 방어
                    if (currentMembers > 0)
                    {
                        // Debug.Log($"[QuickJoin] 빈 방 발견! ({currentMembers}/{maxMembers}) 즉시 입장합니다.");
                        JoinRoom(lobby);
                        return;
                    }
                }
            }

            if (quickJoinNoRoomText != null)
            {
                if (hideQuickJoinNoRoomCoroutine != null)
                    StopCoroutine(hideQuickJoinNoRoomCoroutine);

                hideQuickJoinNoRoomCoroutine = StartCoroutine(ShowAndHideQuickJoinNoRoomText());
            }

            // ★ 퀵 조인 시 빈 방이 없어 입장을 못 한 경우에도 버튼 상호작용 복구 및 포커스 정상 복원
            SetInteractableAll(true);
            if (EventSystem.current != null && lastSelectedBeforeSearch != null && lastSelectedBeforeSearch.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(lastSelectedBeforeSearch);
            }
            else if (quickJoinSelectionButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(quickJoinSelectionButton.gameObject);
            }
            EndPublicListRefreshFocusLock();

            return;
        }

        ApplyFiltersAndRefresh();
    }

    private IEnumerator ShowAndHideQuickJoinNoRoomText()
    {
        quickJoinNoRoomText.SetActive(true);
        yield return new WaitForSeconds(3f);
        quickJoinNoRoomText.SetActive(false);
        hideQuickJoinNoRoomCoroutine = null;
    }

    private void OnFindLobbiesFailed(string error)
    {
        if (!isLocalSearchRequest) return;
        isLocalSearchRequest = false;

        bool isPrivateSearch = clientPrivatePanel != null && clientPrivatePanel.activeInHierarchy;

        if (!isQuickJoining && !isPrivateSearch && (clientPublicPanel == null || !clientPublicPanel.activeInHierarchy))
        {
            isQuickJoining = false;
            EndPublicListRefreshFocusLock();
            return;
        }

        isQuickJoining = false;
        SetInteractableAll(true);

        if (isPrivateSearch)
        {
            ShowError("존재하지 않거나 이미 꽉 찬 방입니다.");
            return;
        }

        // 검색 실패 시에도 포커스 복구
        if (EventSystem.current != null && lastSelectedBeforeSearch != null && lastSelectedBeforeSearch.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedBeforeSearch);
        }
        else if (researchButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(researchButton.gameObject);
        }
        EndPublicListRefreshFocusLock();

        ShowError("방 목록을 불러오지 못했습니다: " + error);
    }

}
