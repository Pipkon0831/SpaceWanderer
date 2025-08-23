using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelButtonHandler : MonoBehaviour
{
    [Tooltip("点击按钮后加载的场景名")]
    public string sceneName;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick_LoadScene);
    }

    private void OnClick_LoadScene()
    {
        AudioManager.Instance.PlaySoundEffect(1);
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"加载场景：{sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("未设置场景名");
        }
    }
}