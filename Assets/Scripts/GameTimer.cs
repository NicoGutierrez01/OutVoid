using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    public static float tiempoTotal = 0f;

    void Update()
    {
        tiempoTotal += Time.deltaTime;

        int horas = (int)tiempoTotal / 3600;
        int minutos = (int)tiempoTotal / 60 % 60;
        int segundos = (int)tiempoTotal % 60;

        if (horas > 0)
        {
            timerText.text = horas.ToString("00") + ":" + minutos.ToString("00") + ":" + segundos.ToString("00");
        }
        else if (minutos > 0)
        {
            timerText.text = minutos.ToString("00") + ":" + segundos.ToString("00");
        }
        else
        {
            timerText.text = segundos.ToString("00");
        }
    }
}