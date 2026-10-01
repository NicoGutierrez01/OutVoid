using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Consola de desarrollador. MapManager la agrega solo en Editor / Development Build.
// No guarda estado propio: los cheats viven en AdministradorDeProgreso y se limpian con ReiniciarProgreso().
// La UI se arma por código y vive en la escena del mapa, así que se destruye en cada cambio de mapa.
public class DevConsole : MonoBehaviour
{
    // También cuenta como abierta el frame en que se cerró, para que el mismo Esc no abra el menú de pausa.
    public static bool Abierta => abierta || frameCierre == Time.frameCount;
    private static bool abierta;
    private static int frameCierre = -1;

    private const int maxLineas = 200;
    private const string colorOk = "#7CFC7C";
    private const string colorError = "#FF6B6B";
    private const string colorEco = "#9A9AB0";

    private class Comando
    {
        public string sintaxis;
        public Action<string[]> accion;
    }

    private readonly Dictionary<string, Comando> comandos = new Dictionary<string, Comando>();
    private readonly List<string> historial = new List<string>();
    private readonly List<string> lineas = new List<string>();
    private int indiceHistorial;

    private GameObject canvasConsola;
    private TMP_InputField input;
    private TextMeshProUGUI textoLog;
    private ScrollRect scroll;

    private float timeScalePrevio;
    private CursorLockMode cursorPrevio;
    private bool cursorVisiblePrevio;

    void Awake()
    {
        Registrar("help", "help", _ => Ayuda());
        Registrar("clear", "clear", _ => { lineas.Clear(); Refrescar(); });
        Registrar("god", "god", _ => Ok($"Modo dios {OnOff(Progreso().modoDios = !Progreso().modoDios)}"));
        Comando ammo = Registrar("infinite_ammo", "infinite_ammo | ammo",
            _ => Ok($"Munición infinita {OnOff(Progreso().municionInfinita = !Progreso().municionInfinita)}"));
        comandos["ammo"] = ammo;
        Registrar("noclip", "noclip", _ => Ok($"Noclip {OnOff(Progreso().noclip = !Progreso().noclip)}"));
        Registrar("pass", "pass <level|round>", Pasar);
        Registrar("upgrade", "upgrade [nombre]", DarMejora);
        Registrar("give", "give xp <cantidad> | give upgrade <nombre>", Dar);
        Registrar("killall", "killall", _ => MatarTodos());

        CrearUI();
        canvasConsola.SetActive(false);
        Info("Consola de desarrollador. Escribí <b>help</b> para ver los comandos.");
    }

    void OnDestroy()
    {
        if (abierta) Cerrar();
        if (canvasConsola != null) Destroy(canvasConsola);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (!abierta)
        {
            if (kb.pKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame)
                Abrir();
            return;
        }

        // P no cierra: se necesita para escribir (pass, ...).
        if (kb.f1Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
            Cerrar();
        else if (kb.upArrowKey.wasPressedThisFrame)
            NavegarHistorial(-1);
        else if (kb.downArrowKey.wasPressedThisFrame)
            NavegarHistorial(1);
    }

    // =========================================================
    // ABRIR / CERRAR
    // =========================================================

    private void Abrir()
    {
        timeScalePrevio = Time.timeScale;
        cursorPrevio = Cursor.lockState;
        cursorVisiblePrevio = Cursor.visible;

        // timeScale 0 ya corta el input de Player, WeaponSystem y PlayerAbilities.
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        abierta = true;
        canvasConsola.SetActive(true);
        indiceHistorial = historial.Count;
        StartCoroutine(Enfocar(""));
    }

    private void Cerrar()
    {
        abierta = false;
        frameCierre = Time.frameCount;
        if (canvasConsola != null) canvasConsola.SetActive(false);

        // Restaura el estado previo (por si se abrió con el menú de pausa o un cofre abiertos).
        Time.timeScale = timeScalePrevio;
        Cursor.lockState = cursorPrevio;
        Cursor.visible = cursorVisiblePrevio;
    }

    // Un frame de espera: evita que la tecla de apertura quede escrita y que TMP pise el caret.
    private IEnumerator Enfocar(string texto)
    {
        yield return null;
        input.text = texto;
        input.ActivateInputField();
        input.MoveTextEnd(false);
    }

    private void NavegarHistorial(int direccion)
    {
        if (historial.Count == 0) return;

        indiceHistorial = Mathf.Clamp(indiceHistorial + direccion, 0, historial.Count);
        StartCoroutine(Enfocar(indiceHistorial < historial.Count ? historial[indiceHistorial] : ""));
    }

    // =========================================================
    // EJECUCIÓN
    // =========================================================

    private Comando Registrar(string nombre, string sintaxis, Action<string[]> accion)
    {
        Comando comando = new Comando { sintaxis = sintaxis, accion = accion };
        comandos[nombre] = comando;
        return comando;
    }

    private void Ejecutar(string linea)
    {
        StartCoroutine(Enfocar(""));

        linea = linea.Trim();
        if (linea.Length == 0) return;

        historial.Add(linea);
        indiceHistorial = historial.Count;
        Log($"> <noparse>{linea}</noparse>", colorEco);

        string[] partes = linea.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        if (!comandos.TryGetValue(partes[0].ToLowerInvariant(), out Comando comando))
        {
            Error($"Comando desconocido '{partes[0]}'. Escribí help.");
            return;
        }

        try
        {
            comando.accion(partes.Skip(1).ToArray());
        }
        catch (Exception e)
        {
            Error(e.Message);
        }
    }

    // =========================================================
    // COMANDOS
    // =========================================================

    private void Ayuda()
    {
        Info("Comandos disponibles:");
        foreach (Comando c in comandos.Values.Distinct())
            Info("  " + c.sintaxis);
        Info("Abrir: P / F1 / º  ·  Cerrar: F1 / º / Esc  ·  Historial: ↑ ↓");
    }

    private void Pasar(string[] args)
    {
        MapManager mapa = MapManager.Instance;
        if (mapa == null) throw new InvalidOperationException("No hay MapManager en la escena.");

        switch (args.Length > 0 ? args[0].ToLowerInvariant() : "")
        {
            case "level":
                Cerrar();
                mapa.AvanzarSiguienteNivel();
                break;

            case "round":
                if (mapa.rondaActual >= mapa.maxRondas)
                {
                    Error("Ya estás en la ronda del jefe: usá pass level.");
                    return;
                }
                mapa.CompletarObjetivoRonda();
                Ok($"Ronda completada. Ronda actual: {mapa.rondaActual}/{mapa.maxRondas}");
                break;

            default:
                Error("Uso: pass <level|round>");
                break;
        }
    }

    private void Dar(string[] args)
    {
        string tipo = args.Length > 0 ? args[0].ToLowerInvariant() : "";

        if (tipo == "upgrade")
        {
            DarMejora(args.Skip(1).ToArray());
        }
        else if (tipo == "xp" && args.Length > 1 && float.TryParse(args[1], out float cantidad) && cantidad > 0)
        {
            if (ExperienceManager.Instancia == null) throw new InvalidOperationException("No hay ExperienceManager activo.");

            ExperienceManager.Instancia.AgregarExperiencia(cantidad);
            Ok($"+{cantidad} XP. Nivel actual: {ExperienceManager.Instancia.nivelActual}");
        }
        else
        {
            Error("Uso: give xp <cantidad> | give upgrade <nombre>");
        }
    }

    private void DarMejora(string[] args)
    {
        PowerUpUIManager manager = PowerUpUIManager.Instancia;
        if (manager == null) throw new InvalidOperationException("No hay PowerUpUIManager (¿cargó UIScene?).");

        List<PowerUpsChest> pool = manager.poolDeMejoras.Where(p => p != null).ToList();

        if (args.Length == 0)
        {
            Info("Mejoras disponibles:");
            foreach (PowerUpsChest p in pool)
                Info($"  {IdMejora(p)}  ({p.rareza}, {p.statAMejorar})");
            return;
        }

        // Acepta el nombre del asset ("disparo_triple", "pacto de sangre") o del stat ("DisparoTriple").
        string buscado = Normalizar(string.Join(" ", args));
        PowerUpsChest mejora = pool.FirstOrDefault(p => Normalizar(p.name) == buscado)
            ?? pool.FirstOrDefault(p => Normalizar(p.statAMejorar.ToString()) == buscado);

        if (mejora == null)
        {
            Error($"Mejora '{string.Join(" ", args)}' no encontrada. Escribí upgrade para ver la lista.");
            return;
        }

        manager.AplicarEfecto(mejora);
        Ok($"Mejora aplicada: {IdMejora(mejora)}");
    }

    private void MatarTodos()
    {
        int muertos = 0;

        foreach (EnemyHealth enemigo in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            enemigo.TakeDamage(1e9f, false, false);
            muertos++;
        }

        foreach (BossHealth jefe in FindObjectsByType<BossHealth>(FindObjectsSortMode.None))
        {
            if (jefe.isDead) continue;
            jefe.TakeDamage(1e9f);
            muertos++;
        }

        Ok($"Eliminados: {muertos}");
    }

    // =========================================================
    // UTILIDADES
    // =========================================================

    private static AdministradorDeProgreso Progreso()
    {
        if (AdministradorDeProgreso.Instancia == null)
            throw new InvalidOperationException("No hay AdministradorDeProgreso en la escena.");
        return AdministradorDeProgreso.Instancia;
    }

    private static string OnOff(bool valor) => valor ? "ON" : "OFF";

    // "Disparo_Triple(Epica)" -> "disparo_triple"
    private static string IdMejora(PowerUpsChest p) =>
        Regex.Replace(p.name, @"\(.*?\)", "").Trim().Replace(' ', '_').ToLowerInvariant();

    // Minúsculas, sin rareza entre paréntesis, sin tildes ni separadores: "Mas_Daño(Comun)" -> "masdano".
    private static string Normalizar(string texto)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in Regex.Replace(texto, @"\(.*?\)", "").Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private void Ok(string texto) => Log(texto, colorOk);
    private void Error(string texto) => Log(texto, colorError);
    private void Info(string texto) => Log(texto, null);

    private void Log(string texto, string color)
    {
        lineas.Add(color == null ? texto : $"<color={color}>{texto}</color>");
        if (lineas.Count > maxLineas) lineas.RemoveAt(0);
        Refrescar();
    }

    private void Refrescar()
    {
        textoLog.text = string.Join("\n", lineas);

        if (canvasConsola.activeInHierarchy)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    // =========================================================
    // UI (armada por código para no editar YAML de escenas/prefabs)
    // =========================================================

    private void CrearUI()
    {
        Color acento = new Color(0.62f, 0.45f, 1f);

        canvasConsola = new GameObject("DevConsoleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasConsola.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasConsola.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Ventana: franja superior oscura semitransparente (45% de la pantalla).
        RectTransform ventana = CrearImagen("Ventana", canvasConsola.transform, new Color(0.04f, 0.04f, 0.07f, 0.9f));
        ventana.anchorMin = new Vector2(0f, 0.55f);
        ventana.anchorMax = Vector2.one;
        ventana.offsetMin = ventana.offsetMax = Vector2.zero;

        // Borde inferior nítido.
        RectTransform borde = CrearImagen("Borde", ventana, acento);
        borde.anchorMin = Vector2.zero;
        borde.anchorMax = new Vector2(1f, 0f);
        borde.pivot = new Vector2(0.5f, 0f);
        borde.sizeDelta = new Vector2(0f, 3f);

        // Log con scroll (rueda del mouse).
        GameObject scrollGO = DefaultControls.CreateScrollView(new DefaultControls.Resources());
        scrollGO.transform.SetParent(ventana, false);
        scrollGO.GetComponent<Image>().color = Color.clear;
        RectTransform rtScroll = (RectTransform)scrollGO.transform;
        rtScroll.anchorMin = Vector2.zero;
        rtScroll.anchorMax = Vector2.one;
        rtScroll.offsetMin = new Vector2(20f, 70f);
        rtScroll.offsetMax = new Vector2(-20f, -16f);

        scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        Destroy(scroll.horizontalScrollbar.gameObject);
        Destroy(scroll.verticalScrollbar.gameObject);
        scroll.horizontalScrollbar = null;
        scroll.verticalScrollbar = null;

        textoLog = scroll.content.gameObject.AddComponent<TextMeshProUGUI>();
        textoLog.fontSize = 22f;
        textoLog.color = Color.white;
        textoLog.richText = true;
        scroll.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Campo de entrada.
        GameObject inputGO = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        inputGO.transform.SetParent(ventana, false);
        inputGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
        RectTransform rtInput = (RectTransform)inputGO.transform;
        rtInput.anchorMin = Vector2.zero;
        rtInput.anchorMax = new Vector2(1f, 0f);
        rtInput.offsetMin = new Vector2(20f, 14f);
        rtInput.offsetMax = new Vector2(-20f, 58f);

        input = inputGO.GetComponent<TMP_InputField>();
        input.onFocusSelectAll = false;
        input.customCaretColor = true;
        input.caretColor = acento;
        input.caretWidth = 2;
        input.textComponent.fontSize = 24f;
        input.textComponent.color = Color.white;
        input.textComponent.richText = false;

        TextMeshProUGUI placeholder = (TextMeshProUGUI)input.placeholder;
        placeholder.text = "help para ver los comandos";
        placeholder.fontSize = 24f;
        placeholder.color = new Color(1f, 1f, 1f, 0.3f);

        input.onValidateInput = (texto, indice, c) => c == '`' ? '\0' : c;
        input.onSubmit.AddListener(Ejecutar);
    }

    private static RectTransform CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }
}
