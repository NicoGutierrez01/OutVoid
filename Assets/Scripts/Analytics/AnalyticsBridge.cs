using UnityEngine;
using Unity.Services.Analytics;

public static class AnalyticsBridge
{
    public static void EnviarLevelStart(int level, int round)
    {
        AnalyticsService.Instance.RecordEvent(new EventManager.LevelStartEvent { level = level, round = round });
        Debug.Log($"[Analytics] START Enviado -> Lvl: {level}, Rnd: {round}");
    }

    public static void EnviarLevelComplete(int level, int round)
    {
        AnalyticsService.Instance.RecordEvent(new EventManager.LevelCompleteEvent {
            level = level,
            round = round,
            time = Mathf.FloorToInt(GameTimer.tiempoTotal)
        });

        Debug.Log($"[Analytics] COMPLETE Enviado -> Lvl: {level}, Rnd: {round}, Tiempo: {Mathf.FloorToInt(GameTimer.tiempoTotal)}");
    }
}