using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Carga la escena siguiente cuando termina una cinemática, o antes si el jugador la salta.
/// Sirve para cinemáticas de Timeline (PlayableDirector) y para CinematicaSalidaPizzeria.
/// Asigna solo la que corresponda.
/// </summary>
public class IrAEscenaAlTerminar : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena a cargar (debe estar en Build Profiles).")]
    [SerializeField] private string escenaSiguiente = "MenuPrincipal";

    [Header("Cinemática (asigna una)")]
    [Tooltip("Para cinemáticas hechas con Timeline.")]
    [SerializeField] private PlayableDirector director;
    [Tooltip("Para la cinemática de salida de la pizzería.")]
    [SerializeField] private CinematicaSalidaPizzeria cinematicaPizzeria;

    [Header("Saltar")]
    [SerializeField] private bool permitirSaltar = true;
    [Tooltip("Segundos al inicio en los que no se puede saltar, para no saltarla sin querer.")]
    [SerializeField] private float esperaAntesDePermitirSaltar = 1f;

    private bool cargando;

    private void OnEnable()
    {
        if (director != null) director.stopped += AlTerminarDirector;
    }

    private void OnDisable()
    {
        if (director != null) director.stopped -= AlTerminarDirector;
    }

    private void Update()
    {
        if (cargando) return;

        // Con Wrap Mode "Hold" el Timeline no avisa con "stopped": se queda en el último cuadro.
        bool timelineEnFinal = director != null && director.duration > 0
                               && director.time >= director.duration - 0.05;
        bool pizzeriaTerminada = cinematicaPizzeria != null && cinematicaPizzeria.Terminada;

        if (timelineEnFinal || pizzeriaTerminada)
        {
            Cargar();
            return;
        }

        if (permitirSaltar && Time.timeSinceLevelLoad >= esperaAntesDePermitirSaltar && SePidioSaltar())
            Cargar();
    }

    private static bool SePidioSaltar()
    {
        bool tecla = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool clic = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool boton = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        return tecla || clic || boton;
    }

    private void AlTerminarDirector(PlayableDirector _) => Cargar();

    private void Cargar()
    {
        if (cargando) return;
        cargando = true;
        SceneManager.LoadScene(escenaSiguiente);
    }
}
