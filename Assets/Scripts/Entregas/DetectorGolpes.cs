using UnityEngine;

/// <summary>
/// Va en la moto (junto al Rigidbody). Cuando choca fuerte, daña la caja de pedidos.
/// Los choques suaves (rozar una pared, aterrizar tranquilo) no cuentan.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DetectorGolpes : MonoBehaviour
{
    [Tooltip("Velocidad de impacto (m/s) a partir de la cual el choque cuenta como golpe.")]
    [SerializeField] private float impactoMinimo = 4f;
    [Tooltip("Daño a la caja por cada m/s de impacto por encima del mínimo.")]
    [SerializeField] private float danioPorImpacto = 5f;
    [Tooltip("Segundos entre golpes, para que un solo choque no cuente varias veces.")]
    [SerializeField] private float tiempoEntreGolpes = 0.5f;
    [Tooltip("No cuenta como golpe caer sobre el suelo (por ejemplo al aterrizar de un salto).")]
    [SerializeField] private bool ignorarAterrizajes = true;
    [Tooltip("Qué tanto debe apuntar hacia arriba la superficie para considerarla suelo (0-1).")]
    [Range(0f, 1f)] [SerializeField] private float umbralSuelo = 0.6f;

    private float ultimoGolpe = -10f;

    /// <summary>
    /// Se dispara con cada golpe fuerte: velocidad de impacto (m/s) y normal del contacto
    /// (hacia arriba = aterrizó sobre el suelo). Lo usan otros sistemas,
    /// por ejemplo las celdas de energía del GestorCamino.
    /// </summary>
    public static event System.Action<float, Vector3> Golpe;

    private void OnCollisionEnter(Collision choque)
    {
        if (Time.time - ultimoGolpe < tiempoEntreGolpes) return;

        float impacto = choque.relativeVelocity.magnitude;
        if (impacto < impactoMinimo) return;

        // Aterrizar sobre el suelo no es un choque (ni gasta el tiempo entre golpes).
        Vector3 normal = choque.contactCount > 0 ? choque.GetContact(0).normal : Vector3.zero;
        if (ignorarAterrizajes && normal.y > umbralSuelo) return;

        ultimoGolpe = Time.time;
        Golpe?.Invoke(impacto, normal);

        if (GestorEntregas.Instancia != null)
            GestorEntregas.Instancia.RegistrarGolpe((impacto - impactoMinimo) * danioPorImpacto);
    }
}
