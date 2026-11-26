using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara en tercera persona que sigue al repartidor con suavidad.
/// - La posición se acerca con retraso (SmoothDamp), sin tirones.
/// - En moto se coloca poco a poco detrás de la moto cuando gira.
/// - A pie se acomoda despacio detrás del personaje cuando gira.
/// - El ratón no la mueve (se puede activar con permitirGiroManual).
/// - Si hay una pared entre el personaje y la cámara, se acerca para no atravesarla.
/// No debe ser hija del personaje: va suelta en la escena.
/// </summary>
public class CamaraSeguimiento : MonoBehaviour
{
    [Tooltip("El repartidor. Cuando va en moto la sigue igual (va sentado en ella).")]
    [SerializeField] private Transform objetivo;

    [Header("Posición")]
    [Tooltip("Distancia detrás del objetivo.")]
    [SerializeField] private float distancia = 6f;
    [Tooltip("Altura sobre los pies del objetivo.")]
    [SerializeField] private float altura = 2.6f;
    [Tooltip("Segundos que tarda en alcanzar su posición. Más alto = más suave.")]
    [SerializeField] private float suavizadoPosicion = 0.25f;

    [Header("Mirada")]
    [Tooltip("Altura del punto al que mira (más o menos el pecho).")]
    [SerializeField] private float alturaMirada = 1.4f;
    [Tooltip("Cuánto mira por delante del objetivo, para ver lo que viene.")]
    [SerializeField] private float miradaAdelante = 3f;
    [SerializeField] private float suavizadoRotacion = 10f;

    [Header("Giro automático")]
    [Tooltip("Qué tan rápido se acomoda detrás de la moto al girar. Más bajo = más suave.")]
    [SerializeField] private float giroEnMoto = 2.5f;
    [Tooltip("Qué tan rápido se acomoda detrás del personaje a pie (0 = no gira sola).")]
    [SerializeField] private float giroAPie = 2f;

    [Header("Giro manual (ratón / stick derecho)")]
    [Tooltip("Desactivado: la cámara solo sigue al personaje y el ratón no la mueve.")]
    [SerializeField] private bool permitirGiroManual = false;
    [SerializeField] private float sensibilidadRaton = 0.12f;
    [SerializeField] private float sensibilidadStick = 120f;
    [Tooltip("Segundos sin tocar el ratón antes de que la cámara vuelva sola detrás de la moto.")]
    [SerializeField] private float esperaAntesDeVolver = 1.5f;

    [Header("Colisión")]
    [SerializeField] private LayerMask capasQueBloquean = ~0;
    [SerializeField] private float radioColision = 0.25f;

    private ConductorMoto conductor;
    private InputAction accionMirar;
    private float yaw;
    private float ultimoGiroManual = -10f;
    private Vector3 velocidad;

    private void Start()
    {
        if (objetivo == null)
        {
            Debug.LogWarning("CamaraSeguimiento no tiene objetivo asignado.", this);
            enabled = false;
            return;
        }

        if (transform.IsChildOf(objetivo))
            transform.SetParent(null, true); // si quedó como hija del personaje, se suelta

        conductor = objetivo.GetComponentInParent<ConductorMoto>();
        if (conductor == null) conductor = objetivo.GetComponentInChildren<ConductorMoto>();
        accionMirar = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Look") : null;

        yaw = objetivo.eulerAngles.y;
        transform.position = PosicionDeseada();
        transform.rotation = Quaternion.LookRotation(PuntoMirada() - transform.position);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Giro manual (solo si está permitido).
        if (permitirGiroManual && accionMirar != null)
        {
            Vector2 mirar = accionMirar.ReadValue<Vector2>();
            bool esRaton = accionMirar.activeControl != null && accionMirar.activeControl.device is Pointer;
            float delta = esRaton ? mirar.x * sensibilidadRaton : mirar.x * sensibilidadStick * dt;
            if (Mathf.Abs(delta) > 0.001f)
            {
                yaw += delta;
                ultimoGiroManual = Time.time;
            }
        }

        // Giro automático: suave detrás de la moto; a pie según giroAPie.
        bool enMoto = conductor != null && conductor.Montado;
        float rapidez = enMoto ? giroEnMoto : giroAPie;
        if (rapidez > 0f && Time.time - ultimoGiroManual > esperaAntesDeVolver)
            yaw = Mathf.LerpAngle(yaw, objetivo.eulerAngles.y, 1f - Mathf.Exp(-rapidez * dt));

        Vector3 deseada = EvitarParedes(PosicionDeseada());
        transform.position = Vector3.SmoothDamp(transform.position, deseada, ref velocidad, suavizadoPosicion);

        Quaternion rotacion = Quaternion.LookRotation(PuntoMirada() - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, 1f - Mathf.Exp(-suavizadoRotacion * dt));
    }

    private Vector3 PosicionDeseada()
    {
        Quaternion giro = Quaternion.Euler(0f, yaw, 0f);
        return objetivo.position + Vector3.up * altura - giro * Vector3.forward * distancia;
    }

    private Vector3 PuntoMirada()
    {
        Quaternion giro = Quaternion.Euler(0f, yaw, 0f);
        return objetivo.position + Vector3.up * alturaMirada + giro * Vector3.forward * miradaAdelante;
    }

    /// <summary>Si una pared tapa al personaje, la cámara se pone delante de esa pared.</summary>
    private Vector3 EvitarParedes(Vector3 deseada)
    {
        Vector3 origen = objetivo.position + Vector3.up * alturaMirada;
        Vector3 dir = deseada - origen;
        float largo = dir.magnitude;
        if (largo < 0.01f) return deseada;

        RaycastHit[] golpes = Physics.SphereCastAll(origen, radioColision, dir / largo, largo, capasQueBloquean, QueryTriggerInteraction.Ignore);
        float masCerca = largo;
        foreach (RaycastHit g in golpes)
        {
            // Ignora al propio repartidor y a su moto.
            if (g.transform.IsChildOf(objetivo.root) || objetivo.IsChildOf(g.transform.root)) continue;
            if (g.distance > 0f && g.distance < masCerca) masCerca = g.distance;
        }
        return origen + dir / largo * Mathf.Max(masCerca - 0.1f, 1f);
    }

    /// <summary>Por si otro script cambia el objetivo (por ejemplo al reaparecer).</summary>
    public void Colocar()
    {
        if (objetivo == null) return;
        yaw = objetivo.eulerAngles.y;
        transform.position = PosicionDeseada();
        velocidad = Vector3.zero;
    }
}
