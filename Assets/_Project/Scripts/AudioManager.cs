using UnityEngine;

/// <summary>
/// 音效管理器 (單例模式)
/// 負責：遊戲音效播放、音效開關切換與 PlayerPrefs 持久化儲存
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string SOUND_PREF_KEY = "SoundEnabled";
    private bool isSoundEnabled = true;

    [Header("音源組件")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    public bool IsSoundEnabled => isSoundEnabled;

    public static System.Action<bool> OnSoundToggled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 讀取 PlayerPrefs 儲存的音效開關狀態 (預設開啟 1)
        isSoundEnabled = PlayerPrefs.GetInt(SOUND_PREF_KEY, 1) == 1;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
        }

        ApplySoundSettings();
    }

    /// <summary>
    /// 切換音效開關
    /// </summary>
    public void ToggleSound()
    {
        SetSoundEnabled(!isSoundEnabled);
    }

    /// <summary>
    /// 設定音效開關並儲存
    /// </summary>
    public void SetSoundEnabled(bool enabled)
    {
        isSoundEnabled = enabled;
        PlayerPrefs.SetInt(SOUND_PREF_KEY, isSoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySoundSettings();

        OnSoundToggled?.Invoke(isSoundEnabled);
        Debug.Log($"<color=#33FFFF>【音效設定】音效已{(isSoundEnabled ? "開啟" : "靜音")}</color>");
    }

    private void ApplySoundSettings()
    {
        if (sfxSource != null) sfxSource.mute = !isSoundEnabled;
        if (bgmSource != null) bgmSource.mute = !isSoundEnabled;
    }

    /// <summary>
    /// 播放音效片段
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (!isSoundEnabled || clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
