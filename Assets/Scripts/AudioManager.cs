using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// 音频类型枚举
public enum AudioType
{
    Master,    // 总音量
    Music,     // 背景音乐
    SoundEffect // 音效
}

// 音频管理单例类，支持多背景音乐和音效
public class AudioManager : MonoBehaviour
{
    // 单例实例
    public static AudioManager Instance;
    
    [Header("音频源组件")]
    [SerializeField] private GameObject settingCanvus;      // 背景音乐源（挂载在子对象上）
    
    [Header("音频源组件")]
    [SerializeField] private AudioSource musicSource;      // 背景音乐源（挂载在子对象上）
    [SerializeField] private AudioSource soundEffectSource; // 音效源（挂载在子对象上）

    [Header("音频资源列表")]
    [Tooltip("存储所有背景音乐")]
    [SerializeField] private List<AudioClip> backgroundMusics = new List<AudioClip>();
    
    [Tooltip("存储所有音效")]
    [SerializeField] private List<AudioClip> soundEffects = new List<AudioClip>();

    [Header("UI元素 - 总音量")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Toggle masterVolumeToggle;

    [Header("UI元素 - 背景音乐")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Toggle musicVolumeToggle;

    [Header("UI元素 - 音效")]
    [SerializeField] private Slider soundEffectVolumeSlider;
    [SerializeField] private Toggle soundEffectVolumeToggle;

    [Header("默认设置")]
    [SerializeField] private float defaultMasterVolume = 1.0f;
    [SerializeField] private float defaultMusicVolume = 0.8f;
    [SerializeField] private float defaultSoundEffectVolume = 1.0f;
    [SerializeField] private bool defaultMasterEnabled = true;
    [SerializeField] private bool defaultMusicEnabled = true;
    [SerializeField] private bool defaultSoundEffectEnabled = true;
    [SerializeField] private int defaultMusicIndex = 0; // 默认播放的背景音乐索引

    // 当前播放的背景音乐索引
    private int _currentMusicIndex = -1;

    // 保存当前音量值（0-1范围）
    private float _masterVolume;
    private float _musicVolume;
    private float _soundEffectVolume;

    // 保存当前开关状态
    private bool _isMasterEnabled;
    private bool _isMusicEnabled;
    private bool _isSoundEffectEnabled;

    private void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 绑定UI事件
        BindUIEvents();
        
        // 播放默认背景音乐
        PlayMusic(defaultMusicIndex);
        
        settingCanvus.SetActive(false); // 默认隐藏设置面板
    }

    // 初始化音频设置
    private void InitializeAudioSettings()
    {
        // 从PlayerPrefs加载保存的设置，没有则使用默认值
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", defaultMasterVolume);
        _musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        _soundEffectVolume = PlayerPrefs.GetFloat("SoundEffectVolume", defaultSoundEffectVolume);

        _isMasterEnabled = PlayerPrefs.GetInt("IsMasterEnabled", defaultMasterEnabled ? 1 : 0) == 1;
        _isMusicEnabled = PlayerPrefs.GetInt("IsMusicEnabled", defaultMusicEnabled ? 1 : 0) == 1;
        _isSoundEffectEnabled = PlayerPrefs.GetInt("IsSoundEffectEnabled", defaultSoundEffectEnabled ? 1 : 0) == 1;

        // 应用初始设置到UI
        UpdateUISettings();
        
        // 应用初始设置到音频源
        ApplyAudioSettings();
    }

    // 绑定UI事件
    private void BindUIEvents()
    {
        // 总音量滑块
        masterVolumeSlider.onValueChanged.AddListener(value => 
        {
            _masterVolume = value;
            ApplyAudioSettings();
        });

        // 总音量开关
        masterVolumeToggle.onValueChanged.AddListener(value => 
        {
            _isMasterEnabled = value;
            ApplyAudioSettings();
        });

        // 背景音乐滑块
        musicVolumeSlider.onValueChanged.AddListener(value => 
        {
            _musicVolume = value;
            ApplyAudioSettings();
        });

        // 背景音乐开关
        musicVolumeToggle.onValueChanged.AddListener(value => 
        {
            _isMusicEnabled = value;
            ApplyAudioSettings();
        });

        // 音效滑块
        soundEffectVolumeSlider.onValueChanged.AddListener(value => 
        {
            _soundEffectVolume = value;
            ApplyAudioSettings();
        });

        // 音效开关
        soundEffectVolumeToggle.onValueChanged.AddListener(value => 
        {
            _isSoundEffectEnabled = value;
            ApplyAudioSettings();
        });
    }

    // 更新UI显示
    private void UpdateUISettings()
    {
        masterVolumeSlider.value = _masterVolume;
        masterVolumeToggle.isOn = _isMasterEnabled;

        musicVolumeSlider.value = _musicVolume;
        musicVolumeToggle.isOn = _isMusicEnabled;

        soundEffectVolumeSlider.value = _soundEffectVolume;
        soundEffectVolumeToggle.isOn = _isSoundEffectEnabled;
    }

    // 应用音频设置到音频源
    private void ApplyAudioSettings()
    {
        // 计算实际应用的音量（总音量 * 各通道音量）
        float finalMusicVolume = _isMasterEnabled && _isMusicEnabled 
            ? _masterVolume * _musicVolume 
            : 0;
            
        float finalSoundEffectVolume = _isMasterEnabled && _isSoundEffectEnabled 
            ? _masterVolume * _soundEffectVolume 
            : 0;

        // 应用到音频源
        musicSource.volume = finalMusicVolume;
        soundEffectSource.volume = finalSoundEffectVolume;

        // 如果音乐被启用且有音频但未播放，则播放
        if (finalMusicVolume > 0 && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.Play();
        }
    }

    // 外部接口：通过索引播放背景音乐（切换音乐）
    public void PlayMusic(int index)
    {
        // 检查索引是否有效
        if (index >= 0 && index < backgroundMusics.Count)
        {
            // 如果是同一首音乐则不重复播放
            if (index == _currentMusicIndex && musicSource.isPlaying)
                return;
                
            _currentMusicIndex = index;
            musicSource.clip = backgroundMusics[index];
            musicSource.loop = true; // 背景音乐默认循环
            
            // 只有在音乐启用的情况下才播放
            if (_isMasterEnabled && _isMusicEnabled)
            {
                musicSource.Play();
            }
        }
        else
        {
            Debug.LogWarning($"背景音乐索引 {index} 无效！");
        }
    }

    // 外部接口：通过名称播放背景音乐
    public void PlayMusic(string musicName)
    {
        int index = backgroundMusics.FindIndex(clip => clip.name == musicName);
        if (index != -1)
        {
            PlayMusic(index);
        }
        else
        {
            Debug.LogWarning($"未找到名为 {musicName} 的背景音乐！");
        }
    }

    // 外部接口：播放下一首背景音乐
    public void PlayNextMusic()
    {
        if (backgroundMusics.Count == 0) return;
        
        int nextIndex = (_currentMusicIndex + 1) % backgroundMusics.Count;
        PlayMusic(nextIndex);
    }

    // 外部接口：播放上一首背景音乐
    public void PlayPreviousMusic()
    {
        if (backgroundMusics.Count == 0) return;
        
        int prevIndex = (_currentMusicIndex - 1 + backgroundMusics.Count) % backgroundMusics.Count;
        PlayMusic(prevIndex);
    }

    // 外部接口：通过索引播放音效
    public void PlaySoundEffect(int index)
    {
        // 检查索引是否有效且音效已启用
        if (index >= 0 && index < soundEffects.Count && _isMasterEnabled && _isSoundEffectEnabled)
        {
            soundEffectSource.PlayOneShot(soundEffects[index]);
        }
        else if (index < 0 || index >= soundEffects.Count)
        {
            Debug.LogWarning($"音效索引 {index} 无效！");
        }
    }

    // 外部接口：通过名称播放音效
    public void PlaySoundEffect(string effectName)
    {
        int index = soundEffects.FindIndex(clip => clip.name == effectName);
        if (index != -1)
        {
            PlaySoundEffect(index);
        }
        else
        {
            Debug.LogWarning($"未找到名为 {effectName} 的音效！");
        }
    }

    // 外部接口：添加背景音乐到列表
    public void AddBackgroundMusic(AudioClip clip)
    {
        if (clip != null && !backgroundMusics.Contains(clip))
        {
            backgroundMusics.Add(clip);
        }
    }

    // 外部接口：添加音效到列表
    public void AddSoundEffect(AudioClip clip)
    {
        if (clip != null && !soundEffects.Contains(clip))
        {
            soundEffects.Add(clip);
        }
    }

    // 外部接口：设置特定类型的音量
    public void SetVolume(AudioType type, float value)
    {
        value = Mathf.Clamp01(value);
        
        switch (type)
        {
            case AudioType.Master:
                _masterVolume = value;
                masterVolumeSlider.value = value;
                break;
            case AudioType.Music:
                _musicVolume = value;
                musicVolumeSlider.value = value;
                break;
            case AudioType.SoundEffect:
                _soundEffectVolume = value;
                soundEffectVolumeSlider.value = value;
                break;
        }
        
        ApplyAudioSettings();
    }

    // 外部接口：设置特定类型的开关状态
    public void SetEnabled(AudioType type, bool enabled)
    {
        switch (type)
        {
            case AudioType.Master:
                _isMasterEnabled = enabled;
                masterVolumeToggle.isOn = enabled;
                break;
            case AudioType.Music:
                _isMusicEnabled = enabled;
                musicVolumeToggle.isOn = enabled;
                // 如果启用音乐且当前有音乐，则播放
                if (enabled && musicSource.clip != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                }
                // 如果禁用音乐，则停止播放
                else if (!enabled && musicSource.isPlaying)
                {
                    musicSource.Stop();
                }
                break;
            case AudioType.SoundEffect:
                _isSoundEffectEnabled = enabled;
                soundEffectVolumeToggle.isOn = enabled;
                break;
        }
        
        ApplyAudioSettings();
    }

    // 保存设置到PlayerPrefs（场景切换或退出时调用）
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
        PlayerPrefs.SetFloat("SoundEffectVolume", _soundEffectVolume);
        
        PlayerPrefs.SetInt("IsMasterEnabled", _isMasterEnabled ? 1 : 0);
        PlayerPrefs.SetInt("IsMusicEnabled", _isMusicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("IsSoundEffectEnabled", _isSoundEffectEnabled ? 1 : 0);
        PlayerPrefs.SetInt("CurrentMusicIndex", _currentMusicIndex);
        
        PlayerPrefs.Save();
    }

    // 从PlayerPrefs加载设置
    public void LoadSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", defaultMasterVolume);
        _musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        _soundEffectVolume = PlayerPrefs.GetFloat("SoundEffectVolume", defaultSoundEffectVolume);
        
        _isMasterEnabled = PlayerPrefs.GetInt("IsMasterEnabled", defaultMasterEnabled ? 1 : 0) == 1;
        _isMusicEnabled = PlayerPrefs.GetInt("IsMusicEnabled", defaultMusicEnabled ? 1 : 0) == 1;
        _isSoundEffectEnabled = PlayerPrefs.GetInt("IsSoundEffectEnabled", defaultSoundEffectEnabled ? 1 : 0) == 1;
        _currentMusicIndex = PlayerPrefs.GetInt("CurrentMusicIndex", defaultMusicIndex);
        
        UpdateUISettings();
        ApplyAudioSettings();
    }

    // 退出游戏时保存设置
    private void OnApplicationQuit()
    {
        SaveSettings();
    }
    
    public void ShowSettingCanvus()
    {
        settingCanvus.SetActive(true);
    }
    
    public GameObject GetSettingCanvas()
    {
        return settingCanvus;
    }
}