using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 钩爪系统核心类，管理钩爪的发射、回收、旋转、过热等所有逻辑
/// </summary>
public class HookSystem : MonoBehaviour
{
    // 钩爪状态：待发射、发射中、回收中
    public enum HookState { ReadyToLaunch, Launching, Retrieving }
    // 旋转方向：顺时针、逆时针
    public enum RotationDir { Clockwise, CounterClockwise }
    // 过热系统状态：过热中，正常运行，冷却中
    public enum OverheatState { Overheating, Normal, Cooling }

    [Header("钩爪素材")]
    [Tooltip("钩爪尖端的预制体")]
    public GameObject hookTipPrefab;

    [Header("绳索设置")]
    [Tooltip("绳索的宽度")]
    public float ropeWidth = 0.4f;
    [Tooltip("绳索使用的材质")]
    public Material ropeMaterial;
    
    [HideInInspector]public Color ropeColor = Color.yellow;
    [HideInInspector]public string ropeSortingLayer = "Default";
    [HideInInspector]public int ropeSortingOrder = 50;

    [Header("基础属性")]
    [Tooltip("钩爪可延伸的最大长度")]
    public float maxLength = 10f;
    [Tooltip("钩爪待命时与中心的偏移距离")]
    public float standbyDistance = 3f;
    [Tooltip("基础旋转速度（度/秒）")]
    public float baseRotateSpeed = 90f;
    [Tooltip("基础发射速度")]
    public float baseLaunchSpeed = 10f;
    [Tooltip("基础回收速度")]
    public float baseRetrieveSpeed = 10f;
    [Tooltip("飞船质量")]
    public float spaceShipMass = 10f;
    
    [Header("最大速度")]
    [Tooltip("旋转加速时能达到的最大速度")]
    public float accelerateRotateSpeed = 180f; 
    [Tooltip("钩爪发射加速时能达到的最大速度")]
    public float accelerateLaunchSpeed = 18f;  
    [Tooltip("钩爪回收加速时能达到的最大速度")]
    public float accelerateRetrieveSpeed = 22f;  
    
    [Header("加速度属性")]
    [Tooltip("旋转速度的加速度")]
    public float rotationSmoothSpeed = 5f;
    [Tooltip("钩爪伸缩速度的加速度")]
    public float lengthSmoothSpeed = 5f;
    [Tooltip("方向切换时的加速度")]
    public float switchDirSmoothSpeed = 8f;

    [Header("过热参数")]
    [Tooltip("初始温度值")]
    public float initialTemperature = 0f;
    [Tooltip("触发过热状态的阈值温度")]
    public float overheatThreshold = 100f;
    [Tooltip("过热后允许继续运行的最大时间")]
    public float maxOverheatTime = 5f;
    [Tooltip("旋转方向切换时产生的热量值")]
    public float rotateSwitchHeat = 5f;
    [Tooltip("冷却时每秒降低的温度值")]
    public float coolingRate = 10f;
    [Tooltip("常态旋转生热系数（常态系数×当前旋转速度×钩爪质量 = 每秒生热）")]
    public float normalRotateHeatCoefficient = 0.1f;
    [Tooltip("热传导系数（越大自然散热越快，1以上可能会导致平衡温度过低出bug")]
    public float k = 0.1f;
    

    [Header("冷却CD")]
    [Tooltip("旋转方向切换的冷却时间（秒）")]
    public float rotateSwitchCD = 1f;
    [Tooltip("加速功能的冷却时间（秒）")]
    public float accelerateCD = 2f;

    [Header("钩爪质量与生热参数")]
    [Tooltip("钩爪自身的质量")]
    public float hook自身质量 = 0.5f; 
    [Tooltip("收放生热系数（总质量×实时速度×此参数 = 每秒生热）")]
    public float heatGenerationCoefficient = 0.3f; 

    [Header("UI显示")]
    [Tooltip("显示温度的滑动条")]
    public Slider temperatureSlider;
    [Tooltip("显示分数的文本")]
    public TextMeshProUGUI scoreText;
    [Tooltip("显示温度百分比的文本")]
    public TextMeshProUGUI temperaturePercentText;
    [Tooltip("显示生命值的滑动条")]
    public Slider healthSlider;
    [Tooltip("显示生命值百分比的文本")]
    public TextMeshProUGUI healthPercentText;

