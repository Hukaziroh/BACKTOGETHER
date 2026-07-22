using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GlobalSceneInputManager : MonoBehaviour
{
    private static GlobalSceneInputManager _instance;
    public static GlobalSceneInputManager Instance => _instance;

    private bool _isResettingFocus = false;

    // 잠금 상태 관리 변수
    private bool _isLocked = false;
    private GameObject _lockedObject = null;

    // 특정 팝업창 내부로만 포커스를 격리하기 위한 변수들
    private GameObject _currentFocusScope = null;
    private List<Selectable> _temporarilyDisabled = new List<Selectable>();

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
        _isLocked = false;
        _lockedObject = null;
        _isResettingFocus = false;

        // 씬 전환 시 범위 제한 초기화
        _currentFocusScope = null;
        _temporarilyDisabled.Clear();

        RefreshAllSelectables();
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        // ★ [추가] 이모지 메뉴가 열려있는 동안에는 전역 포커스 리셋 로직이 간섭하지 않도록 차단
        if (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen())
        {
            return;
        }

        if (_isLocked)
        {
            if (_lockedObject == null)
            {
                UnlockUI();
                return;
            }

            if (EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

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
        yield return null;

        if (EventSystem.current != null)
        {
            // 리셋 대기 중에도 이모지 메뉴가 열렸다면 취소
            if (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen())
            {
                _isResettingFocus = false;
                yield break;
            }

            GameObject selected = EventSystem.current.currentSelectedGameObject;
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

        Selectable[] activeSelectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);

        if (activeSelectables == null || activeSelectables.Length == 0) return;

        List<Selectable> validList = new List<Selectable>();
        foreach (var sel in activeSelectables)
        {
            if (sel != null && sel.interactable)
            {
                // 포커스 범위가 지정되어 있다면 해당 범위 내의 버튼만 유효 목록에 포함
                if (_currentFocusScope != null && !sel.transform.IsChildOf(_currentFocusScope.transform))
                {
                    continue;
                }

                validList.Add(sel);
                ConfigureTrigger(sel.gameObject);
            }
        }

        if (validList.Count == 0) return;

        // 스마트 정렬 시스템
        validList.Sort((a, b) => {
            Canvas canvasA = a.GetComponentInParent<Canvas>();
            Canvas canvasB = b.GetComponentInParent<Canvas>();
            int orderA = canvasA != null ? canvasA.sortingOrder : 0;
            int orderB = canvasB != null ? canvasB.sortingOrder : 0;

            if (orderA != orderB)
            {
                return orderB.CompareTo(orderA);
            }

            if (a.transform.parent != b.transform.parent)
            {
                Transform rootA = GetTopLevelPanel(a.transform, canvasA?.transform);
                Transform rootB = GetTopLevelPanel(b.transform, canvasB?.transform);
                if (rootA != null && rootB != null && rootA != rootB)
                {
                    return rootB.GetSiblingIndex().CompareTo(rootA.GetSiblingIndex());
                }
            }

            if (a.transform.parent == b.transform.parent)
            {
                return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            }

            return b.transform.position.y.CompareTo(a.transform.position.y);
        });

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(validList[0].gameObject);

        UpdateHighlights(activeSelectables);
    }

    private Transform GetTopLevelPanel(Transform child, Transform limit)
    {
        if (child == null) return null;
        Transform current = child;
        while (current.parent != null && current.parent != limit)
        {
            current = current.parent;
        }
        return current;
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
            Selectable[] currentActives = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);
            UpdateHighlights(currentActives);
        });
    }

    private void UpdateHighlights(Selectable[] selectables)
    {
        if (EventSystem.current == null || selectables == null) return;

        GameObject targetObj = (_isLocked && _lockedObject != null) ? _lockedObject : EventSystem.current.currentSelectedGameObject;

        foreach (var sel in selectables)
        {
            if (sel == null) continue;

            Transform highlight = sel.transform.Find("Highlight");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(sel.gameObject == targetObj);
            }
        }
    }

    // 특정 패널(창) 내부로 포커스를 격리하고 배경을 얼리는 함수
    public void SetFocusScope(GameObject scopeRoot)
    {
        if (scopeRoot == null) return;

        ClearFocusScope(); // 기존 격리가 있다면 해제

        _currentFocusScope = scopeRoot;

        // 현재 하이어라키에 켜져 있는 모든 버튼 탐색
        Selectable[] allSelectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);
        foreach (var sel in allSelectables)
        {
            // 지정한 창(scopeRoot)의 자식이 아니면서 현재 켜져 있는 버튼들만 비활성화 대상으로 지정
            if (sel != null && sel.interactable && !sel.transform.IsChildOf(scopeRoot.transform))
            {
                sel.interactable = false;
                _temporarilyDisabled.Add(sel);
            }
        }

        RefreshAllSelectables();
    }

    // 격리를 해제하고 배경 버튼들을 원래대로 복구하는 함수
    public void ClearFocusScope()
    {
        _currentFocusScope = null;

        // 일시정지 시켜두었던 배경 버튼들을 다시 interactable = true로 복구
        foreach (var sel in _temporarilyDisabled)
        {
            if (sel != null) sel.interactable = true;
        }
        _temporarilyDisabled.Clear();

        RefreshAllSelectables();
    }

    public void LockUI(GameObject targetButton)
    {
        _isLocked = true;
        _lockedObject = targetButton;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Selectable[] currentActives = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);
        UpdateHighlights(currentActives);
    }

    public void UnlockUI()
    {
        _isLocked = false;
        _lockedObject = null;
        RefreshAllSelectables();
    }
}