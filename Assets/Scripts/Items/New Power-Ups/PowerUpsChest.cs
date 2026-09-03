using UnityEngine;

public enum RarezaPowerUp { Comun, Rara, Epica, Legendaria }
public enum StatModificado 
{ 
    VidaMaxima, 
    EscudoMaximo, 
    DanoArma, 
    VelocidadRecarga, 
    BalasDeFuego,
    BalasPenetrantes,
    DisparoTriple
}

[CreateAssetMenu(fileName = "NuevoPowerUp", menuName = "Out-Void/Power Up de Cofre")]
public class PowerUpsChest : ScriptableObject
{
    [Header("Información para la UI")]
    public string nombrePowerUp;
    
    [TextArea(2, 3)] 
    public string descripcion;
    
    [Tooltip("El gráfico del icono central")]
    public Sprite iconoUI;

    [Tooltip("La textura del marco con el color o diseño de su rareza")]
    public Sprite marcoUI;
    
    public RarezaPowerUp rareza;

    [Header("Efecto Mecánico")]
    public StatModificado statAMejorar;
    public float valorSuma;
}