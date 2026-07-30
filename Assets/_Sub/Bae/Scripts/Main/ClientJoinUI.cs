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
    private string foundHostAddress = "";
    private bool searchFinished = false;

    private void Start()
    {
        // 시작 시 로딩 및 에러 패널 비활성화
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

        // ★ 핵심 방어: sixDigitUI가 연결 안 되어 있으면 조용히 넘어가지 않고 화면/콘솔에 에러를 박아버립니다!
        if (sixDigitUI == null)
        {
            Debug.LogError("[ClientJoinUI] 인스펙터에 sixDigitUI (SixDigitCodeInputUI)가 할당되지 않았습니다!");
            ShowErrorPopup("코드 입력 UI(SixDigitUI) 연결이 누락되었습니다.\n인스펙터를 확인해 주세요.");
            return;
        }

        // 6자리 조합된 코드 가져오기
        string code = sixDigitUI.GetCode();
        Debug.Log($"[ClientJoinUI] 입력받은 6자리 코드: '{code}'");

        if (string.IsNullOrEmpty(code) || code.Length < 6)
        {
            ShowErrorPopup("6자리 코드를 정확히 입력해 주세요.");
            return;
        }

        // ★ 클릭 즉시 시각적 반응 제공 (로딩 화면 표시)
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
        foundHostAddress = "";

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

        // 검색 성공 시 Mirror 연결 실행
        if (!string.IsNullOrEmpty(foundHostAddress))
        {
            Debug.Log($"[ClientJoinUI] 방 검색 성공! 방장 주소: {foundHostAddress}");

            EosTransport transport = NetworkManager.singleton.transport as EosTransport;
            if (transport != null) transport.ResetIgnoreMessagesAtStartUpTimer();

            NetworkManager.singleton.networkAddress = foundHostAddress;
            NetworkManager.singleton.StartClient(); // 씬 이동 실행
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

                currentSearchHandle.CopySearchResultByIndex(copyOptions, out LobbyDetails lobbyDetails);

                LobbyDetailsCopyInfoOptions infoOptions = new LobbyDetailsCopyInfoOptions();
                lobbyDetails.CopyInfo(infoOptions, out var lobbyInfo);

                foundHostAddress = lobbyInfo?.LobbyOwnerUserId.ToString() ?? "";
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