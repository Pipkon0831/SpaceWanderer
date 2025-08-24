using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 引入UI命名空间（Button组件依赖）

public class ReloadSceneButton : MonoBehaviour
{
    void Start()
    {
        // 1. 获取当前GameObject上的Button组件
        Button restartBtn = GetComponent<Button>();
        
        // 2. 为按钮添加点击事件监听（点击时执行RestartCurrentScene方法）
        if (restartBtn != null)
        {
            restartBtn.onClick.AddListener(RestartCurrentScene);
        }
        else
        {
            Debug.LogError("当前GameObject未挂载Button组件！请先添加Button组件再使用此脚本");
        }
    }

    /// <summary>
    /// 重启当前场景的核心方法
    /// </summary>
    void RestartCurrentScene()
    {
        Time.timeScale = 1f;

        // 1. 获取当前激活场景的名称（也可通过场景索引获取：SceneManager.GetActiveScene().buildIndex）
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // 2. 卸载当前场景并重新加载（异步加载避免卡顿，可选同步加载：SceneManager.LoadScene(currentSceneName)）
        SceneManager.LoadSceneAsync(currentSceneName, LoadSceneMode.Single);
        
        // （可选）打印日志，方便调试
        Debug.Log($"已重启场景：{currentSceneName}");
    }
}