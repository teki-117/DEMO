using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public AudioMixer audioMixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup voiceGroup;

    [Tooltip("战斗音效走的混音组（可选）。留空时直接输出，受主音量控制。")]
    public AudioMixerGroup sfxGroup;

    private AudioSource musicSource;
    private AudioSource voiceSource;
    private AudioSource sfxSource;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.loop = true;

            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.outputAudioMixerGroup = voiceGroup;
            voiceSource.loop = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.outputAudioMixerGroup = sfxGroup != null ? sfxGroup : null;
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            LoadVolumeSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == Constants.MENU_SCENE)
        {
            PlayBackground(Constants.MENU_MUSIC_FILE_NAME);
        }
        else if (scene.name == Constants.GAME_SCENE)
        {
            string lastMusic = GameManager.Instance.currentBackgroundMusic;

            if (!string.IsNullOrEmpty(lastMusic))
            {
                PlayBackground(lastMusic);
            }
        }
    }
    public void PlayBackground(string musicFileName)
    {
        AudioClip clip = Resources.Load<AudioClip>(Constants.MUSIC_PATH + musicFileName);
        if (clip == null)
        {
            Debug.LogError(Constants.AUDIO_LOAD_FAILED + musicFileName);
            return;
        }
        if (musicSource.clip == clip)
        {
            return;
        }
        musicSource.clip = clip;
        musicSource.Play();
    }
    public void PlayVoice(string voiceFileName)
    {
        AudioClip clip = Resources.Load<AudioClip>(Constants.VOCAL_PATH + voiceFileName);

        if (clip == null)
        {
            Debug.LogError(Constants.AUDIO_LOAD_FAILED + voiceFileName);
            return;
        }

        voiceSource.clip = clip;
        voiceSource.Play();
    }
    // 战斗音效：可以重叠播放，clip 为空时什么都不做。
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void LoadVolumeSettings()
    {
        float m = PlayerPrefs.GetFloat(Constants.MASTER_VOLUME, Constants.DEFAULT_VOLUME);
        float mu = PlayerPrefs.GetFloat(Constants.MUSIC_VOLUME, Constants.DEFAULT_VOLUME);
        float v = PlayerPrefs.GetFloat(Constants.VOICE_VOLUME, Constants.DEFAULT_VOLUME);

        audioMixer.SetFloat(Constants.MASTER_VOLUME, SliderToDecibel(m));
        audioMixer.SetFloat(Constants.MUSIC_VOLUME, SliderToDecibel(mu));
        audioMixer.SetFloat(Constants.VOICE_VOLUME, SliderToDecibel(v));
    }

    private float SliderToDecibel(float value)
    {
        return value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
    }
}