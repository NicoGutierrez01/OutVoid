using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenAnimator : MonoBehaviour
{
    [Header("Sprites de la Secuencia")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 4f; // Velocidad de cambio

    private Image imageComponent;
    private int currentIndex = 0;
    private float timer = 0f;

    private void Awake()
    {
        imageComponent = GetComponent<Image>();
    }

    private void OnEnable()
    {
        currentIndex = 0;
        timer = 0f;

        if (imageComponent != null && frames != null && frames.Length > 0)
        {
            imageComponent.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || imageComponent == null) return;

        timer += Time.unscaledDeltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % frames.Length;
            imageComponent.sprite = frames[currentIndex];
        }
    }
}