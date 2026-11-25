using TMPro;
using UnityEngine;

/// <summary>
/// Muestra en pantalla el estado de la ruta: pedido actual, tiempo, temperatura,
/// estado de la caja, puntos y mensajes temporales.
/// Va en un Canvas con dos textos de TextMeshPro.
/// </summary>
public class HUDEntregas : MonoBehaviour
{
    [Tooltip("Texto con la información de la ruta (esquina superior).")]
    [SerializeField] private TextMeshProUGUI textoEstado;
    [Tooltip("Texto para avisos temporales (centro o parte inferior).")]
    [SerializeField] private TextMeshProUGUI textoMensaje;
    [Tooltip("Segundos que se ve cada aviso.")]
    [SerializeField] private float duracionMensaje = 3f;

    private GestorEntregas gestor;
    private float mensajeHasta;

    private void Start()
    {
        gestor = GestorEntregas.Instancia;
        if (gestor == null)
        {
            Debug.LogWarning("No hay GestorEntregas en la escena; el HUD no mostrará nada.", this);
            enabled = false;
            return;
        }

        gestor.MensajeMostrado += MostrarMensaje;
        if (textoMensaje != null) textoMensaje.text = "";
    }

    private void OnDestroy()
    {
        if (gestor != null) gestor.MensajeMostrado -= MostrarMensaje;
    }

    private void Update()
    {
        if (textoEstado != null)
            textoEstado.text = ConstruirTextoEstado();

        if (textoMensaje != null && Time.time > mensajeHasta)
            textoMensaje.text = "";
    }

    private void MostrarMensaje(string mensaje)
    {
        if (textoMensaje == null) return;
        textoMensaje.text = mensaje;
        mensajeHasta = Time.time + duracionMensaje;
    }

    private string ConstruirTextoEstado()
    {
        switch (gestor.Estado)
        {
            case GestorEntregas.EstadoRuta.SinRecoger:
                return "Recoge los pedidos en la pizzería";

            case GestorEntregas.EstadoRuta.Completada:
                return $"Ruta completada\nPuntos: {gestor.PuntosTotales}";

            default:
                DatosEntrega pedido = gestor.PedidoActual;
                float t = gestor.TiempoRestante;
                string tiempo = $"{(int)(t / 60f):00}:{(int)(t % 60f):00}";
                return $"Pedido {gestor.IndiceActual + 1}/{gestor.TotalEntregas}: {pedido.cliente}\n" +
                       $"Tiempo: {tiempo}   Temperatura: {gestor.TemperaturaActual:0}%\n" +
                       $"Caja: {gestor.Integridad:0}%   Puntos: {gestor.PuntosTotales}";
        }
    }
}
