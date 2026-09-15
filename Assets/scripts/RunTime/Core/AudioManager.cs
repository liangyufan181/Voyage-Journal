using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("背景音乐；留空则从 Resources/BGM/MainBGM 自动加载")]
    [SerializeField] private AudioClip bgmClip;

    [Tooltip("进入场景后是否自动开始播放")]
    [SerializeField] private bool playOnAwake = true;

    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.6f;

    private AudioSource bgmSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;          // 循环播放
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        if (bgmClip == null)
        {
            bgmClip = Resources.Load<AudioClip>("BGM/MainBGM");
        }

        if (playOnAwake && bgmClip != null)
        {
            PlayBGM();
        }
        else if (bgmClip == null)
        {
            Debug.LogWarning("[AudioManager] 未找到背景音乐：请在面板拖入 Bgm Clip，或把音乐放到 Resources/BGM/MainBGM.mp3。");
        }
    }

    public void PlayBGM()
    {
        if (bgmClip == null || bgmSource == null) return;

        if (bgmSource.clip != bgmClip)
        {
            bgmSource.clip = bgmClip;
        }
        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    //停止背景音
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null) bgmSource.volume = Mathf.Clamp01(volume);
    }
}
