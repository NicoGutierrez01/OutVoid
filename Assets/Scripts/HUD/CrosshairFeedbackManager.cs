using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairFeedbackManager : MonoBehaviour
{
    [Header("Centro - Mira y Calavera")]
    public Image crosshairImage;
    public Image skullImage; 
    
    [Space]
    public Color colorDefault = Color.white;
    public Color colorHit = Color.red;
    public Color colorHeadshot = Color.yellow;
    public float centralFeedbackDuration = 0.3f;

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

    private Coroutine centralCoroutine;
    private bool isShowingHeadshot = false;

    private void Start()
    {
        // Estado inicial
        crosshairImage.color = colorDefault;
        if (skullImage != null) skullImage.gameObject.SetActive(false);
        
        HideAllPopups();
    }

    public void OnTargetHit()
    {
        if (isShowingHeadshot) return; 
        TriggerCentralFeedback(colorHit, false, false);
    }

    public void OnEnemyKill(bool isHeadshot)
    {
        if (isHeadshot)
        {
            TriggerCentralFeedback(colorHeadshot, true, true);
        }
        else
        {
            if (!isShowingHeadshot) 
            {
                TriggerCentralFeedback(colorHit, true, false);
            }
        }
    }

    private void TriggerCentralFeedback(Color color, bool showSkull, bool headshotLock)
    {
        if (centralCoroutine != null) StopCoroutine(centralCoroutine);
        centralCoroutine = StartCoroutine(CentralFeedbackRoutine(color, showSkull, headshotLock));
    }

    private IEnumerator CentralFeedbackRoutine(Color color, bool showSkull, bool headshotLock)
    {
        isShowingHeadshot = headshotLock;
        
        crosshairImage.color = color;
        if (skullImage != null)
        {
            skullImage.gameObject.SetActive(showSkull);
            skullImage.color = color;
        }

        yield return new WaitForSeconds(centralFeedbackDuration);

        crosshairImage.color = colorDefault;
        if (skullImage != null) skullImage.gameObject.SetActive(false);
        isShowingHeadshot = false;
    }

    public enum WarningType { MinusMagazine, LowAmmo, LastMagazine }
    public enum RewardType { Health, Shield, Bullets, Magazine }

    public void ShowWarning(WarningType type)
    {
        GameObject iconToShow = type switch
        {
            WarningType.MinusMagazine => iconMinusMagazine,
            WarningType.LowAmmo => iconLowAmmo,
            WarningType.LastMagazine => iconLastMagazine,
            _ => null
        };

        if (iconToShow != null) StartCoroutine(ShowPopupRoutine(iconToShow, warningDuration));
    }

    public void ShowReward(RewardType type)
    {
        GameObject iconToShow = type switch
        {
            RewardType.Health => iconPlusHealth,
            RewardType.Shield => iconPlusShield,
            RewardType.Bullets => iconPlusBullets,
            RewardType.Magazine => iconPlusMagazine,
            _ => null
        };

        if (iconToShow != null) StartCoroutine(ShowPopupRoutine(iconToShow, rewardDuration));
    }

    private IEnumerator ShowPopupRoutine(GameObject popupIcon, float duration)
    {
        popupIcon.SetActive(true);
        yield return new WaitForSeconds(duration);
        popupIcon.SetActive(false);
    }

    private void HideAllPopups()
    {
        iconMinusMagazine.SetActive(false);
        iconLowAmmo.SetActive(false);
        iconLastMagazine.SetActive(false);
        
        iconPlusHealth.SetActive(false);
        iconPlusShield.SetActive(false);
        iconPlusBullets.SetActive(false);
        iconPlusMagazine.SetActive(false);
    }
}