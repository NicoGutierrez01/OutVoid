using UnityEngine;

public enum RarezaMejora { Comun, Rara, Epica }
public enum TipoStat { VidaMaxima, Armadura, Velocidad, Recarga, DobleSalto }

[CreateAssetMenu(fileName = "NuevaMejora", menuName = "Out-Void/Mejora Menor")]
public class MejoraData : ScriptableObject
{
    [Header("Información UI")]
    public string nombreMejora;
    [TextArea] public string descripcion;
    public RarezaMejora rareza;

    [Header("Visuales (Low Poly)")]
    public GameObject prefabVisual;
    public Color colorBaliza = Color.white;

    [Header("Efecto Mecánico")]
    public TipoStat statAMejorar;
    public float valorSuma;
}