    [Header("玩家生命属性")]
    [Tooltip("最大生命值")]
    public float maxHealth = 100f;
    [Tooltip("飞船收到伤害调整")]
    public float kHealth = 0.1f; // 受伤参数;

    [Header("屏幕边界设置")]
    [Tooltip("用于检测屏幕边界的主相机")]
    public Camera mainCamera;

    [HideInInspector] public HookState currentState = HookState.ReadyToLaunch; // 当前钩爪状态
    [HideInInspector] public RotationDir currentDir = RotationDir.Clockwise; // 当前旋转方向
    [HideInInspector] public float currentLength = 0f; // 钩爪当前长度
    [HideInInspector] public float currentRotation = 0f; // 当前旋转角度（度）
    [HideInInspector] public float currentTemperature; // 当前温度
    [HideInInspector] public float currentHealth; // 当前生命值
    [HideInInspector] public OverheatState currentOverheatState = OverheatState.Normal; // 当前过热状态
    [HideInInspector] public float currentOverheatTime = 0f; // 过热持续时间
    [HideInInspector] public float spaceShipVelocity = 0f; // 飞船速度（暂未使用）
    [HideInInspector]public int currentScore = 0; // 当前分数

    private float currentRotateSpeed; // 当前旋转速度（度/秒）
    private float currentLaunchSpeed; // 当前发射速度
    private float currentRetrieveSpeed; // 当前回收速度
    private bool isAccelerating = false; // 是否正在加速
    private float rotateSwitchCDTimer = 0f; // 旋转方向切换冷却计时器
    private float accelerateCDTimer = 0f; // 加速功能冷却计时器
    private bool isSwitchingDir = false; // 是否正在切换旋转方向
    private float targetRotateSpeed; // 目标旋转速度（用于平滑过渡）
    private float c = 1f;

    private LineRenderer ropeRenderer; // 绳索渲染器
    private Transform hookTip; // 钩爪尖端Transform
    private HookTipCollisionHandler hookTipCollisionHandler; // 钩爪尖端碰撞处理器

    public static HookSystem Instance; // 单例实例

    private float ambientTemperature = 0f; // 环境温度（用于散热计算）

    private float grabbedMass = 0f; // 抓取物体的总质量

    // 新增：过热进入冷却状态时的事件通知
    public event System.Action OnOverheatEnterCooling;

    /// 初始化单例和绳索渲染器
    private void Awake()
    {
        // 单例模式：确保场景中只有一个HookSystem实例
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        SetupRope(); // 初始化绳索渲染器
    }

    /// 初始化钩爪尖端和初始状态
    private void Start()
    {
        InitFromPrefabs(); // 从预制体初始化钩爪尖端
        Init(); // 初始化状态变量
        if (hookTip != null)
        {
            hookTipCollisionHandler = hookTip.GetComponent<HookTipCollisionHandler>();
            hookTipCollisionHandler.hookSystem = this; // 关联钩爪系统到碰撞处理器
        }
        InitUI(); // 初始化UI显示
    }

