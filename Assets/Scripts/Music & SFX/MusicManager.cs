using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource audioSource;      // MUSICA
    private AudioSource sfxSource;       // SFX

    [Header("Clips - MUSIC")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;
    public AudioClip gameOverMusic;

[Header("Clips - SFX")]
public AudioClip shoot;
public AudioClip reload;
public AudioClip jump;
public AudioClip crouch;
public AudioClip walk;
public AudioClip explosion;
public AudioClip ultimate;
public AudioClip dash;

public AudioClip uiClick;
public AudioClip takingDamage;
public AudioClip outOfAmmo;
public AudioClip throwingDynamite;
public AudioClip melee;

    private string currentState = "";

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 🔥 MUSICA
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 🔥 SFX (SEGUNDO AUDIO SOURCE)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        EvaluateScene(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EvaluateScene(scene.name);
    }

    public void EvaluateScene(string sceneName)
    {
        if (sceneName.Contains("Menu"))
        {
            ApplyMusic("Menu", menuMusic);
        }
        else if (sceneName.Contains("Desert") || sceneName.Contains("Gameplay"))
        {
            ApplyMusic("Gameplay", gameplayMusic);
        }
        else if (sceneName.Contains("GameOver"))
        {
            ApplyMusic("GameOver", gameOverMusic);
        }
        else
        {
            ApplyMusic("Menu", menuMusic);
        }
    }

    public void PlayBossMusic()
    {
        ApplyMusic("Boss", bossMusic);
    }

    public void StopBossMusic()
    {
        EvaluateScene(SceneManager.GetActiveScene().name);
    }

    private void ApplyMusic(string state, AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        if (currentState == state && audioSource.isPlaying)
            return;

        currentState = state;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();

        Debug.Log($"[MUSIC] STATE={state} CLIP={clip.name}");
    }
    public void PlayShoot() => PlaySFX(shoot);
public void PlayReload() => PlaySFX(reload);
public void PlayJump() => PlaySFX(jump);
public void PlayCrouch() => PlaySFX(crouch);
public void PlayWalk() => PlaySFX(walk);
public void PlayExplosion() => PlaySFX(explosion);
public void PlayUltimate() => PlaySFX(ultimate);
public void PlayDash() => PlaySFX(dash);
public void PlayUIClick() => PlaySFX(uiClick);
public void PlayTakingDamage() => PlaySFX(takingDamage);
public void PlayOutOfAmmo() => PlaySFX(outOfAmmo);
public void PlayThrowingDynamite() => PlaySFX(throwingDynamite);
public void PlayMelee() => PlaySFX(melee);
private void PlaySFX(AudioClip clip)
{
    if (clip == null || sfxSource == null) return;

    sfxSource.PlayOneShot(clip);
}
}