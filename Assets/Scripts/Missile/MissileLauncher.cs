using UnityEngine;
using UnityEngine.UI;

public class MissileLauncher : MonoBehaviour
{
    [Header("使用控制")]
    [Tooltip("是否允许使用导弹发射器")]
    public bool canUse = true;
    
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
    public float missileOffsetDistance = 0.5f;

    [Header("瞄准反馈")]
    public float aimTargetRadius = 10f;
    [Tooltip("瞄准线和指示器的基础颜色")]
    public Color aimColor = new Color(1, 1, 0, 0.9f);

    [Header("旋转补偿")]
    [Tooltip("导弹模型初始方向补偿角度（度）")]
    public float rotationOffset = 0f;  // 新增：用于补偿模型本身的初始旋转

    [Header("过热参数")]
    public float launchHeat = 20f;

    [Header("UI显示")]
    public Slider temperatureSlider;

    private KeyCode fireKey = KeyCode.Alpha1;
    private HookSystem hookSystem;
    [HideInInspector] public float lastFireTime = float.MinValue;
    private bool isAiming = false;
    private float aimingTimer = 0f;
    private Vector2 mouseWorldPos;

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
        if (!canUse)
        {
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

    private void HandleAimAndFire()
    {
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

            if (aimingTimer >= aimCancelTime)
            {
                isAiming = false;
            }

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

    private void UpdateMouseWorldPosition()
    {
        Vector3 mouseWorldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos = new Vector2(mouseWorldPos3D.x, mouseWorldPos3D.y);
    }

    private bool CanStartAim()
    {
        return canUse && 
               (Time.time - lastFireTime >= cooldown) && 
               (currentMissileCount > 0) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    private bool CanFire()
    {
        return canUse && 
               (Time.time - lastFireTime >= cooldown) &&
               (currentMissileCount > 0) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    public void FireMissile()
    {
        if (!canUse || missilePrefab == null)
        {
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector2 origin = (Vector2)transform.position + fireDirection * missileOffsetDistance;

        // 核心修复：计算正确的旋转角度并添加补偿
        float angleRad = Mathf.Atan2(fireDirection.y, fireDirection.x);
        float angleDeg = Mathf.Rad2Deg * angleRad;
        // 应用旋转补偿，解决模型初始方向问题
        float finalRotation = angleDeg + rotationOffset;
        
        Quaternion missileRotation = Quaternion.Euler(0, 0, finalRotation);

        GameObject missile = Instantiate(
            missilePrefab,
            origin,
            missileRotation
        );

        Missile missileComponent = missile.GetComponent<Missile>();
        if (missileComponent != null)
        {
            missileComponent.Initialize(missileSpeed, fireDirection);
        }

        currentMissileCount--;
        lastFireTime = Time.time;
        hookSystem.AddHeat(launchHeat);
        AudioManager.Instance.PlaySoundEffect(4);
    }

    void OnGUI()
    {
        if (!canUse || !isAiming || Camera.main == null) 
            return;

        Vector2 fireDir = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector3 worldOrigin = (Vector2)transform.position + fireDir * missileOffsetDistance;
        Vector3 screenStart = Camera.main.WorldToScreenPoint(worldOrigin);
        Vector3 screenEnd = Camera.main.WorldToScreenPoint(mouseWorldPos);

        screenStart.y = Screen.height - screenStart.y;
        screenEnd.y = Screen.height - screenEnd.y;

        float t = aimingTimer / aimCancelTime;
        Color currentColor = Color.Lerp(aimColor, Color.red, t);

        DrawLine(screenStart, screenEnd, currentColor, 2f);
        DrawCircle(screenEnd, aimTargetRadius, currentColor);
    }

    private void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color savedColor = GUI.color;

        Vector2 delta = pointB - pointA;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float length = delta.magnitude;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y - width / 2, length, width), _lineTex);

        GUI.matrix = matrix;
        GUI.color = savedColor;
    }

    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        const int segments = 24;
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

    public void AddcurrentMissileCount(int count)
    {
        currentMissileCount = Mathf.Clamp(currentMissileCount + count, 0, maxMissileCount);
    }
}
