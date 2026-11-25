using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Subtítulo de chat en la parte de abajo de la pantalla: muestra la miniatura de quien escribe,
/// un "escribiendo..." y luego el mensaje letra por letra, como si lo estuvieran tecleando.
/// La cinemática llama a <see cref="Mostrar"/> y <see cref="Ocultar"/>.
/// </summary>
public class SubtituloMensaje : MonoBehaviour
{
    [Header("Referencias de la interfaz")]
    [SerializeField] private CanvasGroup grupo;
    [Tooltip("Panel que sube desde abajo al aparecer.")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text textoRemitente;
    [SerializeField] private Text textoMensaje;
    [SerializeField] private Text textoEscribiendo;

    [Header("Ritmo")]
    [SerializeField] private float letrasPorSegundo = 22f;
    [Tooltip("Pausa extra tras una coma.")]
    [SerializeField] private float pausaComa = 0.2f;
    [Tooltip("Pausa extra tras . ! o ?")]
    [SerializeField] private float pausaPunto = 0.35f;
    [SerializeField] private float duracionAparecer = 0.35f;

    [Header("Sonido de tecleo")]
    [Tooltip("Si se deja vacío, se genera un clic corto por código.")]
    [SerializeField] private AudioClip sonidoTecla;
    [Range(0f, 1f)] [SerializeField] private float volumenTecla = 0.35f;

    private AudioSource fuente;
    private Vector2 posicionPanel;

    private void Awake()
    {
        if (panel != null) posicionPanel = panel.anchoredPosition;
        if (grupo != null) grupo.alpha = 0f;

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        if (sonidoTecla == null) sonidoTecla = CrearClic();
    }

    /// <summary>Aparece, muestra "escribiendo..." y teclea el mensaje.</summary>
    public IEnumerator Mostrar(string remitente, string mensaje, float tiempoEscribiendo)
    {
        textoRemitente.text = remitente;
        textoMensaje.text = "";
        textoEscribiendo.gameObject.SetActive(true);

        // Sube y aparece.
        for (float t = 0f; t < duracionAparecer; t += Time.deltaTime)
        {
            float k = Suavizar(t / duracionAparecer);
            grupo.alpha = k;
            panel.anchoredPosition = posicionPanel + Vector2.down * (40f * (1f - k));
            yield return null;
        }
        grupo.alpha = 1f;
        panel.anchoredPosition = posicionPanel;

        // "escribiendo" con puntitos animados.
        for (float t = 0f; t < tiempoEscribiendo; t += Time.deltaTime)
        {
            int puntos = 1 + (int)(t * 3f) % 3;
            textoEscribiendo.text = "escribiendo" + new string('.', puntos);
            yield return null;
        }
        textoEscribiendo.gameObject.SetActive(false);

        // Tecleo letra por letra.
        for (int i = 0; i < mensaje.Length; i++)
        {
            char c = mensaje[i];
            textoMensaje.text = mensaje.Substring(0, i + 1);
            if (!char.IsWhiteSpace(c))
            {
                fuente.pitch = Random.Range(0.85f, 1.2f);
                fuente.PlayOneShot(sonidoTecla, volumenTecla);
            }

            float espera = 1f / letrasPorSegundo;
            if (c == ',') espera += pausaComa;
            else if (c == '.' || c == '!' || c == '?') espera += pausaPunto;
            yield return new WaitForSeconds(espera);
        }
    }

    /// <summary>Desvanece el subtítulo.</summary>
    public IEnumerator Ocultar(float duracion)
    {
        float inicio = grupo.alpha;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            grupo.alpha = Mathf.Lerp(inicio, 0f, t / duracion);
            yield return null;
        }
        grupo.alpha = 0f;
    }

    private static float Suavizar(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    /// <summary>Clic de tecla sintético: ruido muy corto que se apaga rápido.</summary>
    private static AudioClip CrearClic()
    {
        const int frecuencia = 44100;
        int muestras = frecuencia / 40; // 25 ms
        var datos = new float[muestras];
        var azar = new System.Random(7);
        for (int i = 0; i < muestras; i++)
        {
            float caida = Mathf.Exp(-i / (frecuencia * 0.004f));
            datos[i] = ((float)azar.NextDouble() * 2f - 1f) * caida * 0.6f;
        }
        AudioClip clip = AudioClip.Create("Tecla", muestras, 1, frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
