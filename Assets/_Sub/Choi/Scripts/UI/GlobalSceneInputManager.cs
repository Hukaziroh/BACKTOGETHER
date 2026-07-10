using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GlobalSceneInputManager : MonoBehaviour
{
    private static GlobalSceneInputManager _instance;
    private bool _isResettingFocus = false;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isResettingFocus = false;
        RefreshAllSelectables();
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // [핵심 수정] 포커스가 아예 없거나(null), 선택된 오브젝트가 비활성화(패널 끔) 되었다면 즉시 감지!
        if (selected == null || !selected.activeInHierarchy)
        {
            if (!_isResettingFocus)
            {
                StartCoroutine(ResetFocusDelayed());
            }
        }
    }

    private System.Collections.IEnumerator ResetFocusDelayed()
    {
        _isResettingFocus = true;

        // 유니티 내부 UI 구조가 정리될 때까지 안전하게 1프레임 대기
        yield return null;

        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            // 한 턴 쉬었는데도 여전히 유령 상태거나 null이면 현재 화면 기준으로 강제 재배치
            if (selected == null || !selected.activeInHierarchy)
            {
                RefreshAllSelectables();
            }
        }

        _isResettingFocus = false;
    }

    public void RefreshAllSelectables()
    {
        if (EventSystem.current == null) return;

        // 현재 화면에 '실제로 켜져 있는' UI 요소들만 수집
        Selectable[] activeSelectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (activeSelectables == null || activeSelectables.Length == 0) return;

        List<Selectable> validList = new List<Selectable>();
        foreach (var sel in activeSelectables)
        {
            if (sel != null && sel.interactable)
            {
                validList.Add(sel);
                ConfigureTrigger(sel.gameObject);
            }
        }

        if (validList.Count == 0) return;

        // Y축 좌표 기준 정렬 (화면상 가장 위에 있는 UI가 무조건 첫 타깃)
        // Hierarchy 창에 정렬된 순서대로 우선순위 지정 (같은 부모 안에서 위에 있는 놈이 우선)
        validList.Sort((a, b) => {
            if (a.transform.parent == b.transform.parent)
            {
                return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            }
            // 만약 서로 다른 패널에 속해 있다면, 화면 위쪽에 있는 패널을 우선시
            return b.transform.position.y.CompareTo(a.transform.position.y);
        });

        // 첫 번째 UI 강제 포커스
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(validList[0].gameObject);

        UpdateHighlights(activeSelectables);
    }

    private void ConfigureTrigger(GameObject obj)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        EventTrigger.Entry selectEntry = trigger.triggers.Find(e => e.eventID == EventTriggerType.Select);

        if (selectEntry == null)
        {
            selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            trigger.triggers.Add(selectEntry);
        }

        selectEntry.callback.RemoveAllListeners();
        selectEntry.callback.AddListener((data) =>
        {
            Selectable[] currentActives = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            UpdateHighlights(currentActives);
        });
    }

    private void UpdateHighlights(Selectable[] selectables)
    {
        if (EventSystem.current == null || selectables == null) return;

        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;

        foreach (var sel in selectables)
        {
            if (sel == null) continue;

            Transform highlight = sel.transform.Find("Highlight");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(sel.gameObject == selectedObj);
            }
        }
    }
}