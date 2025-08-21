using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnData
{
    [Header("基础设置")]
    [Tooltip("要生成的Collectible预制体")]
    public GameObject collectiblePrefab;
    
    [Tooltip("游戏开始后多久生成（秒）")]
    public float spawnTime = 0f;
    
    [Tooltip("该物体需要到达目标位置的时间（游戏开始后秒数）")]
    public float targetArrivalTime = 5f;
    
    [Header("目标位置设置（2D坐标）")]
    [Tooltip("物体需要到达的目标位置（X坐标）")]
    public float targetX = 0f;
    
    [Tooltip("物体需要到达的目标位置（Y坐标）")]
    public float targetY = 0f;

    [HideInInspector] public bool hasSpawned = false; // 标记是否已生成

    // 计算并返回目标位置的2D向量
    public Vector2 GetTargetPosition()
    {
        return new Vector2(targetX, targetY);
    }
}

public class CollectibleSpawnManager : MonoBehaviour
{
    [Header("生成配置")]
    [Tooltip("所有需要生成的物体配置列表")]
    public List<SpawnData> spawnConfigs = new List<SpawnData>();

    [Header("调试设置")]
    private bool showDebugInfo = true;
    
    [Tooltip("是否显示移动路径")]
    public bool showPathGizmos = true;
    
    [Tooltip("路径线条颜色")]
    public Color pathColor = Color.yellow;
    
    [Tooltip("生成点标记颜色")]
    public Color spawnPointColor = Color.blue;

    [Header("GUI设置")]
    [Tooltip("GUI显示位置")]
    public Vector2 guiPosition = new Vector2(1800, 10);

    [Tooltip("GUI背景颜色")]
    public Color backgroundColor = new Color(0, 0, 0, 0);

    [Tooltip("GUI文字颜色")]
    public Color textColor = Color.white;

    [Tooltip("GUI字体大小")]
    public int fontSize = 36;

    // 统一游戏时间（自游戏开始后秒数）
    private float currentGameTime = 0f;
    private GUIStyle guiStyle; // 用于存储GUI样式
    private GUIStyleState guiStyleState; // 用于存储GUI样式状态

    private void Start()
    {
        // 初始化GUI样式
        guiStyle = new GUIStyle();
        guiStyleState = new GUIStyleState();
        
        // 设置文字样式
        guiStyleState.textColor = textColor;
        guiStyle.normal = guiStyleState;
        guiStyle.fontSize = fontSize;
        
        // 确保currentGameTime在关卡开始时正确初始化
        currentGameTime = Time.timeSinceLevelLoad;
    }

    private void Update()
    {
        // 更新统一游戏时间（基于关卡加载后的时间）
        currentGameTime = Time.timeSinceLevelLoad;

        // 检查并生成到达生成时间的物体
        CheckAndSpawnCollectibles();
    }

    /// <summary>
    /// 检查所有配置，生成到达生成时间的物体
    /// </summary>
    private void CheckAndSpawnCollectibles()
    {
        foreach (var config in spawnConfigs)
        {
            // 跳过已生成或无效的配置
            if (config.hasSpawned || !IsConfigValid(config))
                continue;

            // 当游戏时间到达生成时间时执行生成
            if (currentGameTime >= config.spawnTime)
            {
                SpawnCollectible(config);
                config.hasSpawned = true;
            }
        }
    }

