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
    public void OnClick_QuickJoin()
    {
        SubscribeEvents();

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowError("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        // ★ 퀵 조인 버튼을 누를 때 현재 선택되어 있던 포커스 오브젝트를 기억해 둠으로써 클릭 직후 백그라운드로 포커스가 새는 현상 차단
        if (EventSystem.current != null)
        {
            lastSelectedBeforeSearch = EventSystem.current.currentSelectedGameObject;
        }

        SetInteractableAll(false);

        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "IS_PUBLIC", Value = "1" }
            }
        };

        isQuickJoining = true;
        isLocalSearchRequest = true;
        lobby.FindLobbies(50, searchOptions);
    }

    private IEnumerator CheckConnectionTimeout()
    {
        float timer = 0f;
        float timeoutDuration = 10f;

        while (!NetworkClient.isConnected)
        {
            if (!NetworkClient.active) break;

            timer += Time.unscaledDeltaTime;
            if (timer >= timeoutDuration)
            {
                Debug.LogWarning("[ClientLobbyManager] P2P 연결 타임아웃!");
                HandleInitialConnectionTimeout();
                yield break;
            }
            yield return null;
        }

        if (!NetworkClient.isConnected)
        {
            HandleInitialConnectionTimeout();
        }
        else
        {
            // ★ 중요: 실제 Mirror 연결까지 성공했다면 Connecting 상태 해제
            // 이제부터 끊기는 건 HostDisconnectHandler가 담당함
            IsConnecting = false;
            // Debug.Log("[ClientLobbyManager] Mirror 연결 성공, HostDisconnectHandler로 감시 이관");
        }
    }

    private void HandleInitialConnectionTimeout()
    {
        if (isCleaningUpFailedConnection) return;
        StartCoroutine(CleanupInitialConnectionFailureRoutine());
    }

    private IEnumerator CleanupInitialConnectionFailureRoutine()
    {
        isCleaningUpFailedConnection = true;
        IsConnecting = false; // 접속 프로세스 종료

        Debug.LogWarning("[ClientLobbyManager] 초기 접속 실패, 네트워크 상태를 초기화합니다.");

        // 재시도 중인 CONNECT를 먼저 취소해야 leave 대기 중 늦게 연결되는 race가 없다.
        if (NetworkManager.singleton != null && NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        var eos = GetEOSLobby();
        if (eos != null && eos.ConnectedToLobby)
        {
            eos.LeaveLobby();

            float leaveTimeout = 5f;
            while (eos.IsLeavingLobby && leaveTimeout > 0f)
            {
                leaveTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        SetInteractableAll(true);
        ShowTimeoutPopup();
        isCleaningUpFailedConnection = false;
    }
}