    /// 设置绳索渲染器（LineRenderer）的基础属性
    private void SetupRope()
    {
        // 获取或添加LineRenderer组件
        ropeRenderer = gameObject.GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        ropeRenderer.enabled = true;

        // 初始化绳索材质（默认使用纯色材质）
        if (ropeMaterial == null)
        {
            ropeMaterial = new Material(Shader.Find("Unlit/Color"));
            ropeMaterial.color = ropeColor;
        }
        else
        {
            if (ropeMaterial.shader.name != "Unlit/Color")
            {
                ropeMaterial.shader = Shader.Find("Unlit/Color");
            }
            ropeMaterial.color = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        }

        // 设置绳索渲染属性
        ropeRenderer.material = ropeMaterial;
        ropeRenderer.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.widthMultiplier = ropeWidth;
        ropeRenderer.positionCount = 2; // 绳索由2个点组成（起点和终点）
        ropeRenderer.useWorldSpace = true; // 使用世界坐标

        // 设置渲染层级
        ropeRenderer.sortingLayerName = ropeSortingLayer;
        ropeRenderer.sortingOrder = ropeSortingOrder;

        // 关闭阴影相关（优化性能）
        ropeRenderer.allowOcclusionWhenDynamic = false;
        ropeRenderer.receiveShadows = false;
        ropeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// 从预制体实例化钩爪尖端
    private void InitFromPrefabs()
    {
        if (hookTipPrefab != null)
        {
            GameObject tipInstance = Instantiate(hookTipPrefab, transform);
            hookTip = tipInstance.transform;
            hookTip.localPosition = Vector3.zero;
            // 确保Z轴位置与父物体一致（2D场景中避免深度问题）
            hookTip.position = new Vector3(hookTip.position.x, hookTip.position.y, transform.position.z);
        }
    }

    /// 初始化所有状态变量
    public void Init()
    {
        currentState = HookState.ReadyToLaunch; // 初始状态：待发射
        currentDir = RotationDir.Clockwise; // 初始旋转方向：顺时针
        currentLength = standbyDistance; // 初始长度：待命距离
        currentRotation = 0f; // 初始旋转角度：0度
        currentTemperature = initialTemperature; // 初始温度
        currentHealth = maxHealth; // 初始生命值
        currentScore = 0; // 初始分数
        currentOverheatState = OverheatState.Normal; // 初始过热状态：正常
        currentOverheatTime = 0f; // 初始过热时间

        // 重置冷却计时器
        rotateSwitchCDTimer = 0f;
        accelerateCDTimer = 0f;
        isAccelerating = false;
        isSwitchingDir = false;

        // 初始化速度
        currentRotateSpeed = baseRotateSpeed;
        currentLaunchSpeed = baseLaunchSpeed;
        currentRetrieveSpeed = baseRetrieveSpeed;
        targetRotateSpeed = currentRotateSpeed;

        grabbedMass = 0f; // 初始无抓取质量

        UpdateUIDisplay(); // 更新UI
    }
    
    /// 每帧更新：处理冷却、输入、状态、速度和位置
    private void Update()
    {
        UpdateCDTimers(Time.deltaTime); // 更新冷却计时器
        HandleInput(); // 处理玩家输入
        UpdateState(Time.deltaTime); // 更新钩爪状态
        UpdateSpeedSmoothing(Time.deltaTime); // 平滑过渡速度
        UpdateHookPosition(); // 更新钩爪位置
        
        UpdateUIDisplay(); // 更新UI显示
    }

    /// 延迟更新：在位置更新后更新绳索路径（避免位置不同步）
    private void LateUpdate()
    {
        UpdateRopePath(); // 更新绳索的起点和终点
    }

    /// 更新冷却计时器
    private void UpdateCDTimers(float deltaTime)
    {
        if (rotateSwitchCDTimer > 0) rotateSwitchCDTimer -= deltaTime; // 旋转切换冷却减少
        if (accelerateCDTimer > 0) accelerateCDTimer -= deltaTime; // 加速冷却减少
    }

    /// 处理玩家输入（发射/回收、切换旋转方向、加速）
    private void HandleInput()
    {
        // 空格：切换发射/回收状态
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchLaunchOrRetrieve();
        }

        // A键：切换旋转方向（需满足：不在切换中、正常状态、冷却结束）
        if (Input.GetKeyDown(KeyCode.A) && !isSwitchingDir && currentOverheatState == OverheatState.Normal &&
            rotateSwitchCDTimer <= 0)
        {
            StartSwitchRotationDir();
        }

        // 左Shift：加速（按住加速，松开结束加速并进入冷却）
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        if (isShiftPressed && !isAccelerating && currentOverheatState == OverheatState.Normal && 
            accelerateCDTimer <= 0)
        {
            isAccelerating = true;
        }
        else if (!isShiftPressed && isAccelerating)
        {
            isAccelerating = false;
            accelerateCDTimer = accelerateCD; // 加速结束后触发冷却
        }
    }

    /// 切换发射或回收状态（待发射→发射中；发射中→回收中）
    private void SwitchLaunchOrRetrieve()
    {
        if (currentState == HookState.ReadyToLaunch)
        {
            currentState = HookState.Launching; // 待发射→发射中
        }
        // 注：回收状态切换在UpdateRetrieving中（长度小于待命距离时自动切换）
    }

    /// 开始切换旋转方向
    private void StartSwitchRotationDir()
    {
        isSwitchingDir = true; // 标记为正在切换方向
        
        // 计算目标旋转速度：加速状态下使用加速速度，否则使用基础速度，方向反转
        float targetSpeed = isAccelerating ? (baseRotateSpeed + accelerateRotateSpeed) : baseRotateSpeed;
        targetRotateSpeed = targetSpeed * (currentDir == RotationDir.Clockwise ? -1 : 1);
        
        // 切换方向（顺时针→逆时针，反之亦然）
        currentDir = currentDir == RotationDir.Clockwise ? RotationDir.CounterClockwise : RotationDir.Clockwise;
        currentTemperature += rotateSwitchHeat; // 切换方向产生热量
        rotateSwitchCDTimer = rotateSwitchCD; // 启动冷却
    }

