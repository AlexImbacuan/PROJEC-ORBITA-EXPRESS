using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moto flotante con física (Rigidbody).
/// Flota con cuatro "resortes" (rayos hacia el suelo desde las esquinas del BoxCollider),
/// se endereza sola, se inclina siguiendo el terreno y se maneja con Move (WASD) y Sprint (Shift).
/// Al flotar no toca el suelo, así que funciona igual sobre tierra, nieve o agua (si el agua tiene collider).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class ControladorMoto : MonoBehaviour
{
    [Header("Flotación")]
    [Tooltip("Distancia entre la parte baja de la moto y el suelo.")]
    [SerializeField] private float alturaFlote = 0.4f;
    [Tooltip("Qué tan fuerte empuja cada resorte hacia arriba.")]
    [SerializeField] private float fuerzaResorte = 60f;
    [Tooltip("Frena el rebote de los resortes. Más alto = menos rebote.")]
    [SerializeField] private float amortiguacion = 10f;
    [Tooltip("Capas que cuentan como suelo para flotar.")]
    [SerializeField] private LayerMask capasSuelo = ~0;

    [Header("Manejo")]
    [SerializeField] private float aceleracion = 15f;
    [SerializeField] private float velocidadMaxima = 14f;
    [SerializeField] private float velocidadMaximaReversa = 5f;
    [SerializeField] private float multiplicadorTurbo = 1.6f;
    [Tooltip("Velocidad de giro máxima, en grados por segundo.")]
    [SerializeField] private float velocidadGiro = 100f;
    [Tooltip("Qué tan rápido alcanza (o frena) el giro. Más alto = más preciso.")]
    [SerializeField] private float respuestaGiro = 6f;
    [Tooltip("Qué tanto evita deslizarse de lado. Bajo = resbala (útil para hielo).")]
    [SerializeField] private float agarreLateral = 4f;
    [Tooltip("Qué tan rápido se detiene al soltar el acelerador.")]
    [SerializeField] private float frenadoSuave = 1.5f;

    [Header("Salto")]
    [Tooltip("Altura aproximada del salto, en metros.")]
    [SerializeField] private float alturaSalto = 1.5f;
    [Tooltip("Segundos tras saltar en los que los resortes no empujan (para que no frenen el salto).")]
    [SerializeField] private float tiempoSinFlotarTrasSalto = 0.3f;

    [Header("Estabilidad")]
    [Tooltip("Fuerza con la que se endereza para quedar paralela al suelo.")]
    [SerializeField] private float fuerzaEnderezar = 8f;
    [Tooltip("Frena el balanceo hacia adelante/atrás y a los lados. Más alto = más firme.")]
    [SerializeField] private float amortiguacionInclinacion = 4f;
    [Tooltip("Altura del centro de masa desde la base de la moto. Más bajo = más estable.")]
    [SerializeField] private float alturaCentroMasa = 0.3f;

    [Header("Control")]
    [Tooltip("Si está desactivado la moto flota pero no responde al teclado. Lo activa el conductor al subirse.")]
    [SerializeField] private bool controlActivo = false;

    [Header("Conductor")]
    [Tooltip("Punto donde se coloca el conductor (hijo de Modelo para que se incline con la moto). Su flecha azul debe mirar al frente.")]
    [SerializeField] private Transform asiento;
    [Tooltip("Punto donde aparece el conductor al bajarse (a un costado de la moto).")]
    [SerializeField] private Transform puntoBajada;

    [Header("Puntos de apoyo del conductor (IK, opcionales)")]
    [Tooltip("Dónde apoya el pie izquierdo. Hijo de Modelo. Flecha azul = hacia donde apunta la punta del pie.")]
    [SerializeField] private Transform pieIzquierdo;
    [SerializeField] private Transform pieDerecho;
    [Tooltip("Dónde agarra el manubrio la mano izquierda. Hijo de Modelo.")]
    [SerializeField] private Transform manoIzquierda;
    [SerializeField] private Transform manoDerecha;

    public Transform Asiento => asiento;
    public Transform PuntoBajada => puntoBajada;
    public Transform PieIzquierdo => pieIzquierdo;
    public Transform PieDerecho => pieDerecho;
    public Transform ManoIzquierda => manoIzquierda;
    public Transform ManoDerecha => manoDerecha;

    [Header("Depuración")]
    [Tooltip("Muestra en pantalla el estado de la moto (entrada, suelo, velocidad). Solo para pruebas.")]
    [SerializeField] private bool mostrarDepuracion = false;

    private const float MargenRayo = 0.05f;
    private int apoyosUltimoPaso;

    private Rigidbody rb;
    private BoxCollider caja;
    private InputAction accionMover;
    private InputAction accionTurbo;
    private InputAction accionSaltar;

    private Vector2 entrada;
    private bool turbo;
    private bool saltoPedido;
    private float tiempoSinFlotar;
    private readonly Vector3[] puntosFlote = new Vector3[4];

    /// <summary>Velocidad hacia adelante en m/s (negativa en reversa).</summary>
    public float VelocidadAdelante { get; private set; }

    /// <summary>True si al menos un resorte toca el suelo.</summary>
    public bool EnSuelo { get; private set; }

    public bool ControlActivo
    {
        get => controlActivo;
        set => controlActivo = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        caja = GetComponent<BoxCollider>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        accionMover = InputSystem.actions.FindAction("Player/Move");
        accionTurbo = InputSystem.actions.FindAction("Player/Sprint");
        accionSaltar = InputSystem.actions.FindAction("Player/Jump");

        CalcularPuntosFlote();

        // Centro de masa bajo (cerca de los propulsores) para que no se mezca como un péndulo.
        Vector3 c = caja.center;
        rb.centerOfMass = new Vector3(c.x, c.y - caja.size.y * 0.5f + alturaCentroMasa, c.z);
    }

    private void Update()
    {
        // La entrada se lee en Update y la física se aplica en FixedUpdate.
        entrada = controlActivo && accionMover != null ? accionMover.ReadValue<Vector2>() : Vector2.zero;
        turbo = controlActivo && accionTurbo != null && accionTurbo.IsPressed();

        // Se guarda el pedido y se aplica en FixedUpdate (la física va allí).
        if (controlActivo && accionSaltar != null && accionSaltar.WasPressedThisFrame())
            saltoPedido = true;
    }

    private void FixedUpdate()
    {
        if (tiempoSinFlotar > 0f)
            tiempoSinFlotar -= Time.fixedDeltaTime;

        Vector3 normalSuelo = AplicarFlotacion();
        Saltar();
        Enderezar(EnSuelo ? normalSuelo : Vector3.up);

        Vector3 adelante = Vector3.ProjectOnPlane(transform.forward, EnSuelo ? normalSuelo : Vector3.up).normalized;
        VelocidadAdelante = Vector3.Dot(rb.linearVelocity, adelante);

        if (!EnSuelo) return; // en el aire no se puede acelerar ni girar

        Acelerar(adelante);
        Girar();
        AgarreLateral();
    }

    /// <summary>Lanza un rayo desde cada esquina y empuja hacia arriba como un resorte.</summary>
    private Vector3 AplicarFlotacion()
    {
        // Justo después de saltar los resortes no empujan, si no frenarían el salto.
        if (tiempoSinFlotar > 0f)
        {
            EnSuelo = false;
            return Vector3.up;
        }

        int apoyos = 0;
        Vector3 sumaNormales = Vector3.zero;
        float largoRayo = alturaFlote * 2f + MargenRayo;

        foreach (Vector3 puntoLocal in puntosFlote)
        {
            Vector3 origen = transform.TransformPoint(puntoLocal);
            if (!Physics.Raycast(origen, -transform.up, out RaycastHit hit, largoRayo, capasSuelo, QueryTriggerInteraction.Ignore))
                continue;

            float distancia = hit.distance - MargenRayo;
            float compresion = alturaFlote - distancia;
            float velocidadVertical = Vector3.Dot(rb.GetPointVelocity(origen), transform.up);
            float fuerza = compresion * fuerzaResorte - velocidadVertical * amortiguacion;

            rb.AddForceAtPosition(transform.up * fuerza, origen, ForceMode.Acceleration);
            apoyos++;
            sumaNormales += hit.normal;
        }

        apoyosUltimoPaso = apoyos;
        EnSuelo = apoyos > 0;
        return EnSuelo ? sumaNormales.normalized : Vector3.up;
    }

    /// <summary>
    /// Salta solo si está flotando sobre algo y hay gravedad.
    /// En el aire o en zonas sin gravedad el pedido de salto se ignora.
    /// </summary>
    private void Saltar()
    {
        if (!saltoPedido) return;
        saltoPedido = false;

        float gravedad = Physics.gravity.magnitude;
        bool hayGravedad = rb.useGravity && gravedad > 0.01f;
        if (!EnSuelo || !hayGravedad) return;

        // Velocidad necesaria para alcanzar la altura: v = raíz(2 · g · h).
        float velocidadSalto = Mathf.Sqrt(2f * gravedad * alturaSalto);
        Vector3 arriba = -Physics.gravity.normalized;
        float velocidadVerticalActual = Vector3.Dot(rb.linearVelocity, arriba);

        rb.AddForce(arriba * (velocidadSalto - velocidadVerticalActual), ForceMode.VelocityChange);
        tiempoSinFlotar = tiempoSinFlotarTrasSalto;
        EnSuelo = false;
    }

    private void Enderezar(Vector3 arribaObjetivo)
    {
        Vector3 eje = Vector3.Cross(transform.up, arribaObjetivo);

        // Rotación de inclinación (todo menos el giro sobre el eje vertical), para frenarla.
        Vector3 velocidadAngular = rb.angularVelocity;
        Vector3 inclinacion = velocidadAngular - transform.up * Vector3.Dot(velocidadAngular, transform.up);

        rb.AddTorque(eje * fuerzaEnderezar - inclinacion * amortiguacionInclinacion, ForceMode.Acceleration);
    }

    private void Acelerar(Vector3 adelante)
    {
        float maxima = entrada.y >= 0f
            ? velocidadMaxima * (turbo ? multiplicadorTurbo : 1f)
            : velocidadMaximaReversa;

        bool avanzando = entrada.y > 0.01f && VelocidadAdelante < maxima;
        bool retrocediendo = entrada.y < -0.01f && VelocidadAdelante > -maxima;

        if (avanzando || retrocediendo)
            rb.AddForce(adelante * (entrada.y * aceleracion), ForceMode.Acceleration);
        else if (Mathf.Abs(entrada.y) <= 0.01f)
            rb.AddForce(-adelante * (VelocidadAdelante * frenadoSuave), ForceMode.Acceleration);
    }

    private void Girar()
    {
        // En reversa el giro se invierte, como en un vehículo real.
        float sentido = VelocidadAdelante < -0.5f ? -1f : 1f;

        // Se fija la velocidad de giro deseada: al soltar la tecla el objetivo es 0 y deja de girar.
        float giroDeseado = entrada.x * sentido * velocidadGiro * Mathf.Deg2Rad;
        float giroActual = Vector3.Dot(rb.angularVelocity, transform.up);
        rb.AddTorque(transform.up * ((giroDeseado - giroActual) * respuestaGiro), ForceMode.Acceleration);
    }

    private void AgarreLateral()
    {
        float velocidadLateral = Vector3.Dot(rb.linearVelocity, transform.right);
        rb.AddForce(-transform.right * (velocidadLateral * agarreLateral), ForceMode.Acceleration);
    }

    /// <summary>Las cuatro esquinas inferiores del BoxCollider, un poco hacia adentro.</summary>
    private void CalcularPuntosFlote()
    {
        if (caja == null) caja = GetComponent<BoxCollider>();

        Vector3 c = caja.center;
        Vector3 mitad = caja.size * 0.5f;
        float y = c.y - mitad.y + MargenRayo;
        float x = mitad.x * 0.8f;
        float z = mitad.z * 0.8f;

        puntosFlote[0] = new Vector3(c.x - x, y, c.z + z);
        puntosFlote[1] = new Vector3(c.x + x, y, c.z + z);
        puntosFlote[2] = new Vector3(c.x - x, y, c.z - z);
        puntosFlote[3] = new Vector3(c.x + x, y, c.z - z);
    }

    // Se ejecuta al agregar el componente en el editor.
    private void Reset()
    {
        AjustarColliderAlModelo();
    }

    /// <summary>Ajusta el BoxCollider al tamaño del modelo hijo. Clic derecho en el componente para usarlo.</summary>
    [ContextMenu("Ajustar collider al modelo")]
    private void AjustarColliderAlModelo()
    {
        Renderer[] renders = GetComponentsInChildren<Renderer>();
        if (renders.Length == 0) return;

        Bounds limites = renders[0].bounds;
        for (int i = 1; i < renders.Length; i++)
            limites.Encapsulate(renders[i].bounds);

        BoxCollider colisionador = GetComponent<BoxCollider>();
        Vector3 tamano = transform.InverseTransformVector(limites.size);
        colisionador.center = transform.InverseTransformPoint(limites.center);
        colisionador.size = new Vector3(Mathf.Abs(tamano.x), Mathf.Abs(tamano.y), Mathf.Abs(tamano.z));
    }

    private void OnGUI()
    {
        if (!mostrarDepuracion || rb == null) return;

        string texto =
            $"MOTO: {name}\n" +
            $"Control activo: {controlActivo}\n" +
            $"Acción Move encontrada: {accionMover != null}  (habilitada: {accionMover?.enabled})\n" +
            $"Entrada: {entrada}   Turbo: {turbo}\n" +
            $"En suelo: {EnSuelo}   Resortes tocando: {apoyosUltimoPaso}/4\n" +
            $"Velocidad adelante: {VelocidadAdelante:0.00} m/s\n" +
            $"Rigidbody dormido: {rb.IsSleeping()}   Kinematic: {rb.isKinematic}\n" +
            $"Posición: {transform.position}";

        GUI.Box(new Rect(10, 10, 460, 140), GUIContent.none);
        GUI.Label(new Rect(20, 15, 450, 135), texto);
    }

    // Dibuja los rayos de flotación en la Scene al seleccionar la moto.
    private void OnDrawGizmosSelected()
    {
        if (caja == null) caja = GetComponent<BoxCollider>();
        if (caja == null) return;

        CalcularPuntosFlote();
        Gizmos.color = Color.cyan;
        foreach (Vector3 puntoLocal in puntosFlote)
        {
            Vector3 origen = transform.TransformPoint(puntoLocal);
            Gizmos.DrawLine(origen, origen - transform.up * (alturaFlote + MargenRayo));
            Gizmos.DrawWireSphere(origen - transform.up * (alturaFlote + MargenRayo), 0.05f);
        }
    }
}
