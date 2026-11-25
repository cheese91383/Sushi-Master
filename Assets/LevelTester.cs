using UnityEngine;
using UnityEngine.InputSystem;

// 測試用：按 Space 開始關卡；1/2/3 = 讓排第一位的客人收到 完美/普通/失敗 的餐
// 也可以把 StartButton() 綁到 UI Button 的 OnClick 上
public class LevelTester : MonoBehaviour
{
    public LevelManager level;
    public bool autoStartOnPlay = false;   // 勾起來：一按 Play 就自動開始

    void Start()
    {
        if (level == null) level = FindFirstObjectByType<LevelManager>();
        if (autoStartOnPlay && level != null) level.StartLevel();
    }

    public void StartButton()
    {
        if (level != null) level.StartLevel();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || level == null) return;

        if (kb.spaceKey.wasPressedThisFrame) level.StartLevel();
        if (kb.digit1Key.wasPressedThisFrame) level.ServeFront(Quality.Perfect);
        if (kb.digit2Key.wasPressedThisFrame) level.ServeFront(Quality.Normal);
        if (kb.digit3Key.wasPressedThisFrame) level.ServeFront(Quality.Fail);
    }
}
