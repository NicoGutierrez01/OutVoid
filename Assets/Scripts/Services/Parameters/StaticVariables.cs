using UnityEngine;

public class StaticVariables : MonoBehaviour
{
    // Clase estática para mantener los datos durante toda la sesión
    public static class SessionData
    {
        // Parámetros de progresión (LevelStartEvent / LevelCompleteEvent)
        public static int level = 1;
        public static int round = 1;
        public static int time = 0; // Podés medirlo en segundos

        // Parámetros de fin de partida (GameOverEvent)
        public static bool win = false;
        public static string chara = "Default"; // Personaje actual
        public static string weapon = "Revolver"; // Arma principal (ej: inicial)

        // Parámetros de interacción (ItemPickEvent / unLockEvent)
        // Generalmente estos se envían en el momento, pero es buena práctica 
        // tenerlos acá si necesitás cachearlos antes de mandar el evento.
        public static string item = "";
        public static string type = "";
        public static string name = "";
        public static int cost = 0;
    }

    private void Awake()
    {
        // Esto asegura que el GameObject que tenga este script no se destruya al cambiar de escena
        DontDestroyOnLoad(gameObject);
    }
}