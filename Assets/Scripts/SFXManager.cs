using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    private AudioSource audioSource;

    public AudioClip shoot;
    public AudioClip reload;
    public AudioClip dash;
    public AudioClip explosion;
    public AudioClip hit;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void PlayExplosion()
    {
        if (Instance == null) return;
        if (explosion == null) return;

        audioSource.PlayOneShot(explosion);
    }

    public void PlayShoot()
    {
        if (Instance == null) return;
        if (shoot == null) return;

        audioSource.PlayOneShot(shoot);
    }

    public void PlayReload()
    {
        if (Instance == null) return;
        if (reload == null) return;

        audioSource.PlayOneShot(reload);
    }

    public void PlayDash()
    {
        if (Instance == null) return;
        if (dash == null) return;

        audioSource.PlayOneShot(dash);
    }

    public void PlayHit()
    {
        if (Instance == null) return;
        if (hit == null) return;

        audioSource.PlayOneShot(hit);
    }
}