#nullable enable
using System.Collections;
using UnityEngine;
using Mirror;
using EpicTransport;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;

public class ClientJoinUI : MonoBehaviour
{
    public SixDigitCodeInputUI? sixDigitUI;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject? loadingPanel;

    private LobbySearch? currentSearchHandle;
    private string foundHostAddress = "";
    private bool searchFinished = false;

    private void Start()
    {
        GameObject? panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);
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

    public void OnClick_ConnectByCode()
    {
        if (sixDigitUI == null) return;

        // ★ 주의: SixDigitCodeInputUI 스크립트에 문자열을 반환하는 함수 이름을 아래에 맞춰주세요!
        // (예: GetCode, GetEnteredCode, currentCode 등)
        string code = sixDigitUI.GetCode();
        if (string.IsNullOrEmpty(code) || code.Length < 6)
        {
            Debug.LogWarning("6자리 코드를 정확히 입력해주세요.");
            return;
        }

        StartCoroutine(SearchAndJoinRoutine(code));
    }

    private IEnumerator SearchAndJoinRoutine(string code)
    {
        GameObject? currentPanel = GetLoadingPanel();
        if (currentPanel != null) currentPanel.SetActive(true);

        searchFinished = false;
        foundHostAddress = "";

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();

        // ★ 수정됨: LobbyInterfaceCreateLobbySearchOptions -> CreateLobbySearchOptions
        CreateLobbySearchOptions searchOptions = new CreateLobbySearchOptions
        {
            MaxResults = 1
        };

        // ★ 수정됨: ref 키워드 삭제
        lobbyInterface.CreateLobbySearch(searchOptions, out currentSearchHandle);

        if (currentSearchHandle != null)
        {
            LobbySearchSetParameterOptions paramOptions = new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "SHORTCODE", Value = code }
            };

            // ★ 수정됨: ref 키워드 삭제
            currentSearchHandle.SetParameter(paramOptions);

            LobbySearchFindOptions findOptions = new LobbySearchFindOptions
            {
                LocalUserId = EOSSDKComponent.LocalUserProductId
            };

            // ★ 수정됨: ref 키워드 삭제
            currentSearchHandle.Find(findOptions, null, OnLobbySearchCompleted);
        }

        float timeout = 5f;
        while (!searchFinished && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!string.IsNullOrEmpty(foundHostAddress))
        {
            yield return new WaitForSecondsRealtime(0.2f);
            Debug.Log($"방 검색 성공! 접속합니다. (ID: {foundHostAddress})");
            NetworkManager.singleton.networkAddress = foundHostAddress;
            NetworkManager.singleton.StartClient();
        }
        else
        {
            Debug.LogError("방을 찾을 수 없거나 코드(대/소문자)를 다시 확인해주세요.");
            currentPanel = GetLoadingPanel();
            if (currentPanel != null) currentPanel.SetActive(false);
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
}