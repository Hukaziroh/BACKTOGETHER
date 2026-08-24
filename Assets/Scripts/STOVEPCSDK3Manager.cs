using System;
using System.Text;
using System.Collections;
using UnityEngine;
using static Stove.PCSDK.Base;
using static Stove.PCSDK.GameSupport;

public class StovePCSDK3Manager : Singleton<StovePCSDK3Manager>
{
    [Header("[StovePCInitializeParam]")]
    [SerializeField] private string environment = "LIVE";
    [SerializeField] private string gameId;
    [SerializeField] private string applicationKey;

    [Header("[Running Setting]")]
    [SerializeField] private float runCallbackInterval = 1.0f;
    private bool isCallbackRunning = false;
    private Coroutine callbackCoroutine;

    [field: Space(10)]
    [field: SerializeField] public bool isInitialized { get; private set; } = false;

    private OnInitializeFinished onInitializeFinished;
    private readonly StringBuilder sb = new StringBuilder(60);

    protected override void Awake()
    {
#if !STOVE_BUILD
        Debug.LogWarning("⚠️ 현재 STOVE_BUILD가 아닙니다. 스토브 매니저를 비활성화합니다.");
        Destroy(gameObject);
        return;
#endif
        base.Awake();

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Start에서도 한번 더 막아줍니다.
#if STOVE_BUILD
        Initialize();
#endif
    }

    private void OnApplicationQuit()
    {
#if STOVE_BUILD
        UnInitialize();
#endif
    }

    private void Initialize()
    {
        StovePCInitializeParam initParam = new StovePCInitializeParam
        {
            environment = environment,
            gameId = gameId,
            applicationKey = applicationKey
        };

        StartRunCallbackLoop();

        Base_RestartAppIfNecessaryAsync(initParam, 10_000, (callbackResult, restartAppIfNecessary) =>
        {
            PrintCallbackResult(callbackResult);
            if (restartAppIfNecessary)
            {
                Debug.LogWarning("스토브 런처를 통해 게임을 실행해야 합니다. 앱을 종료합니다.");
                Application.Quit();
            }
            else
            {
                BaseSDKInitializeFinished();
                Base_Initialize(initParam, onInitializeFinished);
            }
        });
    }

    protected virtual void BaseSDKInitializeFinished()
    {
        onInitializeFinished = (callbackResult) =>
        {
            PrintCallbackResult(callbackResult);

            if (callbackResult.result.IsSuccessful())
            {
                Debug.Log("🎉 STOVE Base SDK 초기화 성공!");
                GameSupport_Initialize();
                Debug.Log("🏆 STOVE GameSupport(도전과제) 모듈 초기화 성공!");
                isInitialized = true;
            }
        };
    }

    private void UnInitialize()
    {
        StopRunCallbackLoop();
        Result result = Base_UnInitialize();
        PrintResult(result);
        isInitialized = false;
    }

    private void StartRunCallbackLoop()
    {
        if (!isCallbackRunning)
        {
            isCallbackRunning = true;
            callbackCoroutine = StartCoroutine(RunCallbackCoroutine());
        }
    }

    private void StopRunCallbackLoop()
    {
        isCallbackRunning = false;
        if (callbackCoroutine != null)
        {
            StopCoroutine(callbackCoroutine);
            callbackCoroutine = null;
        }
    }

    private IEnumerator RunCallbackCoroutine()
    {
        WaitForSeconds wfs = new WaitForSeconds(runCallbackInterval);
        while (isCallbackRunning)
        {
            Base_RunCallback();
            yield return wfs;
        }
    }

    private void PrintResult(Result result)
    {
        sb.Clear();
        sb.AppendLine("# Result");
        sb.AppendLine($" - Result.IsSuccessful : {result.IsSuccessful()}");
        sb.AppendLine($" - Result.sdkName : {result.sdkName}");
        sb.AppendLine($" - Result.methodCode : {result.methodCode}");
        sb.AppendLine($" - Result.resultCode : {result.resultCode}");
        sb.AppendLine($" - Result.exceptionMessage : {result.exceptionMessage}");
        Debug.Log(sb.ToString());
    }

    public void PrintCallbackResult(CallbackResult callbackResult)
    {
        sb.Clear();
        sb.AppendLine("# CallbackResult");
        sb.AppendLine($" - CallbackResult.IsSuccessful : {callbackResult.result.IsSuccessful()}");
        sb.AppendLine($" - CallbackResult.sdkName : {callbackResult.result.sdkName}");
        sb.AppendLine($" - CallbackResult.methodCode : {callbackResult.result.methodCode}");
        sb.AppendLine($" - CallbackResult.resultCode : {callbackResult.result.resultCode}");
        sb.AppendLine($" - CallbackResult.exceptionMessage : {callbackResult.result.exceptionMessage}");
        Debug.Log(sb.ToString());
    }

    public string GetAccessToken()
    {
        string token = default;
        uint strlen = 1024;
        Result result = Base_GetAccessToken(ref token, strlen);
        PrintResult(result);
        return result.IsSuccessful() ? token : null;
    }

    public (bool, StovePCUser) GetUser()
    {
        StovePCUser user = default;
        Result result = Base_GetUser(ref user);
        PrintResult(result);
        return result.IsSuccessful() ? (true, user) : (false, user);
    }

    public (bool, string) GetVersion()
    {
        string version = default;
        uint strlen = 256;
        Result result = Base_GetVersion(ref version, strlen);
        PrintResult(result);
        return result.IsSuccessful() ? (true, version) : (false, version);
    }

    /// <summary>
    /// 스토브 서버에 스탯을 1 증가시켜 업적을 해금합니다.
    /// </summary>
    public void UnlockAchievement(string achievementId)
    {
        if (!isInitialized) return;

        // 업적ID, 올릴수치, 콜백 순서로 바로 넘깁니다.
        GameSupport_ModifyStat(achievementId, 1, (callbackResult, stat) =>
        {
            if (callbackResult.result.IsSuccessful())
            {
                Debug.Log($"[STOVE] 업적 갱신 성공: {achievementId}");
            }
            else
            {
                // 👇 바로 이 부분! GetResultCode()를 지우고 resultCode 로 수정했습니다!
                Debug.LogError($"[STOVE] 업적 갱신 실패: {callbackResult.result.resultCode}");
            }
        });
    }
}