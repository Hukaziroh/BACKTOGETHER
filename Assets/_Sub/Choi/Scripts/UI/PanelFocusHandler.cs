using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PanelFocusHandler : MonoBehaviour
{
    // 패널이 켜질 때 자동으로 선택할 버튼 (비워두면 첫 번째 버튼을 자동 탐색함)
    [SerializeField] private GameObject defaultButton;

    private void OnEnable()
    {
        // 1. 패널이 켜지는 즉시 포커스 설정
        StartCoroutine(SetFocusDelayed());
    }

    private System.Collections.IEnumerator SetFocusDelayed()
    {
        // 2. 한 프레임만 기다림 (패널이 활성화되는 즉시 선택하면 꼬일 수 있어서 한 텀 쉬어줌)
        yield return null;

        GameObject target = defaultButton;

        // 3. 만약 defaultButton을 안 정했으면, 패널 자식 중 첫 번째 버튼을 자동 탐색
        if (target == null)
        {
            Button firstBtn = GetComponentInChildren<Button>();
            if (firstBtn != null) target = firstBtn.gameObject;
        }

        // 4. 선택!
        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}