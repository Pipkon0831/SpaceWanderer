using UnityEngine;
using TMPro;

// 游戏结束方式枚举
public enum GameOverType
{
    Collision,  // 碰撞结束
    Time        // 时间结束
}

public class GameOverController : MonoBehaviour
{
    [Header("结束方式设置")]
    [Tooltip("选择游戏结束的触发方式")]
    public GameOverType gameOverType;

    [Header("时间结束设置")]
    [Tooltip("当选择时间结束时的倒计时时间(秒)")]
    [SerializeField] private float gameTime = 60f;

    private float healthCheckDelay = 1f;

    [Header("分数胜利条件")]
    [Tooltip("达到此分数即判定为胜利")]
    [SerializeField] private int winScoreThreshold = 100;

    [Header("UI元素")]
    [Tooltip("游戏结束时显示的UI面板")]
    [SerializeField] private GameObject gameOverUI;
    [Tooltip("显示结果的TextPro文本")]
    [SerializeField] private TMP_Text resultText;

    private float timer;  // 计时器
    private float startDelayTimer;  // 开始延迟计时器
    private bool isHealthCheckEnabled = false;  // 是否启用生命值检测
    private HookSystem hookSystem;  // 钩子系统引用
    private bool isGameOver = false;  // 游戏是否已结束

    private void Start()
    {
        gameOverUI.SetActive(false);
        // 初始化计时器
        timer = 0f;
        startDelayTimer = 0f;

        // 隐藏游戏结束UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("请分配游戏结束UI面板");
        }

        // 尝试获取场景中的HookSystem
        hookSystem = FindObjectOfType<HookSystem>();
        if (hookSystem == null)
        {
            Debug.LogWarning("场景中未找到HookSystem脚本，请确保场景中存在该脚本");
        }
    }

    private void Update()
    {
        // 如果游戏已经结束，则不再执行任何检测
        if (isGameOver) return;

        // 处理开始延迟，1秒后启用生命值检测
        if (!isHealthCheckEnabled)
        {
            startDelayTimer += Time.deltaTime;
            if (startDelayTimer >= healthCheckDelay)
            {
                isHealthCheckEnabled = true;
                Debug.Log("开始生命值检测");
            }
        }
        // 启用生命值检测后，检查生命值是否为0
        else if (hookSystem != null && hookSystem.currentHealth <= 0)
        {
            GameOver(false);  // 因生命值归零失败
        }

        // 如果是时间结束模式，进行计时
        if (gameOverType == GameOverType.Time)
        {
            timer += Time.deltaTime;
            
            if (timer >= gameTime)
            {
                // 时间结束时根据分数判断结果
                bool isSuccess = hookSystem != null && hookSystem.currentScore >= winScoreThreshold;
                GameOver(isSuccess);
            }
        }
    }

    // 2D碰撞检测
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isGameOver) return;
        
        if (gameOverType == GameOverType.Collision && 
            collision.gameObject.CompareTag("Player"))
        {
            GameOver(false);  // 因碰撞失败
        }
    }

    /// <summary>
    /// 游戏结束处理
    /// </summary>
    /// <param name="isSuccess">是否为胜利</param>
    private void GameOver(bool isSuccess)
    {
        if (isGameOver) return;  // 防止重复调用
        
        isGameOver = true;

        AudioManager.Instance.PauseMusic();
        AudioManager.Instance.PauseAllActiveLoopSounds();
        
        // 暂停游戏
        Time.timeScale = 0f;

        // 获取当前分数
        int currentScore = 0;
        if (hookSystem != null)
        {
            currentScore = hookSystem.currentScore;
        }
        else
        {
            Debug.LogWarning("无法获取分数，HookSystem脚本不存在或未找到");
        }

        // 显示游戏结束UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            
            // 设置结果文本
            if (resultText != null)
            {
                if (isSuccess)
                {
                    resultText.text = "Success!";
                }
                else
                {
                    resultText.text = "Game Over";
                }
            }
            else
            {
                Debug.LogWarning("请分配结果文本组件");
            }
        }

        // 输出游戏结束信息和分数
        Debug.Log($"{(isSuccess ? "游戏胜利" : "游戏结束")} 当前分数：{currentScore}");
    }
}
