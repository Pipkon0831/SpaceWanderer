using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HomePageUIManager : MonoBehaviour
{
    // 静态实例（全局访问）
    public static HomePageUIManager Instance;

    // 存储从游戏场景返回时要显示的面板类型
    public static ExitButtonHandler.TargetPanel TargetPanelOnLoad = ExitButtonHandler.TargetPanel.StartPanel;

    #region 主面板引用
    [Header("主面板引用")]
    public GameObject startPanel;
    public GameObject themeSelectPanel;
    public GameObject levelSelectPanel_Theme1;
    public GameObject levelSelectPanel_Theme2;
    public GameObject levelSelectPanel_Theme3;
    #endregion

    #region 音效配置（新增：统一管理按钮音效索引）
    [Header("按钮音效配置")]
    public int buttonClickSoundIndex = 1; // 按钮点击音效在AudioManager中的索引（目标索引1）
    #endregion

    #region 按钮引用
    [Header("StartPanel 按钮")]
    public Button startButton;
    public Button settingButton;
    public Button quitButton;

    [Header("ThemeSelectPanel 按钮")]
    public Button theme1Button;
    public Button theme2Button;
    public Button theme3Button;
    public Button backToStartButton;

    [Header("LevelSelectPanel 按钮")]
    public Button backToThemeButton_1;
    public Button backToThemeButton_2;
    public Button backToThemeButton_3;
    #endregion

    #region 设置悬浮窗配置
    [Header("设置悬浮窗配置")]
    public bool useOverlay = true; // 是否使用遮罩层
    public Color overlayColor = new Color(0, 0, 0, 0.5f); // 遮罩颜色
    public int baseSortingOrder = 10; // 基础层级
    [Tooltip("悬浮窗内部关闭按钮的名称")]
    public string closeButtonName = "CloseButton"; // 关闭按钮名称

    private GameObject currentSettingWindow; // 当前悬浮窗（从AudioManager获取）
    private GameObject overlay; // 遮罩层（动态创建，随窗口隐藏销毁）
    private List<Selectable> disabledUIElements = new List<Selectable>(); // 记录被禁用的UI
    private bool isSettingWindowOpen = false; // 窗口状态标记
    #endregion


    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 检查AudioManager单例是否存在（提前排查依赖问题）
        if (AudioManager.Instance == null)
        {
            Debug.LogError("HomePageUIManager: 场景中未找到AudioManager单例！请确保AudioManager已在场景中配置。");
        }
    }

    private void Start()
    {
        InitButtonEvents(); // 初始化按钮事件（含音效绑定）
        ShowTargetPanel();  // 显示目标面板
        TargetPanelOnLoad = ExitButtonHandler.TargetPanel.StartPanel; // 重置返回面板标记
    }


    #region 初始化方法（核心修改：按钮事件绑定+音效）
    private void InitButtonEvents()
    {
        CheckButtonReferences(); // 检查按钮引用是否完整

        // ---------------- StartPanel 按钮（绑定：音效 + 原逻辑）----------------
        startButton.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); // 播放按钮音效（索引1）
            OnClick_Start();        // 原面板切换逻辑
        });
        settingButton.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_Setting(); 
        });
        quitButton.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_Quit(); 
        });

        // ---------------- ThemeSelectPanel 按钮（绑定：音效 + 原逻辑）----------------
        theme1Button.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_Theme1(); 
        });
        theme2Button.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_Theme2(); 
        });
        theme3Button.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_Theme3(); 
        });
        backToStartButton.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_BackToStart(); 
        });

        // ---------------- LevelSelectPanel 按钮（绑定：音效 + 原逻辑）----------------
        backToThemeButton_1.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_BackToTheme(); 
        });
        backToThemeButton_2.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_BackToTheme(); 
        });
        backToThemeButton_3.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); 
            OnClick_BackToTheme(); 
        });
    }

    // 检查按钮引用是否为空（避免空引用错误）
    private void CheckButtonReferences()
    {
        if (startButton == null) Debug.LogError("HomePageUIManager: startButton 未赋值！");
        if (settingButton == null) Debug.LogError("HomePageUIManager: settingButton 未赋值！");
        if (quitButton == null) Debug.LogError("HomePageUIManager: quitButton 未赋值！");

        if (theme1Button == null) Debug.LogError("HomePageUIManager: theme1Button 未赋值！");
        if (theme2Button == null) Debug.LogError("HomePageUIManager: theme2Button 未赋值！");
        if (theme3Button == null) Debug.LogError("HomePageUIManager: theme3Button 未赋值！");
        if (backToStartButton == null) Debug.LogError("HomePageUIManager: backToStartButton 未赋值！");

        if (backToThemeButton_1 == null) Debug.LogError("HomePageUIManager: backToThemeButton_1 未赋值！");
        if (backToThemeButton_2 == null) Debug.LogError("HomePageUIManager: backToThemeButton_2 未赋值！");
        if (backToThemeButton_3 == null) Debug.LogError("HomePageUIManager: backToThemeButton_3 未赋值！");

        // 检查AudioManager的设置窗口是否赋值
        if (AudioManager.Instance != null && AudioManager.Instance.GetSettingCanvas() == null)
        {
            Debug.LogError("HomePageUIManager: AudioManager中未赋值settingCanvas！请在AudioManager面板中配置设置窗口。");
        }
    }
    #endregion


    #region 面板切换方法（原逻辑不变）
    private void ShowOnly(GameObject targetPanel)
    {
        // 仅激活目标面板，隐藏其他面板
        startPanel.SetActive(targetPanel == startPanel);
        themeSelectPanel.SetActive(targetPanel == themeSelectPanel);
        levelSelectPanel_Theme1.SetActive(targetPanel == levelSelectPanel_Theme1);
        levelSelectPanel_Theme2.SetActive(targetPanel == levelSelectPanel_Theme2);
        levelSelectPanel_Theme3.SetActive(targetPanel == levelSelectPanel_Theme3);

        // 切换主面板时自动关闭设置窗口
        if (isSettingWindowOpen)
        {
            CloseSettingWindow();
        }
    }

    private void ShowTargetPanel()
    {
        // 根据返回标记显示对应面板
        switch (TargetPanelOnLoad)
        {
            case ExitButtonHandler.TargetPanel.StartPanel:
                ShowOnly(startPanel);
                break;
            case ExitButtonHandler.TargetPanel.ThemeSelectPanel:
                ShowOnly(themeSelectPanel);
                break;
            case ExitButtonHandler.TargetPanel.LevelSelectPanel_Theme1:
                ShowOnly(levelSelectPanel_Theme1);
                break;
            case ExitButtonHandler.TargetPanel.LevelSelectPanel_Theme2:
                ShowOnly(levelSelectPanel_Theme2);
                break;
            case ExitButtonHandler.TargetPanel.LevelSelectPanel_Theme3:
                ShowOnly(levelSelectPanel_Theme3);
                break;
        }
    }

    // 按钮事件核心逻辑（原逻辑不变）
    public void OnClick_Start() => ShowOnly(themeSelectPanel);
    public void OnClick_Quit()
    {
        Debug.Log("退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClick_Theme1() => ShowOnly(levelSelectPanel_Theme1);
    public void OnClick_Theme2() => ShowOnly(levelSelectPanel_Theme2);
    public void OnClick_Theme3() => ShowOnly(levelSelectPanel_Theme3);

    public void OnClick_BackToStart() => ShowOnly(startPanel);
    public void OnClick_BackToTheme() => ShowOnly(themeSelectPanel);
    #endregion


    #region 设置悬浮窗管理（修改：关闭按钮添加音效）
    // 点击设置按钮：显示设置窗口（从AudioManager获取）
    public void OnClick_Setting()
    {
        if (isSettingWindowOpen || AudioManager.Instance == null) return;

        // 调用AudioManager显示窗口并获取引用
        AudioManager.Instance.ShowSettingCanvus();
        currentSettingWindow = AudioManager.Instance.GetSettingCanvas();

        if (currentSettingWindow == null)
        {
            Debug.LogError("HomePageUIManager: 从AudioManager获取的设置窗口为空！");
            return;
        }

        // 窗口基础配置
        SetupWindowCanvas();
        CenterWindow();

        // 创建遮罩（若启用）
        if (useOverlay)
        {
            CreateOverlay();
        }

        // 禁用其他UI+绑定关闭按钮
        DisableOtherUI();
        BindInnerCloseButton();

        // 更新窗口状态
        isSettingWindowOpen = true;
    }

    // 设置窗口Canvas层级（原逻辑不变）
    private void SetupWindowCanvas()
    {
        Canvas windowCanvas = currentSettingWindow.GetComponent<Canvas>();
        if (windowCanvas == null)
        {
            windowCanvas = currentSettingWindow.AddComponent<Canvas>();
            windowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            currentSettingWindow.AddComponent<CanvasScaler>();
            currentSettingWindow.AddComponent<GraphicRaycaster>();
        }
        windowCanvas.overrideSorting = true;
        windowCanvas.sortingOrder = baseSortingOrder + 1;
    }

    // 窗口居中显示（原逻辑不变）
    private void CenterWindow()
    {
        RectTransform windowRect = currentSettingWindow.GetComponent<RectTransform>();
        if (windowRect != null)
        {
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
        }
    }

    // 创建遮罩层（原逻辑不变）
    private void CreateOverlay()
    {
        // 销毁旧遮罩避免重复
        if (overlay != null) Destroy(overlay);

        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(currentSettingWindow.transform);

        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;

        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = overlayColor;

        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = baseSortingOrder;

        Button overlayButton = overlay.AddComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        overlayButton.onClick.AddListener(() => 
        {
            PlayButtonClickSound(); // 遮罩点击也播放音效（可选，按需求决定是否保留）
            CloseSettingWindow(); 
        });
    }

    // 禁用其他UI交互（原逻辑不变）
    private void DisableOtherUI()
    {
        Selectable[] allUI = FindObjectsOfType<Selectable>(true);
        foreach (Selectable ui in allUI)
        {
            bool isSettingButton = ui.gameObject == settingButton.gameObject;
            bool isInWindow = currentSettingWindow != null && ui.transform.IsChildOf(currentSettingWindow.transform);

            if (!isSettingButton && !isInWindow && ui.interactable)
            {
                ui.interactable = false;
                disabledUIElements.Add(ui);
            }
        }
    }

    // 绑定窗口内部关闭按钮（修改：添加音效播放）
    private void BindInnerCloseButton()
    {
        Button closeButton = null;

        // 按名称查找关闭按钮
        if (!string.IsNullOrEmpty(closeButtonName))
        {
            Transform closeBtnTransform = currentSettingWindow.transform.Find(closeButtonName);
            if (closeBtnTransform != null)
            {
                closeButton = closeBtnTransform.GetComponent<Button>();
            }
        }

        // 未找到则查找窗口内第一个Button
        if (closeButton == null)
        {
            closeButton = currentSettingWindow.GetComponentInChildren<Button>(true);
        }

        if (closeButton != null)
        {
            // 绑定：先播放音效，再关闭窗口
            closeButton.onClick.AddListener(() => 
            {
                PlayButtonClickSound(); // 新增：关闭按钮播放音效（索引1）
                CloseSettingWindow(); 
            });
            Debug.Log($"HomePageUIManager: 已绑定设置窗口关闭按钮：{closeButton.name}（含音效）");
        }
        else
        {
            Debug.LogWarning($"HomePageUIManager: 设置窗口中未找到关闭按钮（名称：{closeButtonName}）！");
        }
    }

    // 关闭设置窗口（隐藏而非销毁，原逻辑不变）
    public void CloseSettingWindow()
    {
        if (!isSettingWindowOpen || currentSettingWindow == null) return;

        // 恢复其他UI交互
        foreach (Selectable ui in disabledUIElements)
        {
            if (ui != null) ui.interactable = true;
        }
        disabledUIElements.Clear();

        // 调用AudioManager隐藏窗口（原逻辑：若AudioManager有Hide方法可补充，此处保持原代码）
        if (AudioManager.Instance != null)
        {
            // 注：若AudioManager未实现HideSettingCanvas，需在AudioManager中补充：
            // public void HideSettingCanvus() { settingCanvus?.SetActive(false); }
        }

        // 销毁动态遮罩
        if (overlay != null)
        {
            Destroy(overlay);
            overlay = null;
        }

        // 重置状态
        isSettingWindowOpen = false;
        currentSettingWindow = null;
    }
    #endregion


    #region 音效工具方法（新增：统一播放按钮点击音效）
    /// <summary>
    /// 统一播放按钮点击音效（调用AudioManager的指定索引音效）
    /// </summary>
    private void PlayButtonClickSound()
    {
        // 空引用防护：确保AudioManager实例存在
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySoundEffect(buttonClickSoundIndex); // 播放索引1的音效
        }
        else
        {
            Debug.LogWarning("HomePageUIManager: AudioManager实例不存在，无法播放按钮音效！");
        }
    }
    #endregion


    // 清理事件监听（原逻辑不变，避免内存泄漏）
    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveAllListeners();
        if (settingButton != null) settingButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();

        if (theme1Button != null) theme1Button.onClick.RemoveAllListeners();
        if (theme2Button != null) theme2Button.onClick.RemoveAllListeners();
        if (theme3Button != null) theme3Button.onClick.RemoveAllListeners();
        if (backToStartButton != null) backToStartButton.onClick.RemoveAllListeners();

        if (backToThemeButton_1 != null) backToThemeButton_1.onClick.RemoveAllListeners();
        if (backToThemeButton_2 != null) backToThemeButton_2.onClick.RemoveAllListeners();
        if (backToThemeButton_3 != null) backToThemeButton_3.onClick.RemoveAllListeners();

        // 清理设置窗口关闭按钮监听（避免残留）
        if (currentSettingWindow != null)
        {
            Button closeButton = currentSettingWindow.GetComponentInChildren<Button>(true);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseSettingWindow);
            }
        }
    }
}