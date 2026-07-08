using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    // 💡 1000f가 아닌, 0.2f 정도의 값을 사용하세요.
    // 값이 작을수록 빠르게 따라가고, 클수록 천천히 따라갑니다.
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float yOffset = 2.0f;    // 캐릭터 머리 위 높이

    private Vector3 currentVelocity = Vector3.zero;
    private SpectatorSystem spectatorSystem;

    void LateUpdate()
    {
        // 씬 전환 시 시스템을 다시 찾습니다.
        if (spectatorSystem == null)
        {
            spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();
        }

        // 타겟이 유효한지 확인 후 이동
        if (spectatorSystem != null && spectatorSystem.CurrentTarget != null)
        {
            // 타겟 위치 + 오프셋
            Vector3 targetPos = spectatorSystem.CurrentTarget.position + (Vector3.up * yOffset);

            // 카메라의 Z축(깊이)은 그대로 유지
            targetPos.z = transform.position.z;

            // SmoothDamp로 부드럽게 이동
            // velocity는 내부 계산용이므로 건드릴 필요 없습니다.
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
        }
    }
}