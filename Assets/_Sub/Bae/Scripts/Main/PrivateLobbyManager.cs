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

public partial class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject logo;
    int a = 0;
    [Header("메인 & 실행 버튼 연결")]
    [SerializeField] private Button mainHostButton;
    [SerializeField] private Button makeRoomButton;

    [Header("UI 연결 - 방 설정 (패널 내부)")]
    [SerializeField] private TMP_InputField roomNameInputField;

    [Header("기본 방 이름 목록 (미입력 시 랜덤 선택)")]
    [SerializeField]
    private string[] defaultRoomNames = new string[]
    {
        "Better Together!",
        "Don't Step on My Head",
        "Teamwork Makes the Dream Work",
        "Back Together: Assemble!",
        "Chaos Incoming...",
        "One Mind, Four Bodies"
    };

    [Header("챕터 선택 UI (< Chapter 1 >)")]
    [SerializeField] private GameObject chapterSelectObject;
    [SerializeField] private TextMeshProUGUI chapterDisplayText;
    [SerializeField] private Button prevChapterButton;
    [SerializeField] private Button nextChapterButton;
    [SerializeField] private Image chapterPreviewImage;
    [SerializeField] private Sprite[] chapterSprites;
    [SerializeField] public string[] chapterNames;

    [Header("챕터 잠금 UI")]
    [SerializeField] private GameObject chapterLockObject;

    [Header("방 타입 선택 UI (< Public / Private >)")]
    [SerializeField] private GameObject roomTypeSelectObject;
    [SerializeField] private TextMeshProUGUI roomTypeDisplayText;
    [SerializeField] private Button prevRoomTypeButton;
    [SerializeField] private Button nextRoomTypeButton;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("에러 팝업 UI 연결")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button errorCloseButton;

    [Header("씬 설정")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainSceneName = "Main";

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    public static string currentShortCode = "";
    public static string lastCreatedRoomName = "";
    public static int selectedChapter = 1;
    private int selectedChapterIndex = 1;
    private int maxChapterCount => chapterNames != null && chapterNames.Length > 0 ? chapterNames.Length : 6;
    private bool isPublicRoom = true;

    private bool isCreatingLobby = false;
    private bool attributeUpdateDone = false;
    private bool attributeUpdateFailed = false;
    private bool createLobbySuccess = false;
    private bool createWithoutCheckpoints = false;

    private GameObject currentPanel;
}
