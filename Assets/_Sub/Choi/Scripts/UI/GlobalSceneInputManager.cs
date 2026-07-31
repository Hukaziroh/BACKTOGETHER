using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GlobalSceneInputManager : MonoBehaviour
{
    private static GlobalSceneInputManager _instance;
    public static GlobalSceneInputManager Instance => _instance;

    private bool _isResettingFocus = false;
    private bool _isTransitioning = false;

    // 잠금 상태 관리 변수
    private bool _isLocked = false;
    private GameObject _lockedObject = null;

    // 포커스 범위 및 하이라이트 실시간 추적 변수
    private GameObject _currentFocusScope = null;
    private List<Selectable> _temporarilyDisabled = new List<Selectable>();
    private GameObject _lastSelectedObject = null;

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

        LockAndHideCursor();
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
        _isTransitioning = false;
        _lastSelectedObject = null;

        _currentFocusScope = null;
        _temporarilyDisabled.Clear();

        LockAndHideCursor();
        ClearAllHighlights(); // 씬 진입 시 전 하이라이트 초기화
        RefreshAllSelectables();
    }

    private void LockAndHideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
        {
            LockAndHideCursor();
        }

        if ((EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()) || _isTransitioning)
        {
            return;
        }

        // EventSystem의 선택 상태 변화를 실시간 감지하여 하이라이트 완벽 동기화
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected != _lastSelectedObject)
        {
            // 포커스가 이동하면 이전/현재 외 전체 하이라이트 잔상까지 안전하게 정리
            ClearAllHighlights();

            if (currentSelected != null && currentSelected.activeInHierarchy)
            {
                Transform newHighlight = FindHighlightTransform(currentSelected);
                if (newHighlight != null) newHighlight.gameObject.SetActive(true);
            }

            _lastSelectedObject = currentSelected;
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

        if (currentSelected == null || !currentSelected.activeInHierarchy)
        {
            if (!_isResettingFocus)
            {
                StartCoroutine(ResetFocusDelayed());
            }
        }
    }

    private IEnumerator ResetFocusDelayed()
    {
        _isResettingFocus = true;
        yield return null;

        if (EventSystem.current != null)
        {
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

    public void SetFocusScope(GameObject scopeRoot)
    {
        StartCoroutine(SetFocusScopeRoutine(scopeRoot));
    }

    private IEnumerator SetFocusScopeRoutine(GameObject scopeRoot)
    {
        _isTransitioning = true;

        yield return null;

        _lastSelectedObject = null;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        ClearFocusScope();

        if (scopeRoot == null)
        {
            scopeRoot = FindBestRootPanel();
        }

        if (scopeRoot == null)
        {
            _isTransitioning = false;
            yield break;
        }

        _currentFocusScope = scopeRoot;

        Selectable[] allSelectablesTarget = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);
        foreach (var sel in allSelectablesTarget)
        {
            if (sel != null && sel.interactable && !sel.transform.IsChildOf(scopeRoot.transform))
            {
                sel.interactable = false;
                _temporarilyDisabled.Add(sel);
            }
        }

        RefreshAllSelectables();

        _isTransitioning = false;
    }

    public void ClearFocusScope()
    {
        _currentFocusScope = null;

        foreach (var sel in _temporarilyDisabled)
        {
            if (sel != null) sel.interactable = true;
        }
        _temporarilyDisabled.Clear();

        ClearAllHighlights(); // Scope 해제 시 씬 전체 하이라이트 초기화
    }

    public void RefreshAllSelectables()
    {
        if (EventSystem.current == null) return;

        // 갱신 시점 전체 하이라이트 싹 지우기 (잔상 방지)
        ClearAllHighlights();

        Selectable[] activeSelectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);
        if (activeSelectables == null || activeSelectables.Length == 0) return;

        List<Selectable> validList = new List<Selectable>();
        foreach (var sel in activeSelectables)
        {
            if (sel != null && sel.interactable)
            {
                if (_currentFocusScope != null && !sel.transform.IsChildOf(_currentFocusScope.transform))
                {
                    continue;
                }

                validList.Add(sel);
            }
        }

        if (validList.Count == 0) return;

        validList.Sort((a, b) => {
            Canvas canvasA = a.GetComponentInParent<Canvas>();
            Canvas canvasB = b.GetComponentInParent<Canvas>();
            int orderA = canvasA != null ? canvasA.sortingOrder : 0;
            int orderB = canvasB != null ? canvasB.sortingOrder : 0;

            if (orderA != orderB) return orderB.CompareTo(orderA);
            return CompareHierarchyOrder(a.transform, b.transform);
        });

        _lastSelectedObject = null;
        EventSystem.current.SetSelectedGameObject(null);

        GameObject firstObj = validList[0].gameObject;
        EventSystem.current.SetSelectedGameObject(firstObj);

        // 첫 번째 선택 오브젝트의 Highlight만 활성화
        Transform highlight = FindHighlightTransform(firstObj);
        if (highlight != null)
        {
            highlight.gameObject.SetActive(true);
        }
        _lastSelectedObject = firstObj;
    }

    // 씬에 존재하는 모든 Selectable의 Highlight 자식 오브젝트를 꺼버리는 함수
    private void ClearAllHighlights()
    {
        Selectable[] allSelectables = FindObjectsByType<Selectable>(FindObjectsInactive.Include);
        foreach (var sel in allSelectables)
        {
            if (sel != null)
            {
                Transform highlight = FindHighlightTransform(sel.gameObject);
                if (highlight != null)
                {
                    highlight.gameObject.SetActive(false);
                }
            }
        }
    }

    private Transform FindHighlightTransform(GameObject obj)
    {
        if (obj == null) return null;
        Transform[] transforms = obj.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t != null && t.name.Equals("Highlight", System.StringComparison.OrdinalIgnoreCase))
            {
                return t;
            }
        }
        return null;
    }

    private GameObject FindBestRootPanel()
    {
        Selectable[] allSelectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);
        GameObject bestRoot = null;
        int highestOrder = int.MinValue;
        int highestSibling = int.MinValue;

        foreach (var sel in allSelectables)
        {
            if (sel != null && sel.gameObject.activeInHierarchy)
            {
                Canvas canvas = sel.GetComponentInParent<Canvas>();
                int order = canvas != null ? canvas.sortingOrder : 0;
                Transform topPanel = GetTopLevelPanel(sel.transform, canvas?.transform);

                if (topPanel != null)
                {
                    int sibling = topPanel.GetSiblingIndex();
                    if (order > highestOrder || (order == highestOrder && sibling > highestSibling))
                    {
                        highestOrder = order;
                        highestSibling = sibling;
                        bestRoot = topPanel.gameObject;
                    }
                }
            }
        }
        return bestRoot != null ? bestRoot : (allSelectables.Length > 0 ? allSelectables[0].gameObject : null);
    }

    public void LockUI(GameObject targetButton)
    {
        _isLocked = true;
        _lockedObject = targetButton;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void UnlockUI()
    {
        _isLocked = false;
        _lockedObject = null;
        RefreshAllSelectables();
    }

    private int CompareHierarchyOrder(Transform t1, Transform t2)
    {
        if (t1 == t2) return 0;
        List<Transform> path1 = GetPathToRoot(t1);
        List<Transform> path2 = GetPathToRoot(t2);

        int commonLength = Mathf.Min(path1.Count, path2.Count);
        for (int i = 0; i < commonLength; i++)
        {
            if (path1[i] != path2[i])
            {
                return path1[i].GetSiblingIndex().CompareTo(path2[i].GetSiblingIndex());
            }
        }
        return path1.Count.CompareTo(path2.Count);
    }

    private List<Transform> GetPathToRoot(Transform t)
    {
        List<Transform> path = new List<Transform>();
        Transform current = t;
        while (current != null)
        {
            path.Insert(0, current);
            current = current.parent;
        }
        return path;
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
}