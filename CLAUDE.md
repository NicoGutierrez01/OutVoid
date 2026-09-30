# CLAUDE.md — Out-Void

## Resumen del Proyecto
- **Título:** Out-Void
- **Género:** FPS Roguelite 3D, estética low-poly.
- **Mecánicas clave:**
  - Generación procedimental de biomas y progresión por niveles.
  - Armas con retroceso, cadencia y sensibilidad de cámara.
  - HUD desacoplado y modular en una escena independiente (`UIScene`, aditiva).
  - Telemetría de gameplay con Unity Analytics.
- **Control de versiones:** Git colaborativo. Rama de trabajo `developer`, PRs hacia `main`.

## Stack
- **Unity:** 6000.3.10f1 (Unity 6)
- **Render pipeline:** URP 17.3
- **Paquetes clave:** Input System 1.18 (`Assets/InputSystem_Actions.inputactions`), AI Navigation (NavMesh), Unity Services Analytics, TextMesh Pro, Test Framework
- **Movimiento del jugador:** Kinematic Character Controller (`Assets/KinematicCharacterController/`, asset de terceros)

## Capacidades de Plugins Activas
- **Unity (`/unity:*` skills):** usarlas para consultar jerarquías de escenas, validar prefabs y verificar dependencias de componentes antes de editar o refactorizar scripts.
- **Obsidian:** si se pide documentar diseño (GDD), bugs o mecánicas, crear/actualizar notas en el vault de Obsidian con `[[wikilinks]]` y propiedades frontmatter.

## Estructura de `Assets/`
```
Assets/
  Scripts/            <- código propio (lo único que se edita normalmente)
    Player/           stats, cámara, input, experiencia
    WeaponS/          armas: proyectiles, cadencia, recarga, daño
    Enemies/          EnemyHealth + Kamikaze, Stalker, Striker, Bosses/
    Items/            power-ups, drops, puntos de interés
    Map Generator/    generación procedimental (MapManager, biomas, datos de niveles)
    HUD/              controladores del HUD, menús, indicadores, crosshair
    Music & SFX/      MusicManager, SFXManager
    Services/         init de Unity Services, EventManager (eventos de Analytics), StaticVariables
    Analytics/        AnalyticsBridge
    Objectives/, Portales/, Tombstone/, Objetcs/
  Scenes/             MainMenu, Desert, Forest, UIScene (HUD aditivo), GameOver; ControllerTest (pruebas)
  Prefabs/, Material/, Anims/, Audio/, HUD/, UI/, Settings/
```

## No tocar
- Assets de terceros: `KinematicCharacterController/`, `BTM_Assets/`, `Fantasy Skybox FREE/`, `TextMesh Pro/`, `UnityTechnologies/`, `TutorialInfo/`.
- Generados por Unity: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, `*.csproj`, `*.slnx`.
- Archivos `.meta`: no modificarlos ni borrarlos a mano. Al mover/renombrar un script, mover su `.meta` junto con él.
- `.unity`, `.prefab`, `.asset`: YAML serializado; no editarlos a mano. Preferir cambios en C# o indicar qué ajustar en el Inspector.

## Reglas de Desarrollo (C# / Unity)
1. **Convenciones:**
   - Serialización: `[SerializeField] private` (evitar `public` innecesarios). Código existente usa bastantes `public`; no migrarlo salvo que se pida.
   - Nombres: `PascalCase` para clases y métodos; `camelCase` o `_camelCase` para variables privadas.
   - Campos, comentarios y `[Header]`/`[Tooltip]` en español; mantener ese idioma.
   - Acceso: `TryGetComponent` en vez de `GetComponent`.
   - Rendimiento: prohibido `GetComponent` / `FindObjectOfType` / `FindFirstObjectByType` en `Update()`, `FixedUpdate()` o `LateUpdate()`. Cachear en `Awake()` o `Start()`.
   - Renombrar un campo serializado borra su valor en el Inspector; usar `[FormerlySerializedAs("viejo")]`.
2. **UI modular:** la escena del HUD se comunica con el gameplay mediante eventos C# (`Action` o `UnityEvent`), sin referencias directas entre escenas.
3. **Patrones existentes:** managers globales usan singleton `public static X Instance` (MusicManager, SFXManager, MapManager, ShotManager...) o `Instancia` (ExperienceManager, AdministradorDeProgreso). Reusarlos antes de crear otro. Enemigos usan `NavMeshAgent`. Input con el Input System nuevo, no `UnityEngine.Input`.
4. **Verificación:** no hay tests automatizados; probar en Play mode y revisar la Console. Sin acceso al Editor, los errores de compilación se leen en `%LOCALAPPDATA%\Unity\Editor\Editor.log` (buscar `error CS`; Unity solo recompila cuando la ventana del Editor toma foco).
5. **Editor en vivo:** el proyecto no tiene el paquete `com.unity.pipeline`, así que la CLI `unity` no puede controlar el Editor. No instalarlo sin consultar (agrega una dependencia al equipo).

## Arquitectura clave

### Persistencia entre mapas
- `MapManager` carga cada mapa con `LoadScene` (single) y vuelve a cargar `UIScene` aditiva → todo lo que vive en `UIScene` (p. ej. `ExperienceManager`) se reinicia por mapa.
- Lo que debe sobrevivir la partida va en `AdministradorDeProgreso` (`DontDestroyOnLoad`), y se resetea en `ReiniciarProgreso()`.
- Como `UIScene` puede cargar después que el jugador, no depender solo de eventos de `ExperienceManager` desde el jugador: chequear el estado (ver Uzi).

### Sistema de armas (`WeaponS/WeaponSystem.cs`)
- Hay **un solo `WeaponSystem`** en `Player.prefab` (objeto `Character`). HUD, drops, power-ups, `PlayerAbilities` y `AdministradorDeProgreso` lo referencian: agregar armas como *modos* de este componente, no como scripts nuevos.
- Disparo por Raycast desde `cam` (Main Camera); daño por distancia con `CalcularDanioPorDistancia` (lerp entre `distanciaDanioMaximo` y `distanciaDanioMinimo`).
- Jerarquía: `Player/Camera` (PlayerCamera) contiene `Hands` (revólver, Animator `RightHand.controller`), `LeftHands` (segunda arma de la ulti, inactiva), `Spring/Lean/WalkBob/Main Camera` y `Gun` (muzzle de la dinamita).
- La ulti (`PlayerAbilities.ActivateUlt`) activa `LeftHands`, sube `damage` ×1.2 y lo restaura al terminar, pone `isUltActive` (sin consumo de balas, disparo alternado, cadencia ×0.75). El dash oculta `Hands`/`LeftHands`.

### Uzi (rama `new-gun-uzi`)
- **Desbloqueo:** `WeaponSystem.Update` llama a `ActivarUzi()` cuando `ExperienceManager.nivelActual >= nivelDesbloqueoUzi` (5) o `AdministradorDeProgreso.tieneUzi`. Se hace por polling (no `OnSubioDeNivel`) por el orden de carga de `UIScene`, y espera a que no haya ulti ni recarga activa (si no, la ulti restauraría el daño del revólver).
- **Reemplazo total:** `ActivarUzi` cambia stats (`uzi*`), vuelve automática el arma (gatillo mantenido) y guarda `tieneUzi = true` para los mapas siguientes. Daño y recarga se escalan por las mejoras ya aplicadas al revólver (`damage / danioBaseRevolver`).
- **Munición:** `escalaMunicion` (1 revólver, 5 Uzi) escala drops (`AddAmmo`), reembolso por kill, umbrales de drop en `EnemyHealth` y aviso de pocas balas. Con Uzi no hay reembolso por headshot.
- **Balística:** dispersión angular (bloom) que crece por disparo y se recupera con el tiempo; retroceso procedural en `LateUpdate` (rotación local de Main Camera + kick de `Hands`/`LeftHands`).
- **Visual:** el modelo `Assets/Prefabs/Player/Uzi/Mesh/Hands-Uzi.fbx` está instanciado **dentro de `Player.prefab`** (hijo de `Camera`, inactivo, ya ubicado) y asignado en `WeaponSystem.uziDerecha`. Al activarse: se apagan los renderers y el Animator del revólver, se activa `Hands-Uzi` y se cuelga de `Hands`; la Uzi de la ulti es una copia espejada colgada de `LeftHands`. `gunAnim`/`gunAnimIzquierda` pasan a apuntar a los Animator de la Uzi.
- **Animaciones:** controller `Assets/Anims/Player/Hands/Hands-Uzi.controller` (estados `Armature|Shoot`, default, y `Armature|Recharge`; triggers `Hit`, `Hit2`, `NoBullet`, `Start`). Con Uzi se usa `Animator.Play(estado, 0, 0f)` y `anim.speed` ajusta el clip a la cadencia / `tiempoRecarga`. Al activarse se salta al final del disparo como pose de reposo (no hay clip Idle).
- Stats iniciales: 13 daño (6.5 a ≥22 m), 0.08 s de cadencia, cargador 30, recarga 1.8 s, headshot ×1.5. DPS sostenido ≈ revólver (~93).

## Flujo de Trabajo en Equipo (Git)
- Tras un `git pull`, revisar `git log -n 5 --stat` para entender los cambios recientes del equipo antes de proponer código nuevo.
- Respetar los patrones y scripts establecidos por los demás integrantes.

## Estado y Próximos Pasos
**Uzi (2026-09-30, rama `new-gun-uzi`, sin commitear):** lógica, stats, dispersión, retroceso y montaje de `Hands-Uzi` implementados. En Play mode se confirmó el desbloqueo al nivel 5 y que se oculta el revólver; la última corrección (visibilidad del modelo y animaciones con `Play`) **no está probada todavía**.
- [ ] Probar en Play mode (poner `nivelDesbloqueoUzi = 1`): Uzi visible, `Shoot`/`Recharge` animando, ulti con dos Uzi.
- [ ] Revisar la Uzi izquierda espejada (`espejarUziIzquierda`); si se ve mal, conseguir un FBX de mano izquierda como `LeftHands.fbx`.
- [ ] Reubicar el muzzle flash en la boca del cañón de la Uzi (hoy conserva la posición del revólver, colgado del hueso `Uzi`).
- [ ] Ajustar stats con playtesting.
- [ ] `ExperienceOrb.prefab` aparece modificado en la rama sin ser parte de la Uzi: revisar antes de commitear.

**General:**
- [ ] Refinamiento del input de armas y sensibilidad de cámara.
- [ ] Optimización de la generación procedimental de biomas.
- [ ] Sincronización de eventos de telemetría con Unity Analytics.
