using System.Collections;
using UnityEngine;
using TMPro;

public class TilemapPlayerBouncer : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Vector2 _lastVelocity; // 부딪히기 직전의 속도를 기억할 변수

    [Header("=== 속도 및 회전 설정 ===")]
    [SerializeField] private float moveSpeed = 5f;            // 기본 비행 속도
    [SerializeField] private float critSpeed = 15f;           // 크리티컬 속도
    [SerializeField] private float rotationSpeed = 500f;      // 회전 속도
    [Range(0f, 1f)]
    [SerializeField] private float criticalChance = 0.25f;    // 크리티컬 확률 (25%)

    [Header("=== UI 연출 ===")]
    [SerializeField] private TextMeshProUGUI criticalText;    // UI 크리티컬 텍스트
    [SerializeField] private float textDuration = 0.4f;
    [SerializeField] private float yOffset = 1.2f;            // 캐릭터 머리 위 높이
    [SerializeField] private GameObject clearParticlePrefab;
    [SerializeField] private AudioClip secretCommandSuccessMusic;
    [Range(0f, 1f)]
    [SerializeField] private float secretCommandSuccessMusicVolume = 1f;

    private float _currentSpeed;
    private float _textTimer = 0f;
    private float _speedRestoreTimer = 0f;
    private Camera _mainCamera;
    private Coroutine _secretCommandEffectCoroutine;
    private bool _isSecretCommandEffectPlaying;

    private const float SecretCommandHorizontalTargetX = 3.2f;
    private const float SecretCommandVerticalTargetY = 1.4f;

    private static readonly Vector2[] SecretCommandDirections =
    {
        Vector2.up,
        Vector2.up,
        Vector2.down,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        Vector2.left,
        Vector2.right
    };

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;
        _currentSpeed = moveSpeed;

        // 시작 시 대각선 무작위 방향 발사
        float randomAngle = Random.Range(0, 4) * 90f + 45f;
        float rad = randomAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        _rb.linearVelocity = dir * _currentSpeed;
        if (criticalText != null) criticalText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 1. 매 프레임 무한 회전
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // 2. 크리티컬 감속 처리
        if (_currentSpeed > moveSpeed)
        {
            _speedRestoreTimer -= Time.deltaTime;
            if (_speedRestoreTimer <= 0f)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, moveSpeed, Time.deltaTime * 15f);
            }
        }

        // 3. 크리티컬 텍스트 머리 위 실시간 추적
        if (criticalText != null && criticalText.gameObject.activeSelf)
        {
            _textTimer -= Time.deltaTime;
            if (_textTimer <= 0f)
            {
                criticalText.gameObject.SetActive(false);
            }
            else
            {
                Vector3 worldTargetPos = transform.position + new Vector3(0f, yOffset, 0f);
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldTargetPos);
                criticalText.rectTransform.position = screenPos;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_isSecretCommandEffectPlaying) return;

        // 항상 의도한 현재 속도를 유지하도록 보정
        if (_rb.linearVelocity != Vector2.zero)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * _currentSpeed;
        }

        // [핵심 차단] 부딪히기 직전 프레임의 속도 방향을 안전하게 저장해 둡니다.
        _lastVelocity = _rb.linearVelocity;
    }

    // 타일맵(가시)에 부딪히는 순간 물리 반사각 계산
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isSecretCommandEffectPlaying) return;

        if (collision.gameObject.name.Contains("Tilemap"))
        {
            // 1. 부딪힌 가시 벽면의 방향(법선 벡터)을 가져옴
            Vector2 wallNormal = collision.contacts[0].normal;

            // 2. 들어오던 방향과 벽면 방향을 계산해 완벽한 당구공 반사각을 구함
            Vector2 reflectDir = Vector2.Reflect(_lastVelocity.normalized, wallNormal).normalized;

            // 3. 크리티컬 주사위 굴리기
            if (Random.value <= criticalChance)
            {
                TriggerCriticalBounce(reflectDir);
            }
            else
            {
                // 일반 반사: 반사된 방향으로 원래 속도 지정
                _rb.linearVelocity = reflectDir * _currentSpeed;
            }
        }
    }

    private void TriggerCriticalBounce(Vector2 reflectDir)
    {
        _currentSpeed = critSpeed;
        _speedRestoreTimer = 0.5f;

        // 크리티컬이 터지면 반사된 방향으로 초고속 비행 시킴
        _rb.linearVelocity = reflectDir * _currentSpeed;

        if (criticalText != null)
        {
            criticalText.gameObject.SetActive(true);
            _textTimer = textDuration;
        }
    }

    public static void PlaySecretCommandSuccessEffect()
    {
        TilemapPlayerBouncer[] bouncers =
            FindObjectsByType<TilemapPlayerBouncer>(FindObjectsInactive.Exclude);

        if (bouncers == null || bouncers.Length == 0) return;

        bouncers[0].StartSecretCommandEffect(bouncers);
    }

    private void StartSecretCommandEffect(TilemapPlayerBouncer[] bouncers)
    {
        if (_secretCommandEffectCoroutine != null)
        {
            StopCoroutine(_secretCommandEffectCoroutine);
        }

        _secretCommandEffectCoroutine = StartCoroutine(SecretCommandSuccessRoutine(bouncers));
    }

    private IEnumerator SecretCommandSuccessRoutine(TilemapPlayerBouncer[] bouncers)
    {
        float spacing = 1.1f;
        float startX = -spacing * (bouncers.Length - 1) * 0.5f;
        Vector3[] alignStartPositions = new Vector3[bouncers.Length];
        Vector3[] linePositions = new Vector3[bouncers.Length];

        for (int i = 0; i < bouncers.Length; i++)
        {
            TilemapPlayerBouncer bouncer = bouncers[i];
            if (bouncer == null || bouncer._rb == null) continue;

            alignStartPositions[i] = bouncer.transform.position;
            linePositions[i] = new Vector3(startX + spacing * i, 0f, 0f);
            bouncer._isSecretCommandEffectPlaying = true;
            bouncer._currentSpeed = bouncer.moveSpeed;
            bouncer._rb.linearVelocity = Vector2.zero;
            bouncer.ShowSecretCommandText();
        }

        yield return MoveBouncersToPositions(bouncers, alignStartPositions, linePositions, 0.8f);
        yield return new WaitForSecondsRealtime(0.45f);
        PlaySecretCommandSuccessMusic();

        for (int i = 0; i < SecretCommandDirections.Length; i++)
        {
            Vector2 direction = SecretCommandDirections[i];
            Vector3[] stepStartPositions = CaptureBouncerPositions(bouncers);
            Vector3[] stepEndPositions = new Vector3[bouncers.Length];

            for (int j = 0; j < bouncers.Length; j++)
            {
                TilemapPlayerBouncer bouncer = bouncers[j];
                if (bouncer == null) continue;

                if (direction == Vector2.left)
                {
                    stepEndPositions[j] = new Vector3(
                        -SecretCommandHorizontalTargetX + bouncer.GetLineOffsetX(j, bouncers.Length),
                        0f,
                        0f);
                }
                else if (direction == Vector2.right)
                {
                    stepEndPositions[j] = new Vector3(
                        SecretCommandHorizontalTargetX + bouncer.GetLineOffsetX(j, bouncers.Length),
                        0f,
                        0f);
                }
                else
                {
                    stepEndPositions[j] = new Vector3(
                        bouncer.GetLineOffsetX(j, bouncers.Length),
                        direction.y * SecretCommandVerticalTargetY,
                        0f);
                }
            }

            if (direction == Vector2.up || direction == Vector2.down)
            {
                yield return MoveBouncersToPositions(bouncers, linePositions, stepEndPositions, 0.22f);
                yield return MoveBouncersToPositions(bouncers, stepEndPositions, linePositions, 0.22f);
            }
            else
            {
                yield return MoveBouncersToPositions(bouncers, stepStartPositions, stepEndPositions, 0.32f);
            }

            foreach (TilemapPlayerBouncer bouncer in bouncers)
            {
                if (bouncer == null || bouncer._rb == null) continue;

                bouncer._rb.linearVelocity = Vector2.zero;
            }

            yield return new WaitForSecondsRealtime(0.06f);
        }

        Vector3[] returnStartPositions = CaptureBouncerPositions(bouncers);
        yield return MoveBouncersToPositions(bouncers, returnStartPositions, linePositions, 0.35f);

        foreach (TilemapPlayerBouncer bouncer in bouncers)
        {
            if (bouncer == null || bouncer._rb == null) continue;

            bouncer.PlayClearParticle();
            bouncer._rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSecondsRealtime(1f);

        foreach (TilemapPlayerBouncer bouncer in bouncers)
        {
            if (bouncer == null || bouncer._rb == null) continue;

            bouncer._isSecretCommandEffectPlaying = false;
            bouncer._currentSpeed = bouncer.critSpeed;
            bouncer._speedRestoreTimer = 1.5f;
            bouncer._rb.linearVelocity = bouncer.GetRandomLaunchDirection() * bouncer.critSpeed;
        }

        _secretCommandEffectCoroutine = null;
    }

    private void PlaySecretCommandSuccessMusic()
    {
        if (secretCommandSuccessMusic == null) return;

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = secretCommandSuccessMusicVolume;
        audioSource.clip = secretCommandSuccessMusic;
        audioSource.Play();
    }

    private float GetLineOffsetX(int index, int count)
    {
        float spacing = 1.1f;
        return -spacing * (count - 1) * 0.5f + spacing * index;
    }

    private void PlayClearParticle()
    {
        if (clearParticlePrefab == null) return;

        Vector3 spawnPosition = transform.position + new Vector3(0f, yOffset, 0f);
        GameObject particleObject = Instantiate(clearParticlePrefab, spawnPosition, Quaternion.identity);
        Destroy(particleObject, 1f);
    }

    private Vector2 GetRandomLaunchDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        float rad = randomAngle * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private IEnumerator MoveBouncersToPositions(
        TilemapPlayerBouncer[] bouncers,
        Vector3[] startPositions,
        Vector3[] endPositions,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < bouncers.Length; i++)
            {
                TilemapPlayerBouncer bouncer = bouncers[i];
                if (bouncer == null || bouncer._rb == null) continue;

                bouncer._rb.linearVelocity = Vector2.zero;
                bouncer.transform.position = Vector3.Lerp(startPositions[i], endPositions[i], easedT);
            }

            yield return null;
        }
    }

    private Vector3[] CaptureBouncerPositions(TilemapPlayerBouncer[] bouncers)
    {
        Vector3[] positions = new Vector3[bouncers.Length];

        for (int i = 0; i < bouncers.Length; i++)
        {
            positions[i] = bouncers[i] != null ? bouncers[i].transform.position : Vector3.zero;
        }

        return positions;
    }

    private void ShowSecretCommandText()
    {
        if (criticalText == null) return;

        criticalText.text = "EX OPEN!";
        criticalText.gameObject.SetActive(true);
        _textTimer = 5f;
    }
}
