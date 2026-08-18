using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PlayerRespawn : NetworkBehaviour
{
    private PlayerController controller;
    private bool hasRequestedRespawn = false; // 🌟 R키를 계속 누르고 있어도 리스폰이 반복되지 않도록 방지하는 플래그

    [Header("스폰 시스템")]
    [SyncVar]
    public Vector3 currentSpawnPoint;

    [SyncVar]
    [Tooltip("현재 플레이어가 도달한 가장 뒤쪽 체크포인트 번호")]
    public int currentCheckpointIndex = -1;

    [SyncVar]
    private bool isRespawning = false;

    private float holdTimer = 0f;
    private const float HOLD_TIME_TO_RESPAWN = 2f;

    [Header("팀 리스폰 씬 설정")]
    public List<string> teamRespawnScenes = new List<string> { "chapter4" };

    [Header("리스폰 불가 씬 설정")]
    public List<string> disabledRespawnScenes = new List<string> { "chapter5", "chapter6", "Nchapter1", "Nchapter2", "Nchapter3", "Nchapter4", "Nchapter5", "Nchapter6" };

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartServer()
    {
        currentSpawnPoint = transform.position;
        currentCheckpointIndex = -1;
    }

    void Update()
    {
        if (!isLocalPlayer || isRespawning)
            return;

        // 🌟 [추가됨] 현재 씬이 리스폰 불가 씬이라면 R키 입력과 화면 페이드 연출을 완전히 무시
        string sceneName = SceneManager.GetActiveScene().name;
        if (disabledRespawnScenes != null && disabledRespawnScenes.Contains(sceneName))
            return;

        // 🌟 New Input System의 restartAction 상태 감지
        bool isHoldingRestart = controller.input != null &&
                                controller.input.restartAction != null &&
                                controller.input.restartAction.IsPressed();

        if (isHoldingRestart)
        {
            // 이미 리스폰을 요청한 상태라면 R키를 계속 누르고 있어도 무시 (반복 리스폰 방지)
            if (hasRequestedRespawn)
                return;

            holdTimer += Time.deltaTime;

            // 🌟 R키를 누르고 있는 동안 누적 시간에 비례하여 화면을 점진적으로 검게 만듦 (Fade Out)
            if (ScreenFader.Instance != null && ScreenFader.Instance.fadeCanvasGroup != null)
            {
                ScreenFader.Instance.fadeCanvasGroup.gameObject.SetActive(true);
                ScreenFader.Instance.fadeCanvasGroup.alpha = Mathf.Clamp01(holdTimer / HOLD_TIME_TO_RESPAWN);
            }

            // 🌟 완전히 검은색이 되는 시간(2초)에 도달하면 리스폰 요청
            if (holdTimer >= HOLD_TIME_TO_RESPAWN)
            {
                holdTimer = HOLD_TIME_TO_RESPAWN;
                hasRequestedRespawn = true; // 중복 요청 잠금
                CmdRequestRespawn();
            }
        }
        else
        {
            // 🌟 R키를 떼면 다음 번에 다시 누를 수 있도록 플래그 해제 및 페이드 원복
            hasRequestedRespawn = false;

            if (holdTimer > 0f)
            {
                StartCoroutine(CancelFadeRoutine());
            }
            holdTimer = 0f;
        }
    }

    private IEnumerator CancelFadeRoutine()
    {
        float startAlpha = ScreenFader.Instance != null && ScreenFader.Instance.fadeCanvasGroup != null ? ScreenFader.Instance.fadeCanvasGroup.alpha : 0f;
        float timer = 0f;
        float duration = 0.3f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
            if (ScreenFader.Instance != null && ScreenFader.Instance.fadeCanvasGroup != null)
            {
                ScreenFader.Instance.fadeCanvasGroup.alpha = alpha;
                if (alpha <= 0.01f)
                {
                    ScreenFader.Instance.fadeCanvasGroup.gameObject.SetActive(false);
                }
            }
            yield return null;
        }
        if (ScreenFader.Instance != null && ScreenFader.Instance.fadeCanvasGroup != null)
        {
            ScreenFader.Instance.fadeCanvasGroup.alpha = 0f;
            ScreenFader.Instance.fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    [Command]
    public void CmdRequestRespawn()
    {
        if (isRespawning) return;

        PlayerCombineHandler combineHandler = GetComponent<PlayerCombineHandler>();

        if (combineHandler != null && combineHandler.isCombined)
        {
            StartCoroutine(RespawnCombinedPlayersRoutine());
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        if (disabledRespawnScenes.Contains(sceneName))
            return;

        if (teamRespawnScenes.Contains(sceneName))
        {
            StartCoroutine(RespawnAllPlayersRoutine());
        }
        else
        {
            StartCoroutine(RespawnSinglePlayerRoutine());
        }
    }

    private IEnumerator RespawnCombinedPlayersRoutine()
    {
        PlayerCombineHandler combineHandler = GetComponent<PlayerCombineHandler>();
        if (combineHandler == null) yield break;

        PlayerCombineHandler bodyHandler = combineHandler;
        if (combineHandler.bodyTarget != null)
        {
            PlayerCombineHandler targetBody = combineHandler.bodyTarget.GetComponent<PlayerCombineHandler>();
            if (targetBody != null) bodyHandler = targetBody;
        }

        if (bodyHandler == null) yield break;

        PlayerRespawn bodyRespawn = bodyHandler.GetComponent<PlayerRespawn>();
        if (bodyRespawn == null) yield break;

        // 🌟 합체된 본체와 연관된 모든 파츠/고스트 플레이어들을 정밀 탐색하여 리스트업
        List<PlayerRespawn> combinedPlayers = new List<PlayerRespawn>();
        PlayerRespawn[] allPlayers = FindObjectsByType<PlayerRespawn>(FindObjectsInactive.Exclude);

        foreach (var p in allPlayers)
        {
            if (p == null) continue;
            PlayerCombineHandler combine = p.GetComponent<PlayerCombineHandler>();
            if (combine != null && combine.isCombined)
            {
                if (combine.bodyTarget == bodyHandler.gameObject || p.gameObject == bodyHandler.gameObject)
                {
                    if (!combinedPlayers.Contains(p))
                    {
                        combinedPlayers.Add(p);
                    }
                }
            }
        }

        if (!combinedPlayers.Contains(bodyRespawn))
        {
            combinedPlayers.Add(bodyRespawn);
        }

        // 🌟 1. 연관된 모든 플레이어 화면 페이드 아웃 및 리스폰 상태 잠금
        foreach (var p in combinedPlayers)
        {
            if (p != null)
            {
                p.isRespawning = true;
                if (p.connectionToClient != null)
                {
                    p.TargetRpcPlayFadeOut();
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        Vector3 respawnPosition = bodyRespawn.currentSpawnPoint;

        // 🌟 2. 연관된 모든 플레이어 넉백 리셋 및 물리 비활성화
        foreach (var p in combinedPlayers)
        {
            if (p != null)
            {
                PlayerKnockback knockback = p.GetComponent<PlayerKnockback>();
                if (knockback != null) knockback.ResetKnockback();

                PlayerController pController = p.GetComponent<PlayerController>();
                if (pController != null && pController.rb != null)
                {
                    pController.rb.linearVelocity = Vector2.zero;
                    pController.rb.simulated = false;
                }
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 3. 위치 이동
        bodyRespawn.transform.position = respawnPosition;
        Physics2D.SyncTransforms();

        // 🌟 4. 물리 활성화 및 속도 초기화
        foreach (var p in combinedPlayers)
        {
            if (p != null)
            {
                PlayerController pController = p.GetComponent<PlayerController>();
                if (pController != null && pController.rb != null)
                {
                    pController.rb.simulated = true;
                    pController.rb.linearVelocity = Vector2.zero;
                }
            }
        }

        yield return new WaitForSeconds(0.1f);

        // 🌟 5. 연관된 모든 플레이어의 화면을 밝게 함 (Fade In) 및 리스폰 상태 해제
        foreach (var p in combinedPlayers)
        {
            if (p != null)
            {
                p.TargetRpcPlayFadeIn();
                p.isRespawning = false;
            }
        }
    }

    private IEnumerator RespawnSinglePlayerRoutine()
    {
        isRespawning = true;

        PlayerKnockback knockback = GetComponent<PlayerKnockback>();
        if (knockback != null) knockback.ResetKnockback();

        if (controller != null && controller.rb != null)
        {
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.simulated = false;
        }

        yield return new WaitForSeconds(0.2f);

        transform.position = currentSpawnPoint;
        Physics2D.SyncTransforms();

        if (controller != null && controller.rb != null)
        {
            controller.rb.simulated = true;
        }

        yield return new WaitForSeconds(0.1f);

        // 🌟 리스폰 및 물리 동기화 완료 후 화면을 밝게 함 (Fade In)
        TargetRpcPlayFadeIn();

        isRespawning = false;
    }

    private IEnumerator RespawnAllPlayersRoutine()
    {
        PlayerRespawn[] allPlayers = FindObjectsByType<PlayerRespawn>(FindObjectsInactive.Exclude);

        // 🌟 팀원 전원의 화면을 확실하게 페이드 아웃 시켜 깜빡임 방지
        foreach (var p in allPlayers)
        {
            if (p != null && p.connectionToClient != null)
            {
                p.TargetRpcPlayFadeOut();
            }
        }

        yield return new WaitForSeconds(0.3f);

        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                p.isRespawning = true;

                PlayerKnockback pKnockback = p.GetComponent<PlayerKnockback>();
                if (pKnockback != null) pKnockback.ResetKnockback();

                PlayerController pController = p.GetComponent<PlayerController>();
                if (pController != null && pController.rb != null)
                {
                    pController.rb.linearVelocity = Vector2.zero;
                    pController.rb.simulated = false;
                }
            }
        }

        yield return new WaitForSeconds(0.2f);

        Vector3 centerSpawnPoint = currentSpawnPoint;

        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                int playerIndex = 0;
                CoopPlayerIdentity identity = p.GetComponent<CoopPlayerIdentity>();
                if (identity != null)
                {
                    playerIndex = identity.playerIndex;
                }
                float[] spawnOffsets = { -1.5f, -0.5f, 0.5f, 1.5f };
                float myOffset = spawnOffsets[playerIndex % 4];

                Vector3 finalSpawnPos = centerSpawnPoint + new Vector3(myOffset, 0, 0);

                p.transform.position = finalSpawnPos;
                Physics2D.SyncTransforms();

                PlayerController pController = p.GetComponent<PlayerController>();
                if (pController != null && pController.rb != null)
                {
                    pController.rb.simulated = true;
                }
            }
        }

        yield return new WaitForSeconds(0.1f);

        // 🌟 팀원 전체 리스폰 및 위치 정렬 완료 후 화면을 밝게 함 (Fade In)
        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                p.TargetRpcPlayFadeIn();
                p.isRespawning = false;
            }
        }
    }

    [Server]
    public void UpdateCheckpoint(Vector3 newPos, int newIndex)
    {
        if (newIndex > currentCheckpointIndex)
        {
            currentCheckpointIndex = newIndex;
            currentSpawnPoint = newPos;
        }
    }

    [TargetRpc]
    public void TargetRpcPlayFadeOut()
    {
        if (ScreenFader.Instance != null && ScreenFader.Instance.fadeCanvasGroup != null)
        {
            ScreenFader.Instance.fadeCanvasGroup.gameObject.SetActive(true);
            ScreenFader.Instance.fadeCanvasGroup.alpha = 1f;
        }
    }

    [TargetRpc]
    public void TargetRpcPlayFadeIn()
    {
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeIn(0.5f);
        }
    }
}