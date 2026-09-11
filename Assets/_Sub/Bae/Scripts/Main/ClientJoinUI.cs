#nullable enable
using System.Collections;
using UnityEngine;
using Mirror;
using EpicTransport;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using TMPro;

public class ClientJoinUI : MonoBehaviour
{
    [Header("6자리 코드 UI 연결")]
    public SixDigitCodeInputUI? sixDigitUI;

    [Header("로딩 UI 연결 (비워두면 WalkingLoadingPanel 자동 탐색)")]
    [SerializeField] private GameObject? loadingPanel;

    [Header("에러 팝업 UI 연결 (선택 사항 - 화면에 에러 표시)")]
    [SerializeField] private GameObject? errorPopupPanel;
    [SerializeField] private TextMeshProUGUI? errorMessageText;

    private LobbySearch? currentSearchHandle;
    private LobbyDetails? foundLobbyDetails;
    private bool searchFinished = false;

    // ★ 연타 방지 플래그
    private bool isSearchingCode = false;
    private bool isClosingError = false;

    private void Start()
    {
        GameObject? panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }

    private GameObject? GetLoadingPanel()
    {
        if (loadingPanel != null) return loadingPanel;

        if (WalkingLoadingPanel.Instance != null)
        {
            loadingPanel = WalkingLoadingPanel.Instance.gameObject;
            return loadingPanel;
        }

        return null;
    }

    /// <summary>
    /// 6자리 코드 자물쇠 패널의 [PLAY / JOIN] 버튼 클릭 시 실행
    /// </summary>
    public void OnClick_ConnectByCode()
    {
        // 중복 검색 진행 중이면 중복 클릭 차단
        if (isSearchingCode) return;

        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        if (sixDigitUI == null)
        {
            Debug.LogError("[ClientJoinUI] 인스펙터에 sixDigitUI (SixDigitCodeInputUI)가 할당되지 않았습니다.");
            ShowErrorPopup("코드 입력 UI(SixDigitUI) 연결이 누락되었습니다.\n인스펙터를 확인해주세요.");
            return;
        }

        string code = sixDigitUI.GetCode();
        AutoConnectByCode(code);
    }

    /// <summary>
    /// 외부 플랫폼(Steam/Stove) 초대 수락 등으로 코드를 직접 주입하여 자동 접속할 때 사용
    /// </summary>
    public void AutoConnectByCode(string code)
    {
        if (isSearchingCode) return;

        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            ShowErrorPopup("올바른 6자리 코드를 입력해주세요.");
            return;
        }

        if (isSearchingCode)
        {
            Debug.LogWarning("[ClientJoinUI] 이미 방 참가(코드 검색)가 진행 중입니다.");
            return;
        }

        isSearchingCode = true; // 검색 상태 진입

        GameObject? currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
        }

        StartCoroutine(SearchAndJoinRoutine(code));
    }

    private IEnumerator SearchAndJoinRoutine(string code)
    {
        searchFinished = false;
        foundLobbyDetails = null;

        float eosReadyTimeout = 10f;
        while (!EOSSDKComponent.IsEOSReady() && eosReadyTimeout > 0f)
        {
            eosReadyTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!EOSSDKComponent.IsEOSReady() || EOSSDKComponent.LocalUserProductId == null)
        {
            isSearchingCode = false;
            HideLoadingPanel();
            ShowErrorPopup("EOS 네트워크 초기화 시간이 초과되었습니다. 잠시 후 다시 시도해주세요.");
            yield break;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            isSearchingCode = false; // ★ 리셋
            HideLoadingPanel();
            ShowErrorPopup("EOS 네트워크 시스템이 준비되지 않았습니다.");
            yield break;
        }

        CreateLobbySearchOptions searchOptions = new CreateLobbySearchOptions
        {
            MaxResults = 1
        };

        lobbyInterface.CreateLobbySearch(searchOptions, out currentSearchHandle);

        if (currentSearchHandle != null)
        {
            LobbySearchSetParameterOptions paramOptions = new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "SHORTCODE", Value = code }
            };

            currentSearchHandle.SetParameter(paramOptions);

            LobbySearchFindOptions findOptions = new LobbySearchFindOptions
            {
                LocalUserId = EOSSDKComponent.LocalUserProductId
            };

            currentSearchHandle.Find(findOptions, null, OnLobbySearchCompleted);
        }

        // 기존 방 검색 5초 타임아웃 유지
        float timeout = 5f;
        while (!searchFinished && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // 방 검색을 5초 동안 못 한 경우
        if (timeout <= 0f && !searchFinished)
        {
            Debug.LogWarning($"[ClientJoinUI] 코드 검색 5초 초과.");

            if (currentSearchHandle != null)
            {
                currentSearchHandle.Release();
                currentSearchHandle = null;
            }

            isSearchingCode = false; // ★ 리셋
            HideLoadingPanel();
            ShowErrorPopup("방 검색에 실패했습니다. 코드를 다시 확인해 주세요.");
            yield break;
        }

        // 방 발견 시 공식 방 참가 프로세스를 수행
        if (foundLobbyDetails != null)
        {
            // Debug.Log($"[ClientJoinUI] 코드 [{code}] 방 발견! ClientLobbyManager를 통해 참가를 진행합니다.");

            ClientLobbyManager lobbyManager = FindFirstObjectByType<ClientLobbyManager>();
            if (lobbyManager != null)
            {
                // ClientLobbyManager의 정상적인 JoinRoom(로비 입장 및 StartClient)을 위임
                // 이후 중복 입장 방지는 ClientLobbyManager.IsConnecting이 담당한다.
                isSearchingCode = false;
                lobbyManager.JoinRoom(foundLobbyDetails);
            }
            else
            {
                EOSLobby eosLobby = FindAnyObjectByType<EOSLobby>();
                if (eosLobby != null)
                {
                    isSearchingCode = false;
                    eosLobby.JoinLobby(foundLobbyDetails);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ClientJoinUI] 코드 '{code}'에 해당하는 방을 찾을 수 없습니다.");
            isSearchingCode = false; // ★ 리셋
            HideLoadingPanel();
            ShowErrorPopup($"코드 [{code}] 방을 찾을 수 없습니다.\n코드를 다시 확인해 주세요.");
        }
    }

    private void OnLobbySearchCompleted(LobbySearchFindCallbackInfo data)
    {
        if (currentSearchHandle == null) return;

        if (data.ResultCode == Result.Success && currentSearchHandle != null)
        {
            LobbySearchGetSearchResultCountOptions countOptions = new LobbySearchGetSearchResultCountOptions();
            uint count = currentSearchHandle.GetSearchResultCount(countOptions);

            if (count > 0)
            {
                LobbySearchCopySearchResultByIndexOptions copyOptions = new LobbySearchCopySearchResultByIndexOptions();
                copyOptions.LobbyIndex = 0;

                currentSearchHandle.CopySearchResultByIndex(copyOptions, out foundLobbyDetails);
            }
        }

        searchFinished = true;

        if (currentSearchHandle != null)
        {
            currentSearchHandle.Release();
            currentSearchHandle = null;
        }
    }

    private void HideLoadingPanel()
    {
        GameObject? currentPanel = GetLoadingPanel();
        if (currentPanel != null) currentPanel.SetActive(false);
    }

    private void ShowErrorPopup(string message)
    {
        isSearchingCode = false; // ★ 안전망: 에러 팝업 출력을 요청할 때 플래그 해제
        if (errorPopupPanel != null)
        {
            if (errorMessageText != null) errorMessageText.text = message;
            errorPopupPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[ClientJoinUI] " + message);
        }
    }

    public void OnClick_CloseErrorPopup()
    {
        if (isClosingError) return;
        isSearchingCode = false; // 플래그 해제
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        StartCoroutine(CloseErrorAndResetRoutine());
    }

    private IEnumerator CloseErrorAndResetRoutine()
    {
        isClosingError = true;

        // 진행 중 P2P handshake를 취소한 뒤 EOS Lobby 퇴장을 완료한다.
        if (NetworkManager.singleton != null)
        {
            if (NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
            }

            EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                eosLobby.LeaveLobby();

                float leaveTimeout = 5f;
                while (eosLobby.IsLeavingLobby && leaveTimeout > 0f)
                {
                    leaveTimeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
        isClosingError = false;
    }
}
