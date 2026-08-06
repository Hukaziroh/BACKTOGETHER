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

    [Header("팡파레 사운드")]
    public PlayerSoundLibrary soundLibrary;
    [Range(0f, 1f)] public float fanfareVolume = 0.7f;
    public float soundMinDistance = 9f;
    public float soundMaxDistance = 40f;

    [SyncVar(hook = nameof(OnActivatedForAllChanged))]
    private bool isActivatedForAll = false;

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
        PlayFanfareSound();

        if (clearFanfareTransform == null) return;
        StopAllCoroutines();
        StartCoroutine(FanfareBlinkRoutine());
    }

    private void PlayFanfareSound()
    {
        AudioClip clip = soundLibrary != null ? soundLibrary.checkpointFanfare : null;
        PlayerSoundUtility.PlayPositional(transform.position, clip, fanfareVolume, soundMinDistance, soundMaxDistance);
    }

    private IEnumerator FanfareBlinkRoutine()
    {
        clearFanfareTransform.gameObject.SetActive(true);

        ParticleSystem ps = clearFanfareTransform.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();

        yield return new WaitForSeconds(fanfareDuration);

        if (ps != null) ps.Stop();
    }

    [TargetRpc]
    private void TargetActivateFlag(NetworkConnectionToClient conn)
    {
        UpdateFlagVisual(true);
        PlayFanfare();
    }

    // 이미 ServerCallback이 붙어있으므로 서버에서만 실행됩니다! 아주 훌륭합니다.
    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerRespawn respawnScript = other.GetComponent<PlayerRespawn>();

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
                $"체크포인트 Index = {checkpointIndex} | 팀 전체 체크포인트 갱신 시도"
            );

            List<PlayerRespawn> allRespawns = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();

            foreach (var respawn in allRespawns)
            {
                if (respawn != null)
                {
                    // 🌟 [수정됨] 복잡하게 나뉘어 있던 서버/클라 동기화 호출을 하나로 통합!
                    // SyncVar 구조 덕분에 이 한 줄만 실행하면 알아서 클라이언트까지 저장됩니다.
                    respawn.UpdateCheckpoint(spawnLocation.position, checkpointIndex);
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
                $"체크포인트 Index = {checkpointIndex} | 개인 체크포인트 갱신 시도"
            );

            // 🌟 [수정됨] 마찬가지로 하나로 통합!
            respawnScript.UpdateCheckpoint(spawnLocation.position, checkpointIndex);

            NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
            if (identity != null && activatedByNetId.Add(identity.netId))
            {
                // UI(시각적) 연출은 해당 클라이언트에게만 쏴줘야 하므로 TargetRpc 유지 (완벽함)
                TargetActivateFlag(respawnScript.connectionToClient);
            }
        }
    }
}