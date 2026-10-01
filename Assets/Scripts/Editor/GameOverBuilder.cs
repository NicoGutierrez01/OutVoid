using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static MainMenuBuilder;

// Arma la pantalla de fin de run en la escena GameOver abierta (menú Out-Void > Construir Game Over).
// Reusa el botón "Back" existente (onClick a GameOver.VolverAlMenu + UIButtonSFX) con BotonMenuFX.
// Se puede volver a correr (reemplaza lo generado) y deshacer con Ctrl+Z. Guardar la escena después.
// Ojo: apaga todo hijo del Canvas que no genere él (la UI vieja); para cambiar el layout, editar este builder.
// Los datos los completa GameOver.cs en runtime; acá solo se arma el layout y se asignan las referencias.
//
// Capas de render (mismo patrón que el Main Menu): CanvasFondo (Screen Space - Camera, detrás)
// < partículas 3D (VFXGameOver) < Canvas principal (Overlay, delante).
public static class GameOverBuilder
{
    private const string carpeta = "Assets/HUD/GameOver/";
    private const string rutaFondo = "Assets/HUD/Gameplay/derrota.png";
    private const string rutaIconoTiempo = "Assets/HUD/Gameplay/New Game/Objetive/hud-reloj.png";
    private const string rutaAnton = "Assets/TextMesh Pro/Examples & Extras/Fonts/Anton.ttf";
    private const string rutaFuenteTitulo = carpeta + "Anton GameOver SDF.asset";
    private const string rutaMaterialParticulas =
        "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Materials/Embers.mat";

    private const float distanciaFondo = 100f;
    private const float distanciaParticulas = 60f;

    private static readonly Color violetaNeon = Hex("#9B5DE5");
    private static readonly Color violetaBrillante = Hex("#B388FF");
    private static readonly Color lavanda = Hex("#C4B5FD");
    private static readonly Color claro = Hex("#F3EDFF");
    // Vidrio oscuro con tinte violeta: translúcido para que el desierto se intuya detrás, con contraste para el texto.
    private static readonly Color vidrio = new Color(0.07f, 0.03f, 0.14f, 0.66f);

    // Decoración 3D de la versión anterior: quedaría delante del fondo, se apaga (no se borra).
    private static readonly string[] decoracionVieja = { "Background", "RobotsPuntaje", "SueloMenu", "HitEffect", "w" };

    [MenuItem("Out-Void/Construir Game Over")]
    public static void Construir()
    {
        if (SceneManager.GetActiveScene().name != "GameOver")
        {
            Debug.LogError("[GameOverBuilder] Abrí la escena GameOver primero.");
            return;
        }

        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.isRootCanvas && c.name == "Canvas");
        Camera camara = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault();
        GameOver gameOver = Object.FindFirstObjectByType<GameOver>(FindObjectsInactive.Include);
        Button boton = canvas != null ? canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "Back") : null;
        if (canvas == null || camara == null || gameOver == null || boton == null)
        {
            Debug.LogError("[GameOverBuilder] Falta el Canvas, la cámara, el componente GameOver o el botón 'Back'.");
            return;
        }

        Undo.SetCurrentGroupName("Construir Game Over");
        int grupoUndo = Undo.GetCurrentGroup();

        TMP_FontAsset fuente = TMP_Settings.defaultFontAsset;
        TMP_FontAsset fuenteTitulo = CrearFuenteTitulo(fuente);
        Sprite fondo = CargarSprite(rutaFondo, Vector4.zero);
        Sprite brillo = CrearBrilloRadial();
        Sprite relleno = Generar("panel_relleno.png", 128, 128, new Vector4(30, 30, 30, 30), RellenoAchaflanado);
        Sprite borde = Generar("panel_borde.png", 128, 128, new Vector4(40, 40, 40, 40), BordeAchaflanado);
        Sprite lineaH = Generar("linea_h.png", 256, 4, Vector4.zero, (p, w, h) => Mathf.Pow(Mathf.Sin(Mathf.PI * p.x / w), 0.8f));
        Sprite lineaV = Generar("linea_v.png", 4, 256, Vector4.zero, (p, w, h) => Mathf.Pow(Mathf.Sin(Mathf.PI * p.y / h), 0.8f));
        Sprite degradado = Generar("degradado_vertical.png", 4, 128, Vector4.zero, (p, w, h) => Mathf.SmoothStep(0f, 1f, p.y / h));
        Sprite rombo = Generar("rombo.png", 32, 32, Vector4.zero, (p, w, h) => Supermuestreo(p, w, u => Mathf.Abs(u.x) + Mathf.Abs(u.y) < 0.9f));
        Material materialParticulas = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterialParticulas);

        // --- Limpieza de una construcción anterior (el botón se saca antes de borrar su contenedor) ---
        Undo.SetTransformParent(boton.transform, canvas.transform, "Reubicar botón");
        foreach (string hijo in new[] { "Marco", "Texto", "Fondo", "BordeBase", "HaloBoton" }) DestruirHijo(boton.transform, hijo);
        foreach (string hijo in new[] { "FondoDerrota", "Oscurecer", "PantallaDerrota", "PistasEntrada" }) DestruirHijo(canvas.transform, hijo);
        DestruirRaiz("CanvasFondo");
        DestruirRaiz("VFXGameOver");

        // UI vieja (imagen con AutoSelectOnEnable y textos sueltos): se apaga, el layout nuevo la reemplaza.
        foreach (Transform hijo in canvas.transform.Cast<Transform>().ToArray())
        {
            if (hijo == boton.transform) continue;
            Undo.RecordObject(hijo.gameObject, "Ocultar UI vieja");
            hijo.gameObject.SetActive(false);
        }
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => decoracionVieja.Contains(g.name)))
        {
            Undo.RecordObject(raiz, "Ocultar decoración 3D");
            raiz.SetActive(false);
        }

        // Match por alto: el contenido es una columna vertical arriba-centro y así no cambia de lugar con el ancho.
        CanvasScaler scaler = ObtenerOAgregar<CanvasScaler>(canvas.gameObject);
        ConfigurarScaler(scaler);
        scaler.matchWidthOrHeight = 1f;

        // --- Fondo en su propio canvas en espacio de cámara, para poder dibujar partículas encima ---
        GameObject canvasFondoGO = new GameObject("CanvasFondo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasFondoGO, "Crear CanvasFondo");
        canvasFondoGO.layer = canvas.gameObject.layer;
        Canvas canvasFondo = canvasFondoGO.GetComponent<Canvas>();
        canvasFondo.renderMode = RenderMode.ScreenSpaceCamera;
        canvasFondo.worldCamera = camara;
        canvasFondo.planeDistance = distanciaFondo;
        canvasFondo.sortingOrder = -100;
        ConfigurarScaler(canvasFondoGO.GetComponent<CanvasScaler>());

        // Envelope: cubre 16:9, 21:9 y 4:3 sin deformar (recorta bordes).
        RectTransform fondoRT = CrearUI("FondoDerrota", canvasFondoGO.transform);
        Estirar(fondoRT);
        Image imgFondo = fondoRT.gameObject.AddComponent<Image>();
        imgFondo.sprite = fondo;
        imgFondo.raycastTarget = false;
        if (fondo != null)
        {
            AspectRatioFitter fitter = fondoRT.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = fondo.rect.width / fondo.rect.height;
        }

        // Velo violeta solo arriba (detrás del título y el panel); abajo el vaquero queda limpio.
        RectTransform velo = CrearUI("VeloSuperior", canvasFondoGO.transform);
        velo.anchorMin = new Vector2(0f, 0.35f);
        velo.anchorMax = Vector2.one;
        velo.offsetMin = velo.offsetMax = Vector2.zero;
        Image imgVelo = velo.gameObject.AddComponent<Image>();
        imgVelo.sprite = degradado;
        imgVelo.color = new Color(0.06f, 0.02f, 0.12f, 0.55f);
        imgVelo.raycastTarget = false;

        // --- Contenido: columna arriba-centro ---
        RectTransform pantalla = CrearUI("PantallaDerrota", canvas.transform);
        Estirar(pantalla);
        Vertical(pantalla, 12f);
        VerticalLayoutGroup vPantalla = pantalla.GetComponent<VerticalLayoutGroup>();
        vPantalla.childAlignment = TextAnchor.UpperCenter;
        vPantalla.padding = new RectOffset(0, 0, 26, 0);
        Undo.AddComponent<AutoSelectOnEnable>(pantalla.gameObject).defaultSelection = boton.gameObject;

        (CanvasGroup grupoTitulo, TextMeshProUGUI textoTitulo, RectTransform rtTextoTitulo) = CrearTitulo(pantalla, fuenteTitulo, brillo, lineaH, rombo);

        // Panel de vidrio oscuro: relleno translúcido + borde violeta achaflanado con esquinas reforzadas.
        RectTransform panel = CrearUI("Panel", pantalla);
        CanvasGroup grupoPanel = panel.gameObject.AddComponent<CanvasGroup>();
        grupoPanel.interactable = grupoPanel.blocksRaycasts = false;
        Vertical(panel, 0f);
        panel.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 12, 12);
        LayoutElement lePanel = ObtenerOAgregar<LayoutElement>(panel.gameObject);
        lePanel.preferredWidth = 840f; // ~44% del ancho en 16:9: deja ver el fondo a los lados
        lePanel.flexibleWidth = lePanel.flexibleHeight = 0f; // si no, la grilla flexible lo estira a toda la pantalla

        Capa("Relleno", panel, relleno, vidrio, 1f);
        Image brilloVidrio = Capa("ReflejoVidrio", panel, brillo, new Color(violetaNeon.r, violetaNeon.g, violetaNeon.b, 0.10f), 0f);
        RectTransform rtReflejo = (RectTransform)brilloVidrio.transform;
        rtReflejo.anchorMin = new Vector2(0.1f, 0.5f);
        rtReflejo.anchorMax = new Vector2(0.9f, 1.25f);
        // Halo violeta exterior muy suave: el panel "flota" sobre el fondo sin oscurecerlo.
        Image haloPanel = Capa("HaloPanel", panel, brillo, new Color(violetaNeon.r, violetaNeon.g, violetaNeon.b, 0.12f), 0f);
        RectTransform rtHaloPanel = (RectTransform)haloPanel.transform;
        rtHaloPanel.SetAsFirstSibling();
        rtHaloPanel.anchorMin = new Vector2(-0.08f, -0.35f);
        rtHaloPanel.anchorMax = new Vector2(1.08f, 1.35f);
        Image imgBorde = Capa("Borde", panel, borde, violetaBrillante, 1f);
        imgBorde.gameObject.AddComponent<Parpadeo>().Configurar(imgBorde, 0.75f, 1f, 0f, 0.5f);

        // Grilla 2x3 con divisores, como el concepto.
        RectTransform grilla = CrearUI("Grilla", panel);
        Horizontal(grilla, 0f);
        RectTransform izq = Columna("ColumnaIzquierda", grilla, new RectOffset(6, 30, 0, 0));
        DivisorLinea("DivisorVertical", grilla, lineaV, true);
        RectTransform der = Columna("ColumnaDerecha", grilla, new RectOffset(34, 6, 0, 0));

        Celda asesino = CrearCelda("CeldaAsesino", izq, fuente, Icono("icono_calavera.png", DentroCalavera), "TE MATÓ", "DESCONOCIDO", true);
        DivisorLinea("Divisor", izq, lineaH, false);
        Celda enemigos = CrearCelda("CeldaEnemigos", izq, fuente, Icono("icono_mira.png", DentroMira), "ENEMIGOS ELIMINADOS", "0", false);
        DivisorLinea("Divisor", izq, lineaH, false);
        Celda puntos = CrearCelda("CeldaPuntos", izq, fuente, Icono("icono_estrella.png", DentroEstrella), "PUNTOS OBTENIDOS", "0", false);

        Celda tiempo = CrearCelda("CeldaTiempo", der, fuente, AssetDatabase.LoadAssetAtPath<Sprite>(rutaIconoTiempo), "TIEMPO DE PARTIDA", "00:00", false);
        DivisorLinea("Divisor", der, lineaH, false);
        Celda mejoras = CrearCelda("CeldaMejoras", der, fuente, Icono("icono_gema.png", DentroGema), "MEJORAS RECOLECTADAS", "0", false);
        DivisorLinea("Divisor", der, lineaH, false);
        Celda nivel = CrearCelda("CeldaNivel", der, fuente, Icono("icono_rango.png", DentroRango), "NIVEL MÁXIMO", "1", false);

        // --- Botón: se reusa "Back"; base fija (vidrio + borde tenue) y BotonMenuFX ilumina el borde al seleccionarse ---
        Undo.SetTransformParent(boton.transform, pantalla, "Reubicar botón");
        boton.transform.SetAsLastSibling();
        RectTransform rtBoton = (RectTransform)boton.transform;
        Undo.RecordObject(rtBoton, "Botón");
        rtBoton.anchorMin = rtBoton.anchorMax = rtBoton.pivot = new Vector2(0.5f, 0.5f);
        rtBoton.localScale = Vector3.one;
        Tamano(rtBoton, 400f, 58f);

        Image imgBoton = ObtenerOAgregar<Image>(boton.gameObject);
        imgBoton.sprite = null;
        imgBoton.color = new Color(1f, 1f, 1f, 0f); // transparente pero con raycast para hover/click
        imgBoton.raycastTarget = true;
        Undo.RecordObject(boton, "Botón");
        boton.transition = Selectable.Transition.None;
        boton.targetGraphic = imgBoton;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };

        // Un clic en el fondo no debe deseleccionar el único botón: si no, ENTER / (A) dejan de funcionar.
        InputSystemUIInputModule modulo = Object.FindFirstObjectByType<InputSystemUIInputModule>();
        if (modulo != null)
        {
            Undo.RecordObject(modulo, "Mantener selección");
            modulo.deselectOnBackgroundClick = false;
        }

        // Resplandor detrás del botón (pulso lento) para que pese visualmente como en el concepto.
        Image haloBoton = Capa("HaloBoton", rtBoton, brillo, new Color(violetaNeon.r, violetaNeon.g, violetaNeon.b, 0.2f), 0f);
        RectTransform rtHaloBoton = (RectTransform)haloBoton.transform;
        rtHaloBoton.anchorMin = new Vector2(-0.15f, -0.7f);
        rtHaloBoton.anchorMax = new Vector2(1.15f, 1.7f);
        haloBoton.gameObject.AddComponent<Parpadeo>().Configurar(haloBoton, 0.12f, 0.26f, 0f, 0.5f);
        Capa("Fondo", rtBoton, relleno, new Color(vidrio.r, vidrio.g, vidrio.b, 0.85f), 2f);
        Capa("BordeBase", rtBoton, borde, new Color(violetaBrillante.r, violetaBrillante.g, violetaBrillante.b, 0.8f), 2f);
        Image imgMarco = Capa("Marco", rtBoton, borde, new Color(violetaBrillante.r, violetaBrillante.g, violetaBrillante.b, 0f), 2f);

        TextMeshProUGUI textoBoton = CrearTexto("Texto", rtBoton, fuente, "VOLVER AL MENÚ", 23f, lavanda, TextAlignmentOptions.Center);
        textoBoton.fontStyle = FontStyles.Bold;
        textoBoton.characterSpacing = 22f;
        Estirar((RectTransform)textoBoton.transform);
        ObtenerOAgregar<BotonMenuFX>(boton.gameObject).Configurar(imgMarco, textoBoton);
        if (!boton.TryGetComponent(out UIButtonSFX _)) Undo.AddComponent<UIButtonSFX>(boton.gameObject);

        TextMeshProUGUI pistas = CrearTexto("PistasEntrada", canvas.transform, fuente,
            "[ENTER] / (A) VOLVER AL MENÚ", 20f, new Color(lavanda.r, lavanda.g, lavanda.b, 0.6f), TextAlignmentOptions.BottomLeft);
        Esquina((RectTransform)pistas.transform, Vector2.zero, new Vector2(48f, 32f));

        // --- Partículas (mundo 3D, entre el fondo y la UI) ---
        CrearVFX(camara, fondoRT, rtTextoTitulo, panel, rtBoton, materialParticulas);

        // --- Referencias del script GameOver (campos privados serializados) ---
        SerializedObject so = new SerializedObject(gameOver);
        so.FindProperty("textoTitulo").objectReferenceValue = textoTitulo;
        so.FindProperty("etiquetaAsesino").objectReferenceValue = asesino.etiqueta;
        so.FindProperty("textoAsesino").objectReferenceValue = asesino.valor;
        so.FindProperty("textoTiempo").objectReferenceValue = tiempo.valor;
        so.FindProperty("textoEnemigos").objectReferenceValue = enemigos.valor;
        so.FindProperty("textoMejoras").objectReferenceValue = mejoras.valor;
        so.FindProperty("textoPuntos").objectReferenceValue = puntos.valor;
        so.FindProperty("textoNivel").objectReferenceValue = nivel.valor;
        SerializedProperty soloDerrota = so.FindProperty("objetosSoloDerrota");
        soloDerrota.arraySize = 1;
        soloDerrota.GetArrayElementAtIndex(0).objectReferenceValue = asesino.icono; // la calavera no va en victoria
        so.FindProperty("grupoTitulo").objectReferenceValue = grupoTitulo;
        so.FindProperty("grupoPanel").objectReferenceValue = grupoPanel;
        CanvasGroup[] filas = { asesino.grupo, tiempo.grupo, enemigos.grupo, mejoras.grupo, puntos.grupo, nivel.grupo };
        SerializedProperty lista = so.FindProperty("gruposEstadisticas");
        lista.arraySize = filas.Length;
        for (int i = 0; i < filas.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = filas[i];
        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(grupoUndo);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = pantalla.gameObject;
        Debug.Log("[GameOverBuilder] Game Over construido. Guardá la escena (Ctrl+S).");
    }

    // =========================================================
    // TÍTULO: líneas con rombos arriba/abajo y alas a los costados (siguen el ancho del texto por layout)
    // =========================================================

    private static (CanvasGroup, TextMeshProUGUI, RectTransform) CrearTitulo(Transform padre, TMP_FontAsset fuente, Sprite brillo, Sprite linea, Sprite rombo)
    {
        RectTransform titulo = CrearUI("Titulo", padre);
        CanvasGroup grupo = titulo.gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = grupo.blocksRaycasts = false;
        Vertical(titulo, 0f);

        RectTransform halo = CrearUI("HaloTitulo", titulo);
        Undo.AddComponent<LayoutElement>(halo.gameObject).ignoreLayout = true;
        halo.anchorMin = halo.anchorMax = new Vector2(0.5f, 0.5f);
        halo.sizeDelta = new Vector2(900f, 260f);
        Image imgHalo = halo.gameObject.AddComponent<Image>();
        imgHalo.sprite = brillo;
        imgHalo.color = new Color(violetaNeon.r, violetaNeon.g, violetaNeon.b, 0.35f);
        imgHalo.raycastTarget = false;
        halo.gameObject.AddComponent<Parpadeo>().Configurar(imgHalo, 0.22f, 0.4f, 0.04f, 0.6f);

        LineaConRombo("DecoSuperior", titulo, linea, rombo, 520f, 0.5f);

        RectTransform fila = CrearUI("FilaTitulo", titulo);
        Horizontal(fila, 22f);
        Ala("AlaIzquierda", fila, linea, rombo, 1f);
        TextMeshProUGUI texto = CrearTexto("Texto", fila, fuente, "PERDISTE", 104f, Color.white, TextAlignmentOptions.Center);
        texto.characterSpacing = 6f;
        texto.enableVertexGradient = true;
        texto.colorGradient = new VertexGradient(claro, claro, violetaBrillante, violetaBrillante);
        Ala("AlaDerecha", fila, linea, rombo, 0f);

        LineaConRombo("DecoInferior", titulo, linea, rombo, 640f, 0.5f);
        return (grupo, texto, (RectTransform)texto.transform);
    }

    // Línea horizontal centrada con un rombo en el medio.
    private static void LineaConRombo(string nombre, Transform padre, Sprite linea, Sprite rombo, float ancho, float alfa)
    {
        RectTransform cont = CrearUI(nombre, padre);
        Tamano(cont, ancho, 16f);
        RectTransform rtLinea = CrearUI("Linea", cont);
        rtLinea.anchorMin = new Vector2(0f, 0.5f);
        rtLinea.anchorMax = new Vector2(1f, 0.5f);
        rtLinea.sizeDelta = new Vector2(0f, 2f);
        Imagen(rtLinea, linea, new Color(violetaBrillante.r, violetaBrillante.g, violetaBrillante.b, 0.9f));
        RectTransform rtRombo = CrearUI("Rombo", cont);
        rtRombo.anchorMin = rtRombo.anchorMax = new Vector2(alfa, 0.5f);
        rtRombo.sizeDelta = new Vector2(16f, 16f);
        Imagen(rtRombo, rombo, violetaBrillante);
    }

    // Ala lateral: línea a la altura de la base del texto que termina en un rombo del lado del título.
    private static void Ala(string nombre, Transform padre, Sprite linea, Sprite rombo, float ladoRombo)
    {
        RectTransform ala = CrearUI(nombre, padre);
        Tamano(ala, 170f, 60f);
        RectTransform rtLinea = CrearUI("Linea", ala);
        rtLinea.anchorMin = new Vector2(0f, 0.25f);
        rtLinea.anchorMax = new Vector2(1f, 0.25f);
        rtLinea.sizeDelta = new Vector2(0f, 2f);
        Imagen(rtLinea, linea, new Color(violetaBrillante.r, violetaBrillante.g, violetaBrillante.b, 0.85f));
        RectTransform rtRombo = CrearUI("Rombo", ala);
        rtRombo.anchorMin = rtRombo.anchorMax = new Vector2(ladoRombo, 0.25f);
        rtRombo.sizeDelta = new Vector2(10f, 10f);
        Imagen(rtRombo, rombo, violetaBrillante);
    }

    // Fuente del título: asset estático propio generado desde Anton.ttf (no se toca el asset dinámico de los ejemplos de TMP).
    // Material con brillo violeta (glow) y contorno fino.
    private static TMP_FontAsset CrearFuenteTitulo(TMP_FontAsset respaldo)
    {
        TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(rutaFuenteTitulo);
        if (fa == null)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(rutaAnton);
            if (ttf == null) return respaldo;

            fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 12, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, true);
            if (fa == null) return respaldo;
            fa.name = "Anton GameOver SDF";
            // Mayúsculas completas: si se cambia el texto del título no se mezcla con la fuente de respaldo.
            fa.TryAddCharacters("ABCDEFGHIJKLMNÑOPQRSTUVWXYZÁÉÍÓÚ¡!¿? ", out _);
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            if (respaldo != null) fa.fallbackFontAssetTable = new List<TMP_FontAsset> { respaldo };

            AssetDatabase.CreateAsset(fa, rutaFuenteTitulo);
            fa.atlasTexture.name = "Anton GameOver Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            fa.material.name = "Anton GameOver Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }

        // CreateFontAsset usa el shader Mobile, que no tiene glow: se pasa al Distance Field completo.
        Material mat = fa.material;
        Shader sdf = Shader.Find("TextMeshPro/Distance Field");
        if (sdf != null && mat.shader != sdf) mat.shader = sdf;
        mat.EnableKeyword("GLOW_ON");
        mat.SetColor("_GlowColor", new Color(violetaNeon.r, violetaNeon.g, violetaNeon.b, 0.75f));
        mat.SetFloat("_GlowOffset", 0f);
        mat.SetFloat("_GlowInner", 0.05f);
        mat.SetFloat("_GlowOuter", 0.55f);
        mat.SetFloat("_GlowPower", 0.6f);
        mat.SetFloat("_OutlineWidth", 0.08f);
        mat.SetColor("_OutlineColor", Hex("#6D28D9"));
        EditorUtility.SetDirty(mat);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssetIfDirty(fa); // solo la fuente, no todo el proyecto
        return fa;
    }

    // =========================================================
    // PIEZAS DEL PANEL
    // =========================================================

    private struct Celda
    {
        public CanvasGroup grupo;
        public TextMeshProUGUI etiqueta;
        public TextMeshProUGUI valor;
        public GameObject icono;
    }

    // Ícono (dentro de un hueco fijo, así ocultarlo no corre el texto) + etiqueta chica y valor debajo.
    // destacada: la celda del asesino (ícono y etiqueta en violeta brillante, valor más grande) encabeza la lectura.
    private static Celda CrearCelda(string nombre, Transform padre, TMP_FontAsset fuente, Sprite icono, string etiqueta, string valor, bool destacada)
    {
        RectTransform celda = CrearUI(nombre, padre);
        CanvasGroup grupo = celda.gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = grupo.blocksRaycasts = false;
        Horizontal(celda, 18f);
        HorizontalLayoutGroup h = celda.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleLeft;
        h.padding = new RectOffset(0, 0, 8, 8);
        ObtenerOAgregar<LayoutElement>(celda.gameObject).preferredHeight = 58f;

        RectTransform hueco = CrearUI("Icono", celda);
        Tamano(hueco, 34f, 34f);
        RectTransform rtImagen = CrearUI("Imagen", hueco);
        Estirar(rtImagen);
        Image img = Imagen(rtImagen, icono, destacada ? violetaBrillante : lavanda);
        img.preserveAspect = true;

        RectTransform textos = CrearUI("Textos", celda);
        Vertical(textos, 0f);
        textos.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        TextMeshProUGUI tEtiqueta = CrearTexto("Etiqueta", textos, fuente, etiqueta, 13f,
            destacada ? violetaBrillante : ConAlfa(lavanda, 0.8f), TextAlignmentOptions.Left);
        tEtiqueta.characterSpacing = 14f;

        TextMeshProUGUI tValor = CrearTexto("Valor", textos, fuente, valor, destacada ? 26f : 23f, claro, TextAlignmentOptions.Left);
        tValor.fontStyle = FontStyles.Bold;
        tValor.characterSpacing = 3f;

        return new Celda { grupo = grupo, etiqueta = tEtiqueta, valor = tValor, icono = rtImagen.gameObject };
    }

    private static RectTransform Columna(string nombre, Transform padre, RectOffset padding)
    {
        RectTransform col = CrearUI(nombre, padre);
        Vertical(col, 0f);
        VerticalLayoutGroup v = col.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.childForceExpandWidth = true;
        v.padding = padding;
        LayoutElement le = ObtenerOAgregar<LayoutElement>(col.gameObject);
        le.preferredWidth = 0f; // columnas iguales aunque un valor sea largo
        le.flexibleWidth = 1f;
        return col;
    }

    private static void DivisorLinea(string nombre, Transform padre, Sprite sprite, bool vertical)
    {
        RectTransform rt = CrearUI(nombre, padre);
        Imagen(rt, sprite, new Color(lavanda.r, lavanda.g, lavanda.b, vertical ? 0.4f : 0.3f));
        LayoutElement le = ObtenerOAgregar<LayoutElement>(rt.gameObject);
        if (vertical) { le.preferredWidth = 2f; le.flexibleHeight = 1f; }
        else { le.preferredHeight = 2f; le.flexibleWidth = 1f; }
    }

    // Capa 9-slice que cubre todo el padre, fuera del layout. multiplicador > 1 achica las esquinas (botón).
    private static Image Capa(string nombre, Transform padre, Sprite sprite, Color color, float multiplicador)
    {
        RectTransform rt = CrearUI(nombre, padre);
        Undo.AddComponent<LayoutElement>(rt.gameObject).ignoreLayout = true;
        Estirar(rt);
        Image img = Imagen(rt, sprite, color);
        if (multiplicador > 0f)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = multiplicador;
        }
        return img;
    }

    private static Image Imagen(RectTransform rt, Sprite sprite, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void Horizontal(RectTransform rt, float espaciado)
    {
        HorizontalLayoutGroup grupo = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        grupo.childAlignment = TextAnchor.MiddleCenter;
        grupo.spacing = espaciado;
        grupo.childControlWidth = grupo.childControlHeight = true;
        grupo.childForceExpandWidth = grupo.childForceExpandHeight = false;
    }

    // =========================================================
    // VFX: motas violetas de ambiente, destellos alrededor del título, polvo cálido a ras del suelo,
    // chispas en el borde del panel y destellos en el borde del botón.
    // Viven en mundo 3D entre CanvasFondo (100 u) y la UI Overlay; tiempo sin escalar.
    // Presupuesto: máx. 109 partículas (~80 vivas), 5 sistemas con el mismo material.
    // Sin niebla: con Embers.mat (punto aditivo) se vería como manchas y sumaría overdraw a pantalla completa.
    // =========================================================

    private static void CrearVFX(Camera camara, RectTransform fondo, RectTransform titulo, RectTransform panel,
        RectTransform boton, Material material)
    {
        if (material == null)
        {
            Debug.LogWarning("[GameOverBuilder] No se encontró Embers.mat: se omiten las partículas.");
            return;
        }

        GameObject vfx = new GameObject("VFXGameOver");
        Undo.RegisterCreatedObjectUndo(vfx, "Crear VFXGameOver");

        Particulas("MotasAmbiente", vfx.transform, material, camara, fondo, new Vector2(0.5f, 0.5f), ConfigurarMotas);
        Particulas("DestellosTitulo", vfx.transform, material, camara, titulo, new Vector2(0.5f, 0.5f), ConfigurarDestellos);
        Particulas("PolvoSuelo", vfx.transform, material, camara, fondo, new Vector2(0.5f, 0.14f), ConfigurarPolvoSuelo);
        Particulas("ChispasPanel", vfx.transform, material, camara, panel, new Vector2(0.5f, 0.5f), ConfigurarChispasPanel);
        Particulas("DestellosBoton", vfx.transform, material, camara, boton, new Vector2(0.5f, 0.5f), ConfigurarDestellosBoton);
    }

    // Espacio Local (no World como el menú): el prewarm simula en la posición guardada antes de que el ancla
    // lo ubique, y en World esas partículas quedarían pegadas a la cámara. La cámara no se mueve, no hace falta estela.
    private static void Particulas(string nombre, Transform padre, Material material, Camera camara, RectTransform ancla,
        Vector2 punto, System.Action<ParticleSystem> configurar)
    {
        ParticleSystem ps = CrearParticulas(nombre, padre, material);
        ParticleSystem.MainModule main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        ps.gameObject.AddComponent<AnclaEnPantalla>().Configurar(camara, ancla, punto, distanciaParticulas);
        configurar(ps);
    }

    // Escala: cámara FOV 60 a 60 u -> 1 u ≈ 15.6 px a 1080p (alto visible ≈ 69 u, ancho 16:9 ≈ 123 u, 21:9 ≈ 165 u).
    // Embers.mat es aditivo con color base HDR x2.8 y no hay post: la intensidad se controla con alfa <= 0.5.
    // La deriva sale de velocityOverLifetime en Local (el Box emite por +Z = profundidad, por eso startSpeed 0).
    private static Color ConAlfa(Color c, float a) => new Color(c.r, c.g, c.b, a);

    private static void Deriva(ParticleSystem ps, Vector2 x, Vector2 y)
    {
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(x.x, x.y);
        vel.y = new ParticleSystem.MinMaxCurve(y.x, y.y);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    private static Gradient FadeEntradaSalida(float entrada, float salida) => Degradado(
        new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
        new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, entrada), new GradientAlphaKey(1f, salida), new GradientAlphaKey(0f, 1f) });

    // Destello: crece rápido y se apaga encogiéndose (título, panel y botón).
    private static void TamanoDestello(ParticleSystem ps)
    {
        ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));
    }

    // Emite desde el contorno de un rectángulo en el plano de pantalla (ancho x alto en unidades; z 0 = solo 4 aristas).
    private static void BordeRectangulo(ParticleSystem ps, float ancho, float alto)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.BoxEdge;
        shape.scale = new Vector3(ancho, alto, 0f);
    }

    // Motas violetas que suben lento por toda la pantalla (~37 vivas).
    private static void ConfigurarMotas(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(ConAlfa(lavanda, 0.3f), ConAlfa(violetaNeon, 0.4f));
        main.maxParticles = 45;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 5f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(170f, 72f, 4f); // cubre hasta 21:9

        Deriva(ps, new Vector2(-0.2f, 0.2f), new Vector2(0.2f, 0.6f));
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = FadeEntradaSalida(0.25f, 0.7f);

        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.6f;
        ruido.frequency = 0.15f;
        ruido.scrollSpeed = 0.1f;
    }

    // Destellos que crecen y se apagan alrededor del título (~6 vivos). La caja (~750 x 125 px) acompaña
    // al "PERDISTE" de fuente 104; los que caen detrás de las letras los tapa la UI Overlay.
    private static void ConfigurarDestellos(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 1f, 0.45f), ConAlfa(lavanda, 0.45f));
        main.maxParticles = 12;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 5f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(48f, 8f, 0f);

        Deriva(ps, Vector2.zero, new Vector2(0.3f, 0.8f));
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = FadeEntradaSalida(0.15f, 0.15f);
        TamanoDestello(ps);
    }

    // Chispas violetas que nacen en el borde del panel (840 x ~200 px ≈ 54 x 13 u) y suben apenas (~3 vivas).
    private static void ConfigurarChispasPanel(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(ConAlfa(violetaBrillante, 0.4f), ConAlfa(lavanda, 0.45f));
        main.maxParticles = 8;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 1.6f;

        BordeRectangulo(ps, 54f, 13f);
        Deriva(ps, new Vector2(-0.15f, 0.15f), new Vector2(0.1f, 0.4f));
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = FadeEntradaSalida(0.15f, 0.5f);
        TamanoDestello(ps);
    }

    // Destellos puntuales en el borde del botón (400 x 58 px ≈ 25.6 x 3.7 u): uno cada ~1.5 s, sin deriva.
    private static void ConfigurarDestellosBoton(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 1f, 0.45f), ConAlfa(violetaBrillante, 0.45f));
        main.maxParticles = 4;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 0.7f;

        BordeRectangulo(ps, 25.6f, 3.7f);
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = FadeEntradaSalida(0.15f, 0.3f);
        TamanoDestello(ps);
    }

    // Polvo cálido arrastrado por el viento a ras del suelo (~29 vivas).
    private static void ConfigurarPolvoSuelo(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 7.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.75f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.65f, 0.3f, 0.18f), new Color(1f, 0.45f, 0.15f, 0.28f));
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 5f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(170f, 5f, 4f);

        Deriva(ps, new Vector2(0.4f, 1.2f), new Vector2(0f, 0.2f));
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = FadeEntradaSalida(0.3f, 0.7f);

        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.5f;
        ruido.frequency = 0.2f;
        ruido.scrollSpeed = 0.1f;
    }

    // =========================================================
    // SPRITES PROCEDURALES (blancos, se tiñen con Image.color; se generan una sola vez)
    // =========================================================

    private static Sprite Generar(string archivo, int ancho, int alto, Vector4 borde, System.Func<Vector2, int, int, float> alfa)
    {
        string ruta = carpeta + archivo;
        if (!File.Exists(ruta)) GuardarPNG(ruta, ancho, alto, p => alfa(p, ancho, alto));
        return CargarSprite(ruta, borde);
    }

    private static Sprite Icono(string archivo, System.Func<Vector2, bool> dentro) =>
        Generar(archivo, 128, 128, Vector4.zero, (p, w, h) => Supermuestreo(p, w, dentro));

    // Antialias 4x4; "dentro" recibe coordenadas -1..1.
    private static float Supermuestreo(Vector2 p, int lado, System.Func<Vector2, bool> dentro)
    {
        int cuenta = 0;
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                Vector2 m = p + new Vector2((i - 1.5f) / 4f, (j - 1.5f) / 4f);
                if (dentro(m / (lado / 2f) - Vector2.one)) cuenta++;
            }
        return cuenta / 16f;
    }

    // Distancia firmada a un rectángulo de 128 px con esquinas cortadas a 45°.
    private static float DistanciaAchaflanada(Vector2 p, int w, int h, float margen, float chaflan)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - w / 2f), Mathf.Abs(p.y - h / 2f));
        Vector2 medio = new Vector2(w / 2f - margen, h / 2f - margen);
        float caja = Mathf.Max(q.x - medio.x, q.y - medio.y);
        float corte = (q.x + q.y - (medio.x + medio.y - chaflan)) / Mathf.Sqrt(2f);
        return Mathf.Max(caja, corte);
    }

    private static float RellenoAchaflanado(Vector2 p, int w, int h) =>
        Mathf.Clamp01(0.5f - DistanciaAchaflanada(p, w, h, 12f, 16f));

    // Línea fina + halo exterior suave + esquinas reforzadas (más gruesas) como en el concepto.
    private static float BordeAchaflanado(Vector2 p, int w, int h)
    {
        float d = DistanciaAchaflanada(p, w, h, 12f, 16f);
        Vector2 q = new Vector2(Mathf.Abs(p.x - w / 2f), Mathf.Abs(p.y - h / 2f));
        bool esquina = q.x > w / 2f - 40f && q.y > h / 2f - 40f;
        float linea = Linea(d, esquina ? 2.2f : 1.1f);
        float halo = d > 0f ? 0.45f * Mathf.Exp(-d / 4f) : 0.12f * Mathf.Exp(d / 6f);
        return Mathf.Max(linea, halo);
    }

    // =========================================================
    // ÍCONOS (coordenadas -1..1)
    // =========================================================

    // Calavera: cráneo + mandíbula, con ojos, nariz y dientes recortados.
    private static bool DentroCalavera(Vector2 u)
    {
        bool craneo = (u - new Vector2(0f, 0.15f)).magnitude < 0.68f;
        bool mandibula = Mathf.Abs(u.x) < 0.38f && u.y > -0.85f && u.y < -0.2f;
        if (!craneo && !mandibula) return false;
        bool ojo = (u - new Vector2(-0.27f, 0.05f)).magnitude < 0.17f || (u - new Vector2(0.27f, 0.05f)).magnitude < 0.17f;
        bool nariz = u.y < -0.12f && u.y > -0.32f && Mathf.Abs(u.x) < (u.y + 0.32f) * 0.5f;
        bool dientes = u.y < -0.55f && (Mathf.Abs(u.x - 0.13f) < 0.035f || Mathf.Abs(u.x + 0.13f) < 0.035f);
        return !(ojo || nariz || dientes);
    }

    // Mira: anillo, cuatro marcas y punto central.
    private static bool DentroMira(Vector2 u)
    {
        float r = u.magnitude;
        bool anillo = r > 0.5f && r < 0.64f;
        bool marcas = (Mathf.Abs(u.x) < 0.07f && Mathf.Abs(u.y) > 0.3f && Mathf.Abs(u.y) < 0.95f)
                   || (Mathf.Abs(u.y) < 0.07f && Mathf.Abs(u.x) > 0.3f && Mathf.Abs(u.x) < 0.95f);
        return anillo || marcas || r < 0.1f;
    }

    // Estrella de cinco puntas.
    private static bool DentroEstrella(Vector2 u)
    {
        Vector2[] puntos = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
            puntos[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (i % 2 == 0 ? 0.95f : 0.4f);
        }
        return DentroPoligono(u, puntos);
    }

    // Gema facetada: corona plana arriba, punta abajo, con dos facetas recortadas.
    private static bool DentroGema(Vector2 u)
    {
        Vector2[] contorno = { new(-0.85f, 0.25f), new(-0.45f, 0.65f), new(0.45f, 0.65f), new(0.85f, 0.25f), new(0f, -0.8f) };
        if (!DentroPoligono(u, contorno)) return false;
        bool faceta = Mathf.Abs(u.y - 0.25f) < 0.04f || (u.y < 0.25f && Mathf.Abs(Mathf.Abs(u.x) - (u.y + 0.8f) * 0.35f) < 0.04f);
        return !faceta;
    }

    // Rango: escudo hexagonal hueco con dos chevrones adentro.
    private static bool DentroRango(Vector2 u)
    {
        Vector2[] hex = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / 3f;
            hex[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.95f;
        }
        bool marco = DentroPoligono(u, hex) && !DentroPoligono(u / 0.78f, hex);
        bool chevrones = false;
        if (Mathf.Abs(u.x) < 0.45f)
        {
            foreach (float vertice in new[] { 0.35f, -0.05f })
            {
                float y = vertice - Mathf.Abs(u.x) * 0.75f;
                if (u.y <= y && u.y >= y - 0.18f) chevrones = true;
            }
        }
        return marco || chevrones;
    }

    private static bool DentroPoligono(Vector2 p, Vector2[] poligono)
    {
        bool dentro = false;
        for (int i = 0, j = poligono.Length - 1; i < poligono.Length; j = i++)
        {
            if ((poligono[i].y > p.y) != (poligono[j].y > p.y) &&
                p.x < (poligono[j].x - poligono[i].x) * (p.y - poligono[i].y) / (poligono[j].y - poligono[i].y) + poligono[i].x)
                dentro = !dentro;
        }
        return dentro;
    }
}
