using UnityEngine;
using UnityEngine.UI;

public class ButtonLockTrigger : MonoBehaviour
{
    void Start()
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
}