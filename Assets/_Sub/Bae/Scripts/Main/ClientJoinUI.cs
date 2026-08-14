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

    [Header("타임아웃 설정")]
    [Tooltip("방 접속 시도 후 무한 로딩 방지 제한 시간 (초)")]
    [SerializeField] private float joinTimeout = 10f;

    private LobbySearch? currentSearchHandle;
    private LobbyDetails? foundLobbyDetails;
    private bool searchFinished = false;
    private Coroutine? timeoutCoroutine;

    // ★ 연타 방지 플래그
    private bool isSearchingCode = false;

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
        // ★ 이미 검색 진행 중이면 중복 클릭 차단
        if (isSearchingCode) return;

        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        if (sixDigitUI == null)
        {
            Debug.LogError("[ClientJoinUI] 인스펙터에 sixDigitUI (SixDigitCodeInputUI)가 할당되지 않았습니다!");
            ShowErrorPopup("코드 입력 UI(SixDigitUI) 연결이 누락되었습니다.\n인스펙터를 확인해 주세요.");
            return;
        }

        string code = sixDigitUI.GetCode();
        Debug.Log($"[ClientJoinUI] 입력받은 6자리 코드: '{code}'");

        if (string.IsNullOrEmpty(code) || code.Length < 6)
        {
            ShowErrorPopup("6자리 코드를 정확히 입력해 주세요.");
            return;
        }

        // ★ 검색 시작 상태로 전환
        isSearchingCode = true;

        GameObject? currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
        }

        // 기존 타임아웃 타이머 초기화 후 무한 로딩 감지 코루틴 시작
        CancelTimeout();
        timeoutCoroutine = StartCoroutine(JoinTimeoutRoutine());

        StartCoroutine(SearchAndJoinRoutine(code));
    }

    /// <summary>
    /// 무한 로딩 감지 및 타임아웃 처리 코루틴
    /// </summary>
    private IEnumerator JoinTimeoutRoutine()
    {
        yield return new WaitForSeconds(joinTimeout);

        GameObject? panel = GetLoadingPanel();
        if (panel != null && panel.activeSelf)
        {
            Debug.LogWarning($"[ClientJoinUI] 방 진입 타임아웃 ({joinTimeout}초 초과) -> 접속 시도를 강제 중단합니다.");

            if (currentSearchHandle != null)
            {
                currentSearchHandle.Release();
                currentSearchHandle = null;
            }

            if (NetworkManager.singleton != null)
            {
                if (NetworkManager.singleton.isNetworkActive)
                {
                    NetworkManager.singleton.StopClient();
                }

                // ★ [추가] EosTransport P2P 세션 완전 초기화
                EosTransport transport = NetworkManager.singleton.GetComponent<EosTransport>();
                if (transport != null)
                {
                    transport.Shutdown();
                }

                EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
                if (eosLobby != null && eosLobby.ConnectedToLobby)
                {
                    eosLobby.LeaveLobby();
                }
            }

            isSearchingCode = false;
            HideLoadingPanel();
            ShowErrorPopup("방 접속 시간이 초과되었습니다.\n(서버 응답이 없거나 네트워크가 불안정합니다.)");
        }
    }

    private IEnumerator SearchAndJoinRoutine(string code)
    {
        searchFinished = false;
        foundLobbyDetails = null;

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            isSearchingCode = false; // ★ 리셋
            CancelTimeout();
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
            CancelTimeout();
            HideLoadingPanel();
            ShowErrorPopup("방 검색에 실패했습니다. 코드를 다시 확인해 주세요.");
            yield break;
        }

        // 방 발견 시 EOSLobby를 통해 공식 방 참가 실행
        if (foundLobbyDetails != null)
        {
            Debug.Log($"[ClientJoinUI] 코드 [{code}] 방 발견! EOSLobby를 통해 참가를 진행합니다.");

            EOSLobby eosLobby = FindAnyObjectByType<EOSLobby>();
            if (eosLobby == null && NetworkManager.singleton != null)
            {
                eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            }

            if (eosLobby != null)
            {
                // 정상적으로 참가가 진행되면 씬이 넘어가며, 
                // 타임아웃 발생 시 JoinTimeoutRoutine에서 isSearchingCode가 해제됩니다.
                eosLobby.JoinLobby(foundLobbyDetails);
            }
            else
            {
                Debug.LogError("[ClientJoinUI] EOSLobby 인스턴스를 찾을 수 없습니다.");
                isSearchingCode = false; // ★ 리셋
                CancelTimeout();
                HideLoadingPanel();
                ShowErrorPopup("네트워크 매니저(EOSLobby)를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[ClientJoinUI] 코드 '{code}'에 해당하는 방을 찾을 수 없습니다.");
            isSearchingCode = false; // ★ 리셋
            CancelTimeout();
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

    private void CancelTimeout()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
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
        isSearchingCode = false; // 플래그 해제
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        // 1. Mirror 및 EOS 로비 정리
        if (NetworkManager.singleton != null)
        {
            if (NetworkManager.singleton.isNetworkActive)
            {
                NetworkManager.singleton.StopClient();
            }

            EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                eosLobby.LeaveLobby();
            }
        }

        // 2. 씬 재로드로 좀비 P2P 소켓 제거
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}