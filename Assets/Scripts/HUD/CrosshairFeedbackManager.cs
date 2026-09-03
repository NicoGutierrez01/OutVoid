using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    [Header("Izquierda - Alertas (Negativo)")]
    public GameObject iconMinusMagazine;
    public GameObject iconLowAmmo;
    public GameObject iconLastMagazine;
    public float warningDuration = 1.5f;

    [Header("Derecha - Recompensas (Positivo)")]
    public GameObject iconPlusHealth;
    public GameObject iconPlusShield;
    public GameObject iconPlusBullets;
    public GameObject iconPlusMagazine;
    public float rewardDuration = 1.5f;

    public enum WarningType { MinusMagazine, LowAmmo, LastMagazine }
    public enum RewardType { Health, Shield, Bullets, Magazine }

    private Coroutine centralCoroutine;
    private Coroutine warningCoroutine;
    private Coroutine rewardCoroutine;

    private Vector2 skullPosicionInicial;
    private RectTransform skullRectTransform;

    void Start()
    {
        if (crosshairImage != null) crosshairImage.color = colorDefault;
        
        if (skullImage != null)
        {
            skullRectTransform = skullImage.rectTransform;
            skullPosicionInicial = skullRectTransform.anchoredPosition;
            skullImage.gameObject.SetActive(false);
        }

        ApagarAlertas();
        ApagarRecompensas();
    }

    public void OnTargetHit(bool isHeadshot = false)
    {
        if (centralCoroutine != null) StopCoroutine(centralCoroutine);
        centralCoroutine = StartCoroutine(RutinaFeedbackHit(isHeadshot));
    }

    public void OnEnemyKill(bool isHeadshot = false)
    {
        if (centralCoroutine != null) StopCoroutine(centralCoroutine);
        centralCoroutine = StartCoroutine(RutinaFeedbackKill(isHeadshot));
    }

    private IEnumerator RutinaFeedbackHit(bool isHeadshot)
    {
        Color colorActivo = isHeadshot ? colorHeadshot : colorHit;
        if (crosshairImage != null) crosshairImage.color = colorActivo;

        yield return new WaitForSeconds(centralFeedbackDuration * 0.5f);

        if (crosshairImage != null) crosshairImage.color = colorDefault;
    }

    private IEnumerator RutinaFeedbackKill(bool isHeadshot)
    {
        Color colorActivo = isHeadshot ? colorHeadshot : colorHit;

        if (crosshairImage != null) crosshairImage.color = colorActivo;

        if (skullImage != null && skullRectTransform != null)
        {
            skullImage.sprite = isHeadshot ? spriteCalaveraHeadshot : spriteCalaveraNormal;
            skullRectTransform.anchoredPosition = skullPosicionInicial;
            skullImage.gameObject.SetActive(true);

            Vector2 posicionDestino = skullPosicionInicial + Vector2.down * distanciaCaida;
            float tiempoPasado = 0f;

            while (tiempoPasado < centralFeedbackDuration)
            {
                tiempoPasado += Time.deltaTime;
                float t = tiempoPasado / centralFeedbackDuration;

                skullRectTransform.anchoredPosition = Vector2.Lerp(skullPosicionInicial, posicionDestino, t);

                Color c = colorActivo;
                c.a = Mathf.Lerp(1f, 0f, t);
                skullImage.color = c;

                yield return null;
            }

            skullImage.gameObject.SetActive(false);
            skullRectTransform.anchoredPosition = skullPosicionInicial;
        }
        else
        {
            yield return new WaitForSeconds(centralFeedbackDuration);
        }

        if (crosshairImage != null) crosshairImage.color = colorDefault;
    }

    public void ShowWarning(WarningType type)
    {
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(RutinaWarning(type));
    }

    private IEnumerator RutinaWarning(WarningType type)
    {
        ApagarAlertas();
        if (type == WarningType.MinusMagazine && iconMinusMagazine != null) iconMinusMagazine.SetActive(true);
        else if (type == WarningType.LowAmmo && iconLowAmmo != null) iconLowAmmo.SetActive(true);
        else if (type == WarningType.LastMagazine && iconLastMagazine != null) iconLastMagazine.SetActive(true);
        
        yield return new WaitForSeconds(warningDuration);
        ApagarAlertas();
    }

    private void ApagarAlertas()
    {
        if (iconMinusMagazine != null) iconMinusMagazine.SetActive(false);
        if (iconLowAmmo != null) iconLowAmmo.SetActive(false);
        if (iconLastMagazine != null) iconLastMagazine.SetActive(false);
    }

    public void ShowReward(RewardType type)
    {
        if (rewardCoroutine != null) StopCoroutine(rewardCoroutine);
        rewardCoroutine = StartCoroutine(RutinaReward(type));
    }

    private IEnumerator RutinaReward(RewardType type)
    {
        ApagarRecompensas();
        if (type == RewardType.Health && iconPlusHealth != null) iconPlusHealth.SetActive(true);
        else if (type == RewardType.Shield && iconPlusShield != null) iconPlusShield.SetActive(true);
        else if (type == RewardType.Bullets && iconPlusBullets != null) iconPlusBullets.SetActive(true);
        else if (type == RewardType.Magazine && iconPlusMagazine != null) iconPlusMagazine.SetActive(true);
        
        yield return new WaitForSeconds(rewardDuration);
        ApagarRecompensas();
    }

    private void ApagarRecompensas()
    {
        if (iconPlusHealth != null) iconPlusHealth.SetActive(false);
        if (iconPlusShield != null) iconPlusShield.SetActive(false);
        if (iconPlusBullets != null) iconPlusBullets.SetActive(false);
        if (iconPlusMagazine != null) iconPlusMagazine.SetActive(false);
    }
}