using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonLockTrigger : MonoBehaviour
{
    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            // 기존 등록 리스너와 겹치지 않게 깔끔하게 동적 추가
            btn.onClick.AddListener(() =>
            {
                if (GlobalSceneInputManager.Instance != null)
                {
                    // 클릭/엔터 시 매니저에게 직접 잠금 요청
                    GlobalSceneInputManager.Instance.LockUI(gameObject);
                }
            });
        }
    }

    private void OnEnable()
    {
        // 패널이 켜지거나 오브젝트가 활성화될 때 코루틴으로 포커스 강제 획득
        StartCoroutine(AutoSelectRoutine());
    }

    private IEnumerator AutoSelectRoutine()
    {
        // 유니티 내부에서 컴포넌트 활성화 상태를 완전히 반영할 수 있도록 1프레임 대기
        yield return null;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}