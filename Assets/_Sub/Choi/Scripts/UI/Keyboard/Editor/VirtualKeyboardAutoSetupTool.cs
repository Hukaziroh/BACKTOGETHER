using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using TMPro;

public class VirtualKeyboardAutoSetupTool : EditorWindow
{
    private Transform keyboardRoot;
    private VirtualKeyboardManager keyboardManager;

    [MenuItem("Tools/Virtual Keyboard Auto Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<VirtualKeyboardAutoSetupTool>("Virtual Keyboard Setup Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("풀 키보드 자동 버튼 및 기능 세팅 툴", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        keyboardRoot = (Transform)EditorGUILayout.ObjectField("키보드 루트 (부모 패널)", keyboardRoot, typeof(Transform), true);
        keyboardManager = (VirtualKeyboardManager)EditorGUILayout.ObjectField("VirtualKeyboardManager", keyboardManager, typeof(VirtualKeyboardManager), true);

        EditorGUILayout.Space();
        if (GUILayout.Button("모든 키에 버튼 꽂고 기능 자동 세팅하기", GUILayout.Height(45)))
        {
            SetupKeyboardKeys();
        }
    }

    private void SetupKeyboardKeys()
    {
        if (keyboardRoot == null || keyboardManager == null)
        {
            EditorUtility.DisplayDialog("경고", "키보드 루트(부모 패널)와 VirtualKeyboardManager를 모두 지정해주세요!", "확인");
            return;
        }

        RectTransform[] childRects = keyboardRoot.GetComponentsInChildren<RectTransform>(true);
        int count = 0;

        foreach (var rect in childRects)
        {
            if (rect == keyboardRoot) continue;

            GameObject obj = rect.gameObject;
            string objNameUpper = obj.name.ToUpper();

            // 현재 키보드의 행 컨테이너 이름(1~6)을 숫자 키로 오인하지 않도록 제외한다.
            if (rect.childCount > 0 && int.TryParse(obj.name.Trim(), out _))
            {
                KeyboardKeyButton staleKey = obj.GetComponent<KeyboardKeyButton>();
                if (staleKey != null)
                {
                    Button staleButton = obj.GetComponent<Button>();
                    Undo.DestroyObjectImmediate(staleKey);
                    if (staleButton != null) Undo.DestroyObjectImmediate(staleButton);
                    EditorUtility.SetDirty(obj);
                }

                continue;
            }

            // 1. 단순 패널이나 배경 등 키가 아닌 컨테이너 오브젝트는 건너뜀
            if (objNameUpper.Contains("PANEL") || objNameUpper.Contains("BG") ||
                objNameUpper.Contains("BACKGROUND") || objNameUpper.Contains("ROW") ||
                objNameUpper.Contains("GROUP") || objNameUpper.Contains("KEYBOARD"))
            {
                if (!objNameUpper.Equals("BACKSPACE") && !objNameUpper.Equals("ENTER") && !objNameUpper.Equals("SPACE"))
                {
                    continue;
                }
            }

            // 2. 텍스트 컴포넌트 확인 (TextMeshPro 또는 일반 Text)
            TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
            Text uiText = obj.GetComponentInChildren<Text>();

            string keyText = "";
            if (tmp != null) keyText = tmp.text.Trim();
            else if (uiText != null) keyText = uiText.text.Trim();

            string upperKeyText = keyText.ToUpper();

            // 3. 오브젝트 이름 정제 (예: "Key_A" -> "A", "Btn_Q" -> "Q")
            string cleanName = objNameUpper.Replace("KEY_", "").Replace("BTN_", "").Replace("BUTTON_", "").Trim();

            // 키 타입 및 입력값 결정
            KeyboardKeyButton.KeyType determinedType = KeyboardKeyButton.KeyType.None;
            string determinedValue = "";

            if (objNameUpper.Contains("BACK") || objNameUpper.Contains("DEL") || upperKeyText == "BACKSPACE" || upperKeyText == "DEL" || cleanName == "BACKSPACE" || cleanName == "DEL")
            {
                determinedType = KeyboardKeyButton.KeyType.Backspace;
            }
            else if (objNameUpper.Contains("ENTER") || objNameUpper.Contains("CONFIRM") || objNameUpper.Contains("OK") || upperKeyText == "ENTER" || upperKeyText == "OK" || cleanName == "ENTER" || cleanName == "OK")
            {
                determinedType = KeyboardKeyButton.KeyType.Confirm;
            }
            else if (objNameUpper.Contains("SPACE") || upperKeyText == "SPACE" || cleanName == "SPACE")
            {
                determinedType = KeyboardKeyButton.KeyType.Space;
                determinedValue = " ";
            }
            else if (objNameUpper.Contains("SHIFT") || upperKeyText == "SHIFT" || cleanName == "SHIFT")
            {
                determinedType = KeyboardKeyButton.KeyType.Shift;
            }
            else if (objNameUpper.Contains("CAPS") || cleanName == "CAP" || upperKeyText == "CAPS LOCK")
            {
                determinedType = KeyboardKeyButton.KeyType.CapsLock;
            }
            else if (objNameUpper.Contains("CTRL") || objNameUpper.Contains("ALT") ||
                     objNameUpper.Contains("TAB") || objNameUpper.Contains("ESC") || objNameUpper.Contains("CAPS") ||
                     objNameUpper.Contains("INS") || objNameUpper.Contains("HOME") || objNameUpper.Contains("PAGE") ||
                     objNameUpper.Contains("PRT") || objNameUpper.Contains("SCROLL") || objNameUpper.Contains("PAUSE") ||
                     objNameUpper.Contains("END") || (upperKeyText.StartsWith("F") && upperKeyText.Length <= 3) ||
                     objNameUpper.Contains("UP") || objNameUpper.Contains("DOWN") || objNameUpper.Contains("LEFT") || objNameUpper.Contains("RIGHT"))
            {
                // 시스템 특수키는 입력 제외
                determinedType = KeyboardKeyButton.KeyType.None;
            }
            else
            {
                // 텍스트가 있으면 텍스트 우선, 없으면 오브젝트 이름(cleanName)을 글자로 사용
                string targetStr = !string.IsNullOrEmpty(keyText) ? keyText : (cleanName.Length == 1 ? cleanName : "");

                if (!string.IsNullOrEmpty(targetStr))
                {
                    determinedType = KeyboardKeyButton.KeyType.Char;
                    determinedValue = targetStr;
                }
            }

            if (determinedType == KeyboardKeyButton.KeyType.None)
            {
                continue;
            }

            // 4. Image 컴포넌트 추가 및 Raycast 설정 (클릭 가능하게)
            Image img = obj.GetComponent<Image>();
            if (img == null)
            {
                img = Undo.AddComponent<Image>(obj);
                img.color = new Color(1, 1, 1, 0.01f);
            }
            img.raycastTarget = true;

            // 5. Button 컴포넌트 추가
            Button btn = obj.GetComponent<Button>();
            if (btn == null)
            {
                btn = Undo.AddComponent<Button>(obj);
            }

            // 6. KeyboardKeyButton 컴포넌트 추가 및 값 설정
            KeyboardKeyButton keyBtn = obj.GetComponent<KeyboardKeyButton>();
            if (keyBtn == null)
            {
                keyBtn = Undo.AddComponent<KeyboardKeyButton>(obj);
            }
            keyBtn.manager = keyboardManager;
            keyBtn.keyType = determinedType;
            keyBtn.characterValue = determinedValue;

            // 7. OnClick 이벤트 초기화 후 함수 연결
            SerializedObject serializedObject = new SerializedObject(btn);
            SerializedProperty onClickProperty = serializedObject.FindProperty("m_OnClick");
            onClickProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
            serializedObject.ApplyModifiedProperties();

            UnityEventTools.AddPersistentListener(btn.onClick, keyBtn.OnClickKey);

            EditorUtility.SetDirty(obj);
            count++;
        }

        // 행별 좌우 순환과 위아래 최단 거리 이동을 명시적으로 연결한다.
        keyboardManager.ConfigureKeyboardNavigation();
        EditorUtility.SetDirty(keyboardManager);

        EditorUtility.DisplayDialog("완료", $"총 {count}개의 키에 버튼이 꽂히고 입력 기능이 세팅되었습니다!", "확인");
    }
}
