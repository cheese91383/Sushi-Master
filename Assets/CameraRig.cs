using UnityEngine;
using UnityEngine.InputSystem;

// 掛在 Main Camera 上。
// 流程：開場（第三人稱，從門外看店）→ 進店後切到各「站位」的第一人稱視角。
// 站位 0 = 左邊切魚、1 = 中間握壽司、2 = 右邊倒茶（順序自己決定）。
// 操作：Tab = 開場 → 進店；Q / E 或 左右方向鍵 = 換站位；滑鼠 = 在站位上小幅度轉頭（站位的視角範圍有限制）。
public class CameraRig : MonoBehaviour
{
    [Header("位置（都是場景裡的空物件，藍色箭頭 = 鏡頭朝向）")]
    public Transform introPoint;          // 開場第三人稱位置，放在店外，看著整間店
    public Transform[] stations;          // 各站位的第一人稱位置，高度約 1.6m（師傅眼睛）

    [Header("移動")]
    public float moveTime = 0.8f;         // 換位置要幾秒
    public bool startAtIntro = true;

    [Header("轉頭（第一人稱）")]
    public bool lookEnabled = true;
    public float lookSpeed = 0.1f;
    public float maxYaw = 70f;            // 左右最多轉幾度
    public float maxPitch = 45f;          // 上下最多轉幾度

    public int Current { get; private set; } = -1;   // -1 = 開場視角
    public bool IsMoving { get; private set; }

    Vector3 velocity;
    Transform goal;
    float yaw, pitch;

    void Start()
    {
        if (startAtIntro && introPoint != null) SnapTo(introPoint, -1);
        else if (stations != null && stations.Length > 0) SnapTo(stations[0], 0);
    }

    void Update()
    {
        HandleInput();
        Move();
        Look();
    }

    void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tabKey.wasPressedThisFrame)
        {
            if (Current < 0) GoTo(1);                    // 開場 → 先到中間（握壽司）
            else GoToIntro();
        }
        if (Current >= 0)
        {
            if (kb.eKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) GoTo(Current + 1);
            if (kb.qKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) GoTo(Current - 1);
        }
    }

    public void GoTo(int index)
    {
        if (stations == null || stations.Length == 0) return;
        index = Mathf.Clamp(index, 0, stations.Length - 1);
        Current = index;
        goal = stations[index];
        yaw = pitch = 0f;
        IsMoving = true;
    }

    public void GoToIntro()
    {
        if (introPoint == null) return;
        Current = -1;
        goal = introPoint;
        yaw = pitch = 0f;
        IsMoving = true;
    }

    void SnapTo(Transform t, int index)
    {
        Current = index;
        goal = t;
        transform.SetPositionAndRotation(t.position, t.rotation);
        IsMoving = false;
    }

    void Move()
    {
        if (goal == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, goal.position, ref velocity, moveTime);
        if (IsMoving && (transform.position - goal.position).sqrMagnitude < 0.0004f) IsMoving = false;
    }

    void Look()
    {
        if (goal == null) return;

        if (lookEnabled && Current >= 0 && !IsMoving && Mouse.current != null)
        {
            Vector2 d = Mouse.current.delta.ReadValue();
            yaw = Mathf.Clamp(yaw + d.x * lookSpeed, -maxYaw, maxYaw);
            pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -maxPitch, maxPitch);
        }

        Quaternion target = goal.rotation * Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }
}
