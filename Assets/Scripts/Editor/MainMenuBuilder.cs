using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Arma el layout y los VFX del menú principal en la escena MainMenu abierta (menú Out-Void > Construir Main Menu).
// Reusa los botones Play/Options/Exit existentes: su onClick, UIButtonSFX y AutoSelectOnEnable quedan intactos.
// Se puede volver a correr (reemplaza lo generado) y deshacer con Ctrl+Z. Guardar la escena después.
//
// Capas de render: CanvasFondo (Screen Space - Camera, detrás) < partículas 3D < Canvas principal (Overlay, delante).
// Así las brasas quedan sobre el fondo y el polvo violeta queda detrás del logo y los botones.
public static class MainMenuBuilder
{
    private const string carpetaMenu = "Assets/HUD/Menu/";
    private const string rutaMarco = carpetaMenu + "marco_boton.png";
    private const string rutaBrillo = carpetaMenu + "brillo_radial.png";
    private const string rutaMaterialParticulas =
        "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Materials/Embers.mat";

    // Distancias a la cámara: el fondo bien atrás y las partículas delante de él.
    private const float distanciaFondo = 100f;
    private const float distanciaParticulas = 60f;

    // Punto de la fogata dentro de menu.jpeg (0..1).
    private static readonly Vector2 puntoFogata = new Vector2(0.50f, 0.36f);

    private static readonly Color violetaTitulo = Hex("#B388FF");
    private static readonly Color violetaNeon = Hex("#9B5DE5");
    private static readonly Color cian = Hex("#67E8F9");
    private static readonly Color lavanda = Hex("#C4B5FD");

