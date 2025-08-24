using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject stopUI;
    public Button continueButton;
    public SettingsButtonHandler settingsButtonHandler; // 引用设置按钮处理器

    [Header("按钮音效配置")]
    public int buttonClickSoundIndex = 1; // 与主场景一致，使用索引1的按钮音效
    private bool isAudioManagerReady => AudioManager.Instance != null; // 简化空引用判断

    private bool isPaused = false;

    void Start()
    {
        stopUI.SetActive(false);
        continueButton.gameObject.SetActive(false);
        
        continueButton.onClick.AddListener(() => 
        {
            PlayButtonClickSound();
            ResumeGame();
        });
        
        // 绑定设置按钮处理器的事件（若存在）
        if (settingsButtonHandler != null)
        {
            settingsButtonHandler.OnWindowOpened += HandleWindowOpened;
            settingsButtonHandler.OnWindowClosed += HandleWindowClosed;
            
            BindSettingsButtonSound();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                PauseGame();
                PlayButtonClickSound();
            }
            else if (settingsButtonHandler == null || !settingsButtonHandler.IsWindowOpen())
            {
                ResumeGame();
                PlayButtonClickSound();
            }
        }
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        stopUI.SetActive(true);
        continueButton.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        
        // 暂停音乐和持续音效
        if (isAudioManagerReady)
        {
            AudioManager.Instance.OnGamePause();
        }
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        stopUI.SetActive(false);
        continueButton.gameObject.SetActive(false);
        
        // 恢复音乐和持续音效
        if (isAudioManagerReady)
        {
            AudioManager.Instance.OnGameResume();
        }
    }

    // 处理悬浮窗打开事件
    private void HandleWindowOpened()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    // 处理悬浮窗关闭事件
    private void HandleWindowClosed()
    {
        if (isPaused)
        {
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    // 给设置按钮绑定音效
    private void BindSettingsButtonSound()
    {
        Button settingsButton = settingsButtonHandler.GetComponent<Button>();
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(PlayButtonClickSound);
            Debug.Log("PauseMenuManager: 已给设置按钮绑定点击音效");
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: 未从SettingsButtonHandler获取到设置按钮，无法绑定音效！");
            Debug.LogWarning("解决方案：在SettingsButtonHandler中暴露public Button settingsButton;并赋值");
        }
    }

    // 统一播放按钮点击音效
    private void PlayButtonClickSound()
    {
        if (isAudioManagerReady)
        {
            AudioManager.Instance.PlaySoundEffect(buttonClickSoundIndex);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: AudioManager实例不存在，无法播放按钮音效！");
        }
    }

    // 清理事件监听
    private void OnDestroy()
    {
        // 清理继续按钮监听
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
        }

        // 清理设置按钮处理器的事件
        if (settingsButtonHandler != null)
        {
            settingsButtonHandler.OnWindowOpened -= HandleWindowOpened;
            settingsButtonHandler.OnWindowClosed -= HandleWindowClosed;
            
            // 清理设置按钮的音效监听
            Button settingsButton = settingsButtonHandler.GetComponent<Button>();
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(PlayButtonClickSound);
            }
        }
    }
}
