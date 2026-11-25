using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Datos de una entrega de la ruta (se llenan en el Inspector).</summary>
[Serializable]
public class DatosEntrega
{
    [Tooltip("Nombre del cliente, se muestra en pantalla.")]
    public string cliente = "Cliente";
    [Tooltip("Zona donde se entrega este pedido.")]
    public PuntoEntrega destino;
    [Tooltip("Segundos desde la recogida hasta que el pedido queda totalmente frío.")]
    [Min(5f)] public float segundosParaEnfriarse = 90f;
}

/// <summary>Resultado de una entrega, para mostrarlo en pantalla o reaccionar (animaciones, sonidos).</summary>
public struct ResultadoEntrega
{
    public string cliente;
    public float temperatura;   // 0-100 al momento de entregar
    public float integridad;    // 0-100 al momento de entregar
    public int estrellas;       // 1-5
    public int propina;
}

/// <summary>
/// Lleva la ruta de entregas del nivel: recogida en la pizzería, pedido actual,
/// temperatura (se enfría con el tiempo), integridad de la caja (baja con los golpes),
/// calificación de cada entrega y fin de la ruta.
/// Debe haber uno por escena.
/// </summary>
public class GestorEntregas : MonoBehaviour
{
    public enum EstadoRuta { SinRecoger, EnRuta, Completada }

    public static GestorEntregas Instancia { get; private set; }

    [Header("Ruta")]
    [Tooltip("Entregas en el orden en que se deben hacer.")]
    [SerializeField] private List<DatosEntrega> ruta = new List<DatosEntrega>();

    [Header("Calificación")]
    [Tooltip("Cuánto pesa la temperatura en la calificación (el resto lo pone la integridad).")]
    [Range(0f, 1f)] [SerializeField] private float pesoTemperatura = 0.6f;
    [Tooltip("Propina de una entrega perfecta.")]
    [SerializeField] private int propinaMaxima = 50;
    [Tooltip("Puntos fijos por cada entrega, sin importar cómo llegue.")]
    [SerializeField] private int puntosPorEntrega = 100;

    [Header("Eventos (para conectar animaciones, sonidos o cambiar de escena)")]
    [SerializeField] private UnityEvent alRecogerPedidos;
    [SerializeField] private UnityEvent alEntregar;
    [SerializeField] private UnityEvent alCompletarRuta;

    // Para scripts (por ejemplo el HUD o las animaciones del repartidor).
    public event Action PedidosRecogidos;
    public event Action<ResultadoEntrega> PedidoEntregado;
    public event Action RutaCompletada;
    public event Action<string> MensajeMostrado;

    private float tiempoRecogida;

    public EstadoRuta Estado { get; private set; } = EstadoRuta.SinRecoger;
    public int IndiceActual { get; private set; }
    public int TotalEntregas => ruta.Count;
    public float Integridad { get; private set; } = 100f;
    public int PuntosTotales { get; private set; }

    public DatosEntrega PedidoActual =>
        Estado == EstadoRuta.EnRuta && IndiceActual < ruta.Count ? ruta[IndiceActual] : null;

    /// <summary>Temperatura del pedido actual: 100 = recién salido del horno, 0 = frío.</summary>
    public float TemperaturaActual
    {
        get
        {
            DatosEntrega pedido = PedidoActual;
            if (pedido == null) return 0f;
            float transcurrido = Time.time - tiempoRecogida;
            return Mathf.Clamp01(1f - transcurrido / pedido.segundosParaEnfriarse) * 100f;
        }
    }

    /// <summary>Segundos que le quedan al pedido actual antes de quedar frío.</summary>
    public float TiempoRestante
    {
        get
        {
            DatosEntrega pedido = PedidoActual;
            if (pedido == null) return 0f;
            return Mathf.Max(0f, pedido.segundosParaEnfriarse - (Time.time - tiempoRecogida));
        }
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Debug.LogWarning("Hay más de un GestorEntregas en la escena. Se usará solo el primero.", this);
            enabled = false;
            return;
        }
        Instancia = this;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Start()
    {
        ActualizarMarcadores();
        Mostrar("Ve a la pizzería a recoger los pedidos");
    }

    /// <summary>Lo llama el PuntoRecogida cuando llega el repartidor.</summary>
    public void RecogerPedidos()
    {
        if (Estado != EstadoRuta.SinRecoger) return;

        if (ruta.Count == 0)
        {
            Debug.LogWarning("La ruta no tiene entregas. Agrega entregas en el GestorEntregas.", this);
            return;
        }

        Estado = EstadoRuta.EnRuta;
        IndiceActual = 0;
        tiempoRecogida = Time.time;

        ActualizarMarcadores();
        Mostrar($"¡Pedidos recogidos! Primera entrega: {ruta[0].cliente}");

        PedidosRecogidos?.Invoke();
        alRecogerPedidos?.Invoke();
    }

    /// <summary>Lo llama un PuntoEntrega cuando el repartidor entra a pie.</summary>
    public void IntentarEntregar(PuntoEntrega punto)
    {
        DatosEntrega pedido = PedidoActual;
        if (pedido == null || punto != pedido.destino) return;

        ResultadoEntrega resultado = Calificar(pedido);
        PuntosTotales += puntosPorEntrega + resultado.propina;

        Mostrar($"Entregado a {resultado.cliente}: {resultado.estrellas}/5 estrellas, propina {resultado.propina}");
        PedidoEntregado?.Invoke(resultado);
        alEntregar?.Invoke();

        IndiceActual++;
        if (IndiceActual >= ruta.Count)
        {
            Estado = EstadoRuta.Completada;
            ActualizarMarcadores();
            Mostrar($"¡Ruta completada! Puntos totales: {PuntosTotales}");
            RutaCompletada?.Invoke();
            alCompletarRuta?.Invoke();
        }
        else
        {
            ActualizarMarcadores();
        }
    }

    /// <summary>Baja la integridad de la caja. Lo llama el DetectorGolpes de la moto.</summary>
    public void RegistrarGolpe(float danio)
    {
        if (Estado != EstadoRuta.EnRuta || danio <= 0f) return;

        Integridad = Mathf.Max(0f, Integridad - danio);
        Mostrar($"¡Golpe! Estado de la caja: {Integridad:0}%");
    }

    /// <summary>Muestra un mensaje temporal en el HUD.</summary>
    public void Mostrar(string mensaje)
    {
        MensajeMostrado?.Invoke(mensaje);
    }

    private ResultadoEntrega Calificar(DatosEntrega pedido)
    {
        float temperatura = TemperaturaActual;
        float calidad = (temperatura * pesoTemperatura + Integridad * (1f - pesoTemperatura)) / 100f;

        return new ResultadoEntrega
        {
            cliente = pedido.cliente,
            temperatura = temperatura,
            integridad = Integridad,
            estrellas = 1 + Mathf.RoundToInt(calidad * 4f),
            propina = Mathf.RoundToInt(propinaMaxima * calidad)
        };
    }

    /// <summary>Solo se ve el marcador del destino actual.</summary>
    private void ActualizarMarcadores()
    {
        DatosEntrega actual = PedidoActual;
        foreach (DatosEntrega entrega in ruta)
        {
            if (entrega.destino != null)
                entrega.destino.MarcarComoDestino(entrega == actual);
        }
    }
}
