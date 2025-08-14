using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class SettingsButtonHandler : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private bool useOverlay = true;
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private int baseSortingOrder = 10;

    public System.Action OnWindowOpened;
    public System.Action OnWindowClosed;

    private Button spawnButton;
    private GameObject currentWindow;
    private GameObject overlay;
    private List<Selectable> disabledUIElements = new List<Selectable>();
    private bool isWindowOpen = false;

    private void Start()
    {
        spawnButton = GetComponent<Button>();
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(ToggleSettingsWindow);
        }
        else
        {
            Debug.LogError("当前物体上没有Button组件！");
        }

        // 确保AudioManager单例存在
        if (AudioManager.Instance == null)
        {
            Debug.LogError("场景中没有找到AudioManager单例！");
        }
    }

    public bool IsWindowOpen()
    {
        return isWindowOpen;
    }

    private void ToggleSettingsWindow()
    {
        if (AudioManager.Instance == null) return;

        if (isWindowOpen)
        {
            CloseWindow();
        }
        else
        {
            ShowSettings();
        }
    }

    private void ShowSettings()
    {
        if (isWindowOpen || AudioManager.Instance == null) return;

        // 调用AudioManager的显示方法
        AudioManager.Instance.ShowSettingCanvus();
        
        // 从AudioManager获取设置界面引用
        currentWindow = AudioManager.Instance.GetSettingCanvas();
        
        if (currentWindow == null)
        {
            Debug.LogError("从AudioManager获取的设置界面为空！");
            return;
        }

        Canvas windowCanvas = EnsureCanvasExists();
        PositionWindowInCenter();

        if (useOverlay)
        {
            CreateOverlayAsChild(currentWindow.transform, windowCanvas);
        }

        DisableOtherUI();
        BindCloseEvent();

        isWindowOpen = true;
        OnWindowOpened?.Invoke();
    }

    private Canvas EnsureCanvasExists()
    {
        Canvas windowCanvas = currentWindow.GetComponent<Canvas>();
        if (windowCanvas == null)
        {
            windowCanvas = currentWindow.AddComponent<Canvas>();
            windowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            currentWindow.AddComponent<CanvasScaler>();
            currentWindow.AddComponent<GraphicRaycaster>();
        }
        windowCanvas.sortingOrder = baseSortingOrder + 1;
        return windowCanvas;
    }

    private void PositionWindowInCenter()
    {
        RectTransform windowRect = currentWindow.GetComponent<RectTransform>();
        if (windowRect != null)
        {
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
        }
    }

    private void CreateOverlayAsChild(Transform parent, Canvas windowCanvas)
    {
        // 如果已有遮罩则先销毁
        if (overlay != null)
        {
            Destroy(overlay);
        }

        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(parent);

        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.localScale = Vector3.one;

        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = overlayColor;

        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = windowCanvas.sortingOrder - 1;

        Button overlayButton = overlay.AddComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        overlayButton.onClick.AddListener(CloseWindow); 
    }

    private void DisableOtherUI()
    {
        Selectable[] allUI = FindObjectsOfType<Selectable>(true);
        foreach (Selectable ui in allUI)
        {
            bool isCurrentButton = ui.gameObject == gameObject;
            bool isInWindow = currentWindow != null && ui.transform.IsChildOf(currentWindow.transform);

            if (!isCurrentButton && !isInWindow && ui.interactable)
            {
                ui.interactable = false;
                disabledUIElements.Add(ui);
            }
        }
    }

    private void BindCloseEvent()
    {
        Button closeButton = currentWindow.GetComponentInChildren<Button>(true);
        if (closeButton != null && closeButton.gameObject != gameObject)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    public void CloseWindow()
    {
        if (!isWindowOpen) return;

        foreach (Selectable ui in disabledUIElements)
        {
            if (ui != null) ui.interactable = true;
        }
        disabledUIElements.Clear();

        if (currentWindow != null)
        {
            // 通知AudioManager隐藏设置界面
            if (AudioManager.Instance != null)
            {
                //AudioManager.Instance.HideSettingCanvas();
            }
            currentWindow = null;
        }

        if (overlay != null)
        {
            Destroy(overlay);
            overlay = null;
        }

        isWindowOpen = false;
        OnWindowClosed?.Invoke();
    }

    private void OnDestroy()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveListener(ToggleSettingsWindow);
        }
    }
}
