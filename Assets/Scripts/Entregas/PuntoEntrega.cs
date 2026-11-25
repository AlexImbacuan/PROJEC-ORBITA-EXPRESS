using UnityEngine;

/// <summary>
/// Zona de un cliente. El pedido se entrega cuando el repartidor entra A PIE
/// y este es el destino actual de la ruta. Si llega en moto, se le pide que se baje.
/// Necesita un Collider marcado como "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider))]
public class PuntoEntrega : MonoBehaviour
{
    [Tooltip("Objeto visual (flecha, luz, columna) que se muestra solo cuando este es el destino actual.")]
    [SerializeField] private GameObject marcador;

    public bool EsDestinoActual { get; private set; }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>Lo llama el GestorEntregas para mostrar u ocultar el marcador.</summary>
    public void MarcarComoDestino(bool esDestino)
    {
        EsDestinoActual = esDestino;
        if (marcador != null) marcador.SetActive(esDestino);
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsDestinoActual || GestorEntregas.Instancia == null) return;

        // A pie: el CharacterController del repartidor entra a la zona.
        ConductorMoto repartidor = otro.GetComponentInParent<ConductorMoto>();
        if (repartidor != null && !repartidor.Montado)
        {
            GestorEntregas.Instancia.IntentarEntregar(this);
            return;
        }

        // En moto: el repartidor no tiene collider mientras maneja, entra el de la moto.
        ControladorMoto moto = otro.GetComponentInParent<ControladorMoto>();
        if (moto != null && moto.ControlActivo)
            GestorEntregas.Instancia.Mostrar("Bájate de la moto (E) para entregar el pedido");
    }
}