    /// 更新钩爪状态（旋转、发射、回收、过热逻辑）
    private void UpdateState(float deltaTime)
    {
        // 根据当前状态更新行为
        switch (currentState)
        {
            case HookState.ReadyToLaunch:
                UpdateRotation(deltaTime); // 待命状态：旋转
                break;
            case HookState.Launching:
                UpdateLaunching(deltaTime); // 发射中：增加长度
                // 检测是否超出屏幕边界，超出则回收
                if (IsOutsideScreenBounds(hookTip.position))
                {
                    RetrieveHook();
                }
                break;
            case HookState.Retrieving:
                UpdateRetrieving(deltaTime); // 回收中：减少长度
                break;
        }

        // 计算热量生成
        if (currentState == HookState.Launching || currentState == HookState.Retrieving)
        {
            // 总质量 = 钩爪自身质量 + 抓取质量（仅回收时计算抓取质量）
            float totalMass = hook自身质量; 
            if (currentState == HookState.Retrieving)
            {
                totalMass += grabbedMass; 
            }

            // 当前速度：发射时用发射速度，回收时用回收速度
            float currentSpeed = (currentState == HookState.Launching) ? currentLaunchSpeed : currentRetrieveSpeed;

            // 生热计算：总质量 × 当前速度 × 生热系数 × 时间（每帧热量）
            float heatPerFrame = totalMass * currentSpeed * heatGenerationCoefficient * deltaTime;
            currentTemperature += heatPerFrame;
        }
        else if (currentState == HookState.ReadyToLaunch)
        {
            // 待命旋转时生热：常态系数 × 旋转速度绝对值 × 钩爪质量 × 时间
            float rotateSpeedAbs = Mathf.Abs(currentRotateSpeed);
            float rotateHeat = normalRotateHeatCoefficient * rotateSpeedAbs * hook自身质量 * deltaTime;
            currentTemperature += rotateHeat;
        }

        // 计算散热：根据热传导公式（散热功率 = 温差 × 热传导系数）
        float heatDissipationPower = CalculateHeatDissipationPower();
        float heatChange = -heatDissipationPower * deltaTime; // 散出的热量（负号表示减少）
        float temperatureChange = heatChange / c; // 温度变化 = 热量变化 / 热容量
        currentTemperature += temperatureChange; // 更新温度（可能降低）

        // 过热状态管理
        switch (currentOverheatState)
        {
            case OverheatState.Normal:
                // 温度超过阈值→进入过热状态
                if (currentTemperature >= overheatThreshold)
                {
                    currentOverheatState = OverheatState.Overheating;
                    currentOverheatTime = 0f; // 重置过热时间
                    //isAccelerating = false; // 过热时禁止加速
                }
                else
                {
                    isAccelerating = isAccelerating && accelerateCDTimer <= 0;
                }
                break;
            case OverheatState.Overheating:
                currentOverheatTime += deltaTime; // 累计过热时间
                if (currentOverheatTime >= maxOverheatTime)
                {
                    if (grabbedMass > 0 && currentState == HookState.Retrieving)
                    {
                        ReleaseGrabbedObjects(); // 过热时释放抓取的物体
                    }
                    currentOverheatState = OverheatState.Cooling;
                    currentOverheatTime = 0f;
                    // 修复2：触发冷却事件，通知护盾关闭
                    OnOverheatEnterCooling?.Invoke();
                    isAccelerating = false;
                }
                break;
            case OverheatState.Cooling:
                // 冷却时温度降低（不低于0）
                currentTemperature = Mathf.Max(0, currentTemperature - coolingRate * deltaTime);
                // 温度降至0→回到正常状态
                if (currentTemperature <= overheatThreshold)
                {
                    currentOverheatState = OverheatState.Normal;
                }
                break;
        }

        // 计算目标旋转速度（非切换状态下）
        float targetSpeed = baseRotateSpeed;
        if (isAccelerating)
        {
            targetSpeed += accelerateRotateSpeed; // 加速时增加速度
        }

        if (!isSwitchingDir) 
        {
            // 根据当前方向设置目标速度（顺时针为正，逆时针为负）
            targetRotateSpeed = targetSpeed * (currentDir == RotationDir.Clockwise ? 1 : -1);
        }
    }