    /// <summary>
    /// 生成Collectible物体并计算生成位置
    /// </summary>
    private void SpawnCollectible(SpawnData config)
    {
        // 获取预制体中的Collectible组件
        CollectibleObject prefabCollectible = config.collectiblePrefab.GetComponent<CollectibleObject>();
        if (prefabCollectible == null)
        {
            Debug.LogError($"【Spawn Error】预制体 {config.collectiblePrefab.name} 未挂载CollectibleObject组件！");
            return;
        }

        // 计算运动时间（从生成到到达目标位置的时间差）
        float moveDuration = config.targetArrivalTime - config.spawnTime;
        if (moveDuration <= 0)
        {
            Debug.LogError($"【Time Error】{config.collectiblePrefab.name} 到达时间必须晚于生成时间！（生成时间：{config.spawnTime}，到达时间：{config.targetArrivalTime}）");
            return;
        }

        // 1. 计算物体的运动速度向量（基于预制体自身配置）
        Vector2 velocityDir = prefabCollectible.initialDirection; // 已标准化
        float speed = prefabCollectible.initialSpeed;
        Vector2 movementVelocity = velocityDir * speed;

        // 2. 计算生成位置（反向推导：生成位置 = 目标位置 - 速度×运动时间）
        Vector2 targetPosition = new Vector2(config.targetX, config.targetY); // 直接使用X、Y坐标
        Vector2 spawnPosition = targetPosition - movementVelocity * moveDuration;

        // 3. 实例化物体
        GameObject spawnedObject = Instantiate(
            config.collectiblePrefab, 
            spawnPosition, 
            Quaternion.identity, 
            transform // 父物体设为管理器，方便层级管理
        );

        // 4. 初始化物体运动状态
        CollectibleObject spawnedCollectible = spawnedObject.GetComponent<CollectibleObject>();
        if (spawnedCollectible != null)
        {
            // 强制应用速度（覆盖预制体初始设置，确保运动轨迹正确）
            spawnedCollectible.SetInitialVelocity(speed, velocityDir);
            
            // 应用初始旋转（如果预制体有配置）
            spawnedCollectible.ApplyInitialRotation();

            if (showDebugInfo)
            {
                Debug.Log($"=== 生成物体 ===");
                Debug.Log($"物体名称：{spawnedObject.name}");
                Debug.Log($"生成时间：{config.spawnTime}s，到达时间：{config.targetArrivalTime}s");
                Debug.Log($"运动时间：{moveDuration}s，速度：{movementVelocity}");
                Debug.Log($"生成位置：{spawnPosition}，目标位置（X={config.targetX}, Y={config.targetY}）");
            }
        }
        else
        {
            Debug.LogError($"【Component Error】生成的物体 {spawnedObject.name} 未挂载CollectibleObject组件！");
        }
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    private bool IsConfigValid(SpawnData config)
    {
        // 基础校验
        if (config.collectiblePrefab == null)
        {
            Debug.LogError("SpawnData中未设置collectiblePrefab！");
            return false;
        }

        // 时间逻辑校验
        if (config.targetArrivalTime <= config.spawnTime)
        {
            Debug.LogError($"{config.collectiblePrefab.name} 到达时间必须大于生成时间！");
            return false;
        }

        // 组件校验
        if (config.collectiblePrefab.GetComponent<CollectibleObject>() == null)
        {
            Debug.LogError($"{config.collectiblePrefab.name} 未挂载CollectibleObject组件！");
            return false;
        }

        return true;
    }

private void OnDrawGizmos()
{
    // 绘制目标位置辅助线（场景视图可视化）
    foreach (var config in spawnConfigs)
    {
        // 跳过无效配置
        if (!IsConfigValid(config)) continue;
        
        // 获取目标位置
        Vector2 targetPos = new Vector2(config.targetX, config.targetY);
        
        // 绘制目标位置标记
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, 0.5f); // 目标位置标记（2D圆形）
        Gizmos.DrawLine(targetPos, targetPos + Vector2.up * 1.5f); // 绘制向上的指示线
        
        // 如果启用了路径显示，计算并绘制移动路径
        if (showPathGizmos && !config.hasSpawned)
        {
            // 获取预制体组件以计算路径
            CollectibleObject prefabCollectible = config.collectiblePrefab.GetComponent<CollectibleObject>();
            if (prefabCollectible != null)
            {
                // 计算生成位置
                float moveDuration = config.targetArrivalTime - config.spawnTime;
                if (moveDuration > 0)
                {
                    Vector2 velocityDir = prefabCollectible.initialDirection;
                    float speed = prefabCollectible.initialSpeed;
                    Vector2 movementVelocity = velocityDir * speed;
                    Vector2 spawnPosition = targetPos - movementVelocity * moveDuration;
                    
                    // 绘制生成点标记
                    Gizmos.color = spawnPointColor;
                    Gizmos.DrawWireSphere(spawnPosition, 0.3f);
                    
                    // 绘制移动路径射线
                    Gizmos.color = pathColor;
                    Gizmos.DrawLine(spawnPosition, targetPos);
                    
                    // 绘制路径方向箭头（修复向量类型不匹配问题）
                    Vector2 direction = (targetPos - spawnPosition).normalized;
                    
                    // 将Vector2转换为Vector3进行旋转计算，再转回Vector2
                    Vector3 dir3D = new Vector3(direction.x, direction.y, 0);
                    Vector3 rotatedDir1 = Quaternion.Euler(0, 0, 30) * dir3D;
                    Vector3 rotatedDir2 = Quaternion.Euler(0, 0, -30) * dir3D;
                    
                    // 计算箭头尖端位置
                    Vector2 arrowTip1 = targetPos - direction * 0.3f - new Vector2(rotatedDir1.x, rotatedDir1.y) * 0.2f;
                    Vector2 arrowTip2 = targetPos - direction * 0.3f - new Vector2(rotatedDir2.x, rotatedDir2.y) * 0.2f;
                    
                    // 绘制箭头
                    Gizmos.DrawLine(targetPos, arrowTip1);
                    Gizmos.DrawLine(targetPos, arrowTip2);
                }
            }
        }
    }
}


    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        // 创建GUI背景矩形
        Rect backgroundRect = new Rect(
            guiPosition.x, 
            guiPosition.y, 
            200, 
            40
        );
        
        // 创建文本显示矩形（稍微内缩）
        Rect textRect = new Rect(
            guiPosition.x + 10, 
            guiPosition.y + 10, 
            180, 
            20
        );
        
        // 绘制背景
        GUI.backgroundColor = backgroundColor;
        GUI.Box(backgroundRect, "");
        
        // 格式化并显示时间（保留两位小数）
        string timeText = $"游戏时间: {currentGameTime:F2}秒";
        GUI.Label(textRect, timeText, guiStyle);
    }
}
