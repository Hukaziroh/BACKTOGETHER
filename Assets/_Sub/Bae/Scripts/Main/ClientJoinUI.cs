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
    private LobbyDetails? foundLobbyDetails; // 핸들 저장용으로 변경
    private bool searchFinished = false;

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

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            ShowErrorPopup("EOS 네트워크 시스템이 준비되지 않았습니다.");
            HideLoadingPanel();
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

        // 5초 타임아웃
        float timeout = 5f;
        while (!searchFinished && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // 방 발견 시 EOSLobby를 통해 공식 방 참가 실행
        if (foundLobbyDetails != null)
        {
            Debug.Log($"[ClientJoinUI] 코드 [{code}] 방 발견! EOSLobby를 통해 참가를 진행합니다.");

            EOSLobby eosLobby = FindFirstObjectByType<EOSLobby>();
            if (eosLobby == null && NetworkManager.singleton != null)
            {
                eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            }

            if (eosLobby != null)
            {
                // EOSLobby가 참가를 수행하면 ClientLobbyManager.OnJoinLobbySucceeded가 호출되어 Client 실행 및 씬 이동됨
                eosLobby.JoinLobby(foundLobbyDetails);
            }
            else
            {
                Debug.LogError("[ClientJoinUI] EOSLobby 인스턴스를 찾을 수 없습니다.");
                HideLoadingPanel();
                ShowErrorPopup("네트워크 매니저(EOSLobby)를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[ClientJoinUI] 코드 '{code}'에 해당하는 방을 찾을 수 없습니다.");
            HideLoadingPanel();
            ShowErrorPopup($"코드 [{code}] 방을 찾을 수 없습니다.\n코드를 다시 확인해 주세요.");
        }
    }

    private void OnLobbySearchCompleted(LobbySearchFindCallbackInfo data)
    {
        if (data.ResultCode == Result.Success && currentSearchHandle != null)
        {
            LobbySearchGetSearchResultCountOptions countOptions = new LobbySearchGetSearchResultCountOptions();
            uint count = currentSearchHandle.GetSearchResultCount(countOptions);

            if (count > 0)
            {
                LobbySearchCopySearchResultByIndexOptions copyOptions = new LobbySearchCopySearchResultByIndexOptions();
                copyOptions.LobbyIndex = 0;

                // 문자열 변환 없이 LobbyDetails 핸들을 직접 가져옴
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
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }
}