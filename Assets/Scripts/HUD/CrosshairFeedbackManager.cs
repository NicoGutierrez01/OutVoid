using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CrosshairFeedbackManager : MonoBehaviour
{
    [Header("Centro - Mira y Calavera")]
    public Image crosshairImage;
    public Image skullImage;

    public Sprite spriteCalaveraNormal;
    public Sprite spriteCalaveraHeadshot;

    public Color colorDefault = Color.white;
    public Color colorHit = Color.red;
    public Color colorHeadshot = Color.yellow;
    public float centralFeedbackDuration = 0.35f;

    [Header("Animación de Kill Icon")]
    [Tooltip("Distancia en píxeles que desciende el icono mientras desaparece")]
    public float distanciaCaida = 35f;

    [Header("Animación de Popups Laterales (Subida)")]
    [Tooltip("Distancia en píxeles que ascienden los íconos laterales mientras desaparecen")]
    public float distanciaSubida = 35f;

    [Header("Izquierda - Alertas (Negativo)")]
    public Image iconMinusMagazine;
    public Image iconLowAmmo;
    public float warningDuration = 1.0f;

    [Header("Derecha - Recompensas (Positivo)")]
    public Image iconPlusHealth;
    public Image iconPlusShield;
    public Image iconPlusBullets;
    public Image iconPlusMagazine;
    public float rewardDuration = 1.0f;

    public enum WarningType { MinusMagazine, LowAmmo }
    public enum RewardType { Health, Shield, Bullets, Magazine }

    private Coroutine hitCoroutine;
    private Coroutine killCoroutine;

    private Vector2 skullPosicionInicial;
    private RectTransform skullRectTransform;

    // Manejador por cada popup individual para evitar bugs al activar múltiples
    private class PopupItem
    {
        public Image image;
        public RectTransform rectTransform;
        public Vector2 posicionInicial;
        public Color colorOriginal;
        public Coroutine corrutinaActiva;
    }

    private Dictionary<WarningType, PopupItem> itemsAlerta = new Dictionary<WarningType, PopupItem>();
    private Dictionary<RewardType, PopupItem> itemsRecompensa = new Dictionary<RewardType, PopupItem>();

    void Awake()
    {
        if (skullImage != null)
        {
            skullRectTransform = skullImage.rectTransform;
            skullPosicionInicial = skullRectTransform.anchoredPosition;
        }

        RegistrarPopup(WarningType.MinusMagazine, iconMinusMagazine, itemsAlerta);
        RegistrarPopup(WarningType.LowAmmo, iconLowAmmo, itemsAlerta);

        RegistrarPopup(RewardType.Health, iconPlusHealth, itemsRecompensa);
        RegistrarPopup(RewardType.Shield, iconPlusShield, itemsRecompensa);
        RegistrarPopup(RewardType.Bullets, iconPlusBullets, itemsRecompensa);
        RegistrarPopup(RewardType.Magazine, iconPlusMagazine, itemsRecompensa);
    }

    private void RegistrarPopup<TKey>(TKey key, Image img, Dictionary<TKey, PopupItem> dict)
    {
        if (img == null) return;

        PopupItem item = new PopupItem
        {
            image = img,
            rectTransform = img.rectTransform,
            posicionInicial = img.rectTransform.anchoredPosition,
            colorOriginal = img.color,
            corrutinaActiva = null
        };

        dict[key] = item;
    }

    void Start()
    {
        if (crosshairImage != null) crosshairImage.color = colorDefault;
        ApagarCalavera();
        ApagarTodosLosPopups();
    }

    void OnDisable()
    {
        ApagarCalavera();
        ApagarTodosLosPopups();
        if (crosshairImage != null) crosshairImage.color = colorDefault;
    }

    #region Mira y Calavera (Kill / Hit)
    public void OnTargetHit(bool isHeadshot = false)
    {
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);
        hitCoroutine = StartCoroutine(RutinaFeedbackHit(isHeadshot));
    }

    public void OnEnemyKill(bool isHeadshot = false)
    {
        if (killCoroutine != null)
        {
            StopCoroutine(killCoroutine);
            ApagarCalavera();
        }
        killCoroutine = StartCoroutine(RutinaFeedbackKill(isHeadshot));
    }

    private IEnumerator RutinaFeedbackHit(bool isHeadshot)
    {
        Color colorActivo = isHeadshot ? colorHeadshot : colorHit;
        if (crosshairImage != null) crosshairImage.color = colorActivo;

        yield return new WaitForSeconds(centralFeedbackDuration * 0.5f);

        if (crosshairImage != null) crosshairImage.color = colorDefault;
        hitCoroutine = null;
    }

    private IEnumerator RutinaFeedbackKill(bool isHeadshot)
    {
        Color colorActivo = isHeadshot ? colorHeadshot : colorHit;

        if (crosshairImage != null) crosshairImage.color = colorActivo;

        if (skullImage != null && skullRectTransform != null)
        {
            skullImage.sprite = isHeadshot ? spriteCalaveraHeadshot : spriteCalaveraNormal;
            skullRectTransform.anchoredPosition = skullPosicionInicial;
            
            Color cInicial = colorActivo;
            cInicial.a = 1f;
            skullImage.color = cInicial;
            skullImage.gameObject.SetActive(true);

            Vector2 posicionDestino = skullPosicionInicial + Vector2.down * distanciaCaida;
            float tiempoPasado = 0f;

            while (tiempoPasado < centralFeedbackDuration)
            {
                tiempoPasado += Time.deltaTime;
                float t = Mathf.Clamp01(tiempoPasado / centralFeedbackDuration);

                skullRectTransform.anchoredPosition = Vector2.Lerp(skullPosicionInicial, posicionDestino, t);

                Color c = colorActivo;
                c.a = Mathf.Lerp(1f, 0f, t);
                skullImage.color = c;

                yield return null;
            }

            ApagarCalavera();
        }
        else
        {
            yield return new WaitForSeconds(centralFeedbackDuration);
        }

        if (crosshairImage != null) crosshairImage.color = colorDefault;
        killCoroutine = null;
    }

    private void ApagarCalavera()
    {
        if (skullImage != null)
        {
            Color c = skullImage.color;
            c.a = 0f;
            skullImage.color = c;
            skullImage.gameObject.SetActive(false);
        }
        if (skullRectTransform != null)
        {
            skullRectTransform.anchoredPosition = skullPosicionInicial;
        }
    }
    #endregion

    #region Popups Flotantes (Ascenso y Fade Out)
    public void ShowWarning(WarningType type)
    {
        if (itemsAlerta.TryGetValue(type, out PopupItem item))
        {
            LanzarAnimacionPopup(item, warningDuration);
        }
    }

    public void ShowReward(RewardType type)
    {
        if (itemsRecompensa.TryGetValue(type, out PopupItem item))
        {
            LanzarAnimacionPopup(item, rewardDuration);
        }
    }

    private void LanzarAnimacionPopup(PopupItem item, float duracion)
    {
        if (item.corrutinaActiva != null)
        {
            StopCoroutine(item.corrutinaActiva);
        }
        item.corrutinaActiva = StartCoroutine(RutinaFlotacionAscendente(item, duracion));
    }

    private IEnumerator RutinaFlotacionAscendente(PopupItem item, float duracion)
    {
        // 1. Restaurar posición base y opacidad completa
        item.rectTransform.anchoredPosition = item.posicionInicial;
        Color c = item.colorOriginal;
        c.a = 1f;
        item.image.color = c;
        item.image.gameObject.SetActive(true);

        // 2. Destino hacia ARRIBA
        Vector2 posicionDestino = item.posicionInicial + Vector2.up * distanciaSubida;
        float tiempoPasado = 0f;

        while (tiempoPasado < duracion)
        {
            tiempoPasado += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoPasado / duracion);

            item.rectTransform.anchoredPosition = Vector2.Lerp(item.posicionInicial, posicionDestino, t);

            c.a = Mathf.Lerp(1f, 0f, t);
            item.image.color = c;

            yield return null;
        }

        // 3. Apagado y reseteo definitivo
        item.image.gameObject.SetActive(false);
        item.rectTransform.anchoredPosition = item.posicionInicial;
        c.a = 0f;
        item.image.color = c;
        item.corrutinaActiva = null;
    }

    private void ApagarTodosLosPopups()
    {
        foreach (var item in itemsAlerta.Values)
        {
            if (item.corrutinaActiva != null) StopCoroutine(item.corrutinaActiva);
            item.corrutinaActiva = null;
            item.image.gameObject.SetActive(false);
            item.rectTransform.anchoredPosition = item.posicionInicial;
        }

        foreach (var item in itemsRecompensa.Values)
        {
            if (item.corrutinaActiva != null) StopCoroutine(item.corrutinaActiva);
            item.corrutinaActiva = null;
            item.image.gameObject.SetActive(false);
            item.rectTransform.anchoredPosition = item.posicionInicial;
        }
    }
    #endregion
}