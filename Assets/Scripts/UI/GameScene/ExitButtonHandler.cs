using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ExitButtonHandler : MonoBehaviour
{
    [Tooltip("菜单场景名称")]
    public string menuSceneName = "MenuScene";

    [Tooltip("返回后显示的目标页面")]
    public TargetPanel targetPanel = TargetPanel.StartPanel;

    public enum TargetPanel
    {
        StartPanel,
        ThemeSelectPanel,
        LevelSelectPanel_Theme1,
        LevelSelectPanel_Theme2,
        LevelSelectPanel_Theme3
    }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ExitToMenu);
    }

    private void ExitToMenu()
    {
        AudioManager.Instance.PlaySoundEffect(1);
        Time.timeScale = 1f;

        HomePageUIManager.TargetPanelOnLoad = targetPanel;

        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning("未设置菜单场景名！");
        }
    }
}