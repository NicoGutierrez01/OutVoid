using UnityEngine;

public enum RarezaItem { Comun, Raro, Epico, Legendario }
public enum TipoEfecto { Daño, Velocidad, DobleSalto, EscudoMax, BalasFuego, CooldownDash } 

[CreateAssetMenu(fileName = "NuevaMejora", menuName = "Juego/Item de Mejora")]
public class ItemMejoraData : ScriptableObject
{
    [Header("Info")]
    public string nombreItem;
    [TextArea(2, 3)] public string descripcion;
    
    [Header("Propiedades")]
    public RarezaItem rareza;
    public int nivelMaximo = 3; 
    public TipoEfecto efecto;
    

    [Header("Visual Provisonal")]
    public Color colorProvisorio = Color.white;

    [Header("Visual")]
    public GameObject mallaEspecial;
}