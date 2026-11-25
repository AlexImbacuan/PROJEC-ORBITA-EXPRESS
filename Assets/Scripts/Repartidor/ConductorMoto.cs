using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite al repartidor subirse y bajarse de una moto con la acción Interact (tecla E).
/// Al subirse: desactiva el movimiento a pie, se sienta en el asiento de la moto,
/// activa la animación "Manejando" y le pasa el control a la moto.
/// Al bajarse: hace lo contrario y lo deja de pie a un costado.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ControladorRepartidor))]
public class ConductorMoto : MonoBehaviour
{
    [Tooltip("Distancia máxima a la moto para poder subirse.")]
    [SerializeField] private float radioDeteccion = 2.5f;
    [Tooltip("Velocidad máxima (m/s) a la que se permite bajarse, para no saltar de una moto en marcha.")]
    [SerializeField] private float velocidadMaximaParaBajar = 3f;

    [Header("IK al manejar")]
    [Tooltip("Cuánto siguen los pies y manos a los puntos de apoyo de la moto (0 = nada, 1 = exacto).")]
    [Range(0f, 1f)] [SerializeField] private float pesoPosicionIK = 1f;
    [Tooltip("Cuánto copian la rotación de los puntos de apoyo.")]
    [Range(0f, 1f)] [SerializeField] private float pesoRotacionIK = 0.7f;

    [Header("Inclinación del torso al manejar")]
    [Tooltip("Cuánto dobla la espalda hacia el manubrio (0 = recto, 1 = máximo).")]
    [Range(0f, 1f)] [SerializeField] private float inclinacionTorso = 0.5f;
    [Tooltip("Qué tan abajo del manubrio está el punto hacia el que se inclina. Más alto = se agacha más.")]
    [SerializeField] private float bajadaPuntoTorso = 0.6f;
    [Tooltip("Qué tan adelante del manubrio está el punto hacia el que se inclina.")]
    [SerializeField] private float avancePuntoTorso = 0.8f;
    [Tooltip("Cuánto gira la cabeza hacia ese punto. Bajo = sigue mirando al frente.")]
    [Range(0f, 1f)] [SerializeField] private float pesoCabeza = 0.1f;

    private CharacterController controller;
    private ControladorRepartidor movimientoAPie;
    private Animator animator;
    private InputAction accionInteractuar;

    private ControladorMoto motoActual;

    private static readonly int ParamManejando = Animator.StringToHash("Manejando");
    private static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");

    /// <summary>True mientras va montado en una moto.</summary>
    public bool Montado => motoActual != null;

    /// <summary>
    /// Si es true, la tecla E no hace nada (por ejemplo durante una cinemática).
    /// </summary>
    public bool EntradaBloqueada { get; set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movimientoAPie = GetComponent<ControladorRepartidor>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        accionInteractuar = InputSystem.actions.FindAction("Player/Interact");
    }

    private void Update()
    {
        if (EntradaBloqueada) return;
        if (accionInteractuar == null || !accionInteractuar.WasPressedThisFrame()) return;

        if (Montado)
            IntentarBajar();
        else
            IntentarSubir();
    }

    private void IntentarSubir()
    {
        ControladorMoto moto = BuscarMotoCercana();
        if (moto != null) SubirA(moto);
    }

    /// <summary>
    /// Sube al conductor a la moto indicada sin buscarla ni pulsar E
    /// (lo usan las cinemáticas). Devuelve false si no se pudo.
    /// </summary>
    public bool SubirA(ControladorMoto moto)
    {
        if (moto == null || Montado) return false;

        if (moto.Asiento == null)
        {
            Debug.LogWarning($"La moto '{moto.name}' no tiene asignado el campo Asiento en Controlador Moto.", moto);
            return false;
        }

        motoActual = moto;
        moto.ControlActivo = true;

        // Sin colisionador ni movimiento propio mientras va sentado.
        movimientoAPie.enabled = false;
        controller.enabled = false;

        transform.SetParent(moto.Asiento, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (animator != null)
        {
            animator.SetFloat(ParamVelocidad, 0f);
            animator.SetBool(ParamManejando, true);
        }

        return true;
    }

    private void IntentarBajar()
    {
        // No se puede bajar de una moto en marcha: hay que frenar primero.
        if (Mathf.Abs(motoActual.VelocidadAdelante) > velocidadMaximaParaBajar) return;

        ControladorMoto moto = motoActual;
        motoActual = null;
        moto.ControlActivo = false;

        // Posición de bajada: el punto configurado o, si no hay, a la izquierda de la moto.
        Vector3 posicion = moto.PuntoBajada != null
            ? moto.PuntoBajada.position
            : moto.transform.position - moto.transform.right * 1.2f;

        // De pie y mirando hacia donde mira la moto (sin inclinación).
        Vector3 adelante = Vector3.ProjectOnPlane(moto.transform.forward, Vector3.up);
        Quaternion rotacion = adelante.sqrMagnitude > 0.001f ? Quaternion.LookRotation(adelante) : Quaternion.identity;

        transform.SetParent(null);
        transform.SetPositionAndRotation(posicion, rotacion);

        controller.enabled = true;
        movimientoAPie.enabled = true;

        if (animator != null)
            animator.SetBool(ParamManejando, false);
    }

    private ControladorMoto BuscarMotoCercana()
    {
        Collider[] cercanos = Physics.OverlapSphere(transform.position, radioDeteccion, ~0, QueryTriggerInteraction.Ignore);

        ControladorMoto masCercana = null;
        float menorDistancia = float.MaxValue;

        foreach (Collider c in cercanos)
        {
            ControladorMoto moto = c.GetComponentInParent<ControladorMoto>();
            if (moto == null) continue;

            float distancia = Vector3.Distance(transform.position, moto.transform.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                masCercana = moto;
            }
        }

        return masCercana;
    }

    /// <summary>
    /// Unity lo llama en cada cuadro si la capa del Animator tiene activado "IK Pass".
    /// Mientras va montado, lleva pies y manos a los puntos de apoyo de la moto.
    /// </summary>
    private void OnAnimatorIK(int capa)
    {
        if (animator == null) return;

        InclinarTorso();

        AplicarIK(AvatarIKGoal.LeftFoot, Montado ? motoActual.PieIzquierdo : null);
        AplicarIK(AvatarIKGoal.RightFoot, Montado ? motoActual.PieDerecho : null);
        AplicarIK(AvatarIKGoal.LeftHand, Montado ? motoActual.ManoIzquierda : null);
        AplicarIK(AvatarIKGoal.RightHand, Montado ? motoActual.ManoDerecha : null);
    }

    /// <summary>
    /// Usa el "mirar hacia" del Animator para doblar la columna hacia un punto
    /// delante y debajo del manubrio, así los brazos alcanzan los puños.
    /// </summary>
    private void InclinarTorso()
    {
        bool hayManubrio = Montado && motoActual.ManoIzquierda != null && motoActual.ManoDerecha != null;
        if (!hayManubrio || inclinacionTorso <= 0f)
        {
            animator.SetLookAtWeight(0f);
            return;
        }

        // Ejes del asiento: se inclinan junto con la moto en las curvas.
        Transform referencia = motoActual.Asiento;
        Vector3 centroManubrio = (motoActual.ManoIzquierda.position + motoActual.ManoDerecha.position) * 0.5f;
        Vector3 punto = centroManubrio
                        + referencia.forward * avancePuntoTorso
                        - referencia.up * bajadaPuntoTorso;

        animator.SetLookAtWeight(1f, inclinacionTorso, pesoCabeza, 0f, 0.5f);
        animator.SetLookAtPosition(punto);
    }

    private void AplicarIK(AvatarIKGoal parte, Transform objetivo)
    {
        if (objetivo == null)
        {
            animator.SetIKPositionWeight(parte, 0f);
            animator.SetIKRotationWeight(parte, 0f);
            return;
        }

        animator.SetIKPositionWeight(parte, pesoPosicionIK);
        animator.SetIKRotationWeight(parte, pesoRotacionIK);
        animator.SetIKPosition(parte, objetivo.position);
        animator.SetIKRotation(parte, objetivo.rotation);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}
