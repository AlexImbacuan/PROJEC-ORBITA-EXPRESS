using UnityEngine;

/// <summary>
/// Zona de la pizzería. Al entrar el repartidor (a pie o en moto) recoge los pedidos de la ruta.
/// Necesita un Collider marcado como "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider))]
public class PuntoRecogida : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (GestorEntregas.Instancia == null) return;

        bool repartidorAPie = otro.GetComponentInParent<ConductorMoto>() != null;
        ControladorMoto moto = otro.GetComponentInParent<ControladorMoto>();
        bool motoConConductor = moto != null && moto.ControlActivo;

        if (repartidorAPie || motoConConductor)
            GestorEntregas.Instancia.RecogerPedidos();
    }
}
