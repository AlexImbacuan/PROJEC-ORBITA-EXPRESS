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

    private float ultimoGolpe = -10f;

    private void OnCollisionEnter(Collision choque)
    {
        if (GestorEntregas.Instancia == null) return;
        if (Time.time - ultimoGolpe < tiempoEntreGolpes) return;

        float impacto = choque.relativeVelocity.magnitude;
        if (impacto < impactoMinimo) return;

        ultimoGolpe = Time.time;
        GestorEntregas.Instancia.RegistrarGolpe((impacto - impactoMinimo) * danioPorImpacto);
    }
}
