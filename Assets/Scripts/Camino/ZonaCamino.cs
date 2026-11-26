using UnityEngine;

/// <summary>
/// Zona del camino (necesita un Collider con "Is Trigger"):
/// - PuntoControl: guarda dónde reaparecer si se cae a un hueco.
/// - Meta: la casa del cliente; al llegar se entrega la pizza y se gana.
/// Cuenta tanto si llega en moto como a pie.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaCamino : MonoBehaviour
{
    public enum Tipo { PuntoControl, Meta }

    [SerializeField] private Tipo tipo = Tipo.PuntoControl;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (GestorCamino.Instancia == null) return;

        bool esJugador = otro.GetComponentInParent<ConductorMoto>() != null
                         || otro.GetComponentInParent<ControladorMoto>() != null;
        if (!esJugador) return;

        if (tipo == Tipo.Meta) GestorCamino.Instancia.LlegarAMeta();
        else GestorCamino.Instancia.RegistrarPuntoControl(transform);
    }
}
