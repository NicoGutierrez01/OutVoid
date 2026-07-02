using UnityEngine;
using System;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine.UnityConsent;

public class Services : MonoBehaviour
{
    [SerializeField] private MainMenu sceneController; 

    async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Servicios de Unity inicializados correctamente.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void StartDataCollection()
    {
        EndUserConsent.SetConsentState(new ConsentState { 
            AnalyticsIntent = ConsentStatus.Granted 
        });

        PlayerPrefs.SetInt("AnalyticsConsent", 1);
        PlayerPrefs.Save();
        
        Debug.Log("Analíticas activadas: Permiso concedido.");
        sceneController.MostrarMenuPrincipal(); 
    }

    public void StopDataCollection()
    {
        EndUserConsent.SetConsentState(new ConsentState { 
            AnalyticsIntent = ConsentStatus.Denied 
        });

        PlayerPrefs.SetInt("AnalyticsConsent", 0);
        PlayerPrefs.Save();

        Debug.Log("Analíticas desactivadas: Permiso rechazado.");
        sceneController.MostrarMenuPrincipal();
    }
}