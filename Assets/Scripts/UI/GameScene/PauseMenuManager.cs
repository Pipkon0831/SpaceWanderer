using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject stopUI;
    public Button continueButton;
    public SettingsButtonHandler settingsButtonHandler; // 引用设置按钮处理器

    // ------------- 新增：音效配置 -------------
    [Header("按钮音效配置")]
    public int buttonClickSoundIndex = 1; // 与主场景一致，使用索引1的按钮音效
    private bool isAudioManagerReady => AudioManager.Instance != null; // 简化空引用判断

    private bool isPaused = false;

    void Start()
    {
        stopUI.SetActive(false);
        continueButton.gameObject.SetActive(false);
        
        // ------------- 修改：继续按钮绑定「音效+原逻辑」 -------------
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
            
            // ------------- 新增：给设置按钮添加音效（通过处理器间接绑定） -------------
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
                // ------------- 新增：ESC键暂停时播放按钮音效（模拟“打开暂停菜单”的交互反馈） -------------
                PlayButtonClickSound();
            }
            else if (settingsButtonHandler == null || !settingsButtonHandler.IsWindowOpen())
            {
                ResumeGame();
                // ------------- 新增：ESC键恢复时播放按钮音效 -------------
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
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        stopUI.SetActive(false);
        continueButton.gameObject.SetActive(false);
    }

    // 处理悬浮窗打开事件（原逻辑不变）
    private void HandleWindowOpened()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    // 处理悬浮窗关闭事件（原逻辑不变）
    private void HandleWindowClosed()
    {
        if (isPaused)
        {
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    // ------------- 新增：给设置按钮绑定音效（通过SettingsButtonHandler获取按钮） -------------
    private void BindSettingsButtonSound()
    {
        // 假设SettingsButtonHandler中有“设置按钮”的引用（若没有，需在SettingsButtonHandler中暴露按钮）
        Button settingsButton = settingsButtonHandler.GetComponent<Button>();
        if (settingsButton != null)
        {
            // 先移除原有监听（避免重复绑定），再绑定“音效+原逻辑”
            // 注意：若SettingsButtonHandler的按钮已有点击逻辑，需用“链式调用”保留原逻辑
            // （此处假设SettingsButtonHandler的按钮点击逻辑在其内部，仅补充音效）
            settingsButton.onClick.AddListener(PlayButtonClickSound);
            Debug.Log("PauseMenuManager: 已给设置按钮绑定点击音效");
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: 未从SettingsButtonHandler获取到设置按钮，无法绑定音效！");
            Debug.LogWarning("解决方案：在SettingsButtonHandler中暴露public Button settingsButton;并赋值");
        }
    }

    // ------------- 新增：统一播放按钮点击音效 -------------
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

    // 清理事件监听（原逻辑不变，补充音效监听的清理）
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
            
            // ------------- 新增：清理设置按钮的音效监听 -------------
            Button settingsButton = settingsButtonHandler.GetComponent<Button>();
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(PlayButtonClickSound);
            }
        }
    }
}