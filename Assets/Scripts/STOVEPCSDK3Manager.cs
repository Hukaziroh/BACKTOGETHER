using System;
using System.Text;
using System.Collections;
using UnityEngine;
using static Stove.PCSDK.Base;

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
        // 🚨 핵심 방어 로직: STOVE_BUILD 심볼이 없으면 스토브를 즉시 파괴하고 꺼버림
#if !STOVE_BUILD
        Debug.LogWarning("⚠️ 현재 STOVE_BUILD가 아닙니다. 스토브 매니저를 비활성화합니다.");
        Destroy(gameObject);
        return;
#endif
        // ----------------------------------------------------
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
                isInitialized = true;
                Debug.Log("🎉 STOVE Base SDK 초기화 성공!");
            }
            else
            {
                Debug.LogError("STOVE Base SDK 초기화 실패");
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
}