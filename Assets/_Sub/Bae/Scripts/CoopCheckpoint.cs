using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopCheckpoint : NetworkBehaviour
{
    [Header("스폰될 위치 (빈 오브젝트)")]
    public Transform spawnLocation;

    [Header("체크포인트 순서")]
    [Tooltip("체크포인트의 진행 순서입니다. 앞 체크포인트보다 반드시 큰 숫자를 넣어주세요.")]
    public int checkpointIndex = 0;

    [Header("협동 모드 설정")]
    [Tooltip("체크 시 1명만 밟아도 4명 모두의 부활 위치가 여기로 찍힙니다. (챕터4 로프맵 전용)\n해제 시 밟은 사람 본인만 부활 위치가 바뀝니다.")]
    public bool syncToAllPlayers = false;

    [Header("깃발 비주얼")]
    public SpriteRenderer flagRenderer;
    public Sprite hangingFlagSprite;
    public Sprite raisedFlagSprite;

    [Header("클리어 이펙트 (기본 꺼둔 상태로 시작)")]
    public Transform clearFanfareTransform;
    public float fanfareDuration = 0.2f;

    // syncToAllPlayers(팀 공유) 모드에서만 쓰이는 전체 동기화 상태.
    // 개인 모드에서는 이 값을 안 쓰고, TargetRpc로 밟은 사람 화면에만 반영함.
    [SyncVar(hook = nameof(OnActivatedForAllChanged))]
    private bool isActivatedForAll = false;

    // 개인 모드에서 "이 사람은 이미 이 체크포인트를 밟았는지" 서버가 기억해두는 목록.
    // 없으면 트리거 존을 들락날락할 때마다 깃발/이펙트가 계속 다시 터짐.
    private readonly HashSet<uint> activatedByNetId = new HashSet<uint>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (clearFanfareTransform != null)
            clearFanfareTransform.gameObject.SetActive(false);

        if (syncToAllPlayers)
            UpdateFlagVisual(isActivatedForAll);
    }

    private void OnActivatedForAllChanged(bool oldValue, bool newValue)
    {
        UpdateFlagVisual(newValue);
        if (newValue)
            PlayFanfare();
    }

    private void UpdateFlagVisual(bool activated)
    {
        if (flagRenderer == null) return;
        flagRenderer.sprite = activated ? raisedFlagSprite : hangingFlagSprite;
    }

    private void PlayFanfare()
    {
        if (clearFanfareTransform == null) return;
        StopAllCoroutines();
        StartCoroutine(FanfareBlinkRoutine());
    }

    private IEnumerator FanfareBlinkRoutine()
    {
        clearFanfareTransform.gameObject.SetActive(true);

        ParticleSystem ps = clearFanfareTransform.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();

        yield return new WaitForSeconds(fanfareDuration);

        // SetActive(false) 대신 Stop()만 호출 — 새로 생기는 입자만 막고,
        // 이미 떠 있는 입자는 자기 수명/페이드 곡선대로 자연스럽게 사라지게 둠
        if (ps != null) ps.Stop();
    }

    [TargetRpc]
    private void TargetActivateFlag(NetworkConnectionToClient conn)
    {
        UpdateFlagVisual(true);
        PlayFanfare();
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerRespawn respawnScript =
            other.GetComponent<PlayerRespawn>();

        if (respawnScript == null)
            return;

        // =====================================================
        // 챕터4 : 한 명이 체크포인트를 찍으면 전원 갱신
        // =====================================================

        if (syncToAllPlayers)
        {
            if (!isActivatedForAll)
                isActivatedForAll = true;

            Debug.Log(
                $"[체크포인트] '{other.name}' 진입! " +
                $"체크포인트 Index = {checkpointIndex} | " +
                $"팀 전체 체크포인트 갱신 시도"
            );

            List<PlayerRespawn> allRespawns =
                CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();

            foreach (var respawn in allRespawns)
            {
                if (respawn != null)
                {
                    // [핵심 추가] 서버 쪽 데이터도 강제로 갱신하여 불일치 원천 차단
                    respawn.ServerUpdateSpawnPointIfNewer(spawnLocation.position, checkpointIndex);

                    respawn.RpcUpdateSpawnPointIfNewer(
                        spawnLocation.position,
                        checkpointIndex
                    );
                }
            }
        }

        // =====================================================
        // 일반 맵 : 체크포인트를 밟은 플레이어만 갱신
        // =====================================================

        else
        {
            Debug.Log(
                $"[체크포인트] '{other.name}' 진입! " +
                $"체크포인트 Index = {checkpointIndex} | " +
                $"개인 체크포인트 갱신 시도"
            );

            // [핵심 추가] 서버 쪽 데이터 갱신
            respawnScript.ServerUpdateSpawnPointIfNewer(spawnLocation.position, checkpointIndex);

            respawnScript.TargetUpdateSpawnPointIfNewer(
                respawnScript.connectionToClient,
                spawnLocation.position,
                checkpointIndex
            );

            NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
            if (identity != null && activatedByNetId.Add(identity.netId))
            {
                TargetActivateFlag(respawnScript.connectionToClient);
            }
        }
    }
}