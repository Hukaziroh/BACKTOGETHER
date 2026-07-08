using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
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

        Transform targetToFollow = null;

        // 1. 관전 모드 중인지 확인
        if (spectatorSystem != null && spectatorSystem.CurrentTarget != null)
        {
            targetToFollow = spectatorSystem.CurrentTarget;
        }
        // 2. 관전 모드가 아닐 경우, 본인의 캐릭터를 찾아 타겟으로 설정
        else
        {
            // "Player" 태그를 가진 오브젝트를 찾습니다. 
            // 본인의 캐릭터에 "Player" 태그가 붙어있는지 꼭 확인하세요!
            GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
            if (localPlayer != null)
            {
                targetToFollow = localPlayer.transform;
            }
        }

        // 3. 타겟이 결정되었다면 부드럽게 이동
        if (targetToFollow != null)
        {
            Vector3 targetPos = targetToFollow.position + (Vector3.up * yOffset);

            // 카메라의 Z축(깊이)은 그대로 유지
            targetPos.z = transform.position.z;

            // SmoothDamp로 부드럽게 이동
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
        }
    }
}