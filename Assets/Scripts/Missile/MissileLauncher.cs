using UnityEngine;
using UnityEngine.UI;

public class MissileLauncher : MonoBehaviour
{
    [Header("使用控制")]
    [Tooltip("是否允许使用导弹发射器")]
    public bool canUse = true;  // 新增使用控制变量
    
    [Tooltip("导弹的预制体")]
    public GameObject missilePrefab;

    [Header("数量管理")]
    [Tooltip("当前持有的导弹数量")]
    public int currentMissileCount = 5;
    [Tooltip("最多可存储的导弹数量")]
    public int maxMissileCount = 10;

    [Header("发射参数")]
    [Tooltip("发射冷却时间")]
    public float cooldown = 2f;
    [Tooltip("飞行速度")]
    public float missileSpeed = 10f;
    [Tooltip("瞄准状态的时间")]
    public float aimCancelTime = 3f;

    [Header("发射起点偏移")]
    [Tooltip("导弹从角色中心沿方向偏移的距离")]
    public float missileOffsetDistance = 0.5f;  // 发射起点偏移量

    [Header("瞄准反馈")]
    public float aimTargetRadius = 10f;
    [Tooltip("瞄准线和指示器的基础颜色")]
    public Color aimColor = new Color(1, 1, 0, 0.9f); // 黄色透明

    [Header("过热参数")]
    public float launchHeat = 20f; // 发射一次导弹产生的热量

    [Header("UI显示")]
    public Slider temperatureSlider;

    private KeyCode fireKey = KeyCode.Alpha1;
    private HookSystem hookSystem;           // 能量管理系统引用
    [HideInInspector] public float lastFireTime = float.MinValue; // 上次发射的时间戳
    private bool isAiming = false;           // 是否处于瞄准状态
    private float aimingTimer = 0f;          // 瞄准状态的计时（用于超时判断）
    private Vector2 mouseWorldPos;           // 鼠标在世界空间中的位置

    private static Texture2D _lineTex;

    void Awake()
    {
        hookSystem = GetComponent<HookSystem>();
        currentMissileCount = Mathf.Clamp(currentMissileCount, 0, maxMissileCount);

        if (_lineTex == null)
        {
            _lineTex = new Texture2D(1, 1);
            _lineTex.SetPixel(0, 0, Color.white);
            _lineTex.Apply();
        }
    }

    void Update()
    {
        // 如果不允许使用，直接退出更新逻辑
        if (!canUse)
        {
            // 重置瞄准状态
            if (isAiming)
                isAiming = false;
            return;
        }

        HandleAimAndFire();

        if (isAiming)
        {
            UpdateMouseWorldPosition();
        }

        UpdateUIDisplay();
    }

    private void HandleAimAndFire()    // 瞄准和发射逻辑
    {
        // 如果不允许使用，直接退出
        if (!canUse)
            return;

        if (Input.GetKeyDown(fireKey) && !isAiming && CanStartAim())
        {
            isAiming = true;
            aimingTimer = 0f;
        }

        if (isAiming)
        {
            aimingTimer += Time.deltaTime;

            // 瞄准超时自动取消
            if (aimingTimer >= aimCancelTime)
            {
                isAiming = false;
            }

            // 松开发射键时判断是否发射
            if (Input.GetKeyUp(fireKey))
            {
                if (CanFire())
                {
                    FireMissile();
                }
                isAiming = false;
            }
        }
    }

    private void UpdateMouseWorldPosition()   // 更新鼠标在世界空间的位置
    {
        Vector3 mouseWorldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos = new Vector2(mouseWorldPos3D.x, mouseWorldPos3D.y);
    }

    private bool CanStartAim()  // 判断是否可以开始瞄准
    {
        return canUse &&  // 检查是否允许使用
               (Time.time - lastFireTime >= cooldown) && 
               (currentMissileCount > 0) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    private bool CanFire()  // 判断是否可以发射
    {
        return canUse &&  // 检查是否允许使用
               (Time.time - lastFireTime >= cooldown) &&
               (currentMissileCount > 0) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    public void FireMissile()  // 发射导弹
    {
        // 如果不允许使用或预制体为空，直接退出
        if (!canUse || missilePrefab == null)
        {
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector2 origin = (Vector2)transform.position + fireDirection * missileOffsetDistance; // 计算发射起点

        GameObject missile = Instantiate(
            missilePrefab,
            origin,
            Quaternion.LookRotation(Vector3.forward, fireDirection)
        );

        Missile missileComponent = missile.GetComponent<Missile>();
        if (missileComponent != null)
        {
            missileComponent.Initialize(missileSpeed, fireDirection);
        }

        currentMissileCount--;
        lastFireTime = Time.time;

        // 调用能量系统增加温度
        hookSystem.AddHeat(launchHeat);
        AudioManager.Instance.PlaySoundEffect(4);
    }

    void OnGUI()
    {
        // 如果不允许使用、未处于瞄准状态或相机为空，不绘制瞄准UI
        if (!canUse || !isAiming || Camera.main == null) 
            return;

        Vector2 fireDir = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector3 worldOrigin = (Vector2)transform.position + fireDir * missileOffsetDistance;
        Vector3 screenStart = Camera.main.WorldToScreenPoint(worldOrigin);
        Vector3 screenEnd = Camera.main.WorldToScreenPoint(mouseWorldPos);

        // 转换Y轴坐标（屏幕坐标与世界坐标Y轴方向相反）
        screenStart.y = Screen.height - screenStart.y;
        screenEnd.y = Screen.height - screenEnd.y;

        // 根据瞄准时间变化颜色（从基础色过渡到红色）
        float t = aimingTimer / aimCancelTime;
        Color currentColor = Color.Lerp(aimColor, Color.red, t);

        DrawLine(screenStart, screenEnd, currentColor, 2f);
        DrawCircle(screenEnd, aimTargetRadius, currentColor);
    }


    private void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)  // 绘制瞄准线
    {
        Matrix4x4 matrix = GUI.matrix;
        Color savedColor = GUI.color;

        Vector2 delta = pointB - pointA;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float length = delta.magnitude;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y - width / 2, length, width), _lineTex);

        // 恢复GUI状态
        GUI.matrix = matrix;
        GUI.color = savedColor;
    }

    private void DrawCircle(Vector2 center, float radius, Color color)  // 绘制瞄准目标圈
    {
        const int segments = 24;  // 圆的边数
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * angleStep * i;
            Vector2 newPoint = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            DrawLine(prevPoint, newPoint, color, 2f);
            prevPoint = newPoint;
        }
    }

    private void UpdateUIDisplay()
    {
        if (temperatureSlider != null)
        {
            temperatureSlider.value = hookSystem.currentTemperature;
        }
    }

    public void AddcurrentMissileCount(int count)  // 增加导弹数量
    {
        currentMissileCount = Mathf.Clamp(currentMissileCount + count, 0, maxMissileCount);
    }
}