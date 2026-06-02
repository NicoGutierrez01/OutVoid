using UnityEngine;
using System;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine.UnityConsent;

public class Services : MonoBehaviour
{
    // Asegúrate de que MainMenu exista o tenga su 'using' correspondiente
    [SerializeField] private MainMenu sceneController; 

    async void Awake()
    {
        try
        {
            // Inicializamos los servicios base lo antes posible
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
        
        Debug.Log("Analíticas activadas: Permiso concedido.");
        
        // Ahora vamos al Menú Principal en lugar de EmpezarJuego
        sceneController.MostrarMenuPrincipal(); 
    }

    public void StopDataCollection()
    {
        EndUserConsent.SetConsentState(new ConsentState { 
            AnalyticsIntent = ConsentStatus.Denied 
        });

        Debug.Log("Analíticas desactivadas: Permiso rechazado.");
        
        // Ahora vamos al Menú Principal en lugar de EmpezarJuego
        sceneController.MostrarMenuPrincipal();
    }
}