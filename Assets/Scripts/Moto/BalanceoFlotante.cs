using UnityEngine;

/// <summary>
/// Efectos visuales de la moto flotante (no afectan la física):
/// - Sube y baja suavemente y se mece un poco (flotar).
/// - Se inclina hacia el lado de la curva al girar.
/// - Levanta la nariz al acelerar y la baja al frenar.
/// Va en el objeto "Modelo" (hijo de la moto); lee el Rigidbody del padre.
/// </summary>
public class BalanceoFlotante : MonoBehaviour
{
    [Header("Flotar")]
    [Tooltip("Cuánto sube y baja, en metros.")]
    [SerializeField] private float amplitud = 0.05f;
    [Tooltip("Ciclos de sube y baja por segundo.")]
    [SerializeField] private float frecuencia = 1.2f;
    [Tooltip("Mecido de lado a lado mientras flota, en grados.")]
    [SerializeField] private float balanceoLateral = 1.5f;

    [Header("Inclinación al girar")]
    [Tooltip("Inclinación máxima hacia el lado de la curva, en grados.")]
    [SerializeField] private float inclinacionMaximaGiro = 15f;
    [Tooltip("Grados de inclinación por cada grado/segundo de giro (100°/s × 0.15 = 15°).")]
    [SerializeField] private float sensibilidadGiro = 0.15f;
    [Tooltip("Velocidad (m/s) a la que alcanza la inclinación completa. Más lento = se inclina menos.")]
    [SerializeField] private float velocidadInclinacionCompleta = 10f;

    [Header("Inclinación al acelerar")]
    [Tooltip("Inclinación máxima de la nariz hacia arriba/abajo, en grados.")]
    [SerializeField] private float inclinacionMaximaAceleracion = 5f;
    [Tooltip("Grados de inclinación por cada m/s² de aceleración.")]
    [SerializeField] private float sensibilidadAceleracion = 0.35f;

    [Header("Suavizado")]
    [Tooltip("Qué tan rápido llega a la inclinación deseada. Más alto = más brusco.")]
    [SerializeField] private float suavizado = 5f;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private Rigidbody rb;

    private float velocidadAnterior;
    private float alabeoObjetivo;   // inclinación lateral (curvas)
    private float cabeceoObjetivo;  // nariz arriba/abajo (aceleración)
    private float alabeo;
    private float cabeceo;

    private void Awake()
    {
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
        rb = GetComponentInParent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Transform moto = rb.transform;
        float velocidadAdelante = Vector3.Dot(rb.linearVelocity, moto.forward);

        // Curvas: giro positivo (a la derecha) => inclinación a la derecha (ángulo Z negativo).
        float giroGrados = Vector3.Dot(rb.angularVelocity, moto.up) * Mathf.Rad2Deg; // grados/s
        float factorVelocidad = Mathf.Clamp01(Mathf.Abs(velocidadAdelante) / velocidadInclinacionCompleta);
        alabeoObjetivo = Mathf.Clamp(-giroGrados * sensibilidadGiro * factorVelocidad,
                                     -inclinacionMaximaGiro, inclinacionMaximaGiro);

        // Aceleración: acelerar => nariz arriba (ángulo X negativo); frenar => nariz abajo.
        float aceleracion = (velocidadAdelante - velocidadAnterior) / Time.fixedDeltaTime;
        velocidadAnterior = velocidadAdelante;
        cabeceoObjetivo = Mathf.Clamp(-aceleracion * sensibilidadAceleracion,
                                      -inclinacionMaximaAceleracion, inclinacionMaximaAceleracion);
    }

    private void Update()
    {
        float t = Time.time * frecuencia * Mathf.PI * 2f;
        float k = 1f - Mathf.Exp(-suavizado * Time.deltaTime);

        alabeo = Mathf.Lerp(alabeo, alabeoObjetivo, k);
        cabeceo = Mathf.Lerp(cabeceo, cabeceoObjetivo, k);

        float mecido = Mathf.Sin(t * 0.5f) * balanceoLateral;

        transform.localPosition = posicionInicial + Vector3.up * (Mathf.Sin(t) * amplitud);
        // Se multiplica por la izquierda para girar sobre los ejes del padre (X = derecha, Z = adelante),
        // así funciona aunque el Modelo esté rotado dentro del contenedor.
        transform.localRotation = Quaternion.Euler(cabeceo, 0f, alabeo + mecido) * rotacionInicial;
    }
}
