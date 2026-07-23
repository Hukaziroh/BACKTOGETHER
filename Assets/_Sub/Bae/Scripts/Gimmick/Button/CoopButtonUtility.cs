using System;
using System.Collections.Generic;

/// <summary>
/// CoopButton의 상태 검사와 이벤트 등록을 통합 관리하는 유틸리티 클래스입니다.
/// 인스펙터 설정을 깨지 않고 안전하게 코드를 줄여줍니다.
/// </summary>
public static class CoopButtonUtility
{
    /// <summary>
    /// 배열 안의 모든 버튼에 이벤트를 자동으로 연결해 줍니다.
    /// </summary>
    public static void SubscribeButtons(IEnumerable<CoopButton> buttons, Action<CoopButton> action)
    {
        if (buttons == null) return;
        foreach (var btn in buttons)
        {
            if (btn != null) btn.OnButtonStateChangedEvent += action;
        }
    }

    /// <summary>
    /// 배열 안의 '모든' 버튼이 눌렸는지 확인합니다. (하나라도 안 눌렸거나 비어있으면 false)
    /// </summary>
    public static bool AreAllPressed(IEnumerable<CoopButton> buttons)
    {
        if (buttons == null) return false;

        bool hasValidButton = false;
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            hasValidButton = true;
            if (!btn.isPressed) return false; // 하나라도 안 눌려있으면 즉시 실패
        }

        return hasValidButton; // 유효한 버튼이 하나라도 있었고, 그게 다 눌렸다면 true
    }

    /// <summary>
    /// 배열 안의 버튼 중 '하나라도' 눌렸는지 확인합니다.
    /// </summary>
    public static bool IsAnyPressed(IEnumerable<CoopButton> buttons)
    {
        if (buttons == null) return false;

        foreach (var btn in buttons)
        {
            if (btn != null && btn.isPressed) return true; // 하나라도 눌려있으면 즉시 성공
        }

        return false;
    }
}