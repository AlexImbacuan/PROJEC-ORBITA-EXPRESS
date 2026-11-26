using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD del nivel del camino: celdas de energía (como una batería), cuenta regresiva
/// de la entrega, mensajes y pantalla final (ganar / perder).
/// </summary>
public class HUDCamino : MonoBehaviour
{
    [Header("Energía")]
    [Tooltip("Celdas de la batería, de izquierda a derecha. Deben ser tantas como celdasMaximas del GestorCamino.")]
    [SerializeField] private Image[] celdas;
    [SerializeField] private Color colorLlena = new Color(1f, 0.55f, 0.12f);
    [SerializeField] private Color colorUltima = new Color(1f, 0.2f, 0.15f);
    [SerializeField] private Color colorVacia = new Color(1f, 1f, 1f, 0.12f);

    [Header("Tiempo")]
    [SerializeField] private TextMeshProUGUI textoTiempo;
    [SerializeField] private Image barraTiempo;
    [Tooltip("Por debajo de estos segundos el reloj se pone naranja.")]
    [SerializeField] private float avisoTiempo = 30f;
    [Tooltip("Por debajo de estos segundos el reloj parpadea en rojo.")]
    [SerializeField] private float urgenciaTiempo = 10f;

    [Header("Mensajes")]
    [SerializeField] private TextMeshProUGUI textoMensaje;
    [SerializeField] private float duracionMensaje = 2.5f;

    [Header("Pantalla final")]
    [SerializeField] private CanvasGroup panelFinal;
    [SerializeField] private TextMeshProUGUI tituloFinal;
    [SerializeField] private TextMeshProUGUI detalleFinal;

    private GestorCamino gestor;
    private float mensajeHasta;
    private int celdasAntes = -1;

    private void Start()
    {
        gestor = GestorCamino.Instancia;
        if (gestor != null && celdas != null && celdas.Length != gestor.CeldasMaximas)
            Debug.LogWarning($"El HUD tiene {celdas.Length} celdas pero el GestorCamino usa {gestor.CeldasMaximas}.", this);
        if (gestor == null)
        {
            Debug.LogWarning("No hay GestorCamino en la escena; el HUD no mostrará nada.", this);
            enabled = false;
            return;
        }

        gestor.CeldasCambiaron += ActualizarCeldas;
        gestor.MensajeMostrado += MostrarMensaje;
        gestor.NivelTerminado += MostrarFinal;

        if (panelFinal != null) panelFinal.alpha = 0f;
        ActualizarCeldas(gestor.Celdas, gestor.CeldasMaximas);
    }

    private void OnDestroy()
    {
        if (gestor == null) return;
        gestor.CeldasCambiaron -= ActualizarCeldas;
        gestor.MensajeMostrado -= MostrarMensaje;
        gestor.NivelTerminado -= MostrarFinal;
    }

    private void Update()
    {
        if (textoTiempo != null)
        {
            float t = gestor.TiempoRestante;
            int seg = Mathf.CeilToInt(t);
            textoTiempo.text = $"{seg / 60:00}:{seg % 60:00}";

            Color color = Color.white;
            if (t <= urgenciaTiempo) color = Mathf.Repeat(Time.time, 0.6f) < 0.3f ? colorUltima : Color.white;
            else if (t <= avisoTiempo) color = colorLlena;
            textoTiempo.color = color;

            float pulso = t <= urgenciaTiempo && gestor.EstadoActual == GestorCamino.Estado.Jugando
                ? 1f + 0.08f * Mathf.Sin(Time.time * 12f) : 1f;
            textoTiempo.transform.localScale = Vector3.one * pulso;
        }

        if (barraTiempo != null && gestor.TiempoTotal > 0f)
        {
            barraTiempo.fillAmount = gestor.TiempoRestante / gestor.TiempoTotal;
            barraTiempo.color = gestor.TiempoRestante <= avisoTiempo ? colorUltima : colorLlena;
        }

        if (textoMensaje != null && Time.time > mensajeHasta && gestor.EstadoActual == GestorCamino.Estado.Jugando)
            textoMensaje.text = "";
    }

    private void ActualizarCeldas(int actuales, int maximas)
    {
        if (celdas == null) return;
        for (int i = 0; i < celdas.Length; i++)
        {
            if (celdas[i] == null) continue;
            bool llena = i < actuales;
            celdas[i].color = !llena ? colorVacia : (actuales == 1 ? colorUltima : colorLlena);
        }

        // La celda que se acaba de gastar parpadea.
        if (celdasAntes > actuales && actuales >= 0 && actuales < celdas.Length && celdas[actuales] != null)
            StartCoroutine(Parpadear(celdas[actuales]));
        celdasAntes = actuales;
    }

    private IEnumerator Parpadear(Image celda)
    {
        Transform t = celda.transform;
        for (int i = 0; i < 4; i++)
        {
            celda.color = colorUltima;
            t.localScale = Vector3.one * 1.25f;
            yield return new WaitForSeconds(0.08f);
            celda.color = colorVacia;
            t.localScale = Vector3.one;
            yield return new WaitForSeconds(0.08f);
        }
    }

    private void MostrarMensaje(string mensaje)
    {
        if (textoMensaje == null) return;
        textoMensaje.text = mensaje;
        mensajeHasta = Time.time + duracionMensaje;
    }

    private void MostrarFinal(bool gano)
    {
        if (panelFinal == null) return;
        if (tituloFinal != null)
        {
            tituloFinal.text = gano ? "¡PIZZA ENTREGADA!" : "ENTREGA FALLIDA";
            tituloFinal.color = gano ? colorLlena : colorUltima;
        }
        if (detalleFinal != null)
        {
            int seg = Mathf.CeilToInt(gestor.TiempoRestante);
            detalleFinal.text = (gano ? $"Tiempo sobrante {seg / 60:00}:{seg % 60:00}  ·  Energía {gestor.Celdas}/{gestor.CeldasMaximas}\n" : "")
                                + "Presiona R para volver a intentarlo";
        }
        StartCoroutine(AparecerFinal());
    }

    private IEnumerator AparecerFinal()
    {
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            panelFinal.alpha = t / 0.6f;
            yield return null;
        }
        panelFinal.alpha = 1f;
    }
}
