using UnityEngine;

public class StaticVariables : MonoBehaviour
{
    public static class SessionData
    {
        public static int level = 1;
        public static int round = 1;
        public static int time = 0; 

        public static bool win = false;
        public static string avatar = "Default"; 
        public static string weapon = "Revolver";
        public static string enemy = "Ninguno"; 

        public static string itemName = "";
        public static string type = "";
        public static string name = "";
        public static int cost = 0;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}