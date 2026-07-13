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
        // 새로운 씬으로 넘어가면 잠금 상태 초기화
        _isLocked = false;
        _lockedObject = null;

        _isResettingFocus = false;
        RefreshAllSelectables();
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        // 잠금 상태일 때의 처리
        if (_isLocked)
        {
            if (_lockedObject == null)
            {
                UnlockUI();
                return;
            }

            // 포커스가 다른 곳으로 튀지 못하게 강제 해제 상태 유지
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

        validList.Sort((a, b) => {
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

    // [최적화] 커서 이동(Select) 시 하이라이트 갱신 기능만 깔끔하게 남김
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

    // ButtonLockTrigger가 버튼 눌렸을 때 직접 호출해줄 함수
    public void LockUI(GameObject targetButton)
    {
        _isLocked = true;
        _lockedObject = targetButton;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Selectable[] currentActives = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        UpdateHighlights(currentActives);
    }

    public void UnlockUI()
    {
        _isLocked = false;
        _lockedObject = null;
        RefreshAllSelectables();
    }
}