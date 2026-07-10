using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GlobalMenuManager : MonoBehaviour
{
    private Button[] _allButtons;

    private void Start()
    {
        // 1. 하위 오브젝트에 있는 모든 버튼(비활성화된 것 포함)을 한 번에 다 찾음
        _allButtons = GetComponentsInChildren<Button>(true);

        foreach (var btn in _allButtons)
        {
            AddHighlightTrigger(btn.gameObject);
        }

        // 2. 시작 시 첫 번째 버튼 강제 선택
        if (_allButtons.Length > 0)
        {
            EventSystem.current.SetSelectedGameObject(_allButtons[0].gameObject);
        }
    }

    private void OnEnable()
    {
        // 패널을 껐다 켤 때마다 다시 첫 번째 버튼을 선택해줌
        StartCoroutine(ResetSelection());
    }

    private System.Collections.IEnumerator ResetSelection()
    {
        yield return null;
        if (_allButtons != null && _allButtons.Length > 0)
            EventSystem.current.SetSelectedGameObject(_allButtons[0].gameObject);
    }

    // 모든 버튼에 이벤트 트리거를 자동으로 추가
    private void AddHighlightTrigger(GameObject obj)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        // 선택되었을 때, 모든 버튼을 검사해서 하이라이트 갱신
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        entry.callback.AddListener((data) => UpdateHighlights());
        trigger.triggers.Add(entry);
    }

    // [핵심 로직] 현재 선택된 녀석만 켜고, 나머지는 다 끔
    private void UpdateHighlights()
    {
        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;

        foreach (var btn in _allButtons)
        {
            Transform highlight = btn.transform.Find("Highlight");
            if (highlight != null)
            {
                // 현재 선택된 버튼이면 켜고, 아니면 끔
                highlight.gameObject.SetActive(btn.gameObject == selectedObj);
            }
        }
    }
}