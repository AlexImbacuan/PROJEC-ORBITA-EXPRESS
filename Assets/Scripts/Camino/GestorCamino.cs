using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Reglas del nivel del camino:
/// - La energía son celdas: cada golpe fuerte o cada caída a un hueco gasta una.
/// - La pizza tiene una cuenta regresiva: si llega a 0, se pierde.
/// - Al caer a un hueco se reaparece en el último punto de control.
/// - Se gana al llegar a la zona de entrega (Meta).
/// Hay uno por escena.
/// </summary>
public class GestorCamino : MonoBehaviour
{
    public enum Estado { Jugando, Ganado, Perdido }

    public static GestorCamino Instancia { get; private set; }

    [Header("Jugador")]
    [SerializeField] private ConductorMoto repartidor;
    [SerializeField] private ControladorMoto moto;
    [Tooltip("Dónde empieza (y primer punto de control). La flecha azul apunta hacia el camino.")]
    [SerializeField] private Transform puntoInicio;
    [SerializeField] private bool empezarEnMoto = true;

    [Header("Energía")]
    [SerializeField, Min(1)] private int celdasMaximas = 5;
    [Tooltip("Segundos sin perder otra celda después de un golpe.")]
    [SerializeField] private float invulnerableTrasGolpe = 1.2f;
    [Tooltip("Golpes contra el suelo (aterrizajes) no gastan celda si la normal apunta tanto hacia arriba.")]
    [Range(0f, 1f)] [SerializeField] private float umbralAterrizaje = 0.6f;

    [Header("Tiempo de entrega")]
    [SerializeField] private float segundosParaEntregar = 120f;

    [Header("Caídas")]
    [Tooltip("Si el jugador baja de esta altura, cayó a un hueco.")]
    [SerializeField] private float alturaCaida = -6f;

    // Eventos para el HUD (o sonidos y efectos).
    public event Action<int, int> CeldasCambiaron;   // actuales, máximas
    public event Action<string> MensajeMostrado;
    public event Action<bool> NivelTerminado;         // true = ganó

    public Estado EstadoActual { get; private set; } = Estado.Jugando;
    public int Celdas { get; private set; }
    public int CeldasMaximas => celdasMaximas;
    public float TiempoRestante { get; private set; }
    public float TiempoTotal => segundosParaEntregar;

    private Transform puntoControl;
    private float invulnerableHasta;
    private bool reapareciendo;

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { enabled = false; return; }
        Instancia = this;
    }

    private void OnEnable() => DetectorGolpes.Golpe += AlGolpear;
    private void OnDisable() => DetectorGolpes.Golpe -= AlGolpear;

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Start()
    {
        Celdas = celdasMaximas;
        TiempoRestante = segundosParaEntregar;
        puntoControl = puntoInicio;

        if (empezarEnMoto && repartidor != null && moto != null)
        {
            ColocarMoto(puntoInicio);
            moto.enabled = true;
            repartidor.SubirA(moto);
        }

        CeldasCambiaron?.Invoke(Celdas, celdasMaximas);
        Mostrar("¡Entrega la pizza antes de que se enfríe!");
    }

    private void Update()
    {
        if (EstadoActual != Estado.Jugando)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        TiempoRestante = Mathf.Max(0f, TiempoRestante - Time.deltaTime);
        if (TiempoRestante <= 0f)
        {
            Perder("¡Se acabó el tiempo! La pizza llegó fría");
            return;
        }

        if (!reapareciendo && AlturaJugador() < alturaCaida)
            StartCoroutine(CaidaAlHueco());
    }

    // ------------------------------------------------------------------ energía

    private void AlGolpear(float impacto, Vector3 normal)
    {
        if (EstadoActual != Estado.Jugando || reapareciendo) return;
        if (normal.y > umbralAterrizaje) return; // aterrizaje, no choque
        PerderCelda("¡Golpe!");
    }

    private void PerderCelda(string motivo)
    {
        if (Time.time < invulnerableHasta) return;
        invulnerableHasta = Time.time + invulnerableTrasGolpe;

        Celdas = Mathf.Max(0, Celdas - 1);
        CeldasCambiaron?.Invoke(Celdas, celdasMaximas);

        if (Celdas <= 0) Perder("¡Te quedaste sin energía! La pizza quedó destruida");
        else Mostrar($"{motivo} Energía: {Celdas}/{celdasMaximas}");
    }

    // ------------------------------------------------------------------ caídas y puntos de control

    private float AlturaJugador()
    {
        if (repartidor == null) return 0f;
        return repartidor.Montado && moto != null ? moto.transform.position.y : repartidor.transform.position.y;
    }

    private IEnumerator CaidaAlHueco()
    {
        reapareciendo = true;
        invulnerableHasta = 0f;
        PerderCelda("¡Caíste a un hueco!");
        if (EstadoActual != Estado.Jugando) { reapareciendo = false; yield break; }

        yield return new WaitForSeconds(0.6f);

        // Reaparece montado en el último punto de control.
        ColocarMoto(puntoControl);
        if (repartidor != null && !repartidor.Montado)
        {
            var controller = repartidor.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            repartidor.SubirA(moto);
        }

        var camara = FindFirstObjectByType<CamaraSeguimiento>();
        if (camara != null) camara.Colocar();

        invulnerableHasta = Time.time + invulnerableTrasGolpe;
        reapareciendo = false;
    }

    /// <summary>Lo llama una ZonaCamino de tipo PuntoControl.</summary>
    public void RegistrarPuntoControl(Transform punto)
    {
        if (EstadoActual != Estado.Jugando || punto == puntoControl) return;
        puntoControl = punto;
        Mostrar("Punto de control");
    }

    private void ColocarMoto(Transform punto)
    {
        if (moto == null || punto == null) return;
        var rb = moto.GetComponent<Rigidbody>();
        Vector3 pos = punto.position + Vector3.up * 0.6f;
        Quaternion rot = Quaternion.Euler(0f, punto.eulerAngles.y, 0f);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
        }
        moto.transform.SetPositionAndRotation(pos, rot);
    }

    // ------------------------------------------------------------------ final

    /// <summary>Lo llama la ZonaCamino de tipo Meta.</summary>
    public void LlegarAMeta()
    {
        if (EstadoActual != Estado.Jugando) return;
        EstadoActual = Estado.Ganado;
        if (moto != null) moto.ControlActivo = false;
        int seg = Mathf.CeilToInt(TiempoRestante);
        Mostrar($"¡Pizza entregada! Te sobraron {seg / 60:00}:{seg % 60:00} y {Celdas}/{celdasMaximas} celdas");
        NivelTerminado?.Invoke(true);
    }

    private void Perder(string motivo)
    {
        if (EstadoActual != Estado.Jugando) return;
        EstadoActual = Estado.Perdido;
        if (moto != null) moto.ControlActivo = false;
        Mostrar(motivo);
        NivelTerminado?.Invoke(false);
    }

    private void Mostrar(string mensaje) => MensajeMostrado?.Invoke(mensaje);
}
