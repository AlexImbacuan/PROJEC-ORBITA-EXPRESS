using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el movimiento del repartidor en tercera persona:
/// caminar, correr, girar hacia la dirección de movimiento, saltar y gravedad.
/// Envía al Animator los parámetros "Velocidad" (float), "EnSuelo" (bool),
/// "Saltar" (trigger) y "Esperar" (trigger, tras un rato sin moverse).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ControladorRepartidor : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadCaminar = 2f;
    [SerializeField] private float velocidadCorrer = 5.5f;
    [SerializeField] private float velocidadGiro = 720f; // grados por segundo
    [SerializeField] private float aceleracion = 10f;

    [Header("Salto y gravedad")]
    [SerializeField] private float alturaSalto = 1.2f;
    [SerializeField] private float gravedad = -9.81f;

    [Header("Espera")]
    [Tooltip("Segundos sin moverse antes de reproducir la animación de espera.")]
    [SerializeField] private float tiempoParaEspera = 8f;

    [Header("Referencias")]
    [Tooltip("Cámara usada para calcular la dirección. Si se deja vacío usa Camera.main.")]
    [SerializeField] private Transform camara;

    private CharacterController controller;
    private Animator animator;

    private InputAction accionMover;
    private InputAction accionSaltar;
    private InputAction accionCorrer;

    private float velocidadActual;
    private float velocidadVertical;
    private float tiempoQuieto;

    private static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");
    private static readonly int ParamEnSuelo = Animator.StringToHash("EnSuelo");
    private static readonly int ParamSaltar = Animator.StringToHash("Saltar");
    private static readonly int ParamEsperar = Animator.StringToHash("Esperar");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;

        // Acciones definidas en Assets/InputSystem_Actions (mapa "Player").
        accionMover = InputSystem.actions.FindAction("Player/Move");
        accionSaltar = InputSystem.actions.FindAction("Player/Jump");
        accionCorrer = InputSystem.actions.FindAction("Player/Sprint");
    }

    private void Update()
    {
        Vector2 entrada = accionMover != null ? accionMover.ReadValue<Vector2>() : Vector2.zero;
        bool corriendo = accionCorrer != null && accionCorrer.IsPressed();

        // Dirección relativa a la cámara (W = hacia donde mira la cámara).
        Vector3 direccion = new Vector3(entrada.x, 0f, entrada.y);
        if (camara != null)
        {
            Vector3 adelante = Vector3.ProjectOnPlane(camara.forward, Vector3.up).normalized;
            Vector3 derecha = Vector3.ProjectOnPlane(camara.right, Vector3.up).normalized;
            direccion = adelante * entrada.y + derecha * entrada.x;
        }
        direccion = Vector3.ClampMagnitude(direccion, 1f);

        // Velocidad objetivo y suavizado.
        float velocidadObjetivo = direccion.sqrMagnitude > 0.01f
            ? (corriendo ? velocidadCorrer : velocidadCaminar) * direccion.magnitude
            : 0f;
        velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadObjetivo, aceleracion * Time.deltaTime);

        // Girar hacia la dirección de movimiento.
        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionObjetivo, velocidadGiro * Time.deltaTime);
        }

        // Salto y gravedad.
        bool enSuelo = controller.isGrounded;
        if (enSuelo && velocidadVertical < 0f)
            velocidadVertical = -2f; // mantiene al personaje pegado al suelo

        if (enSuelo && accionSaltar != null && accionSaltar.WasPressedThisFrame())
        {
            velocidadVertical = Mathf.Sqrt(alturaSalto * -2f * gravedad);
            if (animator != null) animator.SetTrigger(ParamSaltar);
        }

        velocidadVertical += gravedad * Time.deltaTime;

        // Mover.
        Vector3 movimiento = direccion.normalized * velocidadActual + Vector3.up * velocidadVertical;
        controller.Move(movimiento * Time.deltaTime);

        // Animator: Velocidad normalizada (0 = quieto, 0.5 = caminar, 1 = correr).
        if (animator != null)
        {
            float velocidadNormalizada = velocidadActual <= velocidadCaminar
                ? Mathf.InverseLerp(0f, velocidadCaminar, velocidadActual) * 0.5f
                : 0.5f + Mathf.InverseLerp(velocidadCaminar, velocidadCorrer, velocidadActual) * 0.5f;

            animator.SetFloat(ParamVelocidad, velocidadNormalizada, 0.1f, Time.deltaTime);
            animator.SetBool(ParamEnSuelo, controller.isGrounded);
        }

        // Animación de espera: si el jugador no hace nada durante un rato.
        bool hayAccion = direccion.sqrMagnitude > 0.01f || !controller.isGrounded;
        tiempoQuieto = hayAccion ? 0f : tiempoQuieto + Time.deltaTime;
        if (tiempoQuieto >= tiempoParaEspera)
        {
            tiempoQuieto = 0f;
            if (animator != null) animator.SetTrigger(ParamEsperar);
        }
    }
}
