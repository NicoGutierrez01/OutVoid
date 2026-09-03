using UnityEngine;

public enum RarezaPowerUp { Comun, Rara, Epica, Legendaria }
public enum StatModificado { VidaMaxima, EscudoMaximo, DanoArma, VelocidadRecarga, BalasDeFuego,BalasPenetrantes,DisparoTriple }

[CreateAssetMenu(fileName = "NuevoPowerUp", menuName = "Out-Void/Power Up de Cofre")]
public class PowerUpsChest : ScriptableObject
{
    [Header("Información para la UI")]
    public string nombrePowerUp;
    
    [TextArea(2, 3)] 
    public string descripcion;
    
    [Tooltip("El gráfico 2D que aparecerá en el botón de elección")]
    public Sprite iconoUI;
    
    public RarezaPowerUp rareza;

    [Header("Efecto Mecánico")]
    public StatModificado statAMejorar;
    
    [Tooltip("Cuánto suma o multiplica este stat al elegirlo")]
    public float valorSuma;
}