    [MenuItem("Out-Void/Construir Main Menu")]
    public static void Construir()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            EditorUtility.DisplayDialog("Out-Void", "Abrí la escena MainMenu primero.", "OK");
            return;
        }

        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.isRootCanvas && c.transform.Find("Panel Principal") != null);
        Camera camara = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault();
        if (canvas == null || camara == null)
        {
            Debug.LogError("[MainMenuBuilder] Falta el Canvas con 'Panel Principal' o la cámara.");
            return;
        }

        Transform panelPrincipal = canvas.transform.Find("Panel Principal");
        string[] nombres = { "Play", "Options", "Exit" };
        string[] etiquetas = { "JUGAR", "OPCIONES", "SALIR" };
        Button[] botones = nombres.Select(n => BuscarBoton(panelPrincipal, n)).ToArray();
        if (botones.Any(b => b == null))
        {
            Debug.LogError("[MainMenuBuilder] Faltan los botones Play/Options/Exit dentro de 'Panel Principal'.");
            return;
        }

        Undo.SetCurrentGroupName("Construir Main Menu");
        int grupoUndo = Undo.GetCurrentGroup();

        TMP_FontAsset fuente = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(t => t.font).FirstOrDefault(f => f != null) ?? TMP_Settings.defaultFontAsset;
        Sprite fondo = CargarSprite(carpetaMenu + "menu.jpeg", Vector4.zero);
        Sprite logo = CargarSprite(carpetaMenu + "outvoid.png", Vector4.zero);
        Sprite marcoSprite = CrearMarco();
        Sprite brillo = CrearBrilloRadial();
        Material materialParticulas = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterialParticulas);

        // --- Limpieza de una construcción anterior ---
        foreach (Button b in botones)
        {
            Undo.SetTransformParent(b.transform, panelPrincipal, "Reubicar botón");
            DestruirHijo(b.transform, "Marco");
            DestruirHijo(b.transform, "Texto");
        }
        DestruirHijo(panelPrincipal, "MenuDerecho");
        DestruirHijo(canvas.transform, "FondoMenu");
        DestruirHijo(canvas.transform, "PistasEntrada");
        DestruirHijo(canvas.transform, "FirmaEquipo");
        DestruirRaiz("CanvasFondo");
        DestruirRaiz("VFXMenu");

        // --- Canvas principal (Overlay, siempre delante) ---
        CanvasScaler scaler = ObtenerOAgregar<CanvasScaler>(canvas.gameObject);
        ConfigurarScaler(scaler);

        // --- Fondo: canvas propio en espacio de cámara, para poder dibujar partículas encima ---
        GameObject canvasFondoGO = new GameObject("CanvasFondo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasFondoGO, "Crear CanvasFondo");
        canvasFondoGO.layer = canvas.gameObject.layer;
        Canvas canvasFondo = canvasFondoGO.GetComponent<Canvas>();
        canvasFondo.renderMode = RenderMode.ScreenSpaceCamera;
        canvasFondo.worldCamera = camara;
        canvasFondo.planeDistance = distanciaFondo;
        canvasFondo.sortingOrder = -100;
        ConfigurarScaler(canvasFondoGO.GetComponent<CanvasScaler>());

        RectTransform fondoRT = CrearUI("FondoMenu", canvasFondoGO.transform);
        Estirar(fondoRT);
        Image imgFondo = fondoRT.gameObject.AddComponent<Image>();
        imgFondo.sprite = fondo;
        imgFondo.raycastTarget = false;
        if (fondo != null)
        {
            // Envelope: cubre la pantalla sin deformar en otros aspect ratios.
            AspectRatioFitter fitter = fondoRT.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = fondo.rect.width / fondo.rect.height;
        }

        // Brillo cálido de la fogata. Reemplaza a una Light: el fondo es una imagen unlit y una luz no lo afectaría.
        RectTransform brilloFogata = CrearUI("BrilloFogata", fondoRT);
        brilloFogata.anchorMin = brilloFogata.anchorMax = puntoFogata;
        brilloFogata.sizeDelta = new Vector2(760f, 760f);
        Image imgBrilloFogata = brilloFogata.gameObject.AddComponent<Image>();
        imgBrilloFogata.sprite = brillo;
        imgBrilloFogata.color = new Color(1f, 0.55f, 0.15f, 0.45f);
        imgBrilloFogata.raycastTarget = false;
        brilloFogata.gameObject.AddComponent<Parpadeo>().Configurar(imgBrilloFogata, 0.3f, 0.55f, 0.08f, 5f);

        // El fondo 3D viejo quedaría delante del CanvasFondo.
        GameObject fondo3D = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == "Background");
        if (fondo3D != null && fondo3D.GetComponent<Renderer>() != null)
        {
            Undo.RecordObject(fondo3D, "Ocultar fondo 3D");
            fondo3D.SetActive(false);
        }

        // El panel viejo tapaba el fondo: se deja transparente y se apaga su imagen hija.
        if (panelPrincipal.TryGetComponent(out Image imgPanel))
        {
            Undo.RecordObject(imgPanel, "Panel transparente");
            imgPanel.color = Color.clear;
        }
        Transform imagenVieja = panelPrincipal.Find("Image");
        if (imagenVieja != null)
        {
            Undo.RecordObject(imagenVieja.gameObject, "Ocultar imagen vieja");
            imagenVieja.gameObject.SetActive(false);
        }

        // --- Columna derecha ---
        RectTransform menu = CrearUI("MenuDerecho", panelPrincipal);
        menu.anchorMin = new Vector2(0.55f, 0f);
        menu.anchorMax = new Vector2(0.95f, 1f);
        menu.pivot = new Vector2(0.5f, 0.5f);
        menu.offsetMin = menu.offsetMax = Vector2.zero;
        Vertical(menu, 80f);

        // Header: halo pulsante detrás del logo (o del texto si no hay logo).
        RectTransform header = CrearUI("Header", menu);
        Vertical(header, 0f);

        RectTransform halo = CrearUI("HaloLogo", header);
        Undo.AddComponent<LayoutElement>(halo.gameObject).ignoreLayout = true;
        halo.anchorMin = halo.anchorMax = new Vector2(0.5f, 0.5f);
        halo.sizeDelta = new Vector2(1000f, 520f);
        Image imgHalo = halo.gameObject.AddComponent<Image>();
        imgHalo.sprite = brillo;
        imgHalo.color = new Color(violetaNeon.r, violetaNeon.g, violetaNeon.b, 0.35f);
        imgHalo.raycastTarget = false;
        halo.gameObject.AddComponent<Parpadeo>().Configurar(imgHalo, 0.22f, 0.42f, 0.05f, 0.6f);

        RectTransform tituloRT;
        if (logo != null)
        {
            tituloRT = CrearUI("Logo", header);
            Image imgLogo = tituloRT.gameObject.AddComponent<Image>();
            imgLogo.sprite = logo;
            imgLogo.preserveAspect = true;
            imgLogo.raycastTarget = false;
            Tamano(tituloRT, 640f, 640f * logo.rect.height / logo.rect.width);
        }
        else
        {
            TextMeshProUGUI titulo = CrearTexto("Titulo", header, fuente, "OUT-VOID", 150f, violetaTitulo, TextAlignmentOptions.Center);
            titulo.fontStyle = FontStyles.Bold;
            tituloRT = (RectTransform)titulo.transform;
            Tamano(tituloRT, 700f, 170f);
        }

        // Botones: se reusan los existentes.
        RectTransform contenedorBotones = CrearUI("Botones", menu);
        Vertical(contenedorBotones, 25f);

        for (int i = 0; i < botones.Length; i++)
        {
            Button boton = botones[i];
            RectTransform rt = (RectTransform)boton.transform;
            Undo.SetTransformParent(rt, contenedorBotones, "Reubicar botón");
            Undo.RecordObject(rt, "Botón");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            Tamano(rt, 460f, 96f);

            // Fondo transparente pero con raycast, para el hover y el click.
            Image imgBoton = ObtenerOAgregar<Image>(boton.gameObject);
            imgBoton.sprite = null;
            imgBoton.color = new Color(1f, 1f, 1f, 0f);
            imgBoton.raycastTarget = true;

            Undo.RecordObject(boton, "Botón");
            boton.transition = Selectable.Transition.None;
            boton.targetGraphic = imgBoton;
            boton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = botones[(i + botones.Length - 1) % botones.Length],
                selectOnDown = botones[(i + 1) % botones.Length]
            };

            // Marco achaflanado: arranca transparente, BotonMenuFX lo hace aparecer.
            RectTransform marco = CrearUI("Marco", rt);
            Estirar(marco);
            Image imgMarco = marco.gameObject.AddComponent<Image>();
            imgMarco.sprite = marcoSprite;
            imgMarco.type = Image.Type.Sliced;
            imgMarco.color = new Color(violetaNeon.r, violetaNeon.g, violetaNeon.b, 0f);
            imgMarco.raycastTarget = false;

            TextMeshProUGUI texto = CrearTexto("Texto", rt, fuente, etiquetas[i], 42f, lavanda, TextAlignmentOptions.Center);
            texto.fontStyle = FontStyles.Bold;
            texto.characterSpacing = 15f;
            Estirar((RectTransform)texto.transform);

            ObtenerOAgregar<BotonMenuFX>(boton.gameObject).Configurar(imgMarco, texto);
        }

        // --- Footer ---
        TextMeshProUGUI pistas = CrearTexto("PistasEntrada", canvas.transform, fuente,
            "[↑][↓] NAVEGAR   |   [ENTER] SELECCIONAR", 22f, new Color(lavanda.r, lavanda.g, lavanda.b, 0.75f), TextAlignmentOptions.BottomLeft);
        Esquina((RectTransform)pistas.transform, new Vector2(0f, 0f), new Vector2(48f, 36f));

        TextMeshProUGUI firma = CrearTexto("FirmaEquipo", canvas.transform, fuente,
            "TALLER DE VIDEOJUEGOS 4 · GUTIERREZ · SALA · GARELLO · GIUSIANI", 20f, new Color(lavanda.r, lavanda.g, lavanda.b, 0.6f), TextAlignmentOptions.BottomRight);
        Esquina((RectTransform)firma.transform, new Vector2(1f, 0f), new Vector2(-48f, 36f));

        // --- Partículas (mundo 3D, entre el fondo y la UI) ---
        GameObject vfx = new GameObject("VFXMenu");
        Undo.RegisterCreatedObjectUndo(vfx, "Crear VFXMenu");

        ParticleSystem brasas = CrearParticulas("BrasasFogata", vfx.transform, materialParticulas);
        brasas.gameObject.AddComponent<AnclaEnPantalla>().Configurar(camara, fondoRT, puntoFogata, distanciaParticulas);
        ConfigurarBrasas(brasas);

        ParticleSystem polvo = CrearParticulas("PolvoLogo", vfx.transform, materialParticulas);
        polvo.gameObject.AddComponent<AnclaEnPantalla>().Configurar(camara, tituloRT, new Vector2(0.5f, 0.5f), distanciaParticulas);
        ConfigurarPolvo(polvo);

        // La cápsula de la versión anterior quedó sin uso.
        AssetDatabase.DeleteAsset(carpetaMenu + "capsula_glow.png");

        Undo.CollapseUndoOperations(grupoUndo);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = menu.gameObject;
        Debug.Log("[MainMenuBuilder] Menú construido. Guardá la escena (Ctrl+S).");
    }

    // =========================================================
    // PARTÍCULAS
    // Unidades: a 60 m de una cámara con FOV 33, la pantalla mide ~35 unidades de alto (1 unidad ≈ 30 px a 1080p).
    // =========================================================

    internal static ParticleSystem CrearParticulas(string nombre, Transform padre, Material material)
    {
        GameObject go = new GameObject(nombre, typeof(ParticleSystem));
        go.transform.SetParent(padre, false);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        return ps;
    }

    private static void ConfigurarBrasas(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.45f, 0.1f));
        main.gravityModifier = -0.05f;
        main.maxParticles = 200;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 22f;

        // Cono hacia arriba de la cámara (el objeto copia la rotación de la cámara).
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 1.6f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = Degradado(
            new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f), new GradientColorKey(new Color(0.8f, 0.15f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });

        ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        // Ruido para que las brasas floten en zigzag.
        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.8f;
        ruido.frequency = 0.4f;
        ruido.scrollSpeed = 0.3f;
    }

    private static void ConfigurarPolvo(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(violetaTitulo, cian);
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 10f;

        // Caja del tamaño aproximado del logo (640x180 px ≈ 21x6 unidades).
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(24f, 8f, 1f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = Degradado(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });

        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.4f;
        ruido.frequency = 0.25f;
        ruido.scrollSpeed = 0.15f;
    }

    internal static Gradient Degradado(GradientColorKey[] colores, GradientAlphaKey[] alfas)
    {
        Gradient g = new Gradient();
        g.SetKeys(colores, alfas);
        return g;
    }

    // =========================================================
    // SPRITES PROCEDURALES (se generan una sola vez)
    // =========================================================

    // Marco alargado con esquinas achaflanadas, doble línea, resplandor y rombos laterales. Blanco: se tiñe con Image.color.
    internal static Sprite CrearMarco()
    {
        const int ancho = 256, alto = 96;
        const float margen = 14f, chaflan = 16f, separacionLineas = 6f, rombo = 7f;

        if (!File.Exists(rutaMarco))
        {
            Vector2 centro = new Vector2(ancho / 2f, alto / 2f);
            Vector2 medio = new Vector2(ancho / 2f - margen, alto / 2f - margen);

            GuardarPNG(rutaMarco, ancho, alto, p =>
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x - centro.x), Mathf.Abs(p.y - centro.y));

                // Distancia firmada a un rectángulo con esquinas cortadas a 45°.
                float caja = Mathf.Max(q.x - medio.x, q.y - medio.y);
                float corte = (q.x + q.y - (medio.x + medio.y - chaflan)) / Mathf.Sqrt(2f);
                float d = Mathf.Max(caja, corte);

                float lineaExterior = Linea(d, 1.3f);
                float lineaInterior = Linea(d + separacionLineas, 0.8f) * 0.7f;
                float halo = d > 0f ? 0.5f * Mathf.Exp(-d / 4.5f) : 0.08f;

                // Rombos sobre los laterales (distancia L1 al centro del lado).
                float dRombo = (Mathf.Abs(q.x - medio.x) + q.y) - rombo;
                float rombos = 1f - Mathf.Clamp01(dRombo);

                return Mathf.Max(lineaExterior, lineaInterior, halo, rombos);
            });
        }

        float bordeX = margen + chaflan + 6f;
        float bordeY = alto / 2f - 1f;
        return CargarSprite(rutaMarco, new Vector4(bordeX, bordeY, bordeX, bordeY));
    }

    internal static Sprite CrearBrilloRadial()
    {
        const int lado = 128;

        if (!File.Exists(rutaBrillo))
        {
            GuardarPNG(rutaBrillo, lado, lado, p =>
            {
                float r = Mathf.Clamp01(Vector2.Distance(p, new Vector2(lado / 2f, lado / 2f)) / (lado / 2f));
                return (1f - r) * (1f - r);
            });
        }

        return CargarSprite(rutaBrillo, Vector4.zero);
    }

    // Línea suavizada de grosor medio "grosor" sobre una distancia firmada.
    internal static float Linea(float d, float grosor) => 1f - Mathf.Clamp01(Mathf.Abs(d) - grosor);

    internal static void GuardarPNG(string ruta, int ancho, int alto, System.Func<Vector2, float> alfa)
    {
        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alfa(new Vector2(x + 0.5f, y + 0.5f))));

        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
    }

    internal static Sprite CargarSprite(string ruta, Vector4 borde)
    {
        TextureImporter importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[MainMenuBuilder] No se encontró {ruta}.");
            return null;
        }

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != borde)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = borde;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static Button BuscarBoton(Transform raiz, string nombre) =>
        raiz.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == nombre);

    internal static void DestruirHijo(Transform padre, string nombre)
    {
        Transform hijo = padre.Find(nombre);
        if (hijo != null) Undo.DestroyObjectImmediate(hijo.gameObject);
    }

    internal static void DestruirRaiz(string nombre)
    {
        GameObject raiz = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == nombre);
        if (raiz != null) Undo.DestroyObjectImmediate(raiz);
    }

    internal static T ObtenerOAgregar<T>(GameObject go) where T : Component
    {
        if (go.TryGetComponent(out T componente))
        {
            Undo.RecordObject(componente, "Modificar " + typeof(T).Name);
            return componente;
        }
        return Undo.AddComponent<T>(go);
    }

    internal static void ConfigurarScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    internal static RectTransform CrearUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        go.layer = padre.gameObject.layer;
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    internal static TextMeshProUGUI CrearTexto(string nombre, Transform padre, TMP_FontAsset fuente, string texto,
        float tamano, Color color, TextAlignmentOptions alineacion)
    {
        TextMeshProUGUI tmp = CrearUI(nombre, padre).gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = fuente;
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.color = color;
        tmp.alignment = alineacion;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    internal static void Vertical(RectTransform rt, float espaciado)
    {
        VerticalLayoutGroup grupo = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        grupo.childAlignment = TextAnchor.MiddleCenter;
        grupo.spacing = espaciado;
        grupo.childControlWidth = grupo.childControlHeight = true;
        grupo.childForceExpandWidth = grupo.childForceExpandHeight = false;
    }

    internal static void Tamano(RectTransform rt, float ancho, float alto)
    {
        LayoutElement le = ObtenerOAgregar<LayoutElement>(rt.gameObject);
        le.preferredWidth = ancho;
        le.preferredHeight = alto;
    }

    internal static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    internal static void Esquina(RectTransform rt, Vector2 ancla, Vector2 posicion)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(1000f, 40f);
    }

    internal static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
}