    /// 释放所有抓取的物体
    private void ReleaseGrabbedObjects()
    {
        hookTipCollisionHandler?.ReleaseAllGrabbedObjects(); // 调用碰撞处理器释放物体
        ResetGrabbedMass(); // 重置抓取质量
        float currentRecoverLength = currentLength; // 记录当前长度
        currentState = HookState.ReadyToLaunch; // 临时切换到待命状态（避免立即回收）
        currentLength = currentRecoverLength;
        // 延迟0.1秒后恢复回收（避免状态冲突）
        Invoke(nameof(ResumeRetrieveAfterRelease), 0.1f);
    }

    /// 释放物体后恢复回收状态
    private void ResumeRetrieveAfterRelease()
    {
        if (currentState == HookState.ReadyToLaunch)
        {
            currentState = HookState.Retrieving; // 恢复回收
        }
    }

    /// 计算散热功率（自然散热）
    private float CalculateHeatDissipationPower()
    {
        float deltaTemperature = currentTemperature - ambientTemperature; // 温差（当前温度 - 环境温度）
        return deltaTemperature * k; // 散热功率 = 温差 × 热传导系数
    }

    /// 平滑过渡速度（旋转、发射、回收速度）
    private void UpdateSpeedSmoothing(float deltaTime)
    {
        // 旋转速度平滑过渡
        // 最大速度变化量 = 加速度 × 时间（切换方向时使用更快的加速度）
        float maxSpeedChange = (isSwitchingDir ? switchDirSmoothSpeed : rotationSmoothSpeed) * deltaTime;
        float speedDiff = targetRotateSpeed - currentRotateSpeed; // 速度差（目标 - 当前）
        
        // 按最大变化量调整当前速度（避免突变）
        if (Mathf.Abs(speedDiff) > maxSpeedChange)
        {
            currentRotateSpeed += Mathf.Sign(speedDiff) * maxSpeedChange; // 向目标速度靠近
        }
        else
        {
            currentRotateSpeed = targetRotateSpeed; // 达到目标速度
            isSwitchingDir = false; // 速度稳定后标记为已完成方向切换
        }

        // 发射速度平滑过渡
        float launchStep = rotationSmoothSpeed * deltaTime; // 每帧最大变化量
        float targetLaunch = isAccelerating ? accelerateLaunchSpeed : baseLaunchSpeed; // 目标发射速度
        currentLaunchSpeed = Mathf.MoveTowards(currentLaunchSpeed, targetLaunch, launchStep); // 平滑移动到目标

        // 回收速度平滑过渡
        currentRetrieveSpeed = CalculateTargetRetrieveSpeed();
    }
    
    /// 计算目标回收速度（受抓取质量影响）
    private float CalculateTargetRetrieveSpeed()
    {
        float massResistance = 1 + grabbedMass; // 质量阻力：抓取质量越大，阻力越大
        float baseSpeed = isAccelerating ? accelerateRetrieveSpeed : baseRetrieveSpeed; // 基础速度（加速/正常）
        // 回收速度 = 基础速度 × 2 / 阻力（确保质量越大速度越慢，保留一个最低值）
        return Mathf.Max(0.1f, baseSpeed * 2f / massResistance);
    }

    /// 更新待命状态下的旋转角度
    private void UpdateRotation(float deltaTime)
    {
        currentRotation += currentRotateSpeed * deltaTime; // 旋转角度 = 当前角度 + 旋转速度 × 时间
        currentRotation = (currentRotation % 360 + 360) % 360; // 角度归一化（0-360度）
    }

    /// 更新发射状态下的钩爪长度
    private void UpdateLaunching(float deltaTime)
    {
        currentLength += currentLaunchSpeed * deltaTime; // 长度 = 当前长度 + 发射速度 × 时间
        // 长度超过最大长度→切换到回收状态
        if (currentLength >= maxLength)
        {
            currentLength = maxLength; // 限制最大长度
            currentState = HookState.Retrieving;
        }
    }

    /// 更新回收状态下的钩爪长度
    private void UpdateRetrieving(float deltaTime)
    {
        currentLength -= currentRetrieveSpeed * deltaTime; // 长度 = 当前长度 - 回收速度 × 时间
        // 长度小于待命距离→切换到待命状态
        if (currentLength <= standbyDistance)
        {
            currentLength = standbyDistance; // 限制最小长度
            currentState = HookState.ReadyToLaunch;
            hookTipCollisionHandler?.OnRetrieveComplete(); // 通知碰撞处理器回收完成
        }
    }

