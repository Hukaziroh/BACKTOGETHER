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

    private float _currentSpeed;
    private float _textTimer = 0f;
    private float _speedRestoreTimer = 0f;
    private Camera _mainCamera;

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
}