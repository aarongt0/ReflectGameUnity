using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I
    {
        get
        {
            if (_i != null) return _i;
            _i = FindFirstObjectByType<AudioManager>();
            if (_i != null) return _i;

            // OPTIONAL: load from Resources (AudioManager prefab)
            var prefab = Resources.Load<AudioManager>("AudioManager");
            if (prefab != null) _i = Instantiate(prefab);

            return _i;
        }
    }

    private static AudioManager _i;


    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;  
    [SerializeField] private AudioSource sfxSource;    
    [SerializeField] private AudioSource p1LoopSource;  
    [SerializeField] private AudioSource p2LoopSource;
    [SerializeField] private Logger logger;
    public float pitchJitter = 0.10f;
    public float pitchClamp;

    [SerializeField] float MUSIC_VOLUME = 1f;
    [SerializeField] bool PlayMusic;
    [SerializeField] AudioClip MUSIC_TRACK;
    [SerializeField] AudioClip FIGHT_START;
    [SerializeField] AudioClip PLAYER_DEATH;

    private void Awake()
    {
        if (_i != null && _i != this) { Destroy(gameObject); return; }
        _i = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"AudioManager Awake on {gameObject.scene.name} active={gameObject.activeInHierarchy}");

        if (musicSource)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        else Debug.LogError("AudioManager missing musicSource", this);

        if (sfxSource)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        else Debug.LogError("AudioManager missing sfxSource", this);

        if (p1LoopSource) { p1LoopSource.loop = true; p1LoopSource.playOnAwake = false; }
        if (p2LoopSource) { p2LoopSource.loop = true; p2LoopSource.playOnAwake = false; }
    }

    private void Start()
    {
        if (PlayMusic) StartMusic(MUSIC_TRACK, MUSIC_VOLUME);
    }

    public void StartMusic(AudioClip track, float volume = 1f)
    {
        if (!track) return;
        if (musicSource.clip == track && musicSource.isPlaying) return;

        musicSource.clip = track;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void PlaySfx(AudioClip clip, float volume = 1f, bool changePitch = true)
    {
        if (!clip || !sfxSource) return;

        float oldPitch = sfxSource.pitch;

        if (changePitch)
        {
            // Change pitch slightly of each sfx
            float newPitch = oldPitch + Random.Range(-pitchJitter, pitchJitter);
            sfxSource.pitch = Mathf.Clamp(newPitch, 0.75f, 1.25f);
        }

        sfxSource.PlayOneShot(clip, volume);

        // Restore so other calls have a stable pitch
        sfxSource.pitch = oldPitch;
    }

    public void SetFighterLoop(int fighterIndex, AudioClip loopClip, float volume = 1f, float pitch = 1f)
    {
        AudioSource src = fighterIndex == 1 ? p1LoopSource : p2LoopSource;
        if (!src) return;

        if (loopClip == null)
        {
            src.Stop();
            src.clip = null;
            return;
        }

        if (src.clip != loopClip)
            src.clip = loopClip;

        src.volume = volume;
        src.pitch = pitch;

        if (!src.isPlaying)
            src.Play();
    }

    void OnEnable()
    {
        GlobalHandler.GameStart += HandleGameStart;
        PlayerRespawn.PlayerDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        GlobalHandler.GameStart -= HandleGameStart;
        PlayerRespawn.PlayerDeath -= HandlePlayerDeath;
    }

    private void HandleGameStart()
    {
        PlaySfx(FIGHT_START, 0.5f);
    }
    private void HandlePlayerDeath()
    {
        PlaySfx(PLAYER_DEATH, 0.5f);
    }
}