    /// 强制回收钩爪（用于超出屏幕边界等情况）
    public void RetrieveHook()
    {
        if (currentState == HookState.Launching)
        {
            currentState = HookState.Retrieving; // 发射中→回收中
            hookTipCollisionHandler?.ResetGrabState(); // 重置抓取状态
        }
    }

    /// 更新钩爪尖端位置（基于当前旋转角度和长度）
    private void UpdateHookPosition()
    {
        if (hookTip == null) return;

        // 计算方向向量（原有逻辑）
        float radians = currentRotation * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        Vector2 hookTipPos = (Vector2)transform.position + direction * currentLength;
        hookTip.position = new Vector3(hookTipPos.x, hookTipPos.y, transform.position.z);

        // 计算角度时增加90度补偿（核心修改）
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle += 90f; // 抵消逆时针90度偏差（若偏差为顺时针，可减90f）
        hookTip.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// 更新绳索路径（起点：自身位置，终点：钩爪尖端位置）
    private void UpdateRopePath()
    {
        if (ropeRenderer == null) return;

        // 绳索起点：当前物体位置
        Vector3 startPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        // 绳索终点：钩爪尖端位置
        Vector3 endPos = hookTip.position; 

        // 更新绳索的两个点
        ropeRenderer.positionCount = 2;
        ropeRenderer.SetPosition(0, startPos);
        ropeRenderer.SetPosition(1, endPos);

        // 确保绳索属性正确
        ropeRenderer.widthMultiplier = ropeWidth;
        ropeRenderer.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.enabled = true;
    }

    /// 初始化UI显示
    private void InitUI()
    {
        if (temperatureSlider != null)
        {
            temperatureSlider.minValue = initialTemperature;
            temperatureSlider.maxValue = overheatThreshold;
            temperatureSlider.value = currentTemperature;
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        UpdateUIDisplay();
    }

    /// 更新UI显示（温度、生命值、分数）
    private void UpdateUIDisplay()
    {
        if (temperatureSlider != null)
        {
            // 温度滑块显示（限制在0到过热阈值之间）
            float displayTemperature = Mathf.Clamp(currentTemperature, initialTemperature, overheatThreshold);
            temperatureSlider.value = displayTemperature;
        }
    
        if (temperaturePercentText != null)
        {
            temperaturePercentText.text = $"{currentTemperature:F1}°C"; // 显示温度（保留1位小数）
        }
    
        if (healthSlider != null) 
            healthSlider.value = currentHealth; // 更新生命值滑块
    
        if (healthPercentText != null)
            healthPercentText.text = $"{(currentHealth / maxHealth) * 100f:F1}%"; // 显示生命值百分比
    
        if (scoreText != null)
            scoreText.text = $"分数: {currentScore}"; // 显示分数
    }

    /// 增加分数
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateUIDisplay();
    }

    /// 受到伤害
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage); // 生命值不低于0
        if (currentHealth <= 0)
        {
            Die(); // 生命值为0时死亡
        }
        UpdateUIDisplay();
    }

    /// 死亡处理
    private void Die()
    {
        Debug.Log("寄了！"); // 输出死亡信息（可扩展为游戏结束逻辑）
    }

    /// 当前发射速度
    public float CurrentLaunchSpeed => currentLaunchSpeed;

    /// 判断物体是否超出屏幕边界
    private bool IsOutsideScreenBounds(Vector3 worldPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // 获取主相机
            if (mainCamera == null) return false;
        }

        // 将世界坐标转换为屏幕坐标（屏幕坐标：左下角(0,0)，右上角(Screen.width, Screen.height)）
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        float buffer = 10f; // 边界缓冲（避免刚好在边缘被误判）
        // 超出屏幕左、右、下、上边界
        return screenPosition.x < -buffer || 
               screenPosition.x > Screen.width + buffer || 
               screenPosition.y < -buffer || 
               screenPosition.y > Screen.height + buffer;
    }
    
    /// 增加温度
    public void AddHeat(float heat)
    {
        currentTemperature += heat;
    }

    /// 增加抓取质量
    public void AddGrabbedMass(float mass)
    {
        grabbedMass = (mass + hook自身质量);
    }

    //重置抓取质量
    public void ResetGrabbedMass()
    {
        grabbedMass = 0f;
    }
}