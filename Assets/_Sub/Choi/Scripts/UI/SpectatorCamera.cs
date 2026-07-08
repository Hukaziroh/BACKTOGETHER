using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    [SerializeField] private float smoothTime = 1000f;
    [SerializeField] private float yOffset = 2.0f;    // 캐릭터 머리 위로 띄움
    private Vector3 currentVelocity = Vector3.zero;
    private SpectatorSystem spectatorSystem;

    void LateUpdate()
    {
        if (spectatorSystem == null) spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();

        if (spectatorSystem != null && spectatorSystem.CurrentTarget != null)
        {
            // 💡 머리 위로 2유닛만큼 offset 추가
            Vector3 targetPos = spectatorSystem.CurrentTarget.position + Vector3.up * yOffset;
            targetPos.z = transform.position.z; // 카메라 Z축은 유지

            // Lerp 대신 SmoothDamp를 쓰면 떨림이 거의 사라집니다.
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
        }
    }
}