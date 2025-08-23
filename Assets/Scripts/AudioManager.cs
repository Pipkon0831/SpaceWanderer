using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

// 音频类型枚举
public enum AudioType
{
    Master,    // 总音量
    Music,     // 背景音乐
    SoundEffect // 音效
}

// 音频管理单例类，支持多音效并发（对象池实现）、多背景音乐
public class AudioManager : MonoBehaviour
{
    // 单例实例
    public static AudioManager Instance;
    
    [Header("基础音频源组件")]
    [SerializeField] private GameObject settingCanvus;      // 设置面板（原功能保留）
    [SerializeField] private AudioSource musicSource;      // 背景音乐源（单独管理，不参与对象池）

    [Header("音效对象池配置")]
    [Tooltip("音效池父对象（用于层级管理，避免场景混乱）")]
    [SerializeField] private Transform soundPoolParent;
    [Tooltip("初始创建的音频源数量（建议5-10，根据音效密度调整）")]
    [SerializeField] private int initialPoolSize = 8;
    [Tooltip("池的最大容量（防止无限创建导致内存溢出，建议15-20）")]
    [SerializeField] private int maxPoolSize = 15;

    [Header("音频资源列表")]
    [Tooltip("存储所有背景音乐")]
    [SerializeField] private List<AudioClip> backgroundMusics = new List<AudioClip>();
    [Tooltip("存储所有音效（短音效+持续音效）")]
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
    [SerializeField] private float defaultMasterVolume = 1.0f;  // 
    [SerializeField] private float defaultMusicVolume = 0.8f;  // 
    [SerializeField] private float defaultSoundEffectVolume = 1.0f; 
    [SerializeField] private bool defaultMasterEnabled = true;
    [SerializeField] private bool defaultMusicEnabled = true;
    [SerializeField] private bool defaultSoundEffectEnabled = true;
    [SerializeField] private int defaultMusicIndex = 0; // 默认播放的背景音乐索引

    // ---------------- 私有核心变量 ----------------
    // 当前播放的背景音乐索引
    private int _currentMusicIndex = -1;

    // 音量与开关状态
    private float _masterVolume;
    private float _musicVolume;
    private float _soundEffectVolume;
    private bool _isMasterEnabled;
    private bool _isMusicEnabled;
    private bool _isSoundEffectEnabled;

    // 音效对象池核心（复用AudioSource，避免频繁创建销毁）
    private List<AudioSource> _soundPool = new List<AudioSource>(); // 闲置音频源池
    private List<AudioSource> _activeSoundSources = new List<AudioSource>(); // 正在使用的音频源
    private Dictionary<AudioSource, bool> _isLoopSource = new Dictionary<AudioSource, bool>(); // 标记持续音效


    private void Awake()
    {
        // 单例模式实现（确保全局唯一，场景切换不销毁）
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSettings(); // 初始化音量/开关设置
            InitializeSoundPool();     // 初始化音效对象池
        }
        else
        {
            Destroy(gameObject); // 重复实例直接销毁
        }
    }

    private void Start()
    {
        BindUIEvents(); // 绑定UI交互事件
        settingCanvus?.SetActive(false); // 默认隐藏设置面板
        _currentMusicIndex = -1;
        PlayMusic(0); // 播放默认背景音乐
        Debug.Log($"当前音乐索引: {_currentMusicIndex}");
    }

    #region 基础初始化与UI绑定（原功能保留）
    // 初始化音频设置（从存档加载或用默认值）
    private void InitializeAudioSettings()
    {
        // 加载存档，无存档则用默认值
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", defaultMasterVolume);
        _musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        _soundEffectVolume = PlayerPrefs.GetFloat("SoundEffectVolume", defaultSoundEffectVolume);

        _isMasterEnabled = PlayerPrefs.GetInt("IsMasterEnabled", defaultMasterEnabled ? 1 : 0) == 1;
        _isMusicEnabled = PlayerPrefs.GetInt("IsMusicEnabled", defaultMusicEnabled ? 1 : 0) == 1;
        _isSoundEffectEnabled = PlayerPrefs.GetInt("IsSoundEffectEnabled", defaultSoundEffectEnabled ? 1 : 0) == 1;
        _currentMusicIndex = PlayerPrefs.GetInt("CurrentMusicIndex", defaultMusicIndex);

        // 同步UI显示与音频源设置
        UpdateUISettings();
        ApplyAudioSettings();
    }

    // 绑定UI滑块/开关的交互事件
    private void BindUIEvents()
    {
        // 总音量滑块
        masterVolumeSlider?.onValueChanged.AddListener(value => 
        {
            _masterVolume = value;
            ApplyAudioSettings();
        });

        // 总音量开关
        masterVolumeToggle?.onValueChanged.AddListener(value => 
        {
            _isMasterEnabled = value;
            ApplyAudioSettings();
        });

        // 背景音乐滑块
        musicVolumeSlider?.onValueChanged.AddListener(value => 
        {
            _musicVolume = value;
            ApplyAudioSettings();
        });

        // 背景音乐开关
        musicVolumeToggle?.onValueChanged.AddListener(value => 
        {
            _isMusicEnabled = value;
            ApplyAudioSettings();
            // 开关切换时控制音乐播放/停止
            if (value && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
            else if (!value && musicSource.isPlaying)
                musicSource.Stop();
        });

        // 音效滑块
        soundEffectVolumeSlider?.onValueChanged.AddListener(value => 
        {
            _soundEffectVolume = value;
            ApplyAudioSettings();
        });

        // 音效开关
        soundEffectVolumeToggle?.onValueChanged.AddListener(value => 
        {
            _isSoundEffectEnabled = value;
            ApplyAudioSettings();
        });
    }

    // 更新UI显示（同步当前音量/开关状态）
    private void UpdateUISettings()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = _masterVolume;
        if (masterVolumeToggle != null) masterVolumeToggle.isOn = _isMasterEnabled;

        if (musicVolumeSlider != null) musicVolumeSlider.value = _musicVolume;
        if (musicVolumeToggle != null) musicVolumeToggle.isOn = _isMusicEnabled;

        if (soundEffectVolumeSlider != null) soundEffectVolumeSlider.value = _soundEffectVolume;
        if (soundEffectVolumeToggle != null) soundEffectVolumeToggle.isOn = _isSoundEffectEnabled;
    }
    #endregion

    #region 音效对象池核心功能（新增）
    // 初始化音效对象池（创建初始数量的AudioSource）
    private void InitializeSoundPool()
    {
        // 自动创建父对象（若未指定），避免场景层级混乱
        if (soundPoolParent == null)
        {
            GameObject poolObj = new GameObject("SoundPool_Parent");
            poolObj.transform.parent = transform; // 作为AudioManager的子对象
            soundPoolParent = poolObj.transform;
        }

        // 创建初始数量的音频源并加入池
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewSoundSource();
        }
    }

    // 新建一个AudioSource并配置默认参数
    private AudioSource CreateNewSoundSource()
    {
        // 防止池容量超过最大值（避免内存溢出）
        if (_soundPool.Count + _activeSoundSources.Count >= maxPoolSize)
        {
            Debug.LogWarning($"音效池已达最大容量（{maxPoolSize}），无法创建新音频源！");
            return null;
        }

        // 创建音频源对象
        GameObject sourceObj = new GameObject($"SoundSource_{_soundPool.Count + _activeSoundSources.Count + 1}");
        sourceObj.transform.parent = soundPoolParent;
        AudioSource source = sourceObj.AddComponent<AudioSource>();

        // 基础配置（确保音效正常播放）
        source.mute = false;
        source.bypassEffects = false;
        source.bypassListenerEffects = false;
        source.bypassReverbZones = false;
        source.playOnAwake = false; // 禁止唤醒自动播放
        source.loop = false;        // 默认非循环（持续音效需手动设置）
        source.priority = 128;      // 优先级（0最高，256最低，默认128）
        source.spatialBlend = 0;    // 2D音效（如需3D音效，可后续手动调整）

        // 初始加入闲置池
        _soundPool.Add(source);
        return source;
    }

    // 从池获取可用的AudioSource（优先用闲置，无闲置则创建新的）
    private AudioSource GetAvailableSource()
    {
        // 1. 优先复用闲置音频源（未在播放的）
        foreach (var source in _soundPool)
        {
            if (!source.isPlaying)
            {
                _soundPool.Remove(source);
                _activeSoundSources.Add(source);
                return source;
            }
        }

        // 2. 闲置池为空，创建新音频源
        AudioSource newSource = CreateNewSoundSource();
        if (newSource != null)
        {
            _soundPool.Remove(newSource);
            _activeSoundSources.Add(newSource);
        }
        return newSource;
    }

    // 回收音频源到闲置池（仅非持续音效自动回收，持续音效需手动回收）
    private void RecycleSource(AudioSource source)
    {
        // 持续音效需先手动停止并标记为非持续，避免误回收
        if (_isLoopSource.ContainsKey(source) && _isLoopSource[source])
        {
            Debug.LogWarning("持续音效需先调用 StopLoopSound 手动停止后再回收！");
            return;
        }

        // 从活跃列表移除，放回闲置池
        if (_activeSoundSources.Contains(source))
        {
            _activeSoundSources.Remove(source);
            _soundPool.Add(source);
            // 重置配置（避免残留影响下次使用）
            source.clip = null;
            source.loop = false;
            if (_isLoopSource.ContainsKey(source))
                _isLoopSource.Remove(source);
        }
    }
    #endregion

    #region 音频播放控制（短音效+持续音效+背景音乐）
    // 应用所有音频设置（音量同步、播放状态控制）
    private void ApplyAudioSettings()
    {
        // 1. 计算最终音量（总音量 × 各通道音量）
        float finalMusicVolume = _isMasterEnabled && _isMusicEnabled ? _masterVolume * _musicVolume : 0;
        float finalSoundVolume = _isMasterEnabled && _isSoundEffectEnabled ? _masterVolume * _soundEffectVolume : 0;

        // 2. 同步背景音乐音量与播放状态
        if (musicSource != null)
        {
            musicSource.volume = finalMusicVolume;
            // 音乐启用且有音频时自动播放
            if (finalMusicVolume > 0 && !musicSource.isPlaying && musicSource.clip != null)
                musicSource.Play();
        }

        // 3. 同步所有活跃音效的音量（包括短音效和持续音效）
        foreach (var source in _activeSoundSources)
        {
            if (source == null) continue;

            source.volume = finalSoundVolume;
            // 音效开关关闭时，强制停止所有音效并回收
            if (finalSoundVolume <= 0 && source.isPlaying)
            {
                source.Stop();
                // 标记持续音效为非持续，便于回收
                if (_isLoopSource.ContainsKey(source))
                    _isLoopSource[source] = false;
                RecycleSource(source);
            }
        }
    }

    // ---------------- 短音效播放（支持多实例并发） ----------------
    /// <summary>
    /// 播放单次短音效（如按钮点击、敌人死亡）
    /// </summary>
    /// <param name="effectName">音效名称（需在soundEffects列表中）</param>
    public void PlaySoundEffect(string effectName)
    {
        // 开关关闭时直接返回
        if (!_isMasterEnabled || !_isSoundEffectEnabled) return;

        // 查找音效在列表中的索引
        int index = soundEffects.FindIndex(clip => clip != null && clip.name == effectName);
        if (index == -1)
        {
            Debug.LogWarning($"未找到名为「{effectName}」的音效，请检查soundEffects列表！");
            return;
        }

        AudioClip targetClip = soundEffects[index];
        AudioSource source = GetAvailableSource(); // 从池获取音频源

        if (source != null && targetClip != null)
        {
            source.clip = targetClip;
            source.Play();
            // 音效播放结束后自动回收（协程等待）
            StartCoroutine(WaitForSoundEnd(source, targetClip.length));
        }
    }

    /// <summary>
    /// 按索引播放单次短音效（备用）
    /// </summary>
    /// <param name="index">音效在soundEffects列表中的索引</param>
    public void PlaySoundEffect(int index)
    {
        if (index < 0 || index >= soundEffects.Count || soundEffects[index] == null)
        {
            Debug.LogWarning($"音效索引「{index}」无效，请检查soundEffects列表！");
            return;
        }
        PlaySoundEffect(soundEffects[index].name);
    }

    // 协程：等待音效播放完成后回收音频源
    private IEnumerator WaitForSoundEnd(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        RecycleSource(source);
    }

    // ---------------- 持续音效播放（支持多实例并发） ----------------
    /// <summary>
    /// 播放持续音效（如钩索收回、引擎运转）
    /// </summary>
    /// <param name="effectName">音效名称（需在soundEffects列表中）</param>
    /// <returns>当前使用的AudioSource（用于后续停止/暂停）</returns>
    public AudioSource StartLoopSound(string effectName)
    {
        // 开关关闭时直接返回
        if (!_isMasterEnabled || !_isSoundEffectEnabled) return null;

        // 查找音效
        int index = soundEffects.FindIndex(clip => clip != null && clip.name == effectName);
        if (index == -1)
        {
            Debug.LogWarning($"未找到名为「{effectName}」的音效，请检查soundEffects列表！");
            return null;
        }

        AudioClip targetClip = soundEffects[index];
        AudioSource source = GetAvailableSource(); // 从池获取音频源

        if (source != null && targetClip != null)
        {
            source.clip = targetClip;
            source.loop = true; // 持续音效必须开启循环
            source.volume = _masterVolume * _soundEffectVolume;
            source.Play();
            // 标记为持续音效（避免自动回收）
            if (!_isLoopSource.ContainsKey(source))
                _isLoopSource.Add(source, true);
            else
                _isLoopSource[source] = true;
        }

        return source; // 返回音频源引用，供外部控制
    }

    /// <summary>
    /// 按索引播放持续音效（备用）
    /// </summary>
    /// <param name="index">音效在soundEffects列表中的索引</param>
    /// <returns>当前使用的AudioSource</returns>
    public AudioSource StartLoopSound(int index)
    {
        if (index < 0 || index >= soundEffects.Count || soundEffects[index] == null)
        {
            Debug.LogWarning($"音效索引「{index}」无效，请检查soundEffects列表！");
            return null;
        }
        return StartLoopSound(soundEffects[index].name);
    }

    /// <summary>
    /// 停止指定的持续音效
    /// </summary>
    /// <param name="loopSource">StartLoopSound返回的音频源引用</param>
    public void StopLoopSound(AudioSource loopSource)
    {
        if (loopSource == null || !_activeSoundSources.Contains(loopSource)) return;

        // 停止播放并重置
        loopSource.Stop();
        loopSource.clip = null;
        loopSource.loop = false;

        // 标记为非持续，回收回池
        if (_isLoopSource.ContainsKey(loopSource))
            _isLoopSource[loopSource] = false;
        RecycleSource(loopSource);
    }

    /// <summary>
    /// 暂停指定的持续音效
    /// </summary>
    public void PauseLoopSound(AudioSource loopSource)
    {
        if (loopSource != null && loopSource.isPlaying)
            loopSource.Pause();
    }

    /// <summary>
    /// 恢复暂停的持续音效
    /// </summary>
    public void ResumeLoopSound(AudioSource loopSource)
    {
        if (loopSource != null && !loopSource.isPlaying)
            loopSource.UnPause();
    }

    // ---------------- 背景音乐控制（原功能保留） ----------------
    /// <summary>
    /// 按索引播放背景音乐
    /// </summary>
    public void PlayMusic(int index)
    {
        Debug.Log("111");
        if (index < 0 || index >= backgroundMusics.Count || backgroundMusics[index] == null)
        {
            Debug.Log("333");
            Debug.LogWarning($"背景音乐索引「{index}」无效，请检查backgroundMusics列表！");
            return;
        }

        // 同一首音乐不重复播放
        if (index == _currentMusicIndex && musicSource.isPlaying)
        {
            Debug.Log("222");
            return;
        }


        _currentMusicIndex = index;
        musicSource.clip = backgroundMusics[index];
        musicSource.loop = true; // 背景音乐默认循环

        // 音乐启用时播放
            Debug.Log("111");
            musicSource.Play();
    }

    /// <summary>
    /// 按名称播放背景音乐
    /// </summary>
    public void PlayMusic(string musicName)
    {
        int index = backgroundMusics.FindIndex(clip => clip != null && clip.name == musicName);
        if (index != -1)
            PlayMusic(index);
        else
            Debug.LogWarning($"未找到名为「{musicName}」的背景音乐，请检查backgroundMusics列表！");
    }

    /// <summary>
    /// 播放下一首背景音乐
    /// </summary>
    public void PlayNextMusic()
    {
        if (backgroundMusics.Count == 0) return;
        int nextIndex = (_currentMusicIndex + 1) % backgroundMusics.Count;
        PlayMusic(nextIndex);
    }

    /// <summary>
    /// 播放上一首背景音乐
    /// </summary>
    public void PlayPreviousMusic()
    {
        if (backgroundMusics.Count == 0) return;
        int prevIndex = (_currentMusicIndex - 1 + backgroundMusics.Count) % backgroundMusics.Count;
        PlayMusic(prevIndex);
    }
    #endregion

    #region 外部接口与存档（原功能保留+扩展）
    /// <summary>
    /// 设置指定类型的音量（0-1范围）
    /// </summary>
    public void SetVolume(AudioType type, float value)
    {
        value = Mathf.Clamp01(value); // 限制音量在0-1之间

        switch (type)
        {
            case AudioType.Master:
                _masterVolume = value;
                if (masterVolumeSlider != null) masterVolumeSlider.value = value;
                break;
            case AudioType.Music:
                _musicVolume = value;
                if (musicVolumeSlider != null) musicVolumeSlider.value = value;
                break;
            case AudioType.SoundEffect:
                _soundEffectVolume = value;
                if (soundEffectVolumeSlider != null) soundEffectVolumeSlider.value = value;
                break;
        }

        ApplyAudioSettings(); // 立即应用音量变化
    }

    /// <summary>
    /// 设置指定类型的开关状态
    /// </summary>
    public void SetEnabled(AudioType type, bool enabled)
    {
        switch (type)
        {
            case AudioType.Master:
                _isMasterEnabled = enabled;
                if (masterVolumeToggle != null) masterVolumeToggle.isOn = enabled;
                break;
            case AudioType.Music:
                _isMusicEnabled = enabled;
                if (musicVolumeToggle != null) musicVolumeToggle.isOn = enabled;
                // 开关切换时控制音乐播放/停止
                if (enabled && musicSource.clip != null && !musicSource.isPlaying)
                    musicSource.Play();
                else if (!enabled && musicSource.isPlaying)
                    musicSource.Stop();
                break;
            case AudioType.SoundEffect:
                _isSoundEffectEnabled = enabled;
                if (soundEffectVolumeToggle != null) soundEffectVolumeToggle.isOn = enabled;
                break;
        }

        ApplyAudioSettings(); // 立即应用开关变化
    }

    /// <summary>
    /// 添加背景音乐到列表（外部动态添加）
    /// </summary>
    public void AddBackgroundMusic(AudioClip clip)
    {
        if (clip != null && !backgroundMusics.Contains(clip))
            backgroundMusics.Add(clip);
    }

    /// <summary>
    /// 添加音效到列表（外部动态添加）
    /// </summary>
    public void AddSoundEffect(AudioClip clip)
    {
        if (clip != null && !soundEffects.Contains(clip))
            soundEffects.Add(clip);
    }

    /// <summary>
    /// 保存当前设置到PlayerPrefs（场景切换/退出时调用）
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
        PlayerPrefs.SetFloat("SoundEffectVolume", _soundEffectVolume);
        
        PlayerPrefs.SetInt("IsMasterEnabled", _isMasterEnabled ? 1 : 0);
        PlayerPrefs.SetInt("IsMusicEnabled", _isMusicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("IsSoundEffectEnabled", _isSoundEffectEnabled ? 1 : 0);
        PlayerPrefs.SetInt("CurrentMusicIndex", _currentMusicIndex);
        
        PlayerPrefs.Save(); // 强制写入本地
    }

    /// <summary>
    /// 从PlayerPrefs加载设置
    /// </summary>
    public void LoadSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", defaultMasterVolume);
        _musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        _soundEffectVolume = PlayerPrefs.GetFloat("SoundEffectVolume", defaultSoundEffectVolume);
        
        _isMasterEnabled = PlayerPrefs.GetInt("IsMasterEnabled", defaultMasterEnabled ? 1 : 0) == 1;
        _isMusicEnabled = PlayerPrefs.GetInt("IsMusicEnabled", defaultMusicEnabled ? 1 : 0) == 1;
        _isSoundEffectEnabled = PlayerPrefs.GetInt("IsSoundEffectEnabled", defaultSoundEffectEnabled ? 1 : 0) == 1;
        _currentMusicIndex = PlayerPrefs.GetInt("CurrentMusicIndex", defaultMusicIndex);
        
        UpdateUISettings(); // 同步UI
        ApplyAudioSettings(); // 同步音频源
    }

    // 显示/隐藏设置面板（原功能保留）
    public void ShowSettingCanvus()
    {
        if (settingCanvus != null)
            settingCanvus.SetActive(true);
    }

    public GameObject GetSettingCanvas() => settingCanvus;
    #endregion

    #region 生命周期与清理
    // 退出游戏时自动保存设置
    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    // 销毁时清理对象池（避免内存泄漏）
    private void OnDestroy()
    {
        // 停止所有活跃音效
        foreach (var source in _activeSoundSources)
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source.gameObject);
            }
        }

        // 清理闲置池
        foreach (var source in _soundPool)
        {
            if (source != null)
                Destroy(source.gameObject);
        }

        _activeSoundSources.Clear();
        _soundPool.Clear();
        _isLoopSource.Clear();
    }
    #endregion
}