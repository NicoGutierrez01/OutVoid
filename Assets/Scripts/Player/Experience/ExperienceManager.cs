using UnityEngine;
using System;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instancia;

    [Header("Nivel")]
    public int nivelActual = 1;

    [Header("Experiencia")]
    public float experienciaActual = 0f;
    public float experienciaNecesaria = 100f;

    [Header("Escalado de XP")]
    public float experienciaBase = 100f;
    public float incrementoPorNivel = 50f;

    public event Action<float, float> OnExperienciaCambiada;
    public event Action<int> OnSubioDeNivel;

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        experienciaNecesaria = experienciaBase;

        OnExperienciaCambiada?.Invoke(
            experienciaActual,
            experienciaNecesaria
        );
    }

    public void AgregarExperiencia(float cantidad)
    {
        if (cantidad <= 0)
            return;

        experienciaActual += cantidad;

        while (experienciaActual >= experienciaNecesaria)
        {
            experienciaActual -= experienciaNecesaria;

            nivelActual++;

            experienciaNecesaria =
                experienciaBase +
                ((nivelActual - 1) * incrementoPorNivel);

            OnSubioDeNivel?.Invoke(nivelActual);
        }

        OnExperienciaCambiada?.Invoke(
            experienciaActual,
            experienciaNecesaria
        );
    }

    public float ObtenerProgreso()
    {
        if (experienciaNecesaria <= 0)
            return 0f;

        return experienciaActual / experienciaNecesaria;
    }